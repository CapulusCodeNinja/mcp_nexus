using System.Linq;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using Xunit;

namespace mcp_nexus_tests.Services
{
	public class McpToolDefinitionServiceTests
	{
		[Fact]
		public void GetAllTools_ReturnsExpectedNumberOfTools()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();

			// Assert
			Assert.Equal(4, tools.Length); // Core tools: nexus_open_dump_analyze_session, nexus_dump_analyze_session_async_command, nexus_dump_analyze_session_async_command_status, nexus_close_dump_analyze_session
		}

		[Fact]
		public void GetAllTools_ContainsRequiredAsyncTool()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();

			// Assert
			var asyncTool = tools.FirstOrDefault(t => t.Name == "nexus_dump_analyze_session_async_command");
			Assert.NotNull(asyncTool);
			Assert.Contains("asynchronous execution", asyncTool.Description);
		}

		[Fact]
		public void GetAllTools_ContainsRequiredStatusTool()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();

			// Assert
			var statusTool = tools.FirstOrDefault(t => t.Name == "nexus_dump_analyze_session_async_command_status");
			Assert.NotNull(statusTool);
			Assert.Contains("poll for the status", statusTool.Description);
		}

		[Fact]
		public void GetAllTools_ContainsAllExpectedOpenTools()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var toolNames = tools.Select(t => t.Name).ToList();

			// Assert - Only dump file analysis tools (remote debugging removed for first release)
			Assert.Contains("nexus_open_dump_analyze_session", toolNames);
			Assert.Contains("nexus_close_dump_analyze_session", toolNames);
		}

		[Fact]
		public void GetAllTools_ContainsAnalysisTools()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var toolNames = tools.Select(t => t.Name).ToList();

			// Assert
			// get_session_info was removed - test now checks for existing analysis tools
		}

		[Fact]
		public void GetAllTools_ContainsCommandManagementTools()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var toolNames = tools.Select(t => t.Name).ToList();

		// Assert - Only status checking (command cancellation removed for first release)
		Assert.Contains("nexus_dump_analyze_session_async_command_status", toolNames);
		}

		[Fact]
		public void GetAllTools_ContainsUtilityTools()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var toolNames = tools.Select(t => t.Name).ToList();

		// Assert
		Assert.DoesNotContain("list_windbg_dumps", toolNames); // Obsolete tool should not be present
		Assert.Contains("nexus_open_dump_analyze_session", toolNames);
		}

		[Fact]
		public void GetAllTools_DoesNotContainDeprecatedTools()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var toolNames = tools.Select(t => t.Name).ToList();

			// Assert
			Assert.DoesNotContain("run_windbg_cmd", toolNames);
			Assert.DoesNotContain("run_windbg_cmd_sync", toolNames);
		}

		[Fact]
		public void GetAllTools_AllToolsHaveValidSchemas()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();

			// Assert
			foreach (var tool in tools)
			{
				Assert.NotNull(tool.Name);
				Assert.NotEmpty(tool.Name);
				Assert.NotNull(tool.Description);
				Assert.NotEmpty(tool.Description);
				Assert.NotNull(tool.InputSchema);
			}
		}

		[Fact]
		public void GetAllTools_OpenDumpToolHasCorrectSchema()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var dumpTool = tools.First(t => t.Name == "nexus_open_dump_analyze_session");

			// Assert
			Assert.Contains("Open the analyze session", dumpTool.Description);
			Assert.NotNull(dumpTool.InputSchema);
		}

		[Fact]
		public void GetAllTools_DumpToolHasCorrectSchema()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var dumpTool = tools.First(t => t.Name == "nexus_open_dump_analyze_session");

			// Assert
			Assert.Contains("Tooling - Open Session", dumpTool.Description);
			Assert.Contains("dump file", dumpTool.Description);
		}
	}
}

