using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Tools;

namespace mcp_nexus_tests.Tools
{
	/// <summary>
	/// Edge case and boundary condition tests for WindbgTool
	/// </summary>
	public class WindbgToolEdgeCaseTests
	{
		private readonly Mock<ILogger<WindbgTool>> m_mockLogger;
		private readonly Mock<ICdbSession> m_mockCdbSession;
		private readonly Mock<ICommandQueueService> m_mockCommandQueueService;
		private readonly WindbgTool m_tool;

		public WindbgToolEdgeCaseTests()
		{
			m_mockLogger = new Mock<ILogger<WindbgTool>>();
			m_mockCdbSession = new Mock<ICdbSession>();
			m_mockCommandQueueService = new Mock<ICommandQueueService>();
			m_tool = new WindbgTool(m_mockLogger.Object, m_mockCdbSession.Object, m_mockCommandQueueService.Object);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public async Task OpenWindbgDump_WithInvalidDumpPath_ReturnsErrorMessage(string invalidPath)
		{
			// Act
			var result = await m_tool.OpenWindbgDump(invalidPath);

			// Assert
			Assert.Contains("cannot be null or empty", result, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task OpenWindbgDump_WithNonExistentFile_ReturnsFileNotFoundError()
		{
			// Arrange
			var nonExistentPath = @"C:\NonExistent\File.dmp";

			// Act
			var result = await m_tool.OpenWindbgDump(nonExistentPath);

			// Assert
			Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public async Task OpenWindbgRemote_WithInvalidConnectionString_ReturnsErrorMessage(string invalidConnectionString)
		{
			// Act
			var result = await m_tool.OpenWindbgRemote(invalidConnectionString);

			// Assert
			Assert.Contains("cannot be null or empty", result, StringComparison.OrdinalIgnoreCase);
		}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task RunWindbgCmdAsync_WithInvalidCommand_ReturnsErrorResponse(string invalidCommand)
	{
		// Act
		var result = await m_tool.RunWindbgCmdAsync(invalidCommand);

		// Assert
		Assert.Contains("Command cannot be null or empty", result);
	}

		[Fact]
		public async Task RunWindbgCmdAsync_WithNoActiveSession_ReturnsErrorResponse()
		{
			// Arrange
			m_mockCdbSession.Setup(x => x.IsActive).Returns(false);

			// Act
			var result = await m_tool.RunWindbgCmdAsync("version");

			// Assert
			Assert.Contains("No active debugging session", result);
		}

		[Fact]
		public async Task GetSessionInfo_WithNoActiveSession_ReturnsNoSessionMessage()
		{
			// Arrange
			m_mockCdbSession.Setup(x => x.IsActive).Returns(false);

			// Act
			var result = await m_tool.GetSessionInfo();

			// Assert
			Assert.Contains("No active debugging session", result);
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("invalid-id")]
		public async Task GetCommandStatus_WithInvalidCommandId_ReturnsErrorResponse(string invalidCommandId)
		{
			// Arrange
			m_mockCommandQueueService.Setup(x => x.GetCommandResult(invalidCommandId))
				.ReturnsAsync("Command not found");

			// Act
			var result = await m_tool.GetCommandStatus(invalidCommandId);

			// Assert
			Assert.Contains("error", result, StringComparison.OrdinalIgnoreCase);
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("invalid-id")]
		public async Task CancelCommand_WithInvalidCommandId_ReturnsErrorResponse(string invalidCommandId)
		{
			// Arrange
			m_mockCommandQueueService.Setup(x => x.CancelCommand(invalidCommandId))
				.Returns(false);

			// Act
			var result = await m_tool.CancelCommand(invalidCommandId);

			// Assert
			Assert.Contains("error", result, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task OpenWindbgDump_WithVeryLongPath_HandlesGracefully()
		{
			// Arrange
			var longPath = new string('a', 300) + ".dmp"; // Very long path

			// Act
			var result = await m_tool.OpenWindbgDump(longPath);

			// Assert
			Assert.NotNull(result);
			// Should either handle it or return an appropriate error
		}

		[Fact]
		public async Task OpenWindbgDump_WithSpecialCharactersInPath_HandlesGracefully()
		{
			// Arrange
			var specialCharPath = @"C:\Test\File With Spaces & Special @#$%.dmp";

			// Act
			var result = await m_tool.OpenWindbgDump(specialCharPath);

			// Assert
			Assert.NotNull(result);
			// Should handle special characters appropriately
		}

		[Fact]
		public async Task RunWindbgCmdAsync_WithVeryLongCommand_HandlesGracefully()
		{
			// Arrange
			m_mockCdbSession.Setup(x => x.IsActive).Returns(true);
			var longCommand = new string('a', 1000); // Very long command
			m_mockCommandQueueService.Setup(x => x.QueueCommand(longCommand))
				.Returns("cmd-123");

			// Act
			var result = await m_tool.RunWindbgCmdAsync(longCommand);

			// Assert
			Assert.NotNull(result);
			Assert.Contains("cmd-123", result);
		}

		[Fact]
		public async Task CloseWindbgDump_WhenAlreadyClosed_HandlesGracefully()
		{
			// Arrange
			m_mockCdbSession.Setup(x => x.IsActive).Returns(false);
			m_mockCdbSession.Setup(x => x.StopSession()).ReturnsAsync(false);

			// Act
			var result = await m_tool.CloseWindbgDump();

			// Assert
			Assert.NotNull(result);
			// Should handle already closed session gracefully
		}

		[Fact]
		public async Task CloseWindbgRemote_WhenAlreadyClosed_HandlesGracefully()
		{
			// Arrange
			m_mockCdbSession.Setup(x => x.IsActive).Returns(false);
			m_mockCdbSession.Setup(x => x.StopSession()).ReturnsAsync(false);

			// Act
			var result = await m_tool.CloseWindbgRemote();

			// Assert
			Assert.NotNull(result);
			// Should handle already closed session gracefully
		}


		[Fact]
		public async Task RunWindbgCmdAsync_WithCommandContainingNewlines_HandlesGracefully()
		{
			// Arrange
			m_mockCdbSession.Setup(x => x.IsActive).Returns(true);
			var commandWithNewlines = "version\r\n!analyze -v\r\nquit";
			m_mockCommandQueueService.Setup(x => x.QueueCommand(commandWithNewlines))
				.Returns("cmd-123");

			// Act
			var result = await m_tool.RunWindbgCmdAsync(commandWithNewlines);

			// Assert
			Assert.NotNull(result);
			Assert.Contains("cmd-123", result);
		}

		[Fact]
		public async Task GetCommandStatus_WithNullCommandId_HandlesGracefully()
		{
			// Act
			var result = await m_tool.GetCommandStatus(null!);

			// Assert
			Assert.NotNull(result);
			Assert.Contains("error", result, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task CancelCommand_WithNullCommandId_HandlesGracefully()
		{
			// Act
			var result = await m_tool.CancelCommand(null!);

			// Assert
			Assert.NotNull(result);
			Assert.Contains("error", result, StringComparison.OrdinalIgnoreCase);
		}
	}
}
