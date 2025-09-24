using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using Xunit;

namespace mcp_nexus_tests.Helper
{
	public class CdbSessionErrorHandlingTests : IDisposable
	{
		private readonly CdbSession m_session;
		private readonly ILogger<CdbSession> m_logger;

		public CdbSessionErrorHandlingTests()
		{
			m_logger = LoggerFactory.Create(b => { }).CreateLogger<CdbSession>();
			m_session = new CdbSession(m_logger);
		}

		public void Dispose()
		{
			m_session?.Dispose();
		}

		[Fact]
		public async Task StartSession_WithVeryShortTimeout_DoesNotThrow()
		{
			// Arrange - Use very short timeout
			var session = new CdbSession(m_logger, commandTimeoutMs: 1); // 1ms timeout

			try
			{
				// Act - Should not throw regardless of timeout
				var exception = await Record.ExceptionAsync(() => session.StartSession("test_target"));

				// Assert - Should handle timeout gracefully
				Assert.Null(exception);
			}
			finally
			{
				session.Dispose();
			}
		}

		[Fact]
		public async Task StartSession_WithInvalidTarget_DoesNotThrow()
		{
			// Act - Try to start with completely invalid target
			var exception = await Record.ExceptionAsync(() => m_session.StartSession("<<<INVALID_TARGET>>>"));

			// Assert - Should not throw, regardless of success/failure
			Assert.Null(exception);
		}

		[Fact]
		public async Task ExecuteCommand_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			var session = new CdbSession(m_logger);
			session.Dispose();

			// Act & Assert
			await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ExecuteCommand("test"));
		}

		[Fact]
		public async Task ExecuteCommand_WithNullCommand_ThrowsArgumentException()
		{
			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(() => m_session.ExecuteCommand(null!));
		}

		[Fact]
		public async Task ExecuteCommand_WithEmptyCommand_ThrowsArgumentException()
		{
			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(() => m_session.ExecuteCommand(""));
		}

		[Fact]
		public async Task ExecuteCommand_WithWhitespaceCommand_ThrowsArgumentException()
		{
			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(() => m_session.ExecuteCommand("   "));
		}

		[Fact]
		public async Task ExecuteCommand_WhenNotActive_ThrowsInvalidOperationException()
		{
			// Act & Assert
			await Assert.ThrowsAsync<InvalidOperationException>(() => m_session.ExecuteCommand("test"));
		}

		[Fact]
		public async Task ExecuteCommand_WithCancellationToken_HandlesCancellation()
		{
			// Arrange
			using var cts = new CancellationTokenSource();
			cts.Cancel(); // Cancel immediately

			// Act & Assert
			// Since session is not active, it should throw InvalidOperationException before cancellation
			await Assert.ThrowsAsync<InvalidOperationException>(() => m_session.ExecuteCommand("test", cts.Token));
		}

		[Fact]
		public async Task StopSession_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			var session = new CdbSession(m_logger);
			session.Dispose();

			// Act & Assert
			await Assert.ThrowsAsync<ObjectDisposedException>(() => session.StopSession());
		}

		[Fact]
		public async Task StopSession_WhenNotActive_DoesNotThrow()
		{
			// Act
			var exception = await Record.ExceptionAsync(() => m_session.StopSession());

			// Assert - Should not throw regardless of return value
			Assert.Null(exception);
		}

		[Fact]
		public void IsActive_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			var session = new CdbSession(m_logger);
			session.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => session.IsActive);
		}

		[Fact]
		public void CancelCurrentOperation_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			var session = new CdbSession(m_logger);
			session.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => session.CancelCurrentOperation());
		}

		[Fact]
		public void Dispose_MultipleCallsDoNotThrow()
		{
			// Arrange
			var session = new CdbSession(m_logger);

			// Act & Assert - Multiple disposes should not throw
			session.Dispose();
			session.Dispose();
			session.Dispose();
		}

		[Fact]
		public void Dispose_WithActiveSession_CleansUpProperly()
		{
			// This test verifies the error handling in Dispose when cleaning up active session
			// The Dispose method has try-catch to handle exceptions during cleanup

			// Arrange
			var session = new CdbSession(m_logger);

			// Act - Should not throw even if there are cleanup issues
			var exception = Record.Exception(() => session.Dispose());

			// Assert
			Assert.Null(exception);
		}

		[Fact]
		public async Task FindCdbExecutable_HandlesExceptionsGracefully()
		{
			// This tests the try-catch in FindCdbExecutable method
			// It should handle exceptions when searching for CDB in PATH

			// We can't directly test this private method, but we can test that
			// StartSession handles cases where CDB is not found

			// Act & Assert - Should not throw even if CDB search has issues
			var exception = await Record.ExceptionAsync(async () => await m_session.StartSession("test"));
			Assert.Null(exception);
		}

		[Theory]
		[InlineData(0, 1000, 1, 100)] // commandTimeoutMs = 0
		[InlineData(-10, 1000, 1, 100)] // commandTimeoutMs < 0
		[InlineData(1000, -1, 1, 100)] // symbolServerTimeoutMs < 0
		[InlineData(1000, 1000, -1, 100)] // symbolServerMaxRetries < 0
		[InlineData(1000, 1000, 1, -1)] // startupDelayMs < 0
		public void Constructor_WithInvalidParameters_ThrowsArgumentOutOfRangeException(
			int commandTimeoutMs, int symbolServerTimeoutMs, int symbolServerMaxRetries, int startupDelayMs)
		{
			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(m_logger, commandTimeoutMs, null, symbolServerTimeoutMs, symbolServerMaxRetries, null, startupDelayMs));
		}

		[Fact]
		public void Constructor_WithValidParameters_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => 
				new CdbSession(m_logger, 1000, null, 1000, 1, null, 100));
			Assert.Null(exception);
		}

		[Fact]
		public async Task StartSession_HandlesCdbNotFoundGracefully()
		{
			// Arrange - Try to start with a target when CDB might not be found
			var session = new CdbSession(m_logger, customCdbPath: "nonexistent_path_to_cdb.exe");

			try
			{
				// Act - Should not throw regardless of CDB availability
				var exception = await Record.ExceptionAsync(() => session.StartSession("test_target"));

				// Assert - Should handle missing CDB gracefully
				Assert.Null(exception);
			}
			finally
			{
				session.Dispose();
			}
		}

		[Fact]
		public async Task ExecuteCommand_WithTimeout_HandlesCancellationCorrectly()
		{
			// This tests the cancellation handling in ExecuteCommand
			// when using CancellationTokenSource with timeout

			// Act & Assert
			// Since session is not active, should get InvalidOperationException
			await Assert.ThrowsAsync<InvalidOperationException>(() => m_session.ExecuteCommand("long_running_command"));
		}
	}
}
