using System.Linq;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
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
			// Core tools for first release: nexus_open_dump, nexus_exec_debugger_command_async, nexus_debugger_command_status, nexus_close_dump
			Assert.Equal(4, tools.Length);
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
			Assert.Contains("EXECUTE COMMANDS", asyncTool.Description);
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
		public void GetAllTools_ContainsOpenDumpTool()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var dumpTool = tools.FirstOrDefault(t => t.Name == "nexus_open_dump");
			Assert.NotNull(dumpTool);
			Assert.Contains("crash dump", dumpTool.Description);
		}

		[Fact]
		public void GetAllTools_ContainsCloseCommands()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert - Only dump file closure (remote debugging removed for first release)
			Assert.Contains(tools, t => t.Name == "nexus_close_dump");
		}

		[Fact]
		public void GetAllTools_ContainsCommandManagementTools()
		{
			// Act
			var tools = m_service.GetAllTools();

		// Assert - Only status checking (command cancellation removed for first release)
		Assert.Contains(tools, t => t.Name == "nexus_debugger_command_status");
		}

	[Fact]
	public void GetAllTools_ContainsNexusTools()
	{
		// Act
		var tools = m_service.GetAllTools();

		// Assert - Core tools for first release
		Assert.Contains(tools, t => t.Name == "nexus_open_dump");
		Assert.Contains(tools, t => t.Name == "nexus_exec_debugger_command_async");
		Assert.Contains(tools, t => t.Name == "nexus_debugger_command_status");
		Assert.Contains(tools, t => t.Name == "nexus_close_dump");
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
		public void GetAllTools_DumpToolHasCorrectSchema()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var dumpTool = tools.First(t => t.Name == "nexus_open_dump");
			Assert.NotNull(dumpTool.InputSchema);
			
			var schemaJson = System.Text.Json.JsonSerializer.Serialize(dumpTool.InputSchema);
			Assert.Contains("dumpPath", schemaJson);
		}

		[Fact]
		public void GetAllTools_GetCommandStatusToolEmphasizesRequirement()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var statusTool = tools.First(t => t.Name == "nexus_debugger_command_status");
			Assert.Contains("GET RESULTS", statusTool.Description);
			Assert.Contains("ONLY way", statusTool.Description);
		}

	// Test removed - nexus_list_debugger_commands no longer advertised

		[Fact]
		public void GetAllTools_StatusToolHasCorrectDescription()
		{
			// Act
			var tools = m_service.GetAllTools();

			// Assert
			var statusTool = tools.First(t => t.Name == "nexus_debugger_command_status");
			Assert.Contains("GET RESULTS", statusTool.Description);
			Assert.Contains("ONLY way", statusTool.Description);
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

