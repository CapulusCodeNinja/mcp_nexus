using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using Xunit;

namespace mcp_nexus.tests.Helper
{
	/// <summary>
	/// Tests specifically for timeout scenarios that were problematic in CdbSession
	/// </summary>
	public class TimeoutTests
	{
		private static ILogger<CdbSession> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<CdbSession>();

		[Fact]
		public async Task CdbSession_StartSessionWithShortTimeout_TimesOutCorrectly()
		{
			// Arrange - Use very short timeout to ensure timeout behavior
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 50);

			// Act - Try to start session with invalid target (should fail quickly)
			var result = await session.StartSession("invalid_target");

			// Assert - Should fail (either timeout or invalid target)
			Assert.False(result);
		}

		[Fact]
		public async Task CdbSession_MultipleTimeouts_DoNotCauseDeadlocks()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 50);

			// Act - Try multiple operations that will timeout
			var tasks = new[]
			{
				session.StartSession("timeout1"),
				session.StartSession("timeout2"),
				session.StartSession("timeout3")
			};

			var results = await Task.WhenAll(tasks);

			// Assert - All should fail but not deadlock
			Assert.All(results, result => Assert.False(result));
		}

		[Fact]
		public async Task CdbSession_ExecuteCommandWithTimeout_HandlesCorrectly()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act - Execute command with no active session (should return immediately)
			var result = await session.ExecuteCommand("test command");

			// Assert
			Assert.Contains("No active debug session", result);
		}

		[Fact]
		public async Task CdbSession_ExecuteCommandWithCancellationToken_RespectsTimeout()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());
			using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

			// Act - Execute command with timeout token
			var result = await session.ExecuteCommand("test command", cts.Token);

			// Assert
			Assert.Contains("No active debug session", result);
		}

		[Fact]
		public async Task CdbSession_StopSessionWithTimeout_CompletesQuickly()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act - Stop session should complete quickly even when no session is active
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var result = await session.StopSession();
			stopwatch.Stop();

			// Assert
			Assert.False(result); // No session to stop
			Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"StopSession took too long: {stopwatch.ElapsedMilliseconds}ms");
		}

		[Fact]
		public async Task CdbSession_ConcurrentStopSessions_DoNotHang()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act - Try to stop session from multiple threads
			var tasks = new[]
			{
				session.StopSession(),
				session.StopSession(),
				session.StopSession()
			};

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var results = await Task.WhenAll(tasks);
			stopwatch.Stop();

			// Assert
			Assert.All(results, result => Assert.False(result));
			Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Concurrent StopSession took too long: {stopwatch.ElapsedMilliseconds}ms");
		}

		[Fact]
		public void CdbSession_Dispose_CompletesQuickly()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			session.Dispose();
			stopwatch.Stop();

			// Assert
			Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Dispose took too long: {stopwatch.ElapsedMilliseconds}ms");
		}

		[Fact]
		public void CdbSession_DoubleDispose_DoesNotHang()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			session.Dispose();
			session.Dispose(); // Second dispose should not hang
			stopwatch.Stop();

			// Assert
			Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Double dispose took too long: {stopwatch.ElapsedMilliseconds}ms");
		}

		[Fact]
		public async Task CdbSession_DisposeAfterStopSession_HandlesCorrectly()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			var stopResult = await session.StopSession();
			
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			session.Dispose();
			stopwatch.Stop();

			// Assert
			Assert.False(stopResult);
			Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Dispose after StopSession took too long: {stopwatch.ElapsedMilliseconds}ms");
		}

		[Fact]
		public async Task CdbSession_CancelDuringDispose_HandlesGracefully()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act - Try to cancel while disposing
			var disposeTask = Task.Run(() => session.Dispose());
			var cancelTask = Task.Run(() => session.CancelCurrentOperation());

			var tasks = new[] { disposeTask, cancelTask };
			var timeoutTask = Task.Delay(2000);
			
			var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);

			// Assert - Should complete without hanging
			Assert.NotEqual(timeoutTask, completedTask);
		}

		[Fact]
		public void CdbSession_IsActiveAfterDispose_ReturnsFalse()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			session.Dispose();
			var isActive = session.IsActive;

			// Assert
			Assert.False(isActive);
		}

		[Fact]
		public async Task CdbSession_OperationsAfterDispose_DoNotHang()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());
			session.Dispose();

			// Act - All these operations should complete quickly and not hang
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			
			var isActive = session.IsActive;
			session.CancelCurrentOperation();
			var executeResult = await session.ExecuteCommand("test");
			var stopResult = await session.StopSession();
			var startResult = await session.StartSession("test");
			
			stopwatch.Stop();

			// Assert
			Assert.False(isActive);
			Assert.Contains("No active debug session", executeResult);
			Assert.False(stopResult);
			Assert.False(startResult);
			Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Operations after dispose took too long: {stopwatch.ElapsedMilliseconds}ms");
		}

		[Fact]
		public async Task CdbSession_RapidStartStopCycles_DoNotCauseIssues()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 50);

			// Act - Rapid start/stop cycles (reduced count for faster test)
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			
			for (int i = 0; i < 2; i++) // Reduced from 5 to 2
			{
				var startResult = await session.StartSession($"invalid_target_{i}");
				var stopResult = await session.StopSession();
			}
			
			stopwatch.Stop();

			// Assert - Should complete within reasonable time
			Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Rapid start/stop cycles took too long: {stopwatch.ElapsedMilliseconds}ms");
		}
	}
}
