using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using nexus.engine;
using nexus.protocol.Tools;
using Moq;

namespace nexus.protocol.unittests.Tools;

/// <summary>
/// Unit tests for EnqueueAsyncDumpAnalyzeCommandTool class.
/// Tests command queuing with various scenarios and error conditions.
/// </summary>
public class EnqueueAsyncDumpAnalyzeCommandToolTests
{
    private readonly Mock<IDebugEngine> m_MockEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the EnqueueAsyncDumpAnalyzeCommandToolTests class.
    /// </summary>
    public EnqueueAsyncDumpAnalyzeCommandToolTests()
    {
        m_MockEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        services.AddSingleton<IDebugEngine>(m_MockEngine.Object);
        services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that nexus_enqueue_async_dump_analyze_command returns queued result with valid parameters.
    /// </summary>
    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_WithValidParameters_ReturnsQueuedResult()
    {
        const string sessionId = "sess-123";
        const string command = "kL";
        const string commandId = "cmd-456";

        m_MockEngine.Setup(e => e.EnqueueCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(commandId);

        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(
            m_ServiceProvider, sessionId, command);

        dynamic response = result;
        ((string)response.commandId).Should().Be(commandId);
        ((string)response.sessionId).Should().Be(sessionId);
        ((string)response.status).Should().Be("Queued");
        ((string)response.operation).Should().Be("nexus_enqueue_async_dump_analyze_command");
    }

    /// <summary>
    /// Verifies that nexus_enqueue_async_dump_analyze_command returns failed result with ArgumentException.
    /// </summary>
    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_WithArgumentException_ReturnsFailedResult()
    {
        const string sessionId = "sess-123";
        const string command = "kL";

        m_MockEngine.Setup(e => e.EnqueueCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new ArgumentException("Invalid session"));

        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(
            m_ServiceProvider, sessionId, command);

        dynamic response = result;
        ((string?)response.commandId).Should().BeNull();
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Be("Invalid session");
    }

    /// <summary>
    /// Verifies that nexus_enqueue_async_dump_analyze_command returns failed result with InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_WithInvalidOperationException_ReturnsFailedResult()
    {
        const string sessionId = "sess-123";
        const string command = "kL";

        m_MockEngine.Setup(e => e.EnqueueCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Queue full"));

        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(
            m_ServiceProvider, sessionId, command);

        dynamic response = result;
        ((string?)response.commandId).Should().BeNull();
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Be("Queue full");
    }

    /// <summary>
    /// Verifies that nexus_enqueue_async_dump_analyze_command returns failed result with unexpected exception.
    /// </summary>
    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_WithUnexpectedException_ReturnsFailedResult()
    {
        const string sessionId = "sess-123";
        const string command = "kL";

        m_MockEngine.Setup(e => e.EnqueueCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Unexpected error"));

        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(
            m_ServiceProvider, sessionId, command);

        dynamic response = result;
        ((string?)response.commandId).Should().BeNull();
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Contain("Unexpected error");
    }

    /// <summary>
    /// Verifies that nexus_enqueue_async_dump_analyze_command includes usage field in response.
    /// </summary>
    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_IncludesUsageField()
    {
        const string sessionId = "sess-123";
        const string command = "kL";
        const string commandId = "cmd-789";

        m_MockEngine.Setup(e => e.EnqueueCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(commandId);

        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(
            m_ServiceProvider, sessionId, command);

        result.Should().NotBeNull();
        var resultType = result.GetType();
        var usageProperty = resultType.GetProperty("usage");
        usageProperty.Should().NotBeNull();
        var usageValue = usageProperty!.GetValue(result);
        usageValue.Should().NotBeNull();
    }
}
