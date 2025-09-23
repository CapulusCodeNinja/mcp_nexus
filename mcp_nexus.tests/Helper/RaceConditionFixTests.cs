using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus.tests.Helper
{
	/// <summary>
	/// Tests that specifically verify the fix for the race condition between 
	/// CancelCurrentOperation and StopSession that was causing 60-second timeouts
	/// in close_windbg_dump operations.
	/// </summary>
	public class RaceConditionFixTests
	{
		private static ILogger<CdbSession> CreateNullLogger()
		{
			return LoggerFactory.Create(builder => { }).CreateLogger<CdbSession>();
		}

		[Fact]
		public async Task StopSession_WaitsForCancelCurrentOperation_NoRaceCondition()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 100);
			var stopwatch = Stopwatch.StartNew();

			// Act - Call StopSession which internally calls CancelCurrentOperationAsync
			// This should complete quickly without the old race condition
			var result = await session.StopSession();
			stopwatch.Stop();

			// Assert - Should complete in reasonable time (not hang for 60+ seconds)
			// The fix makes it properly wait for cancellation (~3 seconds) plus minimal processing
			Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
				$"StopSession took too long: {stopwatch.ElapsedMilliseconds}ms. " +
				"This suggests the race condition still exists.");

			// Should return false since no session was actually started
			Assert.False(result);
		}

		[Fact]
		public async Task StopSession_AfterStartSession_CompletesCleanly()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 100);
			var stopwatch = Stopwatch.StartNew();

			// Act - Start an invalid session (will fail quickly) then stop it
			await session.StartSession("invalid_target_that_will_fail");
			
			// Reset stopwatch to measure just the StopSession time
			stopwatch.Restart();
			var result = await session.StopSession();
			stopwatch.Stop();

			// Assert - Should complete without hanging
			Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
				$"StopSession took too long: {stopwatch.ElapsedMilliseconds}ms. " +
				"This suggests the race condition is still present.");
		}

		[Fact]
		public async Task ConcurrentStopAndCancel_DoNotDeadlock()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 100);
			var stopwatch = Stopwatch.StartNew();

			// Act - Try to trigger the old race condition with concurrent calls
			var stopTask = Task.Run(() => session.StopSession());
			var cancelTask = Task.Run(() => session.CancelCurrentOperation());

			// Both should complete without hanging
			var allTasks = Task.WhenAll(stopTask, cancelTask);
			var timeoutTask = Task.Delay(8000); // 8 second timeout

			var completedTask = await Task.WhenAny(allTasks, timeoutTask);
			stopwatch.Stop();

			// Assert
			Assert.NotEqual(timeoutTask, completedTask);
			Assert.True(stopwatch.ElapsedMilliseconds < 8000, 
				$"Operations took too long: {stopwatch.ElapsedMilliseconds}ms");
		}

		[Fact]
		public async Task StopSession_RepeatedCalls_CompleteReasonably()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 100);
			var stopwatch = Stopwatch.StartNew();

			// Act - Multiple rapid StopSession calls (simulating user spamming close button)
			var tasks = new Task<bool>[3];
			for (int i = 0; i < 3; i++)
			{
				tasks[i] = session.StopSession();
			}

			await Task.WhenAll(tasks);
			stopwatch.Stop();

			// Assert - Should handle multiple calls gracefully without excessive delay
			Assert.True(stopwatch.ElapsedMilliseconds < 15000, 
				$"Multiple StopSession calls took too long: {stopwatch.ElapsedMilliseconds}ms");

			// First call might succeed (if session was active), others should return false
			var results = tasks.Select(t => t.Result).ToArray();
			Assert.True(results.Count(r => r == false) >= 2, 
				"Expected most StopSession calls to return false for inactive session");
		}

		[Fact]
		public async Task StopSession_WithRealProcess_DoesNotHangIndefinitely()
		{
			// Arrange - Use a real but very short timeout to avoid waiting for actual CDB
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 50);
			var stopwatch = Stopwatch.StartNew();

			// Act - Try to start with an invalid target, then stop
			// This will create a real process that fails quickly
			try
			{
				await session.StartSession("-z invalid_dump_file_path.dmp");
			}
			catch
			{
				// Expected to fail
			}

			stopwatch.Restart();
			var result = await session.StopSession();
			stopwatch.Stop();

			// Assert - This is the real test: StopSession should complete without the 60+ second hang
			Assert.True(stopwatch.ElapsedMilliseconds < 15000, 
				$"StopSession with real process took too long: {stopwatch.ElapsedMilliseconds}ms. " +
				"The race condition fix may not be working properly.");
		}
	}
}
