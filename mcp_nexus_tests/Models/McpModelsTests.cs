using System.Text.Json;
using mcp_nexus.Models;
using Xunit;

namespace mcp_nexus_tests.Models
{
    public class McpModelsTests
    {
        // Use the same JSON options as the application to ensure consistent serialization
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // Don't change property names - MCP protocol requires exact field names
        };
        [Fact]
        public void McpRequest_Serialization_IncludesAllProperties()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = "test_method",
                Id = 123,
                Params = JsonDocument.Parse("""{"test": "value"}""").RootElement
            };

            // Act
            var json = JsonSerializer.Serialize(request, s_jsonOptions);

            // Assert
            Assert.Contains("\"jsonrpc\": \"2.0\"", json);
            Assert.Contains("\"method\": \"test_method\"", json);
            Assert.Contains("\"id\": 123", json);
            Assert.Contains("\"params\":", json);
        }

        [Fact]
        public void McpRequest_DefaultValues_AreCorrect()
        {
            // Act
            var request = new McpRequest();

            // Assert
            Assert.Equal("2.0", request.JsonRpc);
            Assert.Equal(string.Empty, request.Method);
            Assert.Null(request.Id);
            Assert.Null(request.Params);
        }

        [Fact]
        public void McpSuccessResponse_Serialization_IncludesAllProperties()
        {
            // Arrange
            var response = new McpSuccessResponse
            {
                JsonRpc = "2.0",
                Id = 456,
                Result = new { success = true, data = "test" }
            };

            // Act
            var json = JsonSerializer.Serialize(response, s_jsonOptions);

            // Assert
            Assert.Contains("\"jsonrpc\": \"2.0\"", json);
            Assert.Contains("\"id\": 456", json);
            Assert.Contains("\"result\":", json);
            Assert.Contains("\"success\": true", json);
        }

        [Fact]
        public void McpErrorResponse_Serialization_IncludesErrorDetails()
        {
            // Arrange
            var response = new McpErrorResponse
            {
                JsonRpc = "2.0",
                Id = 789,
                Error = new McpError
                {
                    Code = -32602,
                    Message = "Invalid params",
                    Data = new { details = "Parameter validation failed" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response, s_jsonOptions);

            // Assert
            Assert.Contains("\"jsonrpc\": \"2.0\"", json);
            Assert.Contains("\"id\": 789", json);
            Assert.Contains("\"error\":", json);
            Assert.Contains("\"code\": -32602", json);
            Assert.Contains("\"message\": \"Invalid params\"", json);
            Assert.Contains("\"data\":", json);
        }

        [Fact]
        public void McpError_DefaultValues_AreCorrect()
        {
            // Act
            var error = new McpError();

            // Assert
            Assert.Equal(0, error.Code);
            Assert.Equal(string.Empty, error.Message);
            Assert.Null(error.Data);
        }

        [Fact]
        public void McpToolSchema_DefaultValues_AreCorrect()
        {
            // Act
            var schema = new McpToolSchema();

            // Assert
            Assert.Equal(string.Empty, schema.Name);
            Assert.Equal(string.Empty, schema.Description);
            Assert.NotNull(schema.InputSchema);
        }

        [Fact]
        public void McpToolSchema_Serialization_IncludesAllProperties()
        {
            // Arrange
            var schema = new McpToolSchema
            {
                Name = "test_tool",
                Description = "A test tool",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        param1 = new { type = "string", description = "Test parameter" }
                    },
                    required = new[] { "param1" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(schema, s_jsonOptions);

            // Assert
            Assert.Contains("\"name\": \"test_tool\"", json);
            Assert.Contains("\"description\": \"A test tool\"", json);
            Assert.Contains("\"inputSchema\":", json);
            Assert.Contains("\"type\": \"object\"", json);
            Assert.Contains("\"properties\":", json);
        }

        [Fact]
        public void McpToolResult_DefaultValues_AreCorrect()
        {
            // Act
            var result = new McpToolResult();

            // Assert
            Assert.NotNull(result.Content);
            Assert.Empty(result.Content);
        }

        [Fact]
        public void McpContent_DefaultValues_AreCorrect()
        {
            // Act
            var content = new McpContent();

            // Assert
            Assert.Equal("text", content.Type);
            Assert.Equal(string.Empty, content.Text);
        }

        [Fact]
        public void McpContent_Serialization_IncludesAllProperties()
        {
            // Arrange
            var content = new McpContent
            {
                Type = "text",
                Text = "Hello, world!"
            };

            // Act
            var json = JsonSerializer.Serialize(content, s_jsonOptions);

            // Assert
            Assert.Contains("\"type\": \"text\"", json);
            Assert.Contains("\"text\": \"Hello, world!\"", json);
        }

        [Fact]
        public void McpInitializeResult_CanBeCreated()
        {
            // Act
            var result = new McpInitializeResult();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void McpInitializeResult_Serialization_ReturnsValidJson()
        {
            // Arrange
            var result = new McpInitializeResult();

            // Act
            var json = JsonSerializer.Serialize(result, s_jsonOptions);

            // Assert
            Assert.NotNull(json);
            Assert.NotEmpty(json);
            // Should be a valid JSON object
            Assert.StartsWith("{", json);
            Assert.EndsWith("}", json);
        }

        [Fact]
        public void McpToolsListResult_CreationAndSerialization()
        {
            // Arrange
            var tools = new McpToolSchema[]
            {
                new() { Name = "tool1", Description = "First tool" },
                new() { Name = "tool2", Description = "Second tool" }
            };
            var result = new McpToolsListResult { Tools = tools };

            // Act
            var json = JsonSerializer.Serialize(result, s_jsonOptions);

            // Assert
            Assert.Contains("\"tools\":", json);
            Assert.Contains("\"name\": \"tool1\"", json);
            Assert.Contains("\"name\": \"tool2\"", json);
        }

        [Fact]
        public void McpRequest_MethodProperty_HasCorrectJsonPropertyName()
        {
            // Arrange
            var request = new McpRequest { Method = "test_method" };

            // Act
            var json = JsonSerializer.Serialize(request, s_jsonOptions);

            // Assert
            // Should serialize as "method" (lowercase) due to JsonPropertyName attribute
            Assert.Contains("\"method\": \"test_method\"", json);
        }

        [Fact]
        public void McpResponse_JsonRpcProperty_HasCorrectJsonPropertyName()
        {
            // Arrange
            var response = new McpSuccessResponse { JsonRpc = "2.0" };

            // Act
            var json = JsonSerializer.Serialize(response, s_jsonOptions);

            // Assert
            // Should serialize as "jsonrpc" (lowercase) due to JsonPropertyName attribute
            Assert.Contains("\"jsonrpc\": \"2.0\"", json);
        }

        [Fact]
        public void McpToolResult_WithMultipleContent_SerializesCorrectly()
        {
            // Arrange
            var result = new McpToolResult
            {
                Content = new[]
                {
                    new McpContent { Type = "text", Text = "First content" },
                    new McpContent { Type = "text", Text = "Second content" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(result, s_jsonOptions);

            // Assert
            Assert.Contains("\"content\":", json);
            Assert.Contains("\"First content\"", json);
            Assert.Contains("\"Second content\"", json);
        }

        [Fact]
        public void McpServerInfoResponse_DefaultValues_AreCorrect()
        {
            // Act
            var response = new McpServerInfoResponse();

            // Assert
            Assert.Equal("2.0", response.JsonRpc);
            Assert.NotNull(response.Result);
        }

        [Fact]
        public void McpServerInfoResponse_Serialization_IncludesAllProperties()
        {
            // Arrange
            var response = new McpServerInfoResponse
            {
                JsonRpc = "2.0",
                Result = new McpServerInfoResult
                {
                    ProtocolVersion = "2025-06-18",
                    Capabilities = new McpCapabilities(),
                    ServerInfo = new McpServerDetails { Name = "test-server", Version = "2.0.0" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response, s_jsonOptions);

            // Assert
            Assert.Contains("\"jsonrpc\": \"2.0\"", json);
            Assert.Contains("\"result\":", json);
            Assert.Contains("\"protocolVersion\": \"2025-06-18\"", json);
            Assert.Contains("\"capabilities\":", json);
            Assert.Contains("\"serverInfo\":", json);
            Assert.Contains("\"test-server\"", json);
        }

        [Fact]
        public void McpServerInfoResult_DefaultValues_AreCorrect()
        {
            // Act
            var result = new McpServerInfoResult();

            // Assert
            Assert.NotNull(result.ProtocolVersion); // Dynamic version from assembly
            Assert.NotNull(result.Capabilities);
            Assert.NotNull(result.ServerInfo);
        }

        [Fact]
        public void McpCapabilities_DefaultValues_AreCorrect()
        {
            // Act
            var capabilities = new McpCapabilities();

            // Assert
            Assert.NotNull(capabilities.Tools);
        }

        [Fact]
        public void McpCapabilities_Serialization_IncludesTools()
        {
            // Arrange
            var capabilities = new McpCapabilities
            {
                Tools = new { listChanged = true, customFeature = "enabled" }
            };

            // Act
            var json = JsonSerializer.Serialize(capabilities, s_jsonOptions);

            // Assert
            Assert.Contains("\"tools\":", json);
            Assert.Contains("\"listChanged\": true", json);
            Assert.Contains("\"customFeature\": \"enabled\"", json);
        }

        [Fact]
        public void McpServerDetails_DefaultValues_AreCorrect()
        {
            // Act
            var details = new McpServerDetails();

            // Assert
            Assert.Equal("mcp-nexus", details.Name);
            Assert.NotNull(details.Version); // Dynamic version from assembly
        }

        [Fact]
        public void McpServerDetails_Serialization_IncludesAllProperties()
        {
            // Arrange
            var details = new McpServerDetails
            {
                Name = "custom-server",
                Version = "3.2.1"
            };

            // Act
            var json = JsonSerializer.Serialize(details, s_jsonOptions);

            // Assert
            Assert.Contains("\"name\": \"custom-server\"", json);
            Assert.Contains("\"version\": \"3.2.1\"", json);
        }

        [Fact]
        public void McpInitializeResult_DefaultValues_AreCorrect()
        {
            // Act
            var result = new McpInitializeResult();

            // Assert
            Assert.NotNull(result.ProtocolVersion); // Dynamic version from assembly
            Assert.NotNull(result.Capabilities);
            Assert.NotNull(result.ServerInfo);
        }

        [Fact]
        public void McpInitializeResult_Serialization_IncludesAllProperties()
        {
            // Arrange
            var result = new McpInitializeResult
            {
                ProtocolVersion = "2025-06-18",
                Capabilities = new McpCapabilities
                {
                    Tools = new { listChanged = true },
                    Notifications = new { commandStatus = true, sessionRecovery = true, serverHealth = true }
                },
                ServerInfo = new McpServerDetails
                {
                    Name = "test-nexus",
                    Version = "2.1.0"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(result, s_jsonOptions);

            // Assert
            Assert.Contains("\"protocolVersion\": \"2025-06-18\"", json);
            Assert.Contains("\"capabilities\":", json);
            Assert.Contains("\"serverInfo\":", json);
            Assert.Contains("\"test-nexus\"", json);
            Assert.Contains("\"listChanged\": true", json);
        }

        [Fact]
        public void McpResponse_DefaultValues_AreCorrect()
        {
            // Act
            var response = new McpResponse();

            // Assert
            Assert.Equal("2.0", response.JsonRpc);
            Assert.Null(response.Id);
            Assert.Null(response.Result);
            Assert.Null(response.Error);
        }

        [Fact]
        public void McpResponse_Serialization_IncludesAllProperties()
        {
            // Arrange
            var response = new McpResponse
            {
                JsonRpc = "2.0",
                Id = 123,
                Result = new { status = "success" },
                Error = new McpError { Code = -32600, Message = "Invalid Request" }
            };

            // Act
            var json = JsonSerializer.Serialize(response, s_jsonOptions);

            // Assert
            Assert.Contains("\"jsonrpc\": \"2.0\"", json);
            Assert.Contains("\"id\": 123", json);
            Assert.Contains("\"result\":", json);
            Assert.Contains("\"error\":", json);
            Assert.Contains("\"status\": \"success\"", json);
        }

        [Fact]
        public void AllModelClasses_CanBeInstantiated()
        {
            // Act & Assert - All models should be instantiable without errors
            Assert.NotNull(new McpRequest());
            Assert.NotNull(new McpResponse());
            Assert.NotNull(new McpSuccessResponse());
            Assert.NotNull(new McpErrorResponse());
            Assert.NotNull(new McpError());
            Assert.NotNull(new McpToolSchema());
            Assert.NotNull(new McpToolResult());
            Assert.NotNull(new McpContent());
            Assert.NotNull(new McpInitializeResult());
            Assert.NotNull(new McpToolsListResult());
            Assert.NotNull(new McpServerInfoResponse());
            Assert.NotNull(new McpServerInfoResult());
            Assert.NotNull(new McpCapabilities());
            Assert.NotNull(new McpServerDetails());
        }
    }
}

