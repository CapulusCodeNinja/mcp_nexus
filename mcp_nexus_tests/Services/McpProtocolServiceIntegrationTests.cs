using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Models;
using mcp_nexus.Tools;
using Xunit;

namespace mcp_nexus_tests.Services
{
	public class McpProtocolServiceIntegrationTests
	{
		private readonly McpProtocolService m_service;
		private readonly Mock<ICdbSession> m_mockCdbSession;

		public McpProtocolServiceIntegrationTests()
		{
			// Create real services but with mocked dependencies for control
			var toolDefinitionService = new McpToolDefinitionService();
			
			// Create a real WindbgTool with mocked dependencies
			var mockWindbgLogger = Mock.Of<ILogger<WindbgTool>>();
			m_mockCdbSession = new Mock<ICdbSession>();
			var mockCommandQueueService = Mock.Of<ICommandQueueService>();
			var windbgTool = new WindbgTool(mockWindbgLogger, m_mockCdbSession.Object, mockCommandQueueService);
			
			// Create a real McpToolExecutionService  
			var mockExecutionLogger = Mock.Of<ILogger<McpToolExecutionService>>();
			var toolExecutionService = new McpToolExecutionService(windbgTool, mockExecutionLogger);
			
			var logger = LoggerFactory.Create(b => { }).CreateLogger<McpProtocolService>();
			
			m_service = new McpProtocolService(
				toolDefinitionService,
				toolExecutionService,
				m_mockCdbSession.Object,
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
			Assert.Contains(toolsListResult.Tools, t => t.Name == "run_windbg_cmd_async");
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
			m_mockCdbSession.Verify(x => x.CancelCurrentOperation(), Times.Once);
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
			m_mockCdbSession.Setup(x => x.CancelCurrentOperation()).Throws(new InvalidOperationException("Cancel failed"));
			
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
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(4, response.Id);
			
			// Should contain method not found error
			var resultJson = JsonSerializer.Serialize(response.Result);
			Assert.Contains("Method not found", resultJson);
			Assert.Contains("unknown/method", resultJson);
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
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(5, response.Id);
			
			var resultJson = JsonSerializer.Serialize(response.Result);
			Assert.Contains("Missing params", resultJson);
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
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(6, response.Id);
			
			var resultJson = JsonSerializer.Serialize(response.Result);
			Assert.Contains("Missing tool name", resultJson);
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
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(7, response.Id);
			
			var resultJson = JsonSerializer.Serialize(response.Result);
			Assert.Contains("Invalid tool name", resultJson);
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
						"name": "run_windbg_cmd_async",
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
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(8, response.Id);
			
			// The tool should return an error about no active session
			var resultString = JsonSerializer.Serialize(response.Result);
			Assert.Contains("No active debugging session", resultString);
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
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(10, response.Id);
			
			var resultJson = JsonSerializer.Serialize(response.Result);
			Assert.Contains("Unknown tool", resultJson);
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
			var response = result as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(11, response.Id);
			
			var resultJson = JsonSerializer.Serialize(response.Result);
			Assert.Contains("COMMAND REMOVED", resultJson);
		}
	}
}
