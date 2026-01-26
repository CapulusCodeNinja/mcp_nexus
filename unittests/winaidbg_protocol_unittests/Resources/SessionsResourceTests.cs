using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using WinAiDbg.Engine.Share;
using WinAiDbg.Protocol.Resources;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Resources;

/// <summary>
/// Unit tests for SessionsResource class.
/// Tests session listing resource with mocked dependencies.
/// </summary>
public class SessionsResourceTests
{
    private readonly Mock<IDebugEngine> m_MockDebugEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsResourceTests"/> class.
    /// </summary>
    public SessionsResourceTests()
    {
        m_MockDebugEngine = new Mock<IDebugEngine>();
        _ = m_MockDebugEngine.Setup(e => e.GetActiveSessions()).Returns(Array.Empty<string>());

        var services = new ServiceCollection();
        _ = services.AddSingleton(m_MockDebugEngine.Object);
        _ = services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that Sessions returns empty list when no sessions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_ReturnsEmptyList()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        _ = result.Should().NotBeNullOrEmpty();
        _ = result.Should().Contain("## Sessions");
        _ = result.Should().Contain("**Count:** 0");
        _ = result.Should().Contain("No active sessions.");
    }

    /// <summary>
    /// Verifies that Sessions returns active sessions when sessions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_WhenSessionsExist_ReturnsSessions()
    {
        _ = m_MockDebugEngine.Setup(e => e.GetActiveSessions()).Returns(new[] { "sess-1", "sess-2" });
        _ = m_MockDebugEngine.Setup(e => e.GetSessionState("sess-1")).Returns(WinAiDbg.Engine.Share.Models.SessionState.Active);
        _ = m_MockDebugEngine.Setup(e => e.GetSessionState("sess-2")).Returns(WinAiDbg.Engine.Share.Models.SessionState.Initializing);
        _ = m_MockDebugEngine.Setup(e => e.IsSessionActive("sess-1")).Returns(true);
        _ = m_MockDebugEngine.Setup(e => e.IsSessionActive("sess-2")).Returns(false);

        var result = await SessionsResource.Sessions(m_ServiceProvider);

        _ = result.Should().Contain("## Sessions");
        _ = result.Should().Contain("**Count:** 2");
        _ = result.Should().Contain("sess-1");
        _ = result.Should().Contain("sess-2");
        _ = result.Should().Contain("| Session ID | State | Active |");
    }

    /// <summary>
    /// Verifies that Sessions includes timestamp in response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_IncludesTimestamp()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        _ = result.Should().Contain("**Timestamp:**");
    }

    /// <summary>
    /// Verifies that Sessions returns valid JSON.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_ReturnsValidJson()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        _ = result.Should().Contain("## Sessions");
    }

    /// <summary>
    /// Verifies that Sessions returns error response when exception occurs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_WithException_ReturnsErrorResponse()
    {
        // Create a service provider that throws when getting ILoggerFactory
        var mockServiceProvider = new Mock<IServiceProvider>();
        _ = mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Throws(new InvalidOperationException("Test error"));

        var result = await SessionsResource.Sessions(mockServiceProvider.Object);

        _ = result.Should().Contain("## Sessions");
        _ = result.Should().Contain("**Status:** Error");
        _ = result.Should().Contain("Test error");
    }

    /// <summary>
    /// Verifies that Sessions throws exception when service provider is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_WithNullServiceProvider_ThrowsException()
    {
        var action = async () => await SessionsResource.Sessions(null!);
        _ = await action.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that Sessions JSON format is indented.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_JsonFormat_IsIndented()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        _ = result.Should().Contain("\n"); // Markdown is multi-line
    }
}
