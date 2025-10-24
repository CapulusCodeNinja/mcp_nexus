using System.Text.Json;

using Nexus.Protocol.Models;

namespace Nexus.Protocol.Unittests.Models;

/// <summary>
/// Unit tests for MCP model classes.
/// Tests JSON serialization, deserialization, and validation.
/// </summary>
public class McpModelsTests
{
    /// <summary>
    /// Verifies that McpResponse with result serializes correctly.
    /// </summary>
    [Fact]
    public void McpResponse_WithResult_SerializesCorrectly()
    {
        var response = new McpResponse
        {
            JsonRpc = "2.0",
            Id = 1,
            Result = new { status = "success" }
        };

        var json = JsonSerializer.Serialize(response);

        _ = json.Should().Contain("\"jsonrpc\":\"2.0\"");
        _ = json.Should().Contain("\"result\"");
        _ = json.Should().Contain("\"status\":\"success\"");
    }

    /// <summary>
    /// Verifies that McpResponse with error serializes correctly.
    /// </summary>
    [Fact]
    public void McpResponse_WithError_SerializesCorrectly()
    {
        var response = new McpResponse
        {
            JsonRpc = "2.0",
            Id = 1,
            Error = new McpError
            {
                Code = -32603,
                Message = "Internal error"
            }
        };

        var json = JsonSerializer.Serialize(response);

        _ = json.Should().Contain("\"error\"");
        _ = json.Should().Contain("\"code\":-32603");
        _ = json.Should().Contain("\"message\":\"Internal error\"");
    }

    /// <summary>
    /// Verifies that McpToolSchema round-trip preserves data.
    /// </summary>
    [Fact]
    public void McpToolSchema_RoundTrip_PreservesData()
    {
        var schema = new McpToolSchema
        {
            Name = "test_tool",
            Description = "Test tool description",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    param1 = new
                    {
                        type = "string"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(schema);
        var deserialized = JsonSerializer.Deserialize<McpToolSchema>(json);

        _ = deserialized.Should().NotBeNull();
        _ = deserialized!.Name.Should().Be("test_tool");
        _ = deserialized.Description.Should().Be("Test tool description");
    }

    /// <summary>
    /// Verifies that McpCommandStatusNotification has correct properties.
    /// </summary>
    [Fact]
    public void McpCommandStatusNotification_HasCorrectProperties()
    {
        var notification = new McpCommandStatusNotification
        {
            SessionId = "sess-001",
            CommandId = "cmd-123",
            Command = "k",
            Status = "Executing",
            Progress = 50,
            Message = "Processing...",
            Timestamp = DateTimeOffset.Now
        };

        _ = notification.SessionId.Should().Be("sess-001");
        _ = notification.CommandId.Should().Be("cmd-123");
        _ = notification.Status.Should().Be("Executing");
        _ = notification.Progress.Should().Be(50);
    }

    /// <summary>
    /// Verifies that McpError with data serializes correctly.
    /// </summary>
    [Fact]
    public void McpError_WithData_SerializesCorrectly()
    {
        var error = new McpError
        {
            Code = -32600,
            Message = "Invalid Request",
            Data = new { details = "Missing required parameter" }
        };

        var json = JsonSerializer.Serialize(error);

        _ = json.Should().Contain("\"code\":-32600");
        _ = json.Should().Contain("\"message\":\"Invalid Request\"");
        _ = json.Should().Contain("\"data\"");
        _ = json.Should().Contain("\"details\"");
    }

    /// <summary>
    /// Verifies that McpError without data serializes correctly.
    /// </summary>
    [Fact]
    public void McpError_WithoutData_SerializesCorrectly()
    {
        var error = new McpError
        {
            Code = -32601,
            Message = "Method not found"
        };

        var json = JsonSerializer.Serialize(error);

        _ = json.Should().Contain("\"code\":-32601");
        _ = json.Should().Contain("\"message\":\"Method not found\"");
    }


    /// <summary>
    /// Verifies that McpToolSchema with complex input schema serializes correctly.
    /// </summary>
    [Fact]
    public void McpToolSchema_WithComplexInputSchema_SerializesCorrectly()
    {
        var schema = new McpToolSchema
        {
            Name = "complex_tool",
            Description = "A tool with complex schema",
            InputSchema = new
            {
                type = "object",
                required = new[] { "param1", "param2" },
                properties = new
                {
                    param1 = new { type = "string", description = "First parameter" },
                    param2 = new { type = "integer", minimum = 0, maximum = 100 }
                }
            }
        };

        var json = JsonSerializer.Serialize(schema);

        _ = json.Should().Contain("\"name\":\"complex_tool\"");
        _ = json.Should().Contain("\"required\"");
        _ = json.Should().Contain("\"properties\"");
    }
}
