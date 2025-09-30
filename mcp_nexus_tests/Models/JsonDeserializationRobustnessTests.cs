using System.Text.Json;
using mcp_nexus.Models;
using Xunit;

namespace mcp_nexus_tests.Models
{
    /// <summary>
    /// Tests for JSON deserialization robustness with various invalid and edge case inputs.
    /// These tests ensure that the MCP models handle malformed JSON gracefully.
    /// </summary>
    public class JsonDeserializationRobustnessTests
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // Don't change property names - MCP protocol requires exact field names
        };

        #region McpRequest Deserialization Tests

        [Fact]
        public void McpRequest_DeserializeInvalidJson_ThrowsJsonException()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act & Assert
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<McpRequest>(invalidJson, s_jsonOptions));
        }

        [Fact]
        public void McpRequest_DeserializeIncompleteJson_ThrowsJsonException()
        {
            // Arrange
            var incompleteJson = "{\"jsonrpc\":\"2.0\",\"method\":\"test\""; // Missing closing brace

            // Act & Assert
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<McpRequest>(incompleteJson, s_jsonOptions));
        }

        [Fact]
        public void McpRequest_DeserializeEmptyObject_ReturnsObjectWithDefaults()
        {
            // Arrange
            var emptyJson = "{}";

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(emptyJson, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2.0", result.JsonRpc); // Default value
            Assert.Equal(string.Empty, result.Method); // Default value
            Assert.Null(result.Params);
            Assert.Null(result.Id);
        }

        [Fact]
        public void McpRequest_DeserializeWithNullValues_HandlesGracefully()
        {
            // Arrange
            var jsonWithNulls = """
            {
                "jsonrpc": null,
                "method": null,
                "params": null,
                "id": null
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(jsonWithNulls, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.JsonRpc);
            Assert.Null(result.Method);
            Assert.Null(result.Params);
            Assert.Null(result.Id);
        }

        [Fact]
        public void McpRequest_DeserializeWithWrongTypes_ThrowsJsonException()
        {
            // Arrange
            var wrongTypesJson = """
            {
                "jsonrpc": 123,
                "method": true,
                "id": "should-be-number-or-string"
            }
            """;

            // Act & Assert
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<McpRequest>(wrongTypesJson, s_jsonOptions));
        }

        [Fact]
        public void McpRequest_DeserializeWithExtraFields_IgnoresExtraFields()
        {
            // Arrange
            var jsonWithExtras = """
            {
                "jsonrpc": "2.0",
                "method": "test",
                "id": 1,
                "extraField": "should be ignored",
                "anotherExtra": 123
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(jsonWithExtras, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2.0", result.JsonRpc);
            Assert.Equal("test", result.Method);
            Assert.Equal(1, ((JsonElement)result.Id!).GetInt32());
        }

        [Theory]
        [InlineData("\"string-id\"")]
        [InlineData("123")]
        [InlineData("123.456")]
        [InlineData("true")]
        [InlineData("false")]
        public void McpRequest_DeserializeWithVariousIdTypes_HandlesCorrectly(string idValue)
        {
            // Arrange
            var json = $$"""
            {
                "jsonrpc": "2.0",
                "method": "test",
                "id": {{idValue}}
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(json, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Id);
        }

        #endregion

        #region McpResponse Deserialization Tests

        [Fact]
        public void McpResponse_DeserializeInvalidJson_ThrowsJsonException()
        {
            // Arrange
            var invalidJson = "{ not valid json at all }";

            // Act & Assert
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<McpResponse>(invalidJson, s_jsonOptions));
        }

        [Fact]
        public void McpResponse_DeserializeWithBothResultAndError_HandlesGracefully()
        {
            // Arrange - This violates JSON-RPC spec but should not crash
            var invalidResponse = """
            {
                "jsonrpc": "2.0",
                "id": 1,
                "result": {"success": true},
                "error": {"code": -1, "message": "error"}
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpResponse>(invalidResponse, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void McpResponse_DeserializeWithNestedComplexResult_HandlesCorrectly()
        {
            // Arrange
            var complexResponse = """
            {
                "jsonrpc": "2.0",
                "id": 1,
                "result": {
                    "tools": [
                        {
                            "name": "test_tool",
                            "description": "A test tool",
                            "inputSchema": {
                                "type": "object",
                                "properties": {
                                    "param1": {"type": "string"},
                                    "param2": {"type": "number"}
                                }
                            }
                        }
                    ]
                }
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpResponse>(complexResponse, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Result);
            Assert.Equal("2.0", result.JsonRpc);
        }

        #endregion

        #region McpError Deserialization Tests

        [Fact]
        public void McpError_DeserializeWithMissingMessage_HandlesGracefully()
        {
            // Arrange
            var errorWithoutMessage = """
            {
                "code": -32600
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpError>(errorWithoutMessage, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-32600, result.Code);
            Assert.Equal(string.Empty, result.Message); // Default value is empty string, not null
        }

        [Fact]
        public void McpError_DeserializeWithComplexData_HandlesCorrectly()
        {
            // Arrange
            var errorWithComplexData = """
            {
                "code": -32603,
                "message": "Internal error",
                "data": {
                    "stackTrace": "Error at line 123",
                    "additionalInfo": {
                        "nested": "value"
                    }
                }
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpError>(errorWithComplexData, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-32603, result.Code);
            Assert.Equal("Internal error", result.Message);
            Assert.NotNull(result.Data);
        }

        #endregion

        #region Edge Cases and Stress Tests

        [Fact]
        public void JsonDeserialization_WithVeryLargeString_HandlesGracefully()
        {
            // Arrange
            var largeString = new string('A', 100000); // 100KB string
            var largeJson = $$"""
            {
                "jsonrpc": "2.0",
                "method": "{{largeString}}",
                "id": 1
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(largeJson, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(largeString, result.Method);
        }

        [Fact]
        public void JsonDeserialization_WithDeeplyNestedObject_HandlesCorrectly()
        {
            // Arrange - Create a deeply nested params object
            var deeplyNestedJson = """
            {
                "jsonrpc": "2.0",
                "method": "test",
                "params": {
                    "level1": {
                        "level2": {
                            "level3": {
                                "level4": {
                                    "level5": {
                                        "value": "deep"
                                    }
                                }
                            }
                        }
                    }
                },
                "id": 1
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(deeplyNestedJson, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Params.HasValue);
        }

        [Fact]
        public void JsonDeserialization_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var unicodeJson = """
            {
                "jsonrpc": "2.0",
                "method": "test_🚀_method",
                "params": {
                    "message": "Hello 世界! Привет мир! 🌟✨"
                },
                "id": "unicode-id-🔧"
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(unicodeJson, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_🚀_method", result.Method);
            Assert.True(result.Params.HasValue);
        }

        [Theory]
        [InlineData("\"\"")]           // Empty string
        [InlineData("\" \"")]          // Single space
        [InlineData("\"\\n\"")]        // Newline
        [InlineData("\"\\t\"")]        // Tab
        [InlineData("\"\\r\\n\"")]     // CRLF
        [InlineData("\"null\"")]       // String "null"
        [InlineData("\"undefined\"")]  // String "undefined"
        public void McpRequest_DeserializeWithSpecialStringValues_HandlesCorrectly(string methodValue)
        {
            // Arrange
            var json = $$"""
            {
                "jsonrpc": "2.0",
                "method": {{methodValue}},
                "id": 1
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(json, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Method);
        }

        [Fact]
        public void JsonDeserialization_WithMixedArrayAndObjectParams_HandlesCorrectly()
        {
            // Arrange
            var mixedParamsJson = """
            {
                "jsonrpc": "2.0",
                "method": "test",
                "params": {
                    "arrayParam": [1, 2, 3, "string", true, null],
                    "objectParam": {
                        "nested": "value"
                    },
                    "primitiveParam": "simple"
                },
                "id": 1
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<McpRequest>(mixedParamsJson, s_jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Params.HasValue);
        }

        #endregion
    }
}
