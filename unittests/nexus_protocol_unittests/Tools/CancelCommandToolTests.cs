using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Nexus.Engine;
using Nexus.Protocol.Tools;

namespace Nexus.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for CancelCommandTool.
/// Tests command cancellation with various scenarios and error conditions.
/// </summary>
public class CancelCommandToolTests
{
    private readonly Mock<IDebugEngine> m_MockEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the CancelCommandToolTests class.
    /// </summary>
    public CancelCommandToolTests()
    {
        m_MockEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        services.AddSingleton<IDebugEngine>(m_MockEngine.Object);
        services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that nexus_cancel_command returns success with valid command.
    /// </summary>
    [Fact]
    public async Task Nexus_cancel_command_WithValidCommand_ReturnsSuccess()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-456";

        m_MockEngine.Setup(e => e.CancelCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = await CancelCommandTool.nexus_cancel_dump_analyze_command(
            m_ServiceProvider, sessionId, commandId);

        dynamic response = result;
        ((string)response.commandId).Should().Be(commandId);
        ((string)response.sessionId).Should().Be(sessionId);
        ((bool)response.cancelled).Should().BeTrue();
        ((string)response.status).Should().Be("Cancelled");
        ((string)response.operation).Should().Be("nexus_cancel_dump_analyze_command");
    }

    /// <summary>
    /// Verifies that nexus_cancel_dump_analyze_command returns NotFound with command not found.
    /// </summary>
    [Fact]
    public async Task Nexus_cancel_command_WithNotFoundCommand_ReturnsNotFound()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-missing";

        m_MockEngine.Setup(e => e.CancelCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var result = await CancelCommandTool.nexus_cancel_dump_analyze_command(
            m_ServiceProvider, sessionId, commandId);

        dynamic response = result;
        ((string)response.commandId).Should().Be(commandId);
        ((bool)response.cancelled).Should().BeFalse();
        ((string)response.status).Should().Be("NotFound");
        ((string)response.message).Should().Contain("not found");
    }

    /// <summary>
    /// Verifies that nexus_cancel_dump_analyze_command returns failed result with ArgumentException.
    /// </summary>
    [Fact]
    public async Task Nexus_cancel_command_WithArgumentException_ReturnsFailedResult()
    {
        const string sessionId = "sess-invalid";
        const string commandId = "cmd-456";

        m_MockEngine.Setup(e => e.CancelCommand(sessionId, commandId))
            .Throws(new ArgumentException("Invalid session"));

        var result = await CancelCommandTool.nexus_cancel_dump_analyze_command(
            m_ServiceProvider, sessionId, commandId);

        dynamic response = result;
        ((bool)response.cancelled).Should().BeFalse();
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Be("Invalid session");
    }

    /// <summary>
    /// Verifies that nexus_cancel_dump_analyze_command returns failed result with unexpected exception.
    /// </summary>
    [Fact]
    public async Task Nexus_cancel_command_WithUnexpectedException_ReturnsFailedResult()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-456";

        m_MockEngine.Setup(e => e.CancelCommand(sessionId, commandId))
            .Throws(new Exception("Unexpected error"));

        var result = await CancelCommandTool.nexus_cancel_dump_analyze_command(
            m_ServiceProvider, sessionId, commandId);

        dynamic response = result;
        ((bool)response.cancelled).Should().BeFalse();
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Contain("Unexpected error");
    }

    /// <summary>
    /// Verifies that nexus_cancel_dump_analyze_command includes usage field in response.
    /// </summary>
    [Fact]
    public async Task Nexus_cancel_command_IncludesUsageField()
    {
        const string sessionId = "sess-789";
        const string commandId = "cmd-101";

        m_MockEngine.Setup(e => e.CancelCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = await CancelCommandTool.nexus_cancel_dump_analyze_command(
            m_ServiceProvider, sessionId, commandId);

        result.Should().NotBeNull();
        var resultType = result.GetType();
        var usageProperty = resultType.GetProperty("usage");
        usageProperty.Should().NotBeNull();
        var usageValue = usageProperty!.GetValue(result);
        usageValue.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that nexus_cancel_dump_analyze_command calls engine method.
    /// </summary>
    [Fact]
    public async Task Nexus_cancel_command_VerifiesEngineIsCalled()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-789";

        m_MockEngine.Setup(e => e.CancelCommand(sessionId, commandId))
            .Returns(true);

        await CancelCommandTool.nexus_cancel_dump_analyze_command(
            m_ServiceProvider, sessionId, commandId);

        m_MockEngine.Verify(e => e.CancelCommand(sessionId, commandId), Times.Once);
    }
}
