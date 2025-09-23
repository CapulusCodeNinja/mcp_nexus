using System.Linq;
using mcp_nexus.Services;
using Xunit;

namespace mcp_nexus.tests.Services
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
			Assert.Equal(11, tools.Length); // All non-deprecated tools
		}

		[Fact]
		public void GetAllTools_ContainsRequiredAsyncTool()
		{
			// Arrange
			var service = new McpToolDefinitionService();

			// Act
			var tools = service.GetAllTools();

			// Assert
			var asyncTool = tools.FirstOrDefault(t => t.Name == "run_windbg_cmd_async");
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
			var statusTool = tools.FirstOrDefault(t => t.Name == "get_command_status");
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
			Assert.Contains("open_windbg_dump", toolNames);
			Assert.Contains("open_windbg_remote", toolNames);
			Assert.Contains("close_windbg_dump", toolNames);
			Assert.Contains("close_windbg_remote", toolNames);
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
			Assert.Contains("get_session_info", toolNames);
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
			Assert.Contains("get_command_status", toolNames);
			Assert.Contains("cancel_command", toolNames);
			Assert.Contains("list_commands", toolNames);
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
			Assert.Contains("list_windbg_dumps", toolNames);
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
			var dumpTool = tools.First(t => t.Name == "open_windbg_dump");

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
			var remoteTool = tools.First(t => t.Name == "open_windbg_remote");

			// Assert
			Assert.Contains("tcp:Port=", remoteTool.Description);
			Assert.Contains("Automatically replaces", remoteTool.Description);
		}
	}
}
