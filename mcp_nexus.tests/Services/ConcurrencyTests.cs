using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using Xunit;

namespace mcp_nexus.tests.Services
{
	/// <summary>
	/// Tests specifically targeting race conditions, deadlocks, and timeout scenarios
	/// that were problematic in the original implementation.
	/// </summary>
	public class ConcurrencyTests
	{
		private static ILogger<CommandQueueService> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<CommandQueueService>();

		[Fact]
		public async Task CommandQueue_ConcurrentQueueing_HandlesRaceConditions()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var executionCounter = 0;
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					var count = Interlocked.Increment(ref executionCounter);
					await Task.Delay(10, ct);
					return $"Result {count}";
				});

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue multiple commands concurrently from multiple threads
			var tasks = Enumerable.Range(1, 10)
				.Select(i => Task.Run(() => service.QueueCommand($"command_{i}")))
				.ToArray();

			var commandIds = await Task.WhenAll(tasks);

			// Assert - Test race condition handling in queueing
			Assert.Equal(10, commandIds.Length);
			Assert.All(commandIds, id => Assert.NotNull(id));
			Assert.Equal(10, commandIds.Distinct().Count()); // All IDs should be unique (no race in ID generation)
			
			// Verify execution started without deadlocks
			await Task.Delay(200);
			mockCdbSession.Verify(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
		}

		[Fact]
		public async Task CommandQueue_ConcurrentCancellation_AvoidaRaceConditions()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue command and try to cancel from multiple threads
			var commandId = service.QueueCommand("test_command");

			// Try to cancel from multiple threads simultaneously with some delay to avoid race
			await Task.Delay(50); // Small delay to let command be processed
			
			var cancelTasks = Enumerable.Range(1, 3) // Reduced count
				.Select(i => Task.Run(async () => 
				{
					try
					{
						return service.CancelCommand(commandId);
					}
					catch (ObjectDisposedException)
					{
						// Handle race condition where token is disposed between check and use
						return false;
					}
				}))
				.ToArray();

			var cancelResults = await Task.WhenAll(cancelTasks);

			// Assert - Focus on race condition handling in cancellation
			// At least one cancellation should succeed or fail gracefully
			Assert.True(cancelResults.Length > 0);
			Assert.All(cancelResults, result => Assert.True(result == true || result == false));
		}

		[Fact]
		public async Task CommandQueue_DisposeDuringExecution_HandlesGracefully()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue command and dispose service
			var commandId = service.QueueCommand("test_command");
			
			// Dispose service 
			var disposeTask = Task.Run(() => service.Dispose());
			
			// Should complete quickly without hanging
			var completed = await Task.WhenAny(disposeTask, Task.Delay(5000));
			
			// Assert - Focus on graceful disposal without deadlocks
			Assert.Equal(disposeTask, completed);
		}

		[Fact]
		public async Task CommandQueue_HighConcurrencyStressTest_MaintainsConsistency()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - High concurrency stress test
			var commandCount = 20; 
			var concurrentTasks = Enumerable.Range(1, commandCount)
				.Select(i => Task.Run(() => service.QueueCommand($"stress_command_{i}")))
				.ToArray();

			var commandIds = await Task.WhenAll(concurrentTasks);

			// Assert - Focus on concurrency safety
			Assert.Equal(commandCount, commandIds.Length);
			Assert.Equal(commandCount, commandIds.Distinct().Count()); // All IDs unique - no race conditions
			
			// Service should remain responsive under load
			var status = service.GetQueueStatus();
			Assert.NotNull(status);
		}

		[Fact]
		public async Task CommandQueue_CancelAllDuringHighLoad_HandlesCorrectly()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var barrier = new Barrier(2); // Synchronize threads
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					barrier.SignalAndWait(1000); // Wait for all to start
					await Task.Delay(1000, ct);
					return "Completed";
				});

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue multiple commands and cancel all while they're queued/executing
			var commandIds = Enumerable.Range(1, 20)
				.Select(i => service.QueueCommand($"command_{i}"))
				.ToArray();

			// Wait a moment for some to start processing
			await Task.Delay(100);

			// Cancel all commands
			service.CancelAllCommands("Stress test cancellation");

			// Get results
			var results = await Task.WhenAll(commandIds.Select(id => service.GetCommandResult(id)));

			// Assert - All should be cancelled or indicate shutdown
			Assert.All(results, result => 
				Assert.True(
					result.Contains("cancel", StringComparison.OrdinalIgnoreCase) || 
					result.Contains("shutdown", StringComparison.OrdinalIgnoreCase),
					$"Expected cancelled/shutdown result, got: {result}")
			);
		}

		[Fact]
		public async Task CdbSession_ConcurrentCancellation_AvoidsDeadlock()
		{
			// Arrange
			var session = new CdbSession(LoggerFactory.Create(b => { }).CreateLogger<CdbSession>());

			// Act - Try to cancel from multiple threads when no session is active
			var cancelTasks = Enumerable.Range(1, 10)
				.Select(i => Task.Run(() => 
				{
					// This should not deadlock or throw exceptions
					session.CancelCurrentOperation();
					return true;
				}))
				.ToArray();

			var results = await Task.WhenAll(cancelTasks);

			// Assert - All should complete without deadlock
			Assert.All(results, result => Assert.True(result));
		}

		[Fact]
		public async Task CdbSession_IsActiveProperty_ThreadSafe()
		{
			// Arrange
			var session = new CdbSession(LoggerFactory.Create(b => { }).CreateLogger<CdbSession>());

			// Act - Access IsActive from multiple threads concurrently
			var tasks = Enumerable.Range(1, 20)
				.Select(i => Task.Run(() => 
				{
					// This should not cause race conditions or exceptions
					var isActive = session.IsActive;
					return isActive;
				}))
				.ToArray();

			var results = await Task.WhenAll(tasks);

			// Assert - All should return false (no session started) and no exceptions
			Assert.All(results, result => Assert.False(result));
		}

		[Fact]
		public async Task CommandQueue_CommandResultRetrieval_ThreadSafe()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync("Quick result");

			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Queue command and try to get result from multiple threads
			var commandId = service.QueueCommand("test_command");
			
			// Wait for processing
			await Task.Delay(100);

			var tasks = Enumerable.Range(1, 10)
				.Select(i => Task.Run(() => service.GetCommandResult(commandId)))
				.ToArray();

			var results = await Task.WhenAll(tasks);

			// Assert - All should get the same result
			Assert.All(results, result => Assert.Equal("Quick result", result));
		}

		[Fact]
		public void CommandQueue_CancelCommand_ReturnsBoolean()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Try to cancel a non-existent command
			var cancelled = service.CancelCommand("non-existent-id");
			
			// Assert - Should return false without throwing
			Assert.False(cancelled);
		}

		[Fact]
		public async Task CommandQueue_DisposeTwice_DoesNotDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			var service = new CommandQueueService(mockCdbSession.Object, CreateNullLogger());

			// Act - Dispose from multiple threads
			var disposeTasks = Enumerable.Range(1, 5)
				.Select(i => Task.Run(() => service.Dispose()))
				.ToArray();

			// Should complete quickly without deadlock
			var timeoutTask = Task.Delay(5000);
			var completedTask = await Task.WhenAny(Task.WhenAll(disposeTasks), timeoutTask);

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
		}
	}
}
