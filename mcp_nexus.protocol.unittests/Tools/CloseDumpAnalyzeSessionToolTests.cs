using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mcp_nexus.Engine;
using mcp_nexus.Protocol.Tools;
using Moq;

namespace mcp_nexus.Protocol.Tests.Tools;

/// <summary>
/// Unit tests for CloseDumpAnalyzeSessionTool class.
/// Tests session closure with various scenarios and error conditions.
/// </summary>
public class CloseDumpAnalyzeSessionToolTests
{
    private readonly Mock<IDebugEngine> m_MockEngine;
    private readonly IServiceProvider m_ServiceProvider;

    public CloseDumpAnalyzeSessionToolTests()
    {
        m_MockEngine = new Mock<IDebugEngine>();
        
        var services = new ServiceCollection();
        services.AddSingleton<IDebugEngine>(m_MockEngine.Object);
        services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        m_ServiceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task nexus_close_dump_analyze_session_WithValidSessionId_ReturnsSuccessResult()
    {
        const string sessionId = "sess-123";
        
        m_MockEngine.Setup(e => e.CloseSessionAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        m_MockEngine.Verify(e => e.CloseSessionAsync(sessionId), Times.Once);
        dynamic response = result;
        ((string)response.sessionId).Should().Be(sessionId);
        ((string)response.status).Should().Be("Success");
        ((string)response.operation).Should().Be("nexus_close_dump_analyze_session");
    }

    [Fact]
    public async Task nexus_close_dump_analyze_session_WithArgumentException_ReturnsFailedResult()
    {
        const string sessionId = "sess-invalid";
        
        m_MockEngine.Setup(e => e.CloseSessionAsync(It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Invalid session ID"));

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        dynamic response = result;
        ((string)response.sessionId).Should().Be(sessionId);
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Be("Invalid session ID");
    }

    [Fact]
    public async Task nexus_close_dump_analyze_session_WithUnexpectedException_ReturnsFailedResult()
    {
        const string sessionId = "sess-123";
        
        m_MockEngine.Setup(e => e.CloseSessionAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        dynamic response = result;
        ((string)response.sessionId).Should().Be(sessionId);
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Contain("Unexpected error");
    }

    [Fact]
    public async Task nexus_close_dump_analyze_session_IncludesUsageField()
    {
        const string sessionId = "sess-789";
        
        m_MockEngine.Setup(e => e.CloseSessionAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        result.Should().NotBeNull();
        var resultType = result.GetType();
        var usageProperty = resultType.GetProperty("usage");
        usageProperty.Should().NotBeNull();
        var usageValue = usageProperty!.GetValue(result);
        usageValue.Should().NotBeNull();
    }
}

