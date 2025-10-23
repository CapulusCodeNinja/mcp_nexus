using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using nexus.protocol.Models;
using nexus.protocol.Notifications;
using System.Text;

namespace nexus.protocol.unittests.Notifications;

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
    /// Initializes a new instance of the StdioNotificationBridgeTests class.
    /// </summary>
    public StdioNotificationBridgeTests()
    {
        var logger = NullLogger<StdioNotificationBridge>.Instance;
        m_Bridge = new StdioNotificationBridge(logger);

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
    /// Verifies that constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new StdioNotificationBridge(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that SendNotificationAsync writes to console with valid notification.
    /// </summary>
    [Fact]
    public async Task SendNotificationAsync_WithValidNotification_WritesToConsole()
    {
        var notification = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/notification",
            Params = new { message = "test" }
        };

        await m_Bridge.SendNotificationAsync(notification);

        var output = m_CapturedOutput.ToString();
        output.Should().Contain("\"jsonrpc\":\"2.0\"");
        output.Should().Contain("\"method\":\"test/notification\"");
    }

    /// <summary>
    /// Verifies that SendNotificationAsync throws ArgumentNullException when notification is null.
    /// </summary>
    [Fact]
    public async Task SendNotificationAsync_WithNullNotification_ThrowsArgumentNullException()
    {
        var action = async () => await m_Bridge.SendNotificationAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("notification");
    }

    /// <summary>
    /// Verifies that SendNotificationAsync writes complete JSON line.
    /// </summary>
    [Fact]
    public async Task SendNotificationAsync_WritesCompleteJsonLine()
    {
        var notification = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/method",
            Params = new { data = "value" }
        };

        await m_Bridge.SendNotificationAsync(notification);

        var output = m_CapturedOutput.ToString().Trim();
        output.Should().StartWith("{");
        output.Should().EndWith("}");
    }

    /// <summary>
    /// Verifies that SendNotificationAsync writes multiple notifications sequentially.
    /// </summary>
    [Fact]
    public async Task SendNotificationAsync_MultipleNotifications_WritesSequentially()
    {
        var notification1 = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/first",
            Params = new { }
        };

        var notification2 = new McpNotification
        {
            JsonRpc = "2.0",
            Method = "test/second",
            Params = new { }
        };

        await m_Bridge.SendNotificationAsync(notification1);
        await m_Bridge.SendNotificationAsync(notification2);

        var output = m_CapturedOutput.ToString();
        output.Should().Contain("test/first");
        output.Should().Contain("test/second");

        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCountGreaterOrEqualTo(2);
    }

    /// <summary>
    /// Verifies that SendNotificationAsync is thread-safe with concurrent calls.
    /// </summary>
    [Fact]
    public async Task SendNotificationAsync_ConcurrentCalls_AreThreadSafe()
    {
        var notifications = Enumerable.Range(0, 10).Select(i => new McpNotification
        {
            JsonRpc = "2.0",
            Method = $"test/notification{i}",
            Params = new { index = i }
        }).ToList();

        var tasks = notifications.Select(n => m_Bridge.SendNotificationAsync(n)).ToArray();

        await Task.WhenAll(tasks);

        var output = m_CapturedOutput.ToString();
        for (int i = 0; i < 10; i++)
        {
            output.Should().Contain($"test/notification{i}");
        }
    }

    /// <summary>
    /// Verifies that SendNotificationAsync serializes complex params correctly.
    /// </summary>
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
                    array = new[] { 1, 2, 3 }
                }
            }
        };

        await m_Bridge.SendNotificationAsync(notification);

        var output = m_CapturedOutput.ToString();
        output.Should().Contain("\"nested\"");
        output.Should().Contain("\"value\":123");
        output.Should().Contain("\"text\":\"test\"");
    }
}
