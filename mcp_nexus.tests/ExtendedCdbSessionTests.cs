using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using Xunit;

namespace mcp_nexus.tests
{
	public class ExtendedCdbSessionTests
	{
		private static ILogger<CdbSession> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<CdbSession>();

		// Note: StartSession with empty/whitespace targets currently tries to start CDB 
		// which may succeed or fail depending on CDB availability, so we skip these tests

		[Fact]
		public async Task ExecuteCommand_EmptyCommand_ReturnsError()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			var result = await session.ExecuteCommand("");

			// Assert
			Assert.Contains("No active debug session", result);
		}

		[Fact]
		public async Task ExecuteCommand_WhitespaceCommand_ReturnsError()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			var result = await session.ExecuteCommand("   ");

			// Assert
			Assert.Contains("No active debug session", result);
		}

		[Fact]
		public async Task ExecuteCommand_WithCancellationToken_EmptyCommand_ReturnsError()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());
			using var cts = new CancellationTokenSource();

			// Act
			var result = await session.ExecuteCommand("", cts.Token);

			// Assert
			Assert.Contains("No active debug session", result);
		}

		[Fact]
		public async Task ExecuteCommand_WithCancellationToken_NoActiveSession_ReturnsError()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());
			using var cts = new CancellationTokenSource();

			// Act
			var result = await session.ExecuteCommand("version", cts.Token);

			// Assert
			Assert.Contains("No active debug session", result);
		}

		[Fact]
		public void CdbSession_WithCustomTimeout_CreatesCorrectly()
		{
			// Arrange & Act
			var session = new CdbSession(CreateNullLogger(), commandTimeoutMs: 60000);

			// Assert
			Assert.False(session.IsActive);
		}

		[Fact]
		public void CdbSession_WithAllParameters_CreatesCorrectly()
		{
			// Arrange & Act
			var session = new CdbSession(
				CreateNullLogger(),
				commandTimeoutMs: 120000,
				customCdbPath: "C:\\CustomPath\\cdb.exe",
				symbolServerTimeoutMs: 60000,
				symbolServerMaxRetries: 3,
				symbolSearchPath: "srv*C:\\Symbols*https://msdl.microsoft.com/download/symbols",
				startupDelayMs: 3000
			);

			// Assert
			Assert.False(session.IsActive);
		}

		// StartSession behavior depends on CDB availability, so we focus on other testable aspects

		[Fact]
		public void IsActive_RepeatedCalls_ReturnConsistentResults()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act & Assert
			for (int i = 0; i < 10; i++)
			{
				Assert.False(session.IsActive);
			}
		}

		[Fact]
		public void CancelCurrentOperation_RepeatedCalls_DoesNotThrow()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act & Assert - should not throw
			for (int i = 0; i < 5; i++)
			{
				session.CancelCurrentOperation();
			}
		}

		[Fact]
		public void Dispose_AfterMultipleOperations_HandlesCorrectly()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act - perform various operations
			_ = session.IsActive;
			session.CancelCurrentOperation();
			_ = session.IsActive;

			// Assert - dispose should not throw
			session.Dispose();
		}

		[Fact]
		public async Task StopSession_RepeatedCalls_HandlesCorrectly()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act & Assert
			var result1 = await session.StopSession();
			var result2 = await session.StopSession();
			var result3 = await session.StopSession();

			Assert.False(result1);
			Assert.False(result2);
			Assert.False(result3);
		}

		[Fact]
		public async Task ExecuteCommand_AfterDispose_HandlesGracefully()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());
			session.Dispose();

			// Act
			var result = await session.ExecuteCommand("version");

			// Assert
			Assert.Contains("No active debug session", result);
		}

		// StartSession after dispose behavior depends on CDB availability

		[Fact]
		public void IsActive_AfterDispose_ReturnsFalse()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());
			session.Dispose();

			// Act & Assert
			Assert.False(session.IsActive);
		}

		[Fact]
		public async Task StopSession_AfterDispose_ReturnsFalse()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());
			session.Dispose();

			// Act
			var result = await session.StopSession();

			// Assert
			Assert.False(result);
		}
	}
}
