using FluentAssertions;

using Moq;

using WinAiDbg.Protocol.Models;
using WinAiDbg.Protocol.Notifications;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Notifications;

/// <summary>
/// Unit tests for the <see cref="McpNotificationService"/> class.
/// Tests notification publishing, error handling, and all notification types.
/// </summary>
public class McpNotificationServiceTests
{
    private readonly Mock<INotificationBridge> m_MockNotificationBridge;
    private readonly McpNotificationService m_Service;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpNotificationServiceTests"/> class.
    /// </summary>
    public McpNotificationServiceTests()
    {
        m_MockNotificationBridge = new Mock<INotificationBridge>();
        m_Service = new McpNotificationService(m_MockNotificationBridge.Object);
    }

    /// <summary>
    /// Verifies that constructor with null notificationBridge throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullNotificationBridge_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new McpNotificationService(null!));
    }

    /// <summary>
    /// Verifies that constructor with valid notificationBridge succeeds.
    /// </summary>
    [Fact]
    public void Constructor_WithValidNotificationBridge_Succeeds()
    {
        // Act
        var service = new McpNotificationService(m_MockNotificationBridge.Object);

        // Assert
        _ = service.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that PublishNotificationAsync sends notification successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PublishNotificationAsync_WithValidData_SendsNotification()
    {
        // Arrange
        var eventType = "test/event";
        var data = new { Test = "value" };

        // Act
        await m_Service.PublishNotificationAsync(eventType, data);

        // Assert
        m_MockNotificationBridge.Verify(
            x => x.SendNotificationAsync(It.Is<McpNotification>(n =>
                n.JsonRpc == "2.0" &&
                n.Method == eventType &&
                n.Params == data)),
            Times.Once);
    }

    /// <summary>
    /// Verifies that PublishNotificationAsync handles exceptions gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PublishNotificationAsync_WhenBridgeThrows_HandlesException()
    {
        // Arrange
        var eventType = "test/event";
        var data = new { Test = "value" };
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .ThrowsAsync(new InvalidOperationException("Bridge error"));

        // Act
        await m_Service.PublishNotificationAsync(eventType, data);

        // Assert - Should not throw, exception should be caught and logged
        m_MockNotificationBridge.Verify(
            x => x.SendNotificationAsync(It.IsAny<McpNotification>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that NotifyCommandStatusAsync publishes correct notification.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandStatusAsync_WithAllParameters_PublishesNotification()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-456";
        var command = "test command";
        var status = "Completed";
        var result = "result data";
        var progress = 100;
        var message = "Command completed";
        var error = (string?)null;

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandStatusAsync(sessionId, commandId, command, status, result, progress, message, error);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/commandStatus");
        _ = capturedNotification.Params.Should().BeOfType<McpCommandStatusNotification>();
        var notification = (McpCommandStatusNotification)capturedNotification.Params!;
        _ = notification.SessionId.Should().Be(sessionId);
        _ = notification.CommandId.Should().Be(commandId);
        _ = notification.Command.Should().Be(command);
        _ = notification.Status.Should().Be(status);
        _ = notification.Result.Should().Be(result);
        _ = notification.Progress.Should().Be(progress);
        _ = notification.Message.Should().Be(message);
        _ = notification.Error.Should().Be(error);
    }

    /// <summary>
    /// Verifies that NotifyCommandStatusAsync handles null optional parameters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandStatusAsync_WithNullOptionalParameters_PublishesNotification()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-456";
        var command = "test command";
        var status = "Running";
        var result = (string?)null;
        var progress = 50;
        var message = (string?)null;
        var error = (string?)null;

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandStatusAsync(sessionId, commandId, command, status, result, progress, message, error);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/commandStatus");
        _ = capturedNotification.Params.Should().BeOfType<McpCommandStatusNotification>();
        var notification = (McpCommandStatusNotification)capturedNotification.Params!;
        _ = notification.SessionId.Should().Be(sessionId);
        _ = notification.CommandId.Should().Be(commandId);
        _ = notification.Result.Should().BeNull();
        _ = notification.Message.Should().BeNull();
        _ = notification.Error.Should().BeNull();
    }

    /// <summary>
    /// Verifies that NotifyCommandHeartbeatAsync publishes correct notification.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandHeartbeatAsync_WithValidTimeSpan_PublishesNotification()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-456";
        var command = "test command";
        var elapsed = new TimeSpan(0, 2, 30);
        var details = "Processing items";

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandHeartbeatAsync(sessionId, commandId, command, elapsed, details);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/commandHeartbeat");
        _ = capturedNotification.Params.Should().BeOfType<McpCommandHeartbeatNotification>();
        var notification = (McpCommandHeartbeatNotification)capturedNotification.Params!;
        _ = notification.SessionId.Should().Be(sessionId);
        _ = notification.CommandId.Should().Be(commandId);
        _ = notification.Command.Should().Be(command);
        _ = notification.ElapsedSeconds.Should().Be(elapsed.TotalSeconds);
        _ = notification.ElapsedDisplay.Should().Be("2m 30s");
        _ = notification.Details.Should().Be(details);
    }

    /// <summary>
    /// Verifies that NotifyCommandHeartbeatAsync handles zero time span.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandHeartbeatAsync_WithZeroTimeSpan_PublishesNotification()
    {
        // Arrange
        var elapsed = TimeSpan.Zero;

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandHeartbeatAsync("session-123", "cmd-456", "test", elapsed, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/commandHeartbeat");
        _ = capturedNotification.Params.Should().BeOfType<McpCommandHeartbeatNotification>();
        var notification = (McpCommandHeartbeatNotification)capturedNotification.Params!;
        _ = notification.ElapsedSeconds.Should().Be(0);
        _ = notification.ElapsedDisplay.Should().Be("0s");
    }

    /// <summary>
    /// Verifies that NotifyCommandHeartbeatAsync handles large time span with hours.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandHeartbeatAsync_WithLargeTimeSpan_PublishesNotification()
    {
        // Arrange
        var elapsed = new TimeSpan(2, 15, 45);

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandHeartbeatAsync("session-123", "cmd-456", "test", elapsed, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/commandHeartbeat");
        _ = capturedNotification.Params.Should().BeOfType<McpCommandHeartbeatNotification>();
        var notification = (McpCommandHeartbeatNotification)capturedNotification.Params!;
        _ = notification.ElapsedSeconds.Should().Be(elapsed.TotalSeconds);
        _ = notification.ElapsedDisplay.Should().Be("2h 15m 45s");
    }

    /// <summary>
    /// Verifies that NotifyCommandHeartbeatAsync handles null details.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandHeartbeatAsync_WithNullDetails_PublishesNotification()
    {
        // Arrange
        var elapsed = new TimeSpan(0, 5, 0);

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandHeartbeatAsync("session-123", "cmd-456", "test", elapsed, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/commandHeartbeat");
        _ = capturedNotification.Params.Should().BeOfType<McpCommandHeartbeatNotification>();
        var notification = (McpCommandHeartbeatNotification)capturedNotification.Params!;
        _ = notification.Details.Should().BeNull();
    }

    /// <summary>
    /// Verifies that NotifySessionRecoveryAsync publishes correct notification.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifySessionRecoveryAsync_WithAllParameters_PublishesNotification()
    {
        // Arrange
        var reason = "Session timeout";
        var recoveryStep = "Reconnected session";
        var success = true;
        var message = "Recovery successful";
        var affectedCommands = new[] { "cmd-1", "cmd-2" };

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifySessionRecoveryAsync(reason, recoveryStep, success, message, affectedCommands);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/sessionRecovery");
        _ = capturedNotification.Params.Should().BeOfType<McpSessionRecoveryNotification>();
        var notification = (McpSessionRecoveryNotification)capturedNotification.Params!;
        _ = notification.Reason.Should().Be(reason);
        _ = notification.RecoveryStep.Should().Be(recoveryStep);
        _ = notification.Success.Should().Be(success);
        _ = notification.Message.Should().Be(message);
        _ = notification.AffectedCommands.Should().NotBeNull();
        _ = notification.AffectedCommands!.Should().BeEquivalentTo(affectedCommands);
    }

    /// <summary>
    /// Verifies that NotifySessionRecoveryAsync handles null affected commands.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifySessionRecoveryAsync_WithNullAffectedCommands_PublishesNotification()
    {
        // Arrange
        var reason = "Session timeout";
        var recoveryStep = "Reconnected session";
        var success = false;
        var message = "Recovery failed";

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifySessionRecoveryAsync(reason, recoveryStep, success, message, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/sessionRecovery");
        _ = capturedNotification.Params.Should().BeOfType<McpSessionRecoveryNotification>();
        var notification = (McpSessionRecoveryNotification)capturedNotification.Params!;
        _ = notification.AffectedCommands.Should().BeNull();
    }

    /// <summary>
    /// Verifies that NotifyServerHealthAsync publishes correct notification.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyServerHealthAsync_WithAllParameters_PublishesNotification()
    {
        // Arrange
        var status = "Healthy";
        var cdbSessionActive = true;
        var queueSize = 5;
        var activeCommands = 2;
        var uptime = TimeSpan.FromHours(1.5);

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyServerHealthAsync(status, cdbSessionActive, queueSize, activeCommands, uptime);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/serverHealth");
        _ = capturedNotification.Params.Should().BeOfType<McpServerHealthNotification>();
        var notification = (McpServerHealthNotification)capturedNotification.Params!;
        _ = notification.Status.Should().Be(status);
        _ = notification.CdbSessionActive.Should().Be(cdbSessionActive);
        _ = notification.QueueSize.Should().Be(queueSize);
        _ = notification.ActiveCommands.Should().Be(activeCommands);
        _ = notification.Uptime.Should().Be(uptime);
    }

    /// <summary>
    /// Verifies that NotifyServerHealthAsync handles null uptime.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyServerHealthAsync_WithNullUptime_PublishesNotification()
    {
        // Arrange
        var status = "Degraded";
        var cdbSessionActive = false;
        var queueSize = 0;
        var activeCommands = 0;

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyServerHealthAsync(status, cdbSessionActive, queueSize, activeCommands, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Method.Should().Be("notifications/serverHealth");
        _ = capturedNotification.Params.Should().BeOfType<McpServerHealthNotification>();
        var notification = (McpServerHealthNotification)capturedNotification.Params!;
        _ = notification.Uptime.Should().BeNull();
    }

    /// <summary>
    /// Verifies that NotifyToolsListChangedAsync publishes correct notification.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyToolsListChangedAsync_PublishesNotification()
    {
        // Act
        await m_Service.NotifyToolsListChangedAsync();

        // Assert
        m_MockNotificationBridge.Verify(
            x => x.SendNotificationAsync(It.Is<McpNotification>(n =>
                n.Method == "notifications/tools/listChanged")),
            Times.Once);
    }

    /// <summary>
    /// Verifies that NotifyResourcesListChangedAsync publishes correct notification.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyResourcesListChangedAsync_PublishesNotification()
    {
        // Act
        await m_Service.NotifyResourcesListChangedAsync();

        // Assert
        m_MockNotificationBridge.Verify(
            x => x.SendNotificationAsync(It.Is<McpNotification>(n =>
                n.Method == "notifications/resources/listChanged")),
            Times.Once);
    }

    /// <summary>
    /// Verifies that NotifyCommandStatusAsync sets timestamp correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandStatusAsync_SetsTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.Now;

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandStatusAsync("session-123", "cmd-456", "test", "Running", null, 0, null, null);

        // Assert
        var after = DateTimeOffset.Now;
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpCommandStatusNotification>();
        var notification = (McpCommandStatusNotification)capturedNotification.Params!;
        _ = notification.Timestamp.Should().BeOnOrAfter(before);
        _ = notification.Timestamp.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Verifies that NotifyCommandHeartbeatAsync sets timestamp correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandHeartbeatAsync_SetsTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.Now;

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandHeartbeatAsync("session-123", "cmd-456", "test", TimeSpan.FromSeconds(30), null);

        // Assert
        var after = DateTimeOffset.Now;
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpCommandHeartbeatNotification>();
        var notification = (McpCommandHeartbeatNotification)capturedNotification.Params!;
        _ = notification.Timestamp.Should().BeOnOrAfter(before);
        _ = notification.Timestamp.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Verifies that NotifyCommandHeartbeatAsync formats time span at exactly 1 minute boundary.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandHeartbeatAsync_WithExactlyOneMinute_FormatsCorrectly()
    {
        // Arrange
        var elapsed = TimeSpan.FromMinutes(1);

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandHeartbeatAsync("session-123", "cmd-456", "test", elapsed, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpCommandHeartbeatNotification>();
        var notification = (McpCommandHeartbeatNotification)capturedNotification.Params!;
        _ = notification.ElapsedDisplay.Should().Be("1m 0s");
    }

    /// <summary>
    /// Verifies that NotifyCommandHeartbeatAsync formats time span at exactly 1 hour boundary.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandHeartbeatAsync_WithExactlyOneHour_FormatsCorrectly()
    {
        // Arrange
        var elapsed = TimeSpan.FromHours(1);

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandHeartbeatAsync("session-123", "cmd-456", "test", elapsed, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpCommandHeartbeatNotification>();
        var notification = (McpCommandHeartbeatNotification)capturedNotification.Params!;
        _ = notification.ElapsedDisplay.Should().Be("1h 0m 0s");
    }

    /// <summary>
    /// Verifies that NotifySessionRecoveryAsync sets timestamp correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifySessionRecoveryAsync_SetsTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.Now;

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifySessionRecoveryAsync("reason", "step", true, "message", null);

        // Assert
        var after = DateTimeOffset.Now;
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpSessionRecoveryNotification>();
        var notification = (McpSessionRecoveryNotification)capturedNotification.Params!;
        _ = notification.Timestamp.Should().BeOnOrAfter(before);
        _ = notification.Timestamp.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Verifies that NotifyServerHealthAsync sets timestamp correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyServerHealthAsync_SetsTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.Now;

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyServerHealthAsync("Healthy", true, 0, 0, TimeSpan.Zero);

        // Assert
        var after = DateTimeOffset.Now;
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpServerHealthNotification>();
        var notification = (McpServerHealthNotification)capturedNotification.Params!;
        _ = notification.Timestamp.Should().BeOnOrAfter(before);
        _ = notification.Timestamp.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Verifies that NotifyCommandStatusAsync handles progress boundary values.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandStatusAsync_WithProgressBoundaryValues_HandlesCorrectly()
    {
        // Arrange
        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act - Test with progress = 0
        await m_Service.NotifyCommandStatusAsync("session-123", "cmd-456", "test", "Running", null, 0, null, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpCommandStatusNotification>();
        var notification0 = (McpCommandStatusNotification)capturedNotification.Params!;
        _ = notification0.Progress.Should().Be(0);

        // Act - Test with progress = 100
        await m_Service.NotifyCommandStatusAsync("session-123", "cmd-456", "test", "Completed", null, 100, null, null);

        // Assert
        _ = capturedNotification!.Params.Should().BeOfType<McpCommandStatusNotification>();
        var notification100 = (McpCommandStatusNotification)capturedNotification.Params!;
        _ = notification100.Progress.Should().Be(100);
    }

    /// <summary>
    /// Verifies that NotifyCommandStatusAsync handles empty strings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandStatusAsync_WithEmptyStrings_HandlesCorrectly()
    {
        // Arrange
        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandStatusAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, string.Empty, string.Empty);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpCommandStatusNotification>();
        var notification = (McpCommandStatusNotification)capturedNotification.Params!;
        _ = notification.SessionId.Should().Be(string.Empty);
        _ = notification.CommandId.Should().Be(string.Empty);
        _ = notification.Command.Should().Be(string.Empty);
        _ = notification.Status.Should().Be(string.Empty);
        _ = notification.Result.Should().Be(string.Empty);
        _ = notification.Message.Should().Be(string.Empty);
        _ = notification.Error.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that NotifyCommandHeartbeatAsync handles very small time spans (less than a second).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyCommandHeartbeatAsync_WithFractionalSeconds_FormatsCorrectly()
    {
        // Arrange
        var elapsed = TimeSpan.FromMilliseconds(500);

        McpNotification? capturedNotification = null;
        _ = m_MockNotificationBridge.Setup(x => x.SendNotificationAsync(It.IsAny<McpNotification>()))
            .Callback<McpNotification>(n => capturedNotification = n);

        // Act
        await m_Service.NotifyCommandHeartbeatAsync("session-123", "cmd-456", "test", elapsed, null);

        // Assert
        _ = capturedNotification.Should().NotBeNull();
        _ = capturedNotification!.Params.Should().BeOfType<McpCommandHeartbeatNotification>();
        var notification = (McpCommandHeartbeatNotification)capturedNotification.Params!;
        _ = notification.ElapsedDisplay.Should().Be("0s");
    }
}
