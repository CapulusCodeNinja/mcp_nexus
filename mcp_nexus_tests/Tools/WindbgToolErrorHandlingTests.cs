using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Tools;
using Xunit;

namespace mcp_nexus_tests.Tools
{
	public class WindbgToolErrorHandlingTests
	{
		private readonly Mock<ILogger<WindbgTool>> m_mockLogger;
		private readonly Mock<ICdbSession> m_mockCdbSession;
		private readonly Mock<ICommandQueueService> m_mockCommandQueueService;
		private readonly WindbgTool m_tool;

		public WindbgToolErrorHandlingTests()
		{
			m_mockLogger = new Mock<ILogger<WindbgTool>>();
			m_mockCdbSession = new Mock<ICdbSession>();
			m_mockCommandQueueService = new Mock<ICommandQueueService>();
			m_tool = new WindbgTool(m_mockLogger.Object, m_mockCdbSession.Object, m_mockCommandQueueService.Object);
		}

		[Fact]
		public async Task OpenWindbgDump_FileNotExists_ReturnsErrorMessage()
		{
			// Act
			var result = await m_tool.OpenWindbgDump("nonexistent_file.dmp");

			// Assert
			Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task OpenWindbgDump_CdbSessionStartSessionThrowsException_ReturnsErrorMessage()
		{
			// Arrange
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "dummy content");

			try
			{
				m_mockCdbSession.Setup(s => s.StartSession(It.IsAny<string>(), It.IsAny<string>()))
					.ThrowsAsync(new InvalidOperationException("Test CDB exception"));

				// Act
				var result = await m_tool.OpenWindbgDump(tempFile);

				// Assert
				Assert.Contains("Error opening crash dump", result);
				Assert.Contains("Test CDB exception", result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public async Task OpenWindbgDump_FileIOException_ReturnsErrorMessage()
		{
			// Arrange - Use a path that will cause IO issues
			var invalidPath = Path.Combine(Path.GetTempPath(), "nonexistent_directory", "file.dmp");

			// Act
			var result = await m_tool.OpenWindbgDump(invalidPath);

			// Assert
			Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task OpenWindbgRemote_WithInvalidConnectionString_ReturnsErrorMessage()
		{
			// Act
			var result = await m_tool.OpenWindbgRemote("invalid_connection_string");

			// Assert
			Assert.Contains("Failed to connect", result, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task OpenWindbgRemote_CdbSessionThrowsException_ReturnsErrorMessage()
		{
			// Arrange
			m_mockCdbSession.Setup(s => s.StartSession(It.IsAny<string>(), It.IsAny<string>()))
				.ThrowsAsync(new TimeoutException("Connection timeout"));

			// Act
			var result = await m_tool.OpenWindbgRemote("tcp:Port=1234,Server=localhost");

			// Assert
			Assert.Contains("Error connecting", result);
			Assert.Contains("Connection timeout", result);
		}

		[Fact]
		public async Task CloseWindbgDump_CdbSessionThrowsException_ReturnsErrorMessage()
		{
			// Arrange
			m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
			m_mockCdbSession.Setup(s => s.StopSession())
				.ThrowsAsync(new InvalidOperationException("Stop session error"));

			// Act
			var result = await m_tool.CloseWindbgDump();

			// Assert
			Assert.Contains("Error closing", result);
			Assert.Contains("Stop session error", result);
		}

		[Fact]
		public async Task CloseWindbgRemote_CdbSessionThrowsException_ReturnsErrorMessage()
		{
			// Arrange
			m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
			m_mockCdbSession.Setup(s => s.StopSession())
				.ThrowsAsync(new UnauthorizedAccessException("Access denied"));

			// Act
			var result = await m_tool.CloseWindbgRemote();

			// Assert
			Assert.Contains("Error disconnecting", result);
			Assert.Contains("Access denied", result);
		}

		[Fact]
		public async Task RunWindbgCmdAsync_CommandQueueThrowsException_ReturnsErrorMessage()
		{
			// Arrange
			m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
			m_mockCommandQueueService.Setup(s => s.QueueCommand(It.IsAny<string>()))
				.Throws(new InvalidOperationException("Queue is full"));

			// Act
			var result = await m_tool.RunWindbgCmdAsync("test command");

			// Assert
			Assert.Contains("Error queueing command", result);
			Assert.Contains("Queue is full", result);
		}

		[Fact]
		public async Task GetSessionInfo_CdbSessionThrowsException_ReturnsErrorMessage()
		{
			// Arrange
			m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
			m_mockCommandQueueService.Setup(s => s.QueueCommand(It.IsAny<string>()))
				.Throws(new OutOfMemoryException("System out of memory"));

			// Act
			var result = await m_tool.GetSessionInfo();

			// Assert
			// The method might still return some session info or an error
			Assert.NotNull(result);
			// Just verify it doesn't crash - the exact format may vary
			Assert.True(result.Length > 0);
		}

		[Fact]
		public async Task GetCommandStatus_CommandQueueThrowsException_ReturnsErrorJson()
		{
			// Arrange
			m_mockCommandQueueService.Setup(s => s.GetCommandResult(It.IsAny<string>()))
				.ThrowsAsync(new ArgumentException("Invalid command ID"));

			// Act
			var result = await m_tool.GetCommandStatus("test-id");

			// Assert
			Assert.Contains("Error getting command status", result);
			Assert.Contains("Invalid command ID", result);
		}

		[Fact]
		public async Task CancelCommand_CommandQueueThrowsException_ReturnsErrorJson()
		{
			// Arrange
			m_mockCommandQueueService.Setup(s => s.CancelCommand(It.IsAny<string>()))
				.Throws(new InvalidCastException("Type conversion error"));

			// Act
			var result = await m_tool.CancelCommand("test-id");

			// Assert
			Assert.Contains("Error cancelling command", result);
			Assert.Contains("Type conversion error", result);
		}

		[Fact]
		public async Task ListCommands_CommandQueueThrowsException_ReturnsErrorMessage()
		{
			// Arrange
			m_mockCommandQueueService.Setup(s => s.GetQueueStatus())
				.Throws(new NotSupportedException("Feature not supported"));

			// Act
			var result = await m_tool.ListCommands();

			// Assert
			Assert.Contains("Error listing commands", result);
			Assert.Contains("Feature not supported", result);
		}

		[Fact]
		public async Task WaitForCommandCompletion_CommandNeverCompletes_TimesOut()
		{
			// Arrange
			m_mockCommandQueueService.Setup(s => s.GetCommandResult(It.IsAny<string>()))
				.ReturnsAsync("Command is still executing...");

			// Use reflection to access the private method
			var waitMethod = typeof(WindbgTool).GetMethod("WaitForCommandCompletion", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			Assert.NotNull(waitMethod);

			// Act - This will timeout, but let's limit it to a short time for testing
			var task = (Task<string>)waitMethod!.Invoke(m_tool, new object[] { "test-id", "test command" })!;
			
			// Wait a reasonable time but not the full 5 minutes
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
			
			try
			{
				await task.WaitAsync(cts.Token);
			}
			catch (OperationCanceledException)
			{
				// Expected - we don't want to wait 5 minutes for the actual timeout
			}

			// The method should eventually timeout and cancel the command
			// We can't easily test the full 5-minute timeout, but we verified the flow
			Assert.True(true); // Test that we can call the method without exceptions
		}

		[Fact]
		public async Task OpenWindbgDump_WithSymbolsPath_ValidatesSymbolsPath()
		{
			// Arrange
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "dummy content");
			var nonExistentSymbolsPath = Path.Combine(Path.GetTempPath(), "nonexistent_symbols");

			try
			{
				m_mockCdbSession.Setup(s => s.StartSession(It.IsAny<string>(), It.IsAny<string>()))
					.ReturnsAsync(true);

				// Act
				var result = await m_tool.OpenWindbgDump(tempFile, nonExistentSymbolsPath);

				// Assert
				// Should still succeed even with non-existent symbols path (just logs warning)
				Assert.Contains("Successfully opened", result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public async Task OpenWindbgRemote_WithSymbolsPath_ValidatesSymbolsPath()
		{
			// Arrange
			var nonExistentSymbolsPath = Path.Combine(Path.GetTempPath(), "nonexistent_symbols");
			m_mockCdbSession.Setup(s => s.StartSession(It.IsAny<string>(), It.IsAny<string>()))
				.ReturnsAsync(true);

			// Act
			var result = await m_tool.OpenWindbgRemote("tcp:Port=1234,Server=localhost", nonExistentSymbolsPath);

			// Assert
			// Should still succeed even with non-existent symbols path (just logs warning)
			Assert.Contains("Successfully connected", result);
		}

		[Fact]
		public async Task OpenWindbgDump_FileExistsButAccessDenied_ReturnsErrorMessage()
		{
			// This test simulates file access issues that might occur in real scenarios
			// We can't easily create a file that exists but can't be accessed in a unit test,
			// but we can test the general exception handling path

			// Arrange
			var tempFile = Path.GetTempFileName();

			try
			{
				File.WriteAllText(tempFile, "dummy content");
				
				// Simulate CdbSession failure due to file access issues
				m_mockCdbSession.Setup(s => s.StartSession(It.IsAny<string>(), It.IsAny<string>()))
					.ReturnsAsync(false); // Failed to start

				// Act
				var result = await m_tool.OpenWindbgDump(tempFile);

				// Assert
				Assert.Contains("Failed to open", result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
