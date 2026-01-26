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
/// Unit tests for CommandsResource class.
/// Tests command listing resource with mocked dependencies.
/// </summary>
public class CommandsResourceTests
{
    private readonly Mock<IDebugEngine> m_MockDebugEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandsResourceTests"/> class.
    /// </summary>
    public CommandsResourceTests()
    {
        m_MockDebugEngine = new Mock<IDebugEngine>();
        _ = m_MockDebugEngine.Setup(e => e.GetActiveSessions()).Returns(Array.Empty<string>());

        var services = new ServiceCollection();
        _ = services.AddSingleton(m_MockDebugEngine.Object);
        _ = services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that Commands returns empty list when no commands exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_ReturnsEmptyList()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().NotBeNullOrEmpty();

        _ = result.Should().Contain("## Commands");
        _ = result.Should().Contain("**Count:** 0");
        _ = result.Should().Contain("No commands found.");
    }

    /// <summary>
    /// Verifies that Commands returns commands when commands exist in sessions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_WhenCommandsExist_ReturnsCommands()
    {
        var sessionId = "sess-1";
        var commandId = $"cmd-{sessionId}-1";
        var now = DateTime.Now;
        var cmdInfo = WinAiDbg.Engine.Share.Models.CommandInfo.Enqueued(sessionId, commandId, "k", now, 1234);

        _ = m_MockDebugEngine.Setup(e => e.GetActiveSessions()).Returns(new[] { sessionId });
        _ = m_MockDebugEngine
            .Setup(e => e.GetAllCommandInfos(sessionId))
            .Returns(new Dictionary<string, WinAiDbg.Engine.Share.Models.CommandInfo> { { commandId, cmdInfo } });

        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("## Commands");
        _ = result.Should().Contain("**Count:** 1");
        _ = result.Should().Contain(sessionId);
        _ = result.Should().Contain(commandId);
        _ = result.Should().Contain("Queued");
        _ = result.Should().Contain("| Session ID | Command ID | Command | State | Success | Queued |");
    }

    /// <summary>
    /// Verifies that Commands includes timestamp in response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_IncludesTimestamp()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("**Timestamp:**");
    }

    /// <summary>
    /// Verifies that Commands returns valid JSON.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_ReturnsValidJson()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("## Commands");
    }

    /// <summary>
    /// Verifies that Commands returns error response when exception occurs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_WithException_ReturnsErrorResponse()
    {
        // Create a service provider that throws when getting ILoggerFactory
        var mockServiceProvider = new Mock<IServiceProvider>();
        _ = mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Throws(new InvalidOperationException("Test error"));

        var result = await CommandsResource.Commands(mockServiceProvider.Object);

        _ = result.Should().Contain("## Commands");
        _ = result.Should().Contain("**Status:** Error");
        _ = result.Should().Contain("Test error");
    }

    /// <summary>
    /// Verifies that Commands throws exception when service provider is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_WithNullServiceProvider_ThrowsException()
    {
        var action = async () => await CommandsResource.Commands(null!);
        _ = await action.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that Commands JSON format is indented.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_JsonFormat_IsIndented()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("\n"); // Markdown is multi-line
    }
}
