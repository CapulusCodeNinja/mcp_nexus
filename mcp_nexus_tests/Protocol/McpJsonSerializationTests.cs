using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Models;
using mcp_nexus.Protocol;
using mcp_nexus.Tools;
using Xunit;

namespace mcp_nexus_tests.Protocol
{
    public class McpJsonSerializationTests
    {
        private JsonSerializerOptions _jsonOptions = null!;

        public McpJsonSerializationTests()
        {
            // Use the same serialization options as the MCP controller
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null // Don't change property names - MCP protocol requires exact field names like 'jsonrpc'
            };
        }

        [Fact]
        public void McpSuccessResponse_ShouldSerializeWithLowercaseFieldNames()
        {
            // Arrange
            var response = new McpSuccessResponse
            {
                Id = 123,
                Result = new { message = "test" }
            };

            // Act
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            var jsonDoc = JsonDocument.Parse(json);

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("jsonrpc", out var jsonrpcProp), "Should have 'jsonrpc' field (lowercase)");
            Assert.Equal("2.0", jsonrpcProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("id", out var idProp), "Should have 'id' field (lowercase)");
            Assert.Equal(123, idProp.GetInt32());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var resultProp), "Should have 'result' field (lowercase)");
            
            // Should NOT have uppercase versions
            Assert.False(jsonDoc.RootElement.TryGetProperty("Id", out _), "Should NOT have 'Id' field (uppercase)");
            Assert.False(jsonDoc.RootElement.TryGetProperty("Result", out _), "Should NOT have 'Result' field (uppercase)");
        }

        [Fact]
        public void McpErrorResponse_ShouldSerializeWithLowercaseFieldNames()
        {
            // Arrange
            var response = new McpErrorResponse
            {
                Id = 456,
                Error = new McpError
                {
                    Code = -32600,
                    Message = "Invalid Request",
                    Data = "test data"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            var jsonDoc = JsonDocument.Parse(json);

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("jsonrpc", out var jsonrpcProp), "Should have 'jsonrpc' field (lowercase)");
            Assert.Equal("2.0", jsonrpcProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("id", out var idProp), "Should have 'id' field (lowercase)");
            Assert.Equal(456, idProp.GetInt32());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("error", out var errorProp), "Should have 'error' field (lowercase)");
            
            // Check error object fields
            Assert.True(errorProp.TryGetProperty("code", out var codeProp), "Error should have 'code' field (lowercase)");
            Assert.Equal(-32600, codeProp.GetInt32());
            
            Assert.True(errorProp.TryGetProperty("message", out var messageProp), "Error should have 'message' field (lowercase)");
            Assert.Equal("Invalid Request", messageProp.GetString());
            
            Assert.True(errorProp.TryGetProperty("data", out var dataProp), "Error should have 'data' field (lowercase)");
            Assert.Equal("test data", dataProp.GetString());
        }

        [Fact]
        public void McpToolResult_ShouldSerializeWithLowercaseFieldNames()
        {
            // Arrange
            var toolResult = new McpToolResult
            {
                Content = new[]
                {
                    new McpContent
                    {
                        Type = "text",
                        Text = "Hello World"
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(toolResult, _jsonOptions);
            var jsonDoc = JsonDocument.Parse(json);

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("content", out var contentProp), "Should have 'content' field (lowercase)");
            Assert.Equal(JsonValueKind.Array, contentProp.ValueKind);
            
            var firstContent = contentProp.EnumerateArray().First();
            Assert.True(firstContent.TryGetProperty("type", out var typeProp), "Content should have 'type' field (lowercase)");
            Assert.Equal("text", typeProp.GetString());
            
            Assert.True(firstContent.TryGetProperty("text", out var textProp), "Content should have 'text' field (lowercase)");
            Assert.Equal("Hello World", textProp.GetString());
        }

        [Fact]
        public void McpToolSchema_ShouldSerializeWithLowercaseFieldNames()
        {
            // Arrange
            var toolSchema = new McpToolSchema
            {
                Name = "test_tool",
                Description = "A test tool",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        param1 = new { type = "string" }
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(toolSchema, _jsonOptions);
            var jsonDoc = JsonDocument.Parse(json);

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("name", out var nameProp), "Should have 'name' field (lowercase)");
            Assert.Equal("test_tool", nameProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("description", out var descProp), "Should have 'description' field (lowercase)");
            Assert.Equal("A test tool", descProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("inputSchema", out var schemaProp), "Should have 'inputSchema' field (camelCase)");
            Assert.Equal(JsonValueKind.Object, schemaProp.ValueKind);
        }

        [Fact]
        public void McpToolsListResult_ShouldSerializeWithLowercaseFieldNames()
        {
            // Arrange
            var toolsListResult = new McpToolsListResult
            {
                Tools = new[]
                {
                    new McpToolSchema
                    {
                        Name = "tool1",
                        Description = "First tool",
                        InputSchema = new { }
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(toolsListResult, _jsonOptions);
            var jsonDoc = JsonDocument.Parse(json);

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("tools", out var toolsProp), "Should have 'tools' field (lowercase)");
            Assert.Equal(JsonValueKind.Array, toolsProp.ValueKind);
            
            var firstTool = toolsProp.EnumerateArray().First();
            Assert.True(firstTool.TryGetProperty("name", out var nameProp), "Tool should have 'name' field (lowercase)");
            Assert.Equal("tool1", nameProp.GetString());
        }

        [Fact]
        public void McpInitializeResult_ShouldSerializeWithLowercaseFieldNames()
        {
            // Arrange
            var initResult = new McpInitializeResult();

            // Act
            var json = JsonSerializer.Serialize(initResult, _jsonOptions);
            var jsonDoc = JsonDocument.Parse(json);

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("protocolVersion", out var versionProp), "Should have 'protocolVersion' field (camelCase)");
            Assert.Equal("2025-06-18", versionProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("capabilities", out var capsProp), "Should have 'capabilities' field (lowercase)");
            Assert.Equal(JsonValueKind.Object, capsProp.ValueKind);
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("serverInfo", out var serverProp), "Should have 'serverInfo' field (camelCase)");
            Assert.Equal(JsonValueKind.Object, serverProp.ValueKind);
        }

        [Fact]
        public void McpNotification_ShouldSerializeWithLowercaseFieldNames()
        {
            // Arrange
            var notification = new McpNotification
            {
                Method = "notifications/test",
                Params = new { message = "test notification" }
            };

            // Act
            var json = JsonSerializer.Serialize(notification, _jsonOptions);
            var jsonDoc = JsonDocument.Parse(json);

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("jsonrpc", out var jsonrpcProp), "Should have 'jsonrpc' field (lowercase)");
            Assert.Equal("2.0", jsonrpcProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("method", out var methodProp), "Should have 'method' field (lowercase)");
            Assert.Equal("notifications/test", methodProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("params", out var paramsProp), "Should have 'params' field (lowercase)");
            Assert.Equal(JsonValueKind.Object, paramsProp.ValueKind);
            
            // Notifications should NOT have 'id' field
            Assert.False(jsonDoc.RootElement.TryGetProperty("id", out _), "Notifications should NOT have 'id' field");
        }

        [Fact]
        public void McpCommandStatusNotification_ShouldSerializeWithLowercaseFieldNames()
        {
            // Arrange
            var statusNotification = new McpCommandStatusNotification
            {
                SessionId = "sess-123",
                CommandId = "cmd-456",
                Command = "!analyze -v",
                Status = "completed",
                Progress = 100,
                Message = "Analysis complete",
                Result = "Crash analysis results"
            };

            // Act
            var json = JsonSerializer.Serialize(statusNotification, _jsonOptions);
            var jsonDoc = JsonDocument.Parse(json);

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("sessionId", out var sessionProp), "Should have 'sessionId' field (camelCase)");
            Assert.Equal("sess-123", sessionProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("commandId", out var cmdProp), "Should have 'commandId' field (camelCase)");
            Assert.Equal("cmd-456", cmdProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("command", out var commandProp), "Should have 'command' field (lowercase)");
            Assert.Equal("!analyze -v", commandProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("status", out var statusProp), "Should have 'status' field (lowercase)");
            Assert.Equal("completed", statusProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("progress", out var progressProp), "Should have 'progress' field (lowercase)");
            Assert.Equal(100, progressProp.GetInt32());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("message", out var messageProp), "Should have 'message' field (lowercase)");
            Assert.Equal("Analysis complete", messageProp.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var resultProp), "Should have 'result' field (lowercase)");
            Assert.Equal("Crash analysis results", resultProp.GetString());
        }

        [Fact]
        public void AllJsonRpcResponses_ShouldNeverHaveUppercaseFieldNames()
        {
            // This test ensures we never accidentally introduce uppercase field names
            // that would break JSON-RPC 2.0 compliance
            
            var testCases = new object[]
            {
                new McpSuccessResponse { Id = 1, Result = "test" },
                new McpErrorResponse { Id = 2, Error = new McpError { Code = -1, Message = "error" } },
                new McpNotification { Method = "test", Params = "params" }
            };

            foreach (var testCase in testCases)
            {
                var json = JsonSerializer.Serialize(testCase, _jsonOptions);
                var jsonDoc = JsonDocument.Parse(json);

                // Check for common uppercase violations
                Assert.False(jsonDoc.RootElement.TryGetProperty("Id", out _), 
                    $"{testCase.GetType().Name} should not have uppercase 'Id' field");
                Assert.False(jsonDoc.RootElement.TryGetProperty("Result", out _), 
                    $"{testCase.GetType().Name} should not have uppercase 'Result' field");
                Assert.False(jsonDoc.RootElement.TryGetProperty("Error", out _), 
                    $"{testCase.GetType().Name} should not have uppercase 'Error' field");
                Assert.False(jsonDoc.RootElement.TryGetProperty("Method", out _), 
                    $"{testCase.GetType().Name} should not have uppercase 'Method' field");
                Assert.False(jsonDoc.RootElement.TryGetProperty("Params", out _), 
                    $"{testCase.GetType().Name} should not have uppercase 'Params' field");
            }
        }
    }
}
