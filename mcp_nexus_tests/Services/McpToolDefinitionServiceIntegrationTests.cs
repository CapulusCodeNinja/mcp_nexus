using System.Linq;
using mcp_nexus.Services;
using mcp_nexus.Models;
using Xunit;

namespace mcp_nexus_tests.Services
{
	public class McpToolDefinitionServiceIntegrationTests
	{
		private readonly McpToolDefinitionService m_service;

		public McpToolDefinitionServiceIntegrationTests()
		{
			m_service = new McpToolDefinitionService();
		}

		[Fact]
		public void GetAllTools_ReturnsNonEmptyArray()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			Assert.NotNull(tools);
			Assert.NotEmpty(tools);
		}

		[Fact]
		public void GetAllTools_ReturnsExpectedNumberOfTools()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			// Should have 11 tools based on the implementation
			Assert.Equal(8, tools.Length); // 3 tools removed: list_windbg_dumps, get_session_info, get_current_time
		}

		[Fact]
		public void GetAllTools_AllToolsHaveRequiredProperties()
		{
			// Act
			var tools = m_service.GetAllTools();

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
		public void GetAllTools_ContainsRunWindbgCmdAsyncTool()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var asyncTool = tools.FirstOrDefault(t => t.Name == "nexus_exec_debugger_command_async");
			Assert.NotNull(asyncTool);
			Assert.Contains("ASYNC QUEUE", asyncTool.Description);
			Assert.Contains("commandId", asyncTool.Description);
		}

		[Fact]
		public void GetAllTools_ContainsOpenWindbgDumpTool()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var dumpTool = tools.FirstOrDefault(t => t.Name == "nexus_open_dump");
			Assert.NotNull(dumpTool);
			Assert.Contains("crash dump", dumpTool.Description);
		}

		[Fact]
		public void GetAllTools_ContainsOpenWindbgRemoteTool()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var remoteTool = tools.FirstOrDefault(t => t.Name == "nexus_start_remote_debug");
			Assert.NotNull(remoteTool);
			Assert.Contains("remote", remoteTool.Description);
		}

		[Fact]
		public void GetAllTools_ContainsCloseCommands()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			Assert.Contains(tools, t => t.Name == "nexus_close_dump");
			Assert.Contains(tools, t => t.Name == "nexus_stop_remote_debug");
		}

		[Fact]
		public void GetAllTools_ContainsCommandManagementTools()
		{
			// Act
			var tools = m_service.GetAllTools();

		// Assert
		Assert.Contains(tools, t => t.Name == "nexus_debugger_command_status");
		Assert.Contains(tools, t => t.Name == "nexus_debugger_command_cancel");
		Assert.Contains(tools, t => t.Name == "nexus_list_debugger_commands");
		}

	[Fact]
	public void GetAllTools_ContainsNexusTools()
	{
		// Act
		var tools = m_service.GetAllTools();

		// Assert
		Assert.Contains(tools, t => t.Name == "nexus_open_dump");
		Assert.Contains(tools, t => t.Name == "nexus_start_remote_debug");
		Assert.Contains(tools, t => t.Name == "nexus_exec_debugger_command_async");
		Assert.Contains(tools, t => t.Name == "nexus_debugger_command_status");
		Assert.DoesNotContain(tools, t => t.Name == "list_windbg_dumps");
		Assert.DoesNotContain(tools, t => t.Name == "get_session_info");
		Assert.DoesNotContain(tools, t => t.Name == "get_current_time");
	}

		[Fact]
		public void GetAllTools_DoesNotContainDeprecatedTools()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			// These deprecated tools should not be in the list
			Assert.DoesNotContain(tools, t => t.Name == "run_windbg_cmd");
			Assert.DoesNotContain(tools, t => t.Name == "run_windbg_cmd_sync");
		}

		[Fact]
		public void GetAllTools_AllToolNamesAreUnique()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var toolNames = tools.Select(t => t.Name).ToList();
			Assert.Equal(toolNames.Count, toolNames.Distinct().Count());
		}

		[Fact]
		public void GetAllTools_OpenDumpToolHasCorrectSchema()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var dumpTool = tools.First(t => t.Name == "nexus_open_dump");
			Assert.NotNull(dumpTool.InputSchema);
			
			// InputSchema should be a properly structured object
			var schemaJson = System.Text.Json.JsonSerializer.Serialize(dumpTool.InputSchema);
			Assert.Contains("dumpPath", schemaJson);
			Assert.Contains("symbolsPath", schemaJson);
			Assert.Contains("required", schemaJson);
		}

		[Fact]
		public void GetAllTools_RemoteToolHasCorrectSchema()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var remoteTool = tools.First(t => t.Name == "nexus_start_remote_debug");
			Assert.NotNull(remoteTool.InputSchema);
			
			var schemaJson = System.Text.Json.JsonSerializer.Serialize(remoteTool.InputSchema);
			Assert.Contains("connectionString", schemaJson);
			Assert.Contains("symbolsPath", schemaJson);
		}

		[Fact]
		public void GetAllTools_GetCommandStatusToolEmphasizesRequirement()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var statusTool = tools.First(t => t.Name == "nexus_debugger_command_status");
			Assert.Contains("REQUIRED", statusTool.Description);
			Assert.Contains("ONLY way", statusTool.Description);
		}

		[Fact]
		public void GetAllTools_ListCommandsToolHasCorrectDescription()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var listTool = tools.First(t => t.Name == "nexus_list_debugger_commands");
			Assert.Contains("queue status", listTool.Description);
			Assert.Contains("queued", listTool.Description);
		}

		[Fact]
		public void GetAllTools_CancelCommandToolHasCorrectDescription()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var cancelTool = tools.First(t => t.Name == "nexus_debugger_command_cancel");
			Assert.Contains("Cancel", cancelTool.Description);
			Assert.Contains("long-running", cancelTool.Description);
		}

		[Fact]
		public void GetAllTools_GetCurrentTimeToolExists()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
		// get_current_time tool was removed - test skipped
		var timeTool = tools.FirstOrDefault(t => t.Name == "get_current_time");
		Assert.Null(timeTool); // Tool should not exist
		}
	}
}
