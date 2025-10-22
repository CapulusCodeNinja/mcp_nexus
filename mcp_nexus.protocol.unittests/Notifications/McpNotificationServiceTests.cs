using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mcp_nexus.Protocol.Models;
using mcp_nexus.Protocol.Notifications;

namespace mcp_nexus.Protocol.Tests.Notifications;

/// <summary>
/// Unit tests for McpNotificationService class.
/// Tests notification creation and dispatching with mocked bridges.
/// </summary>
public class McpNotificationServiceTests
{
    private readonly Mock<INotificationBridge> m_MockBridge;
    private readonly McpNotificationService m_Service;

    public McpNotificationServiceTests()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<McpNotificationService>();
        m_MockBridge = new Mock<INotificationBridge>();
        m_Service = new McpNotificationService(logger, m_MockBridge.Object);
    }

    [Fact]
    public async Task PublishNotificationAsync_ValidNotification_CallsBridge()
    {
        var eventType = "test/event";
        var data = new { message = "test" };

        await m_Service.PublishNotificationAsync(eventType, data);

        m_MockBridge.Verify(b => b.SendNotificationAsync(It.Is<McpNotification>(
            n => n.Method == eventType && n.JsonRpc == "2.0"
        )), Times.Once);
    }

    [Fact]
    public async Task NotifyCommandStatusAsync_ValidParameters_SendsNotification()
    {
        await m_Service.NotifyCommandStatusAsync(
            "sess-001", "cmd-123", "k", "Executing", null, 50, "Processing", null);

        m_MockBridge.Verify(b => b.SendNotificationAsync(It.Is<McpNotification>(
            n => n.Method == "notifications/commandStatus"
        )), Times.Once);
    }

    [Fact]
    public async Task NotifyCommandHeartbeatAsync_ValidParameters_SendsNotification()
    {
        var elapsed = TimeSpan.FromMinutes(2);

        await m_Service.NotifyCommandHeartbeatAsync(
            "sess-001", "cmd-123", "!analyze -v", elapsed, "Still analyzing");

        m_MockBridge.Verify(b => b.SendNotificationAsync(It.Is<McpNotification>(
            n => n.Method == "notifications/commandHeartbeat"
        )), Times.Once);
    }

    [Fact]
    public async Task NotifySessionRecoveryAsync_ValidParameters_SendsNotification()
    {
        await m_Service.NotifySessionRecoveryAsync(
            "Timeout", "Restarting session", true, "Recovery successful", new[] { "cmd-1", "cmd-2" });

        m_MockBridge.Verify(b => b.SendNotificationAsync(It.Is<McpNotification>(
            n => n.Method == "notifications/sessionRecovery"
        )), Times.Once);
    }

    [Fact]
    public async Task NotifyServerHealthAsync_ValidParameters_SendsNotification()
    {
        await m_Service.NotifyServerHealthAsync(
            "healthy", true, 5, 2, TimeSpan.FromHours(1));

        m_MockBridge.Verify(b => b.SendNotificationAsync(It.Is<McpNotification>(
            n => n.Method == "notifications/serverHealth"
        )), Times.Once);
    }

    [Fact]
    public async Task NotifyToolsListChangedAsync_SendsNotification()
    {
        await m_Service.NotifyToolsListChangedAsync();

        m_MockBridge.Verify(b => b.SendNotificationAsync(It.Is<McpNotification>(
            n => n.Method == "notifications/tools/listChanged"
        )), Times.Once);
    }

    [Fact]
    public async Task NotifyResourcesListChangedAsync_SendsNotification()
    {
        await m_Service.NotifyResourcesListChangedAsync();

        m_MockBridge.Verify(b => b.SendNotificationAsync(It.Is<McpNotification>(
            n => n.Method == "notifications/resources/listChanged"
        )), Times.Once);
    }

    [Fact]
    public async Task PublishNotificationAsync_BridgeThrows_LogsErrorButDoesNotThrow()
    {
        m_MockBridge
            .Setup(b => b.SendNotificationAsync(It.IsAny<McpNotification>()))
            .ThrowsAsync(new IOException("Bridge error"));

        await m_Service.Invoking(s => s.PublishNotificationAsync("test", new { }))
            .Should().NotThrowAsync();
    }
}

