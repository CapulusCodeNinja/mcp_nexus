using System.Text.Json;
using mcp_nexus.Models;
using Xunit;

namespace mcp_nexus_tests.Models
{
    public class ModelTests
    {
        // Use the same JSON options as the application to ensure consistent serialization
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // Don't change property names - MCP protocol requires exact field names
        };
        [Fact]
        public void McpRequest_Serialization_WorksCorrectly()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Id = 123,
                Method = "tools/call",
                Params = JsonDocument.Parse("{\"name\":\"test\",\"arguments\":{}}").RootElement
            };

            // Act
            var json = JsonSerializer.Serialize(request, s_jsonOptions);
            var deserialized = JsonSerializer.Deserialize<McpRequest>(json, s_jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("2.0", deserialized.JsonRpc);
            Assert.Equal(123, ((JsonElement)deserialized.Id!).GetInt32());
            Assert.Equal("tools/call", deserialized.Method);
            Assert.True(deserialized.Params.HasValue);
        }

        [Fact]
        public void McpResponse_Serialization_WorksCorrectly()
        {
            // Arrange
            var response = new McpResponse
            {
                JsonRpc = "2.0",
                Id = 456,
                Result = new { success = true, message = "OK" }
            };

            // Act
            var json = JsonSerializer.Serialize(response, s_jsonOptions);
            var deserialized = JsonSerializer.Deserialize<McpResponse>(json, s_jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("2.0", deserialized.JsonRpc);
            Assert.Equal(456, ((JsonElement)deserialized.Id!).GetInt32());
            Assert.NotNull(deserialized.Result);
        }

        [Fact]
        public void McpError_Serialization_WorksCorrectly()
        {
            // Arrange
            var error = new McpError
            {
                Code = -32601,
                Message = "Method not found",
                Data = new { method = "unknown/method" }
            };

            // Act
            var json = JsonSerializer.Serialize(error);
            var deserialized = JsonSerializer.Deserialize<McpError>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(-32601, deserialized.Code);
            Assert.Equal("Method not found", deserialized.Message);
            Assert.NotNull(deserialized.Data);
        }

        [Fact]
        public void McpToolSchema_Creation_WorksCorrectly()
        {
            // Arrange & Act
            var schema = new McpToolSchema
            {
                Name = "test_tool",
                Description = "A test tool for validation",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        param1 = new { type = "string", description = "First parameter" },
                        param2 = new { type = "number", description = "Second parameter" }
                    },
                    required = new[] { "param1" }
                }
            };

            // Assert
            Assert.Equal("test_tool", schema.Name);
            Assert.Equal("A test tool for validation", schema.Description);
            Assert.NotNull(schema.InputSchema);
        }

        [Fact]
        public void McpInitializeResult_Creation_WorksCorrectly()
        {
            // Arrange & Act
            var result = new McpInitializeResult();

            // Assert
            Assert.NotNull(result.ProtocolVersion); // Dynamic version from assembly
            Assert.NotNull(result.Capabilities);
            Assert.NotNull(result.ServerInfo);
            // ServerInfo and Capabilities are anonymous objects, just verify they exist
        }

        [Fact]
        public void McpToolsListResult_Creation_WorksCorrectly()
        {
            // Arrange
            var tools = new[]
            {
                new McpToolSchema { Name = "tool1", Description = "First tool", InputSchema = new { } },
                new McpToolSchema { Name = "tool2", Description = "Second tool", InputSchema = new { } }
            };

            // Act
            var result = new McpToolsListResult
            {
                Tools = tools
            };

            // Assert
            Assert.NotNull(result.Tools);
            Assert.Equal(2, result.Tools.Length);
            Assert.Equal("tool1", result.Tools[0].Name);
            Assert.Equal("tool2", result.Tools[1].Name);
        }

        [Fact]
        public void McpRequest_WithoutId_IsNotification()
        {
            // Arrange
            var notification = new McpRequest
            {
                JsonRpc = "2.0",
                Method = "notifications/initialized"
                // No Id = notification
            };

            // Act
            var json = JsonSerializer.Serialize(notification, s_jsonOptions);

            // Assert
            Assert.Contains("\"id\": null", json); // Current implementation serializes null ID
            Assert.Contains("\"method\": \"notifications/initialized\"", json);
        }

        [Fact]
        public void McpResponse_WithError_SerializesCorrectly()
        {
            // Arrange
            var response = new McpResponse
            {
                JsonRpc = "2.0",
                Id = 789,
                Error = new McpError
                {
                    Code = -32600,
                    Message = "Invalid request"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response, s_jsonOptions);
            var deserialized = JsonSerializer.Deserialize<McpResponse>(json, s_jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("2.0", deserialized.JsonRpc);
            Assert.Equal(789, ((JsonElement)deserialized.Id!).GetInt32());
            Assert.NotNull(deserialized.Error);
            Assert.Equal(-32600, deserialized.Error.Code);
            Assert.Equal("Invalid request", deserialized.Error.Message);
        }

        [Fact]
        public void McpRequest_WithComplexParams_HandlesCorrectly()
        {
            // Arrange
            var complexParams = JsonDocument.Parse("""
				{
					"name": "open_windbg_dump",
					"arguments": {
						"dumpPath": "C:\\temp\\crash.dmp",
						"symbolsPath": "C:\\symbols",
						"options": {
							"verbose": true,
							"timeout": 30000
						}
					}
				}
				""").RootElement;

            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Id = 999,
                Method = "tools/call",
                Params = complexParams
            };

            // Act
            var json = JsonSerializer.Serialize(request, s_jsonOptions);
            var deserialized = JsonSerializer.Deserialize<McpRequest>(json, s_jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.True(deserialized.Params.HasValue);
            Assert.True(deserialized.Params.Value.TryGetProperty("name", out var nameElement));
            Assert.Equal("open_windbg_dump", nameElement.GetString());
        }
    }
}

