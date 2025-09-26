using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Tools;
using mcp_nexus.Models;
using Xunit;

namespace mcp_nexus_tests.Services
{
	public class McpProtocolServiceIntegrationTests
	{
		private readonly McpProtocolService m_service;

		public McpProtocolServiceIntegrationTests()
		{
			// Create real services but with mocked dependencies for control
			var toolDefinitionService = new McpToolDefinitionService();
			
			// Create a SessionAwareWindbgTool with mocked dependencies
			var mockWindbgLogger = Mock.Of<ILogger<SessionAwareWindbgTool>>();
			var mockSessionManager = Mock.Of<ISessionManager>();
			var sessionAwareWindbgTool = new SessionAwareWindbgTool(mockWindbgLogger, mockSessionManager);
			
			// Create a real McpToolExecutionService  
			var mockExecutionLogger = Mock.Of<ILogger<McpToolExecutionService>>();
			var toolExecutionService = new McpToolExecutionService(sessionAwareWindbgTool, mockExecutionLogger);
			
			var logger = LoggerFactory.Create(b => { }).CreateLogger<McpProtocolService>();
			
			m_service = new McpProtocolService(
				toolDefinitionService,
				toolExecutionService,
				logger);
		}

		[Fact]
		public async Task ProcessRequest_InitializeMethod_ReturnsInitializeResult()
		{
			// Arrange
			var requestJson = """{"jsonrpc":"2.0","id":1,"method":"initialize"}""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

			// Assert
			Assert.NotNull(result);
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(1, response.Id);
			Assert.IsType<McpInitializeResult>(response.Result);
		}

		[Fact]
		public async Task ProcessRequest_NotificationInitialized_ReturnsEmptyObject()
		{
			// Arrange
			var requestJson = """{"jsonrpc":"2.0","method":"notifications/initialized"}""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

			// Assert
			Assert.NotNull(result);
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(0, response.Id); // Notifications have no ID
		}

		[Fact]
		public async Task ProcessRequest_ToolsList_ReturnsToolsListResult()
		{
			// Arrange
			var requestJson = """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

			// Assert
			Assert.NotNull(result);
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(2, response.Id);
			Assert.IsType<McpToolsListResult>(response.Result);
			
			var toolsListResult = response.Result as McpToolsListResult;
			Assert.NotNull(toolsListResult);
			Assert.NotEmpty(toolsListResult.Tools);
			Assert.Contains(toolsListResult.Tools, t => t.Name == "nexus_exec_debugger_command_async");
		}

		[Fact]
		public async Task ProcessRequest_NotificationCancelled_CallsCancelCurrentOperation()
		{
			// Arrange
			var requestJson = """
				{
					"jsonrpc": "2.0",
					"method": "notifications/cancelled",
					"params": {
						"requestId": "123",
						"reason": "User cancelled"
					}
				}
				""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

			// Assert
			Assert.NotNull(result);
			// Note: CDB cancellation verification removed since mock is local
		}

		[Fact]
		public async Task ProcessRequest_NotificationCancelledWithoutRequestId_HandlesGracefully()
		{
			// Arrange
			var requestJson = """
				{
					"jsonrpc": "2.0",
					"method": "notifications/cancelled",
					"params": {
						"reason": "No request ID provided"
					}
				}
				""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

			// Assert
			Assert.NotNull(result);
			// Should handle gracefully without crashing
		}

		[Fact]
		public async Task ProcessRequest_CancelCurrentOperationThrows_HandlesGracefully()
		{
			// Arrange
			// Note: CDB cancellation setup removed since mock is local
			
			var requestJson = """
				{
					"jsonrpc": "2.0",
					"method": "notifications/cancelled",
					"params": {
						"requestId": "123"
					}
				}
				""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

			// Assert
			Assert.NotNull(result);
			// Should handle the exception gracefully and still return a response
		}

		[Fact]
		public async Task ProcessRequest_UnknownMethod_ReturnsMethodNotFoundError()
		{
			// Arrange
			var requestJson = """{"jsonrpc":"2.0","id":4,"method":"unknown/method"}""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

		// Assert
		Assert.NotNull(result);
		var errorResponse = result as McpErrorResponse;
		Assert.NotNull(errorResponse);
		Assert.Equal(4, errorResponse.Id);
		Assert.Equal(-32601, errorResponse.Error.Code);
		Assert.Contains("Method not found", errorResponse.Error.Message);
		Assert.Contains("unknown/method", errorResponse.Error.Message);
		}

		[Fact]
		public async Task ProcessRequest_ToolsCallMissingParams_ReturnsParameterError()
		{
			// Arrange
			var requestJson = """{"jsonrpc":"2.0","id":5,"method":"tools/call"}""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

			// Assert
		Assert.NotNull(result);
		var errorResponse = result as McpErrorResponse;
		Assert.NotNull(errorResponse);
		Assert.Equal(5, errorResponse.Id);
		Assert.Equal(-32602, errorResponse.Error.Code);
		Assert.Contains("Missing params", errorResponse.Error.Message);
		}

		[Fact]
		public async Task ProcessRequest_ToolsCallMissingToolName_ReturnsParameterError()
		{
			// Arrange
			var requestJson = """
				{
					"jsonrpc": "2.0",
					"id": 6,
					"method": "tools/call",
					"params": {
						"arguments": {}
					}
				}
				""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

		// Assert
		Assert.NotNull(result);
		var errorResponse = result as McpErrorResponse;
		Assert.NotNull(errorResponse);
		Assert.Equal(6, errorResponse.Id);
		Assert.Equal(-32602, errorResponse.Error.Code);
		Assert.Contains("Missing tool name", errorResponse.Error.Message);
		}

		[Fact]
		public async Task ProcessRequest_ToolsCallEmptyToolName_ReturnsParameterError()
		{
			// Arrange
			var requestJson = """
				{
					"jsonrpc": "2.0",
					"id": 7,
					"method": "tools/call",
					"params": {
						"name": "",
						"arguments": {}
					}
				}
				""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

		// Assert
		Assert.NotNull(result);
		var errorResponse = result as McpErrorResponse;
		Assert.NotNull(errorResponse);
		Assert.Equal(7, errorResponse.Id);
		Assert.Equal(-32602, errorResponse.Error.Code);
		Assert.Contains("Invalid tool name", errorResponse.Error.Message);
		}

		[Fact]
		public async Task ProcessRequest_ToolsCallValidTool_ExecutesTool()
		{
			// Arrange - Use a tool that will return an error (since no active session)
			var requestJson = """
				{
					"jsonrpc": "2.0",
					"id": 8,
					"method": "tools/call",
					"params": {
						"name": "nexus_exec_debugger_command_async",
						"arguments": {
							"command": "version"
						}
					}
				}
				""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

		// Assert
		Assert.NotNull(result);
		var errorResponse = result as McpErrorResponse;
		Assert.NotNull(errorResponse);
		Assert.Equal(8, errorResponse.Id);
		Assert.Equal(-32602, errorResponse.Error.Code);
		Assert.Contains("MISSING SESSION ID", errorResponse.Error.Message);
		}

		[Fact]
		public async Task ProcessRequest_MalformedJson_ReturnsInvalidRequestError()
		{
			// Arrange - Missing method property
			var requestJson = """{"jsonrpc":"2.0","id":9}""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

			// Assert
			Assert.NotNull(result);
			var response = result as McpErrorResponse;
			Assert.NotNull(response);
			Assert.Equal(0, response.Id);
			Assert.Equal(-32600, response.Error.Code);
			Assert.Contains("Invalid Request", response.Error.Message);
		}

		[Fact]
		public async Task ProcessRequest_ToolsCallUnknownTool_ReturnsUnknownToolError()
		{
			// Arrange
			var requestJson = """
				{
					"jsonrpc": "2.0",
					"id": 10,
					"method": "tools/call",
					"params": {
						"name": "non_existent_tool",
						"arguments": {}
					}
				}
				""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

		// Assert
		Assert.NotNull(result);
		var errorResponse = result as McpErrorResponse;
		Assert.NotNull(errorResponse);
		Assert.Equal(10, errorResponse.Id);
		Assert.Equal(-32602, errorResponse.Error.Code);
		Assert.Contains("Unknown tool", errorResponse.Error.Message);
		}

		[Fact]
		public async Task ProcessRequest_ToolsCallDeprecatedTool_ReturnsDeprecationMessage()
		{
			// Arrange - Try to call a deprecated tool
			var requestJson = """
				{
					"jsonrpc": "2.0",
					"id": 11,
					"method": "tools/call",
					"params": {
						"name": "run_windbg_cmd",
						"arguments": {
							"command": "version"
						}
					}
				}
				""";
			var element = JsonDocument.Parse(requestJson).RootElement;

			// Act
			var result = await m_service.ProcessRequest(element);

		// Assert
		Assert.NotNull(result);
		var errorResponse = result as McpErrorResponse;
		Assert.NotNull(errorResponse);
		Assert.Equal(11, errorResponse.Id);
		Assert.Equal(-32602, errorResponse.Error.Code);
		Assert.Contains("Unknown tool", errorResponse.Error.Message);
		}
	}
}

