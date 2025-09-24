using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using Xunit;

namespace mcp_nexus_tests.Services
{
	/// <summary>
	/// Tests specifically designed to detect deadlock scenarios that occurred
	/// in the original implementation
	/// </summary>
	public class DeadlockTests
	{
		private static ILogger<CommandQueueService> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<CommandQueueService>();

		[Fact]
		public async Task CommandQueue_QueueAndCancelSimultaneously_NoDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var startBarrier = new Barrier(2);
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					startBarrier.SignalAndWait(); // Coordinate with test thread
					await Task.Delay(1000, ct);
					return "Completed";
				});

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue and cancel simultaneously to trigger potential deadlock
			var queueTask = Task.Run(() => service.QueueCommand("deadlock_test"));
			var commandId = await queueTask;
			
			startBarrier.SignalAndWait(); // Wait for execution to start
			
			var cancelTask = Task.Run(() => service.CancelCommand(commandId));
			var getResultTask = Task.Run(() => service.GetCommandResult(commandId));

			// All operations should complete within reasonable time (no deadlock)
			var timeoutTask = Task.Delay(5000);
			var completedTask = await Task.WhenAny(
				Task.WhenAll(cancelTask, getResultTask),
				timeoutTask
			);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
			
			var cancelResult = await cancelTask;
			var result = await getResultTask;
			
		Assert.True(cancelResult);
		// The result might be "still executing" if cancel hasn't completed yet, or "cancelled" if it has
		Assert.True(
			result.Contains("cancel", StringComparison.OrdinalIgnoreCase) || 
			result.Contains("still executing", StringComparison.OrdinalIgnoreCase),
			$"Expected result to contain 'cancel' or 'still executing', but got: {result}");
		}

		[Fact]
		public async Task CommandQueue_CancelAllDuringDispose_NoDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var executionBarrier = new Barrier(2);
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					executionBarrier.SignalAndWait();
					await Task.Delay(2000, ct);
					return "Completed";
				});

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Start command execution, then dispose and cancel all simultaneously
			var commandId = service.QueueCommand("dispose_test");
			executionBarrier.SignalAndWait(); // Wait for execution to start

			var disposeTask = Task.Run(() => service.Dispose());
			var cancelAllTask = Task.Run(() => service.CancelAllCommands("Dispose test"));

			// Should complete without deadlock
			var timeoutTask = Task.Delay(5000);
			var completedTask = await Task.WhenAny(
				Task.WhenAll(disposeTask, cancelAllTask),
				timeoutTask
			);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
		}

		[Fact]
		public async Task CommandQueue_MultipleGetResults_NoDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync("Quick result");

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue command and get result from multiple threads
			var commandId = service.QueueCommand("multi_result_test");

			var resultTasks = Enumerable.Range(1, 10)
				.Select(i => Task.Run(() => service.GetCommandResult(commandId)))
				.ToArray();

			// Should complete without deadlock
			var timeoutTask = Task.Delay(3000);
			var completedTask = await Task.WhenAny(
				Task.WhenAll(resultTasks),
				timeoutTask
			);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
			
			var results = await Task.WhenAll(resultTasks);
			// Commands may still be executing or have completed - both are acceptable for deadlock prevention
			Assert.All(results, result => 
				Assert.True(result.Contains("Quick result") || result.Contains("still executing"), 
					$"Expected either 'Quick result' or 'still executing', but got: {result}"));
		}

		[Fact]
		public async Task CommandQueue_ConcurrentQueueStatusRequests_NoDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var executionGate = new SemaphoreSlim(0, 1);
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					await executionGate.WaitAsync(ct);
					return "Completed";
				});

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue commands and get status from multiple threads
			var commandIds = Enumerable.Range(1, 5)
				.Select(i => service.QueueCommand($"status_test_{i}"))
				.ToArray();

			var statusTasks = Enumerable.Range(1, 20)
				.Select(i => Task.Run(() => service.GetQueueStatus()))
				.ToArray();

			var currentCommandTasks = Enumerable.Range(1, 10)
				.Select(i => Task.Run(() => service.GetCurrentCommand()))
				.ToArray();

			// Should complete without deadlock
			var timeoutTask = Task.Delay(2000);
			var allTasks = statusTasks.Cast<Task>().Concat(currentCommandTasks.Cast<Task>()).ToArray();
			var completedTask = await Task.WhenAny(
				Task.WhenAll(allTasks),
				timeoutTask
			);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
			
			// Release execution to clean up
			executionGate.Release();
			service.Dispose();
		}

		[Fact]
		public async Task CdbSession_ConcurrentIsActiveAndCancelOperation_NoDeadlock()
		{
			// Arrange
			var session = new CdbSession(LoggerFactory.Create(b => { }).CreateLogger<CdbSession>());

			// Act - Access IsActive and CancelCurrentOperation concurrently
			var tasks = Enumerable.Range(1, 20)
				.Select(i => Task.Run(() =>
				{
					if (i % 2 == 0)
					{
						var isActive = session.IsActive;
						return isActive.ToString();
					}
					else
					{
						session.CancelCurrentOperation();
						return "Cancel called";
					}
				}))
				.ToArray();

			// Should complete without deadlock
			var timeoutTask = Task.Delay(2000);
			var completedTask = await Task.WhenAny(
				Task.WhenAll(tasks),
				timeoutTask
			);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
			
			var results = await Task.WhenAll(tasks);
			Assert.Equal(20, results.Length);
		}

		[Fact]
		public async Task CdbSession_StopSessionDuringCancel_NoDeadlock()
		{
			// Arrange
			var session = new CdbSession(LoggerFactory.Create(b => { }).CreateLogger<CdbSession>());

			// Act - Try StopSession and CancelCurrentOperation simultaneously
			var stopTasks = Enumerable.Range(1, 5)
				.Select(i => Task.Run(() => session.StopSession()))
				.ToArray();

			var cancelTasks = Enumerable.Range(1, 5)
				.Select(i => Task.Run(() => session.CancelCurrentOperation()))
				.ToArray();

			// Should complete without deadlock
			var timeoutTask = Task.Delay(3000);
			var completedTask = await Task.WhenAny(
				Task.WhenAll(stopTasks.Cast<Task>().Concat(cancelTasks.Select(t => (Task)t))),
				timeoutTask
			);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
			
			var stopResults = await Task.WhenAll(stopTasks);
			Assert.All(stopResults, result => Assert.False(result)); // No session to stop
		}

		[Fact]
		public async Task CommandQueue_NestedLockOperations_NoDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var operationCounter = 0;
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					Interlocked.Increment(ref operationCounter);
					await Task.Delay(100, ct);
					return $"Result {operationCounter}";
				});

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Perform operations that could potentially cause nested locking issues
			var commandId1 = service.QueueCommand("nested_test_1");
			var commandId2 = service.QueueCommand("nested_test_2");

			var operationTasks = new Task[]
			{
				Task.Run(() => service.GetCommandResult(commandId1)),
				Task.Run(() => service.GetCommandResult(commandId2)),
				Task.Run(() => service.GetQueueStatus()),
				Task.Run(() => service.GetCurrentCommand()),
				Task.Run(() => service.CancelCommand(commandId1)),
				Task.Run(() => service.CancelCommand(commandId2))
			};

			// Should complete without deadlock
			var timeoutTask = Task.Delay(5000);
			var completedTask = await Task.WhenAny(
				Task.WhenAll(operationTasks),
				timeoutTask
			);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
		}

		[Fact]
		public async Task CommandQueue_DisposeDuringActiveOperations_NoDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var blockingBarrier = new Barrier(2);
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					blockingBarrier.SignalAndWait(); // Signal test and wait
					await Task.Delay(2000, ct);
					return "Completed";
				});

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Start operations and dispose while they're running
			var commandId = service.QueueCommand("dispose_during_operation");
			blockingBarrier.SignalAndWait(); // Wait for execution to start

			var operationTasks = new Task[]
			{
				Task.Run(() => service.GetQueueStatus()),
				Task.Run(() => service.GetCurrentCommand()),
				Task.Run(() => service.GetCommandResult(commandId)),
				Task.Run(() => service.Dispose())
			};

			// Should complete without deadlock
			var timeoutTask = Task.Delay(5000);
			var completedTask = await Task.WhenAny(
				Task.WhenAll(operationTasks),
				timeoutTask
			);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
		}

		[Fact]
		public async Task CommandQueue_ExceptionDuringDispose_NoDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			// Setup CDB session to throw during cancellation
			mockCdbSession.Setup(x => x.CancelCurrentOperation())
				.Throws(new InvalidOperationException("Simulated error"));

			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					await Task.Delay(1000, ct);
					return "Completed";
				});

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue command and dispose (which should handle exceptions gracefully)
			var commandId = service.QueueCommand("exception_test");
			await Task.Delay(50); // Let execution start

			var disposeTask = Task.Run(() => service.Dispose());

			// Should complete without deadlock even with exceptions
			var timeoutTask = Task.Delay(3000);
			var completedTask = await Task.WhenAny(disposeTask, timeoutTask);

			// Assert
			Assert.Equal(disposeTask, completedTask);
		}
	}
}
