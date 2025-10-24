using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Nexus.Engine;
using Nexus.Protocol.Tools;

namespace Nexus.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for CloseDumpAnalyzeSessionTool class.
/// Tests session closure with various scenarios and error conditions.
/// </summary>
public class CloseDumpAnalyzeSessionToolTests
{
    private readonly Mock<IDebugEngine> m_MockEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the CloseDumpAnalyzeSessionToolTests class.
    /// </summary>
    public CloseDumpAnalyzeSessionToolTests()
    {
        m_MockEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        _ = services.AddSingleton<IDebugEngine>(m_MockEngine.Object);
        _ = services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that nexus_close_dump_analyze_session returns success result with valid session ID.
    /// </summary>
    [Fact]
    public async Task Nexus_close_dump_analyze_session_WithValidSessionId_ReturnsSuccessResult()
    {
        const string sessionId = "sess-123";

        _ = m_MockEngine.Setup(e => e.CloseSessionAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        m_MockEngine.Verify(e => e.CloseSessionAsync(sessionId), Times.Once);
        dynamic response = result;
        _ = ((string)response.sessionId).Should().Be(sessionId);
        _ = ((string)response.status).Should().Be("Success");
        _ = ((string)response.operation).Should().Be("nexus_close_dump_analyze_session");
    }

    /// <summary>
    /// Verifies that nexus_close_dump_analyze_session returns failed result with ArgumentException.
    /// </summary>
    [Fact]
    public async Task Nexus_close_dump_analyze_session_WithArgumentException_ReturnsFailedResult()
    {
        const string sessionId = "sess-invalid";

        _ = m_MockEngine.Setup(e => e.CloseSessionAsync(It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Invalid session ID"));

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        dynamic response = result;
        _ = ((string)response.sessionId).Should().Be(sessionId);
        _ = ((string)response.status).Should().Be("Failed");
        _ = ((string)response.message).Should().Be("Invalid session ID");
    }

    /// <summary>
    /// Verifies that nexus_close_dump_analyze_session returns failed result with unexpected exception.
    /// </summary>
    [Fact]
    public async Task Nexus_close_dump_analyze_session_WithUnexpectedException_ReturnsFailedResult()
    {
        const string sessionId = "sess-123";

        _ = m_MockEngine.Setup(e => e.CloseSessionAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        dynamic response = result;
        _ = ((string)response.sessionId).Should().Be(sessionId);
        _ = ((string)response.status).Should().Be("Failed");
        _ = ((string)response.message).Should().Contain("Unexpected error");
    }

    /// <summary>
    /// Verifies that nexus_close_dump_analyze_session includes usage field in response.
    /// </summary>
    [Fact]
    public async Task Nexus_close_dump_analyze_session_IncludesUsageField()
    {
        const string sessionId = "sess-789";

        _ = m_MockEngine.Setup(e => e.CloseSessionAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        _ = result.Should().NotBeNull();
        var resultType = result.GetType();
        var usageProperty = resultType.GetProperty("usage");
        _ = usageProperty.Should().NotBeNull();
        var usageValue = usageProperty!.GetValue(result);
        _ = usageValue.Should().NotBeNull();
    }
}
