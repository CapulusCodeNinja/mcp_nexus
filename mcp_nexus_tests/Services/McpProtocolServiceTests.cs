using System.Text.Json;
using Xunit;

namespace mcp_nexus_tests.Services
{
	public class McpProtocolServiceTests
	{
		// Test basic JSON parsing scenarios that the service handles
		[Fact]
		public void JsonElement_ParseValidMcpRequest_WorksCorrectly()
		{
			// Arrange
			var jsonString = """{"jsonrpc":"2.0","id":1,"method":"initialize"}""";

			// Act
			var element = JsonDocument.Parse(jsonString).RootElement;

			// Assert
			Assert.True(element.TryGetProperty("jsonrpc", out var jsonrpcProp));
			Assert.Equal("2.0", jsonrpcProp.GetString());
			Assert.True(element.TryGetProperty("id", out var idProp));
			Assert.Equal(1, idProp.GetInt32());
			Assert.True(element.TryGetProperty("method", out var methodProp));
			Assert.Equal("initialize", methodProp.GetString());
		}

		[Fact]
		public void JsonElement_ParseInvalidJson_ThrowsException()
		{
			// Arrange
			var invalidJson = """{"jsonrpc":"2.0","id":1,invalid}""";

			// Act & Assert
			// JsonDocument.Parse throws JsonReaderException for invalid JSON
			bool exceptionThrown = false;
			try
			{
				JsonDocument.Parse(invalidJson);
			}
			catch (Exception ex) when (ex.GetType().Name == "JsonReaderException")
			{
				exceptionThrown = true;
				Assert.Contains("invalid start of a property name", ex.Message);
			}
			Assert.True(exceptionThrown, "Expected JsonReaderException to be thrown");
		}

		[Fact]
		public void JsonElement_MissingProperties_HandledCorrectly()
		{
			// Arrange
			var jsonString = """{"jsonrpc":"2.0"}"""; // Missing id and method

			// Act
			var element = JsonDocument.Parse(jsonString).RootElement;

			// Assert
			Assert.True(element.TryGetProperty("jsonrpc", out _));
			Assert.False(element.TryGetProperty("id", out _));
			Assert.False(element.TryGetProperty("method", out _));
		}

		[Fact]
		public void JsonElement_ComplexParams_ParseCorrectly()
		{
			// Arrange
			var jsonString = """
				{
					"jsonrpc": "2.0",
					"id": 1,
					"method": "tools/call",
					"params": {
						"name": "test_tool",
						"arguments": {
							"param1": "value1",
							"param2": 42
						}
					}
				}
				""";

			// Act
			var element = JsonDocument.Parse(jsonString).RootElement;

			// Assert
			Assert.True(element.TryGetProperty("params", out var paramsElement));
			Assert.True(paramsElement.TryGetProperty("name", out var nameElement));
			Assert.Equal("test_tool", nameElement.GetString());
			Assert.True(paramsElement.TryGetProperty("arguments", out var argsElement));
			Assert.True(argsElement.TryGetProperty("param1", out var param1Element));
			Assert.Equal("value1", param1Element.GetString());
		}

		[Fact]
		public void JsonElement_ArrayProperties_ParseCorrectly()
		{
			// Arrange
			var jsonString = """
				{
					"tools": [
						{"name": "tool1", "type": "analysis"},
						{"name": "tool2", "type": "debugging"}
					]
				}
				""";

			// Act
			var element = JsonDocument.Parse(jsonString).RootElement;

			// Assert
			Assert.True(element.TryGetProperty("tools", out var toolsElement));
			Assert.Equal(JsonValueKind.Array, toolsElement.ValueKind);
			Assert.Equal(2, toolsElement.GetArrayLength());
		}
	}
}
