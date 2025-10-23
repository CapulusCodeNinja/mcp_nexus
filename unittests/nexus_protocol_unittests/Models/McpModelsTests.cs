using System.Text.Json;
using nexus.protocol.Models;

namespace nexus.protocol.unittests.Models;

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

        json.Should().Contain("\"jsonrpc\":\"2.0\"");
        json.Should().Contain("\"result\"");
        json.Should().Contain("\"status\":\"success\"");
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

        json.Should().Contain("\"error\"");
        json.Should().Contain("\"code\":-32603");
        json.Should().Contain("\"message\":\"Internal error\"");
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
                    param1 = new { type = "string" }
                }
            }
        };

        var json = JsonSerializer.Serialize(schema);
        var deserialized = JsonSerializer.Deserialize<McpToolSchema>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("test_tool");
        deserialized.Description.Should().Be("Test tool description");
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

        notification.SessionId.Should().Be("sess-001");
        notification.CommandId.Should().Be("cmd-123");
        notification.Status.Should().Be("Executing");
        notification.Progress.Should().Be(50);
    }
}
