using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Controllers;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Models;
using Xunit;

namespace mcp_nexus_tests.Controllers
{
	public class McpControllerTests
	{
		private readonly McpController m_controller;
		private readonly Mock<ILogger<McpController>> m_mockLogger;

		public McpControllerTests()
		{
			// Create real dependencies for McpProtocolService
			var toolDefinitionService = new McpToolDefinitionService();
			
			// Create SessionAwareWindbgTool with proper dependencies
			var mockWindbgLogger = LoggerFactory.Create(b => { }).CreateLogger<mcp_nexus.Tools.SessionAwareWindbgTool>();
			var mockSessionManager = Mock.Of<mcp_nexus.Session.ISessionManager>();
			var sessionAwareWindbgTool = new mcp_nexus.Tools.SessionAwareWindbgTool(mockWindbgLogger, mockSessionManager);
			
			var mockExecutionLogger = LoggerFactory.Create(b => { }).CreateLogger<McpToolExecutionService>();
			var toolExecutionService = new McpToolExecutionService(sessionAwareWindbgTool, mockExecutionLogger);
			
			var mockProtocolLogger = LoggerFactory.Create(b => { }).CreateLogger<McpProtocolService>();
			// Mock ICdbSession for McpProtocolService
			var mockCdbSession = Mock.Of<mcp_nexus.Debugger.ICdbSession>();
			
			var protocolService = new McpProtocolService(
				toolDefinitionService,
				toolExecutionService,
				mockCdbSession,
				mockProtocolLogger);

			m_mockLogger = new Mock<ILogger<McpController>>();
			m_controller = new McpController(protocolService, m_mockLogger.Object);

			// Set up controller context
			m_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			};
		}

		[Fact]
		public async Task HandleMcpRequest_ValidInitializeRequest_ReturnsOkResult()
		{
			// Arrange
			var requestBody = """{"jsonrpc":"2.0","id":1,"method":"initialize"}""";
			var requestBytes = Encoding.UTF8.GetBytes(requestBody);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;
			m_controller.Request.Headers["Content-Type"] = "application/json";

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.IsType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.NotNull(okResult?.Value);
		}

		[Fact]
		public async Task HandleMcpRequest_ValidToolsListRequest_ReturnsOkResult()
		{
			// Arrange
			var requestBody = """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""";
			var requestBytes = Encoding.UTF8.GetBytes(requestBody);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;
			m_controller.Request.Headers["Content-Type"] = "application/json";

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.IsType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.NotNull(okResult?.Value);
			
			// Should contain tools list response
			var response = okResult.Value as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(2, response.Id);
		}

		[Fact]
		public async Task HandleMcpRequest_InvalidJson_ReturnsParseError()
		{
			// Arrange
			var invalidJson = """{"jsonrpc":"2.0","id":1,invalid}""";
			var requestBytes = Encoding.UTF8.GetBytes(invalidJson);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.IsType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.NotNull(okResult?.Value);
			
			// Should contain parse error
			var responseJson = JsonSerializer.Serialize(okResult.Value);
			Assert.Contains("-32700", responseJson); // Parse error code
			Assert.Contains("Parse error", responseJson);
		}

		[Fact]
		public async Task HandleMcpRequest_SetsSessionIdHeader()
		{
			// Arrange
			var customSessionId = "test-session-123";
			var requestBody = """{"jsonrpc":"2.0","id":1,"method":"initialize"}""";
			var requestBytes = Encoding.UTF8.GetBytes(requestBody);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;
			m_controller.Request.Headers["Mcp-Session-Id"] = customSessionId;

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.True(m_controller.Response.Headers.ContainsKey("Mcp-Session-Id"));
			Assert.Equal(customSessionId, m_controller.Response.Headers["Mcp-Session-Id"].FirstOrDefault());
		}

		[Fact]
		public async Task HandleMcpRequest_NoSessionIdHeader_GeneratesNewSessionId()
		{
			// Arrange
			var requestBody = """{"jsonrpc":"2.0","id":1,"method":"initialize"}""";
			var requestBytes = Encoding.UTF8.GetBytes(requestBody);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.True(m_controller.Response.Headers.ContainsKey("Mcp-Session-Id"));
			var sessionId = m_controller.Response.Headers["Mcp-Session-Id"].FirstOrDefault();
			Assert.NotNull(sessionId);
			Assert.NotEmpty(sessionId!);
		}

		[Fact]
		public async Task HandleMcpRequest_SetsCorsHeaders()
		{
			// Arrange
			var requestBody = """{"jsonrpc":"2.0","id":1,"method":"initialize"}""";
			var requestBytes = Encoding.UTF8.GetBytes(requestBody);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.Equal("*", m_controller.Response.Headers["Access-Control-Allow-Origin"].FirstOrDefault());
			Assert.Equal("GET, POST, OPTIONS", m_controller.Response.Headers["Access-Control-Allow-Methods"].FirstOrDefault());
			Assert.Equal("Content-Type, Mcp-Session-Id", m_controller.Response.Headers["Access-Control-Allow-Headers"].FirstOrDefault());
		}

		[Fact]
		public void HandleMcpGetRequest_ReturnsServerInfo()
		{
			// Act
			var result = m_controller.HandleMcpGetRequest();

			// Assert
			Assert.IsType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.NotNull(okResult?.Value);
			Assert.IsType<McpServerInfoResponse>(okResult.Value);
			
			var serverInfo = okResult.Value as McpServerInfoResponse;
			Assert.NotNull(serverInfo);
			Assert.Equal("2.0", serverInfo.JsonRpc);
		}

		[Fact]
		public void HandleMcpGetRequest_SetsCorsHeaders()
		{
			// Act
			var result = m_controller.HandleMcpGetRequest();

			// Assert
			Assert.Equal("*", m_controller.Response.Headers["Access-Control-Allow-Origin"].FirstOrDefault());
			Assert.Equal("GET, POST, OPTIONS", m_controller.Response.Headers["Access-Control-Allow-Methods"].FirstOrDefault());
			Assert.Equal("Content-Type, Mcp-Session-Id", m_controller.Response.Headers["Access-Control-Allow-Headers"].FirstOrDefault());
		}

		[Fact]
		public void HandlePreflight_ReturnsOkWithCorsHeaders()
		{
			// Act
			var result = m_controller.HandlePreflight();

			// Assert
			Assert.IsType<OkResult>(result);
			Assert.Equal("*", m_controller.Response.Headers["Access-Control-Allow-Origin"].FirstOrDefault());
			Assert.Equal("GET, POST, OPTIONS", m_controller.Response.Headers["Access-Control-Allow-Methods"].FirstOrDefault());
			Assert.Equal("Content-Type, Mcp-Session-Id", m_controller.Response.Headers["Access-Control-Allow-Headers"].FirstOrDefault());
		}

		[Fact]
		public async Task HandleMcpRequest_WithNotificationMethod_ReturnsOkResult()
		{
			// Arrange
			var requestBody = """{"jsonrpc":"2.0","method":"notifications/initialized"}""";
			var requestBytes = Encoding.UTF8.GetBytes(requestBody);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.IsType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.NotNull(okResult?.Value);
		}

		[Fact]
		public async Task HandleMcpRequest_WithToolsCallMethod_ReturnsOkResult()
		{
			// Arrange
			var requestBody = """
				{
					"jsonrpc": "2.0",
					"id": 3,
					"method": "tools/call",
					"params": {
						"name": "run_windbg_cmd_async",
						"arguments": {
							"command": "version"
						}
					}
				}
				""";
			var requestBytes = Encoding.UTF8.GetBytes(requestBody);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.IsType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.NotNull(okResult?.Value);
			
			var response = okResult.Value as McpSuccessResponse;
			Assert.NotNull(response);
			Assert.Equal(3, response.Id);
		}

		[Fact]
		public async Task HandleMcpRequest_WithParametersInRequest_ProcessesCorrectly()
		{
			// Arrange
			var requestBody = """
				{
					"jsonrpc": "2.0",
					"id": 4,
					"method": "tools/call",
					"params": {
						"name": "unknown_tool",
						"arguments": {
							"param1": "value1",
							"param2": 42
						}
					}
				}
				""";
			var requestBytes = Encoding.UTF8.GetBytes(requestBody);
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.IsType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.NotNull(okResult?.Value);
		}

		[Fact]
		public async Task HandleMcpRequest_EmptyRequestBody_ReturnsParseError()
		{
			// Arrange
			var requestBytes = Encoding.UTF8.GetBytes("");
			var requestStream = new MemoryStream(requestBytes);
			
			m_controller.Request.Body = requestStream;

			// Act
			var result = await m_controller.HandleMcpRequest();

			// Assert
			Assert.IsType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.NotNull(okResult?.Value);
			
			var responseJson = JsonSerializer.Serialize(okResult.Value);
			Assert.Contains("-32700", responseJson); // Parse error code
		}
	}
}

