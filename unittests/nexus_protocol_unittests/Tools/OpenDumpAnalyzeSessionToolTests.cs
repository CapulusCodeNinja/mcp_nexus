using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using nexus.engine;
using nexus.external_apis.FileSystem;
using nexus.protocol.Tools;

namespace nexus.protocol.unittests.Tools;

/// <summary>
/// Unit tests for OpenDumpAnalyzeSessionTool class.
/// Tests session creation with various scenarios and error conditions.
/// </summary>
public class OpenDumpAnalyzeSessionToolTests
{
    private readonly Mock<IDebugEngine> m_MockEngine;
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the OpenDumpAnalyzeSessionToolTests class.
    /// </summary>
    public OpenDumpAnalyzeSessionToolTests()
    {
        m_MockEngine = new Mock<IDebugEngine>();
        m_MockFileSystem = new Mock<IFileSystem>();

        // By default, mock FileExists to return true (file exists)
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
        m_MockFileSystem.Setup(fs => fs.GetFileName(It.IsAny<string>())).Returns<string>(path => Path.GetFileName(path));

        var services = new ServiceCollection();
        services.AddSingleton<IDebugEngine>(m_MockEngine.Object);
        services.AddSingleton<IFileSystem>(m_MockFileSystem.Object);
        services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that nexus_open_dump_analyze_session returns success result with valid dump path.
    /// </summary>
    [Fact]
    public async Task nexus_open_dump_analyze_session_WithValidDumpPath_ReturnsSuccessResult()
    {
        const string dumpPath = @"C:\dumps\test.dmp";
        const string sessionId = "sess-123";

        m_MockEngine.Setup(e => e.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionId);

        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath);

        dynamic response = result;
        ((string)response.sessionId).Should().Be(sessionId);
        ((string)response.status).Should().Be("Success");
        ((string)response.operation).Should().Be("nexus_open_dump_analyze_session");
    }

    /// <summary>
    /// Verifies that nexus_open_dump_analyze_session passes symbols path to engine.
    /// </summary>
    [Fact]
    public async Task nexus_open_dump_analyze_session_WithSymbolsPath_PassesToEngine()
    {
        const string dumpPath = @"C:\dumps\test.dmp";
        const string symbolsPath = @"C:\symbols";
        const string sessionId = "sess-456";

        m_MockEngine.Setup(e => e.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionId);

        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(
            m_ServiceProvider, dumpPath, symbolsPath);

        m_MockEngine.Verify(e => e.CreateSessionAsync(dumpPath, symbolsPath, It.IsAny<CancellationToken>()), Times.Once);
        dynamic response = result;
        ((string)response.sessionId).Should().Be(sessionId);
    }

    /// <summary>
    /// Verifies that nexus_open_dump_analyze_session returns failed result when file not found.
    /// </summary>
    [Fact]
    public async Task nexus_open_dump_analyze_session_WithFileNotFoundException_ReturnsFailedResult()
    {
        const string dumpPath = @"C:\dumps\missing.dmp";

        // Mock FileExists to return false for this test
        m_MockFileSystem.Setup(fs => fs.FileExists(dumpPath)).Returns(false);

        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath);

        dynamic response = result;
        ((string?)response.sessionId).Should().BeNull();
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Contain("Dump file not found");
    }

    /// <summary>
    /// Verifies that nexus_open_dump_analyze_session returns failed result with InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task nexus_open_dump_analyze_session_WithInvalidOperationException_ReturnsFailedResult()
    {
        const string dumpPath = @"C:\dumps\test.dmp";

        m_MockEngine.Setup(e => e.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Max sessions reached"));

        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath);

        dynamic response = result;
        ((string?)response.sessionId).Should().BeNull();
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Be("Max sessions reached");
    }

    /// <summary>
    /// Verifies that nexus_open_dump_analyze_session returns failed result with unexpected exception.
    /// </summary>
    [Fact]
    public async Task nexus_open_dump_analyze_session_WithUnexpectedException_ReturnsFailedResult()
    {
        const string dumpPath = @"C:\dumps\test.dmp";

        m_MockEngine.Setup(e => e.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected failure"));

        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath);

        dynamic response = result;
        ((string?)response.sessionId).Should().BeNull();
        ((string)response.status).Should().Be("Failed");
        ((string)response.message).Should().Contain("Unexpected error");
    }

    /// <summary>
    /// Verifies that nexus_open_dump_analyze_session includes usage field in response.
    /// </summary>
    [Fact]
    public async Task nexus_open_dump_analyze_session_IncludesUsageField()
    {
        const string dumpPath = @"C:\dumps\test.dmp";
        const string sessionId = "sess-789";

        m_MockEngine.Setup(e => e.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionId);

        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath);

        result.Should().NotBeNull();
        var resultType = result.GetType();
        var usageProperty = resultType.GetProperty("usage");
        usageProperty.Should().NotBeNull();
        var usageValue = usageProperty!.GetValue(result);
        usageValue.Should().NotBeNull();
    }
}
