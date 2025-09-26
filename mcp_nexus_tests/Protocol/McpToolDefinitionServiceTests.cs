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
			Assert.Equal(8, tools.Length); // 3 tools removed: list_windbg_dumps, get_session_info, get_current_time
		}

		[Fact]
		public void GetAllTools_ContainsRequiredAsyncTool()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();

			// Assert
			var asyncTool = tools.FirstOrDefault(t => t.Name == "nexus_exec_debugger_command_async");
			Assert.NotNull(asyncTool);
			Assert.Contains("ASYNC QUEUE", asyncTool.Description);
		}

		[Fact]
		public void GetAllTools_ContainsRequiredStatusTool()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();

			// Assert
			var statusTool = tools.FirstOrDefault(t => t.Name == "nexus_debugger_command_status");
			Assert.NotNull(statusTool);
			Assert.Contains("REQUIRED", statusTool.Description);
		}

		[Fact]
		public void GetAllTools_ContainsAllExpectedOpenTools()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var toolNames = tools.Select(t => t.Name).ToList();

			// Assert
			Assert.Contains("nexus_open_dump", toolNames);
			Assert.Contains("nexus_start_remote_debug", toolNames);
			Assert.Contains("nexus_close_dump", toolNames);
			Assert.Contains("nexus_stop_remote_debug", toolNames);
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

			// Assert
			Assert.Contains("nexus_debugger_command_status", toolNames);
			Assert.Contains("nexus_debugger_command_cancel", toolNames);
			Assert.Contains("nexus_list_debugger_commands", toolNames);
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
		Assert.Contains("nexus_open_dump", toolNames);
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
			var dumpTool = tools.First(t => t.Name == "nexus_open_dump");

			// Assert
			Assert.Contains("Automatically replaces", dumpTool.Description);
			Assert.NotNull(dumpTool.InputSchema);
		}

		[Fact]
		public void GetAllTools_RemoteToolHasCorrectSchema()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();
			var remoteTool = tools.First(t => t.Name == "nexus_start_remote_debug");

			// Assert
			Assert.Contains("tcp:Port=", remoteTool.Description);
			Assert.Contains("Automatically replaces", remoteTool.Description);
		}
	}
}

