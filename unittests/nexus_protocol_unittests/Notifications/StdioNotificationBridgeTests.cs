using FluentAssertions;

using Nexus.Protocol.Models;
using Nexus.Protocol.Notifications;

using Xunit;

namespace Nexus.Protocol.Unittests.Notifications;

/// <summary>
/// Unit tests for StdioNotificationBridge class.
/// Tests notification sending to stdout with captured console output.
/// </summary>
public class StdioNotificationBridgeTests : IDisposable
{
    private readonly StdioNotificationBridge m_Bridge;
    private readonly StringWriter m_CapturedOutput;
    private readonly TextWriter m_OriginalOutput;

    /// <summary>
    /// Initializes a new instance of the <see cref="StdioNotificationBridgeTests"/> class.
    /// </summary>
    public StdioNotificationBridgeTests()
    {
        m_Bridge = new StdioNotificationBridge();

        // Capture console output
        m_OriginalOutput = Console.Out;
        m_CapturedOutput = new StringWriter();
        Console.SetOut(m_CapturedOutput);
    }

    /// <summary>
    /// Disposes resources and restores console output.
    /// </summary>
    public void Dispose()
    {
        // Restore original console output
        Console.SetOut(m_OriginalOutput);
        m_CapturedOutput?.Dispose();
    }

    /// <summary>
    /// Verifies that constructor creates instance successfully.
    /// </summary>
    [Fact]
    public void Constructor_CreatesInstance()
    {
        var action = () => new StdioNotificationBridge();

        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that SendNotificationAsync writes to console with valid notification.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendNotificationAsync_WithValidNotification_WritesToConsole()
    {
        var notification = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/notification",
            Params = new { message = "test" },
        };

        await m_Bridge.SendNotificationAsync(notification);

        var output = m_CapturedOutput.ToString();
        _ = output.Should().Contain("\"jsonrpc\":\"2.0\"");
        _ = output.Should().Contain("\"method\":\"test/notification\"");
    }

    /// <summary>
    /// Verifies that SendNotificationAsync throws ArgumentNullException when notification is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendNotificationAsync_WithNullNotification_ThrowsArgumentNullException()
    {
        var action = async () => await m_Bridge.SendNotificationAsync(null!);

        _ = await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("notification");
    }

    /// <summary>
    /// Verifies that SendNotificationAsync writes complete JSON line.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendNotificationAsync_WritesCompleteJsonLine()
    {
        var notification = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/method",
            Params = new { data = "value" },
        };

        await m_Bridge.SendNotificationAsync(notification);

        var output = m_CapturedOutput.ToString().Trim();
        _ = output.Should().StartWith("{");
        _ = output.Should().EndWith("}");
    }

    /// <summary>
    /// Verifies that SendNotificationAsync writes multiple notifications sequentially.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendNotificationAsync_MultipleNotifications_WritesSequentially()
    {
        var notification1 = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/first",
            Params = new { },
        };

        var notification2 = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/second",
            Params = new { },
        };

        await m_Bridge.SendNotificationAsync(notification1);
        await m_Bridge.SendNotificationAsync(notification2);

        var output = m_CapturedOutput.ToString();
        _ = output.Should().Contain("test/first");
        _ = output.Should().Contain("test/second");

        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        _ = lines.Should().HaveCountGreaterOrEqualTo(2);
    }

    /// <summary>
    /// Verifies that SendNotificationAsync is thread-safe with concurrent calls.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendNotificationAsync_ConcurrentCalls_AreThreadSafe()
    {
        var notifications = Enumerable.Range(0, 10).Select(i => new McpNotification
        {
            JsonRpc = "2.0",
            Method = $"test/notification{i}",
            Params = new { index = i },
        }).ToList();

        var tasks = notifications.Select(n => m_Bridge.SendNotificationAsync(n)).ToArray();

        await Task.WhenAll(tasks);

        var output = m_CapturedOutput.ToString();
        for (var i = 0; i < 10; i++)
        {
            _ = output.Should().Contain($"test/notification{i}");
        }
    }

    /// <summary>
    /// Verifies that SendNotificationAsync serializes complex params correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendNotificationAsync_WithComplexParams_SerializesCorrectly()
    {
        var notification = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/complex",
            Params = new
            {
                nested = new
                {
                    value = 123,
                    text = "test",
                    array = new[] { 1, 2, 3 },
                },
            },
        };

        await m_Bridge.SendNotificationAsync(notification);

        var output = m_CapturedOutput.ToString();
        _ = output.Should().Contain("\"nested\"");
        _ = output.Should().Contain("\"value\":123");
        _ = output.Should().Contain("\"text\":\"test\"");
    }
}
