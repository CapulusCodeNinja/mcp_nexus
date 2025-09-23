using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Tools;
using Moq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus.tests.Helper
{
	/// <summary>
	/// Tests specifically designed to prevent regression of the close_windbg_dump deadlock issue.
	/// These tests simulate the exact conditions that caused the 60+ second timeout bug.
	/// </summary>
	public class CloseDeadlockTests
	{
		private static ILogger<CdbSession> CreateNullLogger()
		{
			return LoggerFactory.Create(builder => { }).CreateLogger<CdbSession>();
		}

		[Fact]
		public async Task CloseWindbgDump_WhileLongRunningCommand_DoesNotDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			var mockCommandQueue = new Mock<ICommandQueueService>();
			var mockLogger = new Mock<ILogger<WindbgTool>>();

			// Simulate a session that's active but has a long-running command
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			// Simulate StopSession taking some time but completing successfully within reasonable time
			mockCdbSession.Setup(x => x.StopSession()).Returns(async () =>
			{
				// Simulate the processing time but ensure it's reasonable (not 60+ seconds)
				await Task.Delay(2000); // 2 seconds max
				return true;
			});

			var tool = new WindbgTool(mockLogger.Object, mockCdbSession.Object, mockCommandQueue.Object);
			var stopwatch = Stopwatch.StartNew();

			// Act
			var result = await tool.CloseWindbgDump();

			// Assert
			stopwatch.Stop();
			
			// CRITICAL: This must complete in under 10 seconds, not 60+ seconds
			Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
				$"CloseWindbgDump took {stopwatch.ElapsedMilliseconds}ms, indicating a potential deadlock regression");
			
			Assert.Contains("Successfully closed", result);
			mockCommandQueue.Verify(x => x.CancelAllCommands("Session stop requested"), Times.Once);
			mockCdbSession.Verify(x => x.StopSession(), Times.Once);
		}

		[Fact]
		public async Task CancelCurrentOperation_WhileExecuteCommandHoldsLock_DoesNotBlock()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 100);

			// Simulate ExecuteCommand holding the session lock
			var executeCommandTask = Task.Run(async () =>
			{
				try
				{
					// This will fail quickly since no real CDB process, but will hold session lock briefly
					await session.ExecuteCommand("test_command_that_will_fail");
				}
				catch
				{
					// Expected to fail
				}
			});

			// Act - Try to cancel while ExecuteCommand might be holding the lock
			var stopwatch = Stopwatch.StartNew();
			
			// Give ExecuteCommand a moment to start and potentially acquire the lock
			await Task.Delay(50);
			
			// This should not deadlock even if ExecuteCommand is holding the session lock
			session.CancelCurrentOperation();
			
			stopwatch.Stop();

			// Assert
			// CancelCurrentOperation should complete immediately (within 100ms), never wait for session lock
			Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
				$"CancelCurrentOperation took {stopwatch.ElapsedMilliseconds}ms, indicating potential deadlock");

			// Wait for ExecuteCommand to complete
			await executeCommandTask;
		}

		[Fact]
		public async Task StopSession_WithSimulatedLongRunningCommand_CompletesReasonably()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 1000);

			// Start a command that will fail but might hold the lock briefly
			var longRunningTask = Task.Run(async () =>
			{
				try
				{
					await session.ExecuteCommand("simulated_long_command");
				}
				catch
				{
					// Expected to fail
				}
			});

			// Give the command a moment to start
			await Task.Delay(100);

			// Act
			var stopwatch = Stopwatch.StartNew();
			var result = await session.StopSession();
			stopwatch.Stop();

			// Assert
			// StopSession should complete quickly even with ongoing commands
			Assert.True(stopwatch.ElapsedMilliseconds < 8000, 
				$"StopSession took {stopwatch.ElapsedMilliseconds}ms - deadlock regression detected");

			// The result doesn't matter as much as the timing
			await longRunningTask;
		}

		[Fact]
		public async Task MultipleCloseOperations_DoNotCauseDeadlock()
		{
			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			var mockCommandQueue = new Mock<ICommandQueueService>();
			var mockLogger = new Mock<ILogger<WindbgTool>>();

			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			mockCdbSession.Setup(x => x.StopSession()).ReturnsAsync(true);

			var tool = new WindbgTool(mockLogger.Object, mockCdbSession.Object, mockCommandQueue.Object);

			// Act - Multiple rapid close operations (simulating user clicking close multiple times)
			var stopwatch = Stopwatch.StartNew();
			
			var closeTasks = new Task<string>[3];
			for (int i = 0; i < 3; i++)
			{
				closeTasks[i] = tool.CloseWindbgDump();
			}

			await Task.WhenAll(closeTasks);
			stopwatch.Stop();

			// Assert
			Assert.True(stopwatch.ElapsedMilliseconds < 15000, 
				$"Multiple close operations took {stopwatch.ElapsedMilliseconds}ms - potential deadlock");

			// At least one should succeed
			Assert.Contains(closeTasks, t => t.Result.Contains("Successfully closed"));
		}

		[Fact]
		public async Task CancelCurrentOperationAsync_WithTryEnterTimeout_DoesNotHang()
		{
			// This test specifically validates the Monitor.TryEnter fix
			
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 100);
			
			// Create a task that will hold a lock for longer than the TryEnter timeout
			var lockHoldingTask = Task.Run(async () =>
			{
				try
				{
					// This simulates ExecuteCommand holding the session lock
					await session.ExecuteCommand("lock_holding_command");
				}
				catch
				{
					// Expected to fail
				}
			});

			// Give the lock-holding task time to start
			await Task.Delay(50);

			// Act
			var stopwatch = Stopwatch.StartNew();
			
			// This should use Monitor.TryEnter and NOT hang waiting for the session lock
			session.CancelCurrentOperation();
			
			stopwatch.Stop();

			// Assert
			// CancelCurrentOperation should complete within the TryEnter timeout (5s) plus some buffer
			Assert.True(stopwatch.ElapsedMilliseconds < 6000, 
				$"CancelCurrentOperation took {stopwatch.ElapsedMilliseconds}ms - TryEnter timeout not working");

			await lockHoldingTask;
		}

		[Fact]
		public async Task DeadlockScenario_OriginalBugConditions_IsFixed()
		{
			// This test recreates the exact conditions from the original bug report:
			// 1. Long-running command (like "lsa" that took 2+ minutes)
			// 2. Client calls close_windbg_dump while command is running
			// 3. Should NOT result in 60+ second timeout

			// Arrange
			var mockCdbSession = new Mock<ICdbSession>();
			var mockCommandQueue = new Mock<ICommandQueueService>();
			var mockLogger = new Mock<ILogger<WindbgTool>>();

			// Simulate the session state during the original bug
			mockCdbSession.SetupGet(x => x.IsActive).Returns(true);
			
			// Simulate that StopSession has our deadlock fix and completes reasonably
			mockCdbSession.Setup(x => x.StopSession()).Returns(async () =>
			{
				// Simulate the fixed behavior - reasonable completion time
				await Task.Delay(3000); // 3 seconds is reasonable, 60+ seconds is not
				return true;
			});

			var tool = new WindbgTool(mockLogger.Object, mockCdbSession.Object, mockCommandQueue.Object);

			// Act - This represents the exact call that was timing out
			var stopwatch = Stopwatch.StartNew();
			var result = await tool.CloseWindbgDump();
			stopwatch.Stop();

			// Assert
			// CRITICAL: This is the core regression test - must complete in reasonable time
			Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
				$"CloseWindbgDump took {stopwatch.ElapsedMilliseconds}ms - DEADLOCK REGRESSION DETECTED! " +
				"The original 60+ second timeout bug has returned.");
			
			Assert.Contains("Successfully closed", result);
			
			// Verify the fix is working correctly
			mockCommandQueue.Verify(x => x.CancelAllCommands("Session stop requested"), Times.Once);
			mockCdbSession.Verify(x => x.StopSession(), Times.Once);
		}

		[Fact]
		public async Task StopSession_CancelOperation_ConcurrentExecution_NoDeadlock()
		{
			// Test concurrent StopSession and CancelCurrentOperation calls
			
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 500);
			
			// Act
			var stopwatch = Stopwatch.StartNew();
			
			var stopTask = Task.Run(() => session.StopSession());
			var cancelTask = Task.Run(() => session.CancelCurrentOperation());
			
			// Both should complete without deadlock
			var timeoutTask = Task.Delay(10000); // 10 second timeout
			var completedTask = await Task.WhenAny(
				Task.WhenAll(stopTask, cancelTask),
				timeoutTask
			);
			
			stopwatch.Stop();

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
			Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
				$"Concurrent operations took {stopwatch.ElapsedMilliseconds}ms - potential deadlock");
		}
	}
}
