using System.Text.Json;

using FluentAssertions;

using WinAiDbg.Protocol.Models;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Models;

/// <summary>
/// Unit tests for notification model classes.
/// Tests property access, default values, and JSON serialization.
/// </summary>
public class NotificationModelsTests
{
    /// <summary>
    /// Verifies that McpCommandHeartbeatNotification has correct default values.
    /// </summary>
    [Fact]
    public void McpCommandHeartbeatNotification_DefaultValues()
    {
        var notification = new McpCommandHeartbeatNotification();

        _ = notification.SessionId.Should().BeNull();
        _ = notification.CommandId.Should().BeEmpty();
        _ = notification.Command.Should().BeEmpty();
        _ = notification.ElapsedSeconds.Should().Be(0);
        _ = notification.ElapsedDisplay.Should().BeEmpty();
        _ = notification.Details.Should().BeNull();
        _ = notification.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that McpCommandHeartbeatNotification properties can be set.
    /// </summary>
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
            Timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
        };

        _ = notification.SessionId.Should().Be("sess-001");
        _ = notification.CommandId.Should().Be("cmd-123");
        _ = notification.Command.Should().Be("!analyze -v");
        _ = notification.ElapsedSeconds.Should().Be(120.5);
        _ = notification.ElapsedDisplay.Should().Be("2m 1s");
        _ = notification.Details.Should().Be("Still analyzing");
        _ = notification.Timestamp.Year.Should().Be(2025);
    }

    /// <summary>
    /// Verifies that McpCommandHeartbeatNotification serializes to JSON correctly.
    /// </summary>
    [Fact]
    public void McpCommandHeartbeatNotification_SerializesToJson()
    {
        var notification = new McpCommandHeartbeatNotification
        {
            SessionId = "sess-001",
            CommandId = "cmd-123",
            Command = "kL",
            ElapsedSeconds = 10.5,
            ElapsedDisplay = "10s",
        };

        var json = JsonSerializer.Serialize(notification);

        _ = json.Should().Contain("\"sessionId\":\"sess-001\"");
        _ = json.Should().Contain("\"commandId\":\"cmd-123\"");
        _ = json.Should().Contain("\"command\":\"kL\"");
        _ = json.Should().Contain("\"elapsedSeconds\":10.5");
    }

    /// <summary>
    /// Verifies that McpCommandStatusNotification has correct default values.
    /// </summary>
    [Fact]
    public void McpCommandStatusNotification_DefaultValues()
    {
        var notification = new McpCommandStatusNotification();

        _ = notification.SessionId.Should().BeNull();
        _ = notification.CommandId.Should().BeEmpty();
        _ = notification.Command.Should().BeEmpty();
        _ = notification.Status.Should().BeEmpty();
        _ = notification.Result.Should().BeNull();
        _ = notification.Progress.Should().BeNull();
        _ = notification.Message.Should().BeNull();
        _ = notification.Error.Should().BeNull();
        _ = notification.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that McpCommandStatusNotification properties can be set.
    /// </summary>
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
            Timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
        };

        _ = notification.SessionId.Should().Be("sess-001");
        _ = notification.CommandId.Should().Be("cmd-123");
        _ = notification.Status.Should().Be("Completed");
        _ = notification.Result.Should().Be("Success");
        _ = notification.Progress.Should().Be(100);
        _ = notification.Message.Should().Be("Done");
    }

    /// <summary>
    /// Verifies that McpServerHealthNotification has correct default values.
    /// </summary>
    [Fact]
    public void McpServerHealthNotification_DefaultValues()
    {
        var notification = new McpServerHealthNotification();

        _ = notification.Status.Should().BeEmpty();
        _ = notification.CdbSessionActive.Should().BeFalse();
        _ = notification.QueueSize.Should().Be(0);
        _ = notification.ActiveCommands.Should().Be(0);
        _ = notification.Uptime.Should().BeNull();
        _ = notification.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that McpServerHealthNotification properties can be set.
    /// </summary>
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
            Timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
        };

        _ = notification.Status.Should().Be("healthy");
        _ = notification.CdbSessionActive.Should().BeTrue();
        _ = notification.QueueSize.Should().Be(5);
        _ = notification.ActiveCommands.Should().Be(10);
        _ = notification.Uptime.Should().Be(TimeSpan.FromHours(2));
    }

    /// <summary>
    /// Verifies that McpSessionRecoveryNotification has correct default values.
    /// </summary>
    [Fact]
    public void McpSessionRecoveryNotification_DefaultValues()
    {
        var notification = new McpSessionRecoveryNotification();

        _ = notification.Reason.Should().BeEmpty();
        _ = notification.RecoveryStep.Should().BeEmpty();
        _ = notification.Success.Should().BeFalse();
        _ = notification.Message.Should().BeEmpty();
        _ = notification.AffectedCommands.Should().BeNull();
        _ = notification.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that McpSessionRecoveryNotification properties can be set.
    /// </summary>
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
            Timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
        };

        _ = notification.Reason.Should().Be("Timeout");
        _ = notification.RecoveryStep.Should().Be("Restart");
        _ = notification.Success.Should().BeTrue();
        _ = notification.Message.Should().Be("Session recovered");
        _ = notification.AffectedCommands.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that McpError has correct default values.
    /// </summary>
    [Fact]
    public void McpError_DefaultValues()
    {
        var error = new McpError();

        _ = error.Code.Should().Be(0);
        _ = error.Message.Should().BeEmpty();
        _ = error.Data.Should().BeNull();
    }

    /// <summary>
    /// Verifies that McpError properties can be set.
    /// </summary>
    [Fact]
    public void McpError_PropertiesCanBeSet()
    {
        var error = new McpError
        {
            Code = -32700,
            Message = "Parse error",
            Data = "Additional details",
        };

        _ = error.Code.Should().Be(-32700);
        _ = error.Message.Should().Be("Parse error");
        _ = error.Data.Should().Be("Additional details");
    }

    /// <summary>
    /// Verifies that McpResponse has correct default values.
    /// </summary>
    [Fact]
    public void McpResponse_DefaultValues()
    {
        var response = new McpResponse();

        _ = response.JsonRpc.Should().Be("2.0");
        _ = response.Id.Should().BeNull();
        _ = response.Result.Should().BeNull();
        _ = response.Error.Should().BeNull();
    }

    /// <summary>
    /// Verifies that McpResponse properties can be set.
    /// </summary>
    [Fact]
    public void McpResponse_PropertiesCanBeSet()
    {
        var error = new McpError { Code = -32600, Message = "Invalid Request" };
        var response = new McpResponse
        {
            JsonRpc = "2.0",
            Id = 123,
            Result = null,
            Error = error,
        };

        _ = response.JsonRpc.Should().Be("2.0");
        _ = response.Id.Should().Be(123);
        _ = response.Result.Should().BeNull();
        _ = response.Error.Should().NotBeNull();
        _ = response.Error!.Code.Should().Be(-32600);
    }
}
