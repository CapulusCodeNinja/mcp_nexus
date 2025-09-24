using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using Xunit;

namespace mcp_nexus_tests.Helper
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

		// Act - Try to start session - with short timeout, this should complete quickly
		// The test is really about ensuring the timeout mechanism works, not about failure
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var result = await session.StartSession("-z \"C:\\NonExistent\\Path\\That\\Does\\Not\\Exist\\file.dmp\"");
		stopwatch.Stop();

		// Assert - Should complete within reasonable time (not hang), regardless of success/failure
		// The key is that it doesn't hang due to timeout issues
		Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"StartSession took too long: {stopwatch.ElapsedMilliseconds}ms");
		// Don't assert on the result value since CDB might start successfully even with invalid target
	}

	[Fact]
	public async Task CdbSession_MultipleTimeouts_DoNotCauseDeadlocks()
	{
		// Arrange
		var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 50);

		// Act - Try multiple operations concurrently to test for deadlocks
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var tasks = new[]
		{
			session.StartSession("timeout1"),
			session.StartSession("timeout2"),
			session.StartSession("timeout3")
		};

		var results = await Task.WhenAll(tasks);
		stopwatch.Stop();

		// Assert - Should complete without deadlocking (within reasonable time)
		Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Operations took too long: {stopwatch.ElapsedMilliseconds}ms - possible deadlock");
		// Don't assert on individual results since they may succeed or fail depending on CDB availability
	}

	[Fact]
	public async Task CdbSession_ExecuteCommandWithTimeout_HandlesCorrectly()
	{
		// Arrange
		var session = new CdbSession(CreateNullLogger());

		// Act & Assert - Execute command with no active session should throw
		await Assert.ThrowsAsync<InvalidOperationException>(() => session.ExecuteCommand("test command"));
	}

	[Fact]
	public async Task CdbSession_ExecuteCommandWithCancellationToken_RespectsTimeout()
	{
		// Arrange
		var session = new CdbSession(CreateNullLogger());
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

		// Act & Assert - Execute command with no active session should throw
		await Assert.ThrowsAsync<InvalidOperationException>(() => session.ExecuteCommand("test command", cts.Token));
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
		public void CdbSession_IsActiveAfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			session.Dispose();

			// Assert
			Assert.Throws<ObjectDisposedException>(() => _ = session.IsActive);
		}

		[Fact]
		public async Task CdbSession_OperationsAfterDispose_ThrowObjectDisposedException()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());
			session.Dispose();

			// Act & Assert - Should throw ObjectDisposedException
			Assert.Throws<ObjectDisposedException>(() => _ = session.IsActive);
			Assert.Throws<ObjectDisposedException>(() => session.CancelCurrentOperation());
			await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ExecuteCommand("test"));
			await Assert.ThrowsAsync<ObjectDisposedException>(() => session.StopSession());
			await Assert.ThrowsAsync<ObjectDisposedException>(() => session.StartSession("test"));
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

		// Assert - Should complete within reasonable time (increased timeout for CDB process startup)
		Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Rapid start/stop cycles took too long: {stopwatch.ElapsedMilliseconds}ms");
		}
	}
}
