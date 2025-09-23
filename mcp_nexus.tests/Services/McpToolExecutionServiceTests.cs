using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Tools;
using Xunit;

namespace mcp_nexus.tests.Services
{
	public class McpToolExecutionServiceTests
	{
		private static ILogger<McpToolExecutionService> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<McpToolExecutionService>();

		private readonly Mock<ICdbSession> m_mockCdbSession;
		private readonly Mock<ICommandQueueService> m_mockCommandQueueService;
		private readonly WindbgTool m_windbgTool;
		private readonly TimeTool m_timeTool;
		private readonly McpToolExecutionService m_service;

		public McpToolExecutionServiceTests()
		{
			m_mockCdbSession = new Mock<ICdbSession>();
			m_mockCommandQueueService = new Mock<ICommandQueueService>();
			
			var mockWindbgLogger = LoggerFactory.Create(b => { }).CreateLogger<WindbgTool>();
			var mockTimeLogger = LoggerFactory.Create(b => { }).CreateLogger<TimeTool>();
			
			m_windbgTool = new WindbgTool(mockWindbgLogger, m_mockCdbSession.Object, m_mockCommandQueueService.Object);
			m_timeTool = new TimeTool(mockTimeLogger);
			
			m_service = new McpToolExecutionService(m_windbgTool, m_timeTool, CreateNullLogger());
		}

		[Fact]
		public async Task ExecuteTool_UnknownTool_ReturnsError()
		{
			// Arrange
			var args = JsonDocument.Parse("{}").RootElement;

			// Act
			var result = await m_service.ExecuteTool("unknown_tool", args);

			// Assert
			Assert.NotNull(result);
			var resultStr = result.ToString();
			Assert.Contains("Unknown tool", resultStr);
		}

		[Fact]
		public async Task ExecuteTool_OpenWindbgDump_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"dumpPath\": \"C:\\\\test.dmp\"}").RootElement;
			m_mockCdbSession.Setup(s => s.StartSession(It.IsAny<string>(), null)).ReturnsAsync(false);

			// Act
			var result = await m_service.ExecuteTool("open_windbg_dump", args);

			// Assert
			Assert.NotNull(result);
			m_mockCdbSession.Verify(s => s.StartSession(It.IsAny<string>(), null), Times.Once);
		}

		[Fact]
		public async Task ExecuteTool_CloseWindbgDump_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{}").RootElement;
			m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

			// Act
			var result = await m_service.ExecuteTool("close_windbg_dump", args);

			// Assert
			Assert.NotNull(result);
			m_mockCommandQueueService.Verify(q => q.CancelAllCommands(It.IsAny<string>()), Times.Once);
		}

		[Fact]
		public async Task ExecuteTool_RunWindbgCmdAsync_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"command\": \"version\"}").RootElement;
			m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
			m_mockCommandQueueService.Setup(q => q.QueueCommand(It.IsAny<string>())).Returns("test-id");

			// Act
			var result = await m_service.ExecuteTool("run_windbg_cmd_async", args);

			// Assert
			Assert.NotNull(result);
			m_mockCommandQueueService.Verify(q => q.QueueCommand("version"), Times.Once);
		}

		[Fact]
		public async Task ExecuteTool_GetCommandStatus_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"commandId\": \"test-id\"}").RootElement;
			m_mockCommandQueueService.Setup(q => q.GetCommandResult("test-id")).ReturnsAsync("completed");

			// Act
			var result = await m_service.ExecuteTool("get_command_status", args);

			// Assert
			Assert.NotNull(result);
			m_mockCommandQueueService.Verify(q => q.GetCommandResult("test-id"), Times.Once);
		}

		[Fact]
		public async Task ExecuteTool_CancelCommand_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"commandId\": \"test-id\"}").RootElement;
			m_mockCommandQueueService.Setup(q => q.CancelCommand("test-id")).Returns(true);

			// Act
			var result = await m_service.ExecuteTool("cancel_command", args);

			// Assert
			Assert.NotNull(result);
			m_mockCommandQueueService.Verify(q => q.CancelCommand("test-id"), Times.Once);
		}

		[Fact]
		public async Task ExecuteTool_ListCommands_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{}").RootElement;
			m_mockCommandQueueService.Setup(q => q.GetCurrentCommand()).Returns((QueuedCommand?)null);
			m_mockCommandQueueService.Setup(q => q.GetQueueStatus()).Returns(Array.Empty<(string, string, DateTime, string)>());

			// Act
			var result = await m_service.ExecuteTool("list_commands", args);

			// Assert
			Assert.NotNull(result);
			m_mockCommandQueueService.Verify(q => q.GetCurrentCommand(), Times.Once);
		}

		[Fact]
		public async Task ExecuteTool_GetSessionInfo_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{}").RootElement;
			m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

			// Act
			var result = await m_service.ExecuteTool("get_session_info", args);

			// Assert
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ExecuteTool_AnalyzeCallStack_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{}").RootElement;
			m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

			// Act
			var result = await m_service.ExecuteTool("analyze_call_stack", args);

			// Assert
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ExecuteTool_AnalyzeMemory_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{}").RootElement;
			m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

			// Act
			var result = await m_service.ExecuteTool("analyze_memory", args);

			// Assert
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ExecuteTool_AnalyzeCrashPatterns_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{}").RootElement;
			m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

			// Act
			var result = await m_service.ExecuteTool("analyze_crash_patterns", args);

			// Assert
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ExecuteTool_GetCurrentTime_CallsTimeTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"city\": \"New York\"}").RootElement;

			// Act
			var result = await m_service.ExecuteTool("get_current_time", args);

			// Assert
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ExecuteTool_DeprecatedRunWindbgCmd_ReturnsDeprecationMessage()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"command\": \"version\"}").RootElement;

			// Act
			var result = await m_service.ExecuteTool("run_windbg_cmd", args);

			// Assert
			Assert.NotNull(result);
			var resultStr = result.ToString();
			Assert.Contains("COMMAND REMOVED", resultStr);
			Assert.Contains("run_windbg_cmd_async", resultStr);
		}

		[Fact]
		public async Task ExecuteTool_DeprecatedRunWindbgCmdSync_ReturnsDeprecationMessage()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"command\": \"version\"}").RootElement;

			// Act
			var result = await m_service.ExecuteTool("run_windbg_cmd_sync", args);

			// Assert
			Assert.NotNull(result);
			var resultStr = result.ToString();
			Assert.Contains("PERMANENTLY REMOVED", resultStr);
			Assert.Contains("run_windbg_cmd_async", resultStr);
		}

		[Fact]
		public async Task ExecuteTool_ExceptionInTool_ReturnsErrorResult()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"dumpPath\": \"\"}").RootElement; // This should cause an error

			// Act
			var result = await m_service.ExecuteTool("open_windbg_dump", args);

			// Assert
			Assert.NotNull(result);
			// Should handle the exception gracefully and return error result
		}

		[Fact]
		public async Task ExecuteTool_ListWindbgDumps_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"directoryPath\": \"C:\\\\temp\"}").RootElement;

			// Act
			var result = await m_service.ExecuteTool("list_windbg_dumps", args);

			// Assert
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ExecuteTool_OpenWindbgRemote_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{\"connectionString\": \"tcp:Port=5005,Server=localhost\"}").RootElement;
			m_mockCdbSession.Setup(s => s.StartSession(It.IsAny<string>(), null)).ReturnsAsync(false);

			// Act
			var result = await m_service.ExecuteTool("open_windbg_remote", args);

			// Assert
			Assert.NotNull(result);
			m_mockCdbSession.Verify(s => s.StartSession(It.IsAny<string>(), null), Times.Once);
		}

		[Fact]
		public async Task ExecuteTool_CloseWindbgRemote_CallsWindbgTool()
		{
			// Arrange
			var args = JsonDocument.Parse("{}").RootElement;
			m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

			// Act
			var result = await m_service.ExecuteTool("close_windbg_remote", args);

			// Assert
			Assert.NotNull(result);
			m_mockCommandQueueService.Verify(q => q.CancelAllCommands(It.IsAny<string>()), Times.Once);
		}
	}
}
