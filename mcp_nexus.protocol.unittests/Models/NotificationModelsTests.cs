using mcp_nexus.Protocol.Models;
using System.Text.Json;

namespace mcp_nexus.Protocol.Tests.Models;

/// <summary>
/// Unit tests for notification model classes.
/// Tests property access, default values, and JSON serialization.
/// </summary>
public class NotificationModelsTests
{
    [Fact]
    public void McpCommandHeartbeatNotification_DefaultValues()
    {
        var notification = new McpCommandHeartbeatNotification();

        notification.SessionId.Should().BeNull();
        notification.CommandId.Should().BeEmpty();
        notification.Command.Should().BeEmpty();
        notification.ElapsedSeconds.Should().Be(0);
        notification.ElapsedDisplay.Should().BeEmpty();
        notification.Details.Should().BeNull();
        notification.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void McpCommandHeartbeatNotification_PropertiesCanBeSet()
    {
        var notification = new McpCommandHeartbeatNotification
        {
            SessionId = "sess-001",
            CommandId = "cmd-123",
            Command = "!analyze -v",
            ElapsedSeconds = 120.5,
            ElapsedDisplay = "2m 1s",
            Details = "Still analyzing",
            Timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

        notification.SessionId.Should().Be("sess-001");
        notification.CommandId.Should().Be("cmd-123");
        notification.Command.Should().Be("!analyze -v");
        notification.ElapsedSeconds.Should().Be(120.5);
        notification.ElapsedDisplay.Should().Be("2m 1s");
        notification.Details.Should().Be("Still analyzing");
        notification.Timestamp.Year.Should().Be(2025);
    }

    [Fact]
    public void McpCommandHeartbeatNotification_SerializesToJson()
    {
        var notification = new McpCommandHeartbeatNotification
        {
            SessionId = "sess-001",
            CommandId = "cmd-123",
            Command = "kL",
            ElapsedSeconds = 10.5,
            ElapsedDisplay = "10s"
        };

        var json = JsonSerializer.Serialize(notification);

        json.Should().Contain("\"sessionId\":\"sess-001\"");
        json.Should().Contain("\"commandId\":\"cmd-123\"");
        json.Should().Contain("\"command\":\"kL\"");
        json.Should().Contain("\"elapsedSeconds\":10.5");
    }

    [Fact]
    public void McpCommandStatusNotification_DefaultValues()
    {
        var notification = new McpCommandStatusNotification();

        notification.SessionId.Should().BeNull();
        notification.CommandId.Should().BeEmpty();
        notification.Command.Should().BeEmpty();
        notification.Status.Should().BeEmpty();
        notification.Result.Should().BeNull();
        notification.Progress.Should().BeNull();
        notification.Message.Should().BeNull();
        notification.Error.Should().BeNull();
        notification.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void McpCommandStatusNotification_PropertiesCanBeSet()
    {
        var notification = new McpCommandStatusNotification
        {
            SessionId = "sess-001",
            CommandId = "cmd-123",
            Command = "lm",
            Status = "Completed",
            Result = "Success",
            Progress = 100,
            Message = "Done",
            Error = null,
            Timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

        notification.SessionId.Should().Be("sess-001");
        notification.CommandId.Should().Be("cmd-123");
        notification.Status.Should().Be("Completed");
        notification.Result.Should().Be("Success");
        notification.Progress.Should().Be(100);
        notification.Message.Should().Be("Done");
    }

    [Fact]
    public void McpServerHealthNotification_DefaultValues()
    {
        var notification = new McpServerHealthNotification();

        notification.Status.Should().BeEmpty();
        notification.CdbSessionActive.Should().BeFalse();
        notification.QueueSize.Should().Be(0);
        notification.ActiveCommands.Should().Be(0);
        notification.Uptime.Should().BeNull();
        notification.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void McpServerHealthNotification_PropertiesCanBeSet()
    {
        var notification = new McpServerHealthNotification
        {
            Status = "healthy",
            CdbSessionActive = true,
            QueueSize = 5,
            ActiveCommands = 10,
            Uptime = TimeSpan.FromHours(2),
            Timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

        notification.Status.Should().Be("healthy");
        notification.CdbSessionActive.Should().BeTrue();
        notification.QueueSize.Should().Be(5);
        notification.ActiveCommands.Should().Be(10);
        notification.Uptime.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void McpSessionRecoveryNotification_DefaultValues()
    {
        var notification = new McpSessionRecoveryNotification();

        notification.Reason.Should().BeEmpty();
        notification.RecoveryStep.Should().BeEmpty();
        notification.Success.Should().BeFalse();
        notification.Message.Should().BeEmpty();
        notification.AffectedCommands.Should().BeNull();
        notification.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void McpSessionRecoveryNotification_PropertiesCanBeSet()
    {
        var notification = new McpSessionRecoveryNotification
        {
            Reason = "Timeout",
            RecoveryStep = "Restart",
            Success = true,
            Message = "Session recovered",
            AffectedCommands = new[] { "cmd-1", "cmd-2" },
            Timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

        notification.Reason.Should().Be("Timeout");
        notification.RecoveryStep.Should().Be("Restart");
        notification.Success.Should().BeTrue();
        notification.Message.Should().Be("Session recovered");
        notification.AffectedCommands.Should().HaveCount(2);
    }

    [Fact]
    public void McpError_DefaultValues()
    {
        var error = new McpError();

        error.Code.Should().Be(0);
        error.Message.Should().BeEmpty();
        error.Data.Should().BeNull();
    }

    [Fact]
    public void McpError_PropertiesCanBeSet()
    {
        var error = new McpError
        {
            Code = -32700,
            Message = "Parse error",
            Data = "Additional details"
        };

        error.Code.Should().Be(-32700);
        error.Message.Should().Be("Parse error");
        error.Data.Should().Be("Additional details");
    }

    [Fact]
    public void McpResponse_DefaultValues()
    {
        var response = new McpResponse();

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().BeNull();
        response.Result.Should().BeNull();
        response.Error.Should().BeNull();
    }

    [Fact]
    public void McpResponse_PropertiesCanBeSet()
    {
        var error = new McpError { Code = -32600, Message = "Invalid Request" };
        var response = new McpResponse
        {
            JsonRpc = "2.0",
            Id = 123,
            Result = null,
            Error = error
        };

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().Be(123);
        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(-32600);
    }
}

