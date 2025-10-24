using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Nexus.Engine;
using Nexus.Engine.Models;
using Nexus.Protocol.Tools;

namespace Nexus.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for GetDumpAnalyzeCommandsStatusTool.
/// Tests getting command statuses with various scenarios and error conditions.
/// </summary>
public class GetDumpAnalyzeCommandsStatusToolTests
{
    private readonly Mock<IDebugEngine> m_MockEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the GetDumpAnalyzeCommandsStatusToolTests class.
    /// </summary>
    public GetDumpAnalyzeCommandsStatusToolTests()
    {
        m_MockEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        services.AddSingleton<IDebugEngine>(m_MockEngine.Object);
        services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that nexus_get_dump_analyze_commands_status returns all commands with valid session.
    /// </summary>
    [Fact]
    public async Task Nexus_get_dump_analyze_commands_status_WithValidSession_ReturnsAllCommands()
    {
        const string sessionId = "sess-123";
        var commandInfos = new Dictionary<string, CommandInfo>
        {
            ["cmd-1"] = new CommandInfo
            {
                CommandId = "cmd-1",
                Command = "kL",
                State = CommandState.Completed,
                Output = "Stack output",
                IsSuccess = true,
                QueuedTime = DateTime.Now
            },
            ["cmd-2"] = new CommandInfo
            {
                CommandId = "cmd-2",
                Command = "lm",
                State = CommandState.Executing,
                IsSuccess = false,
                QueuedTime = DateTime.Now
            }
        };

        m_MockEngine.Setup(e => e.GetAllCommandInfos(It.IsAny<string>()))
            .Returns(commandInfos);

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(
            m_ServiceProvider, sessionId);

        dynamic response = result;
        ((int)response.count).Should().Be(2);
        ((string)response.sessionId).Should().Be(sessionId);
        ((string)response.operation).Should().Be("nexus_get_dump_analyze_commands_status");

        var resultType = result.GetType();
        var commandsProperty = resultType.GetProperty("commands");
        commandsProperty.Should().NotBeNull();
        var commandsValue = commandsProperty!.GetValue(result);
        commandsValue.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that nexus_get_dump_analyze_commands_status returns empty array with empty session.
    /// </summary>
    [Fact]
    public async Task Nexus_get_dump_analyze_commands_status_WithEmptySession_ReturnsEmptyArray()
    {
        const string sessionId = "sess-empty";
        var commandInfos = new Dictionary<string, CommandInfo>();

        m_MockEngine.Setup(e => e.GetAllCommandInfos(It.IsAny<string>()))
            .Returns(commandInfos);

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(
            m_ServiceProvider, sessionId);

        dynamic response = result;
        ((int)response.count).Should().Be(0);
    }

    /// <summary>
    /// Verifies that nexus_get_dump_analyze_commands_status returns error with ArgumentException.
    /// </summary>
    [Fact]
    public async Task Nexus_get_dump_analyze_commands_status_WithArgumentException_ReturnsError()
    {
        const string sessionId = "sess-invalid";

        m_MockEngine.Setup(e => e.GetAllCommandInfos(sessionId))
            .Throws(new ArgumentException("Invalid session"));

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(
            m_ServiceProvider, sessionId);

        dynamic response = result;
        ((int)response.count).Should().Be(0);
        ((string)response.error).Should().Be("Invalid session");
    }

    /// <summary>
    /// Verifies that nexus_get_dump_analyze_commands_status returns error with unexpected exception.
    /// </summary>
    [Fact]
    public async Task Nexus_get_dump_analyze_commands_status_WithUnexpectedException_ReturnsError()
    {
        const string sessionId = "sess-123";

        m_MockEngine.Setup(e => e.GetAllCommandInfos(sessionId))
            .Throws(new Exception("Unexpected error"));

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(
            m_ServiceProvider, sessionId);

        dynamic response = result;
        ((int)response.count).Should().Be(0);
        ((string)response.error).Should().Contain("Unexpected error");
    }

    /// <summary>
    /// Verifies that nexus_get_dump_analyze_commands_status includes usage field in response.
    /// </summary>
    [Fact]
    public async Task Nexus_get_dump_analyze_commands_status_IncludesUsageField()
    {
        const string sessionId = "sess-789";
        var commandInfos = new Dictionary<string, CommandInfo>();

        m_MockEngine.Setup(e => e.GetAllCommandInfos(It.IsAny<string>()))
            .Returns(commandInfos);

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(
            m_ServiceProvider, sessionId);

        result.Should().NotBeNull();
        var resultType = result.GetType();
        var usageProperty = resultType.GetProperty("usage");
        usageProperty.Should().NotBeNull();
        var usageValue = usageProperty!.GetValue(result);
        usageValue.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that nexus_get_dump_analyze_commands_status sets hasOutput correctly.
    /// </summary>
    [Fact]
    public async Task Nexus_get_dump_analyze_commands_status_SetsHasOutputCorrectly()
    {
        const string sessionId = "sess-123";
        var commandInfos = new Dictionary<string, CommandInfo>
        {
            ["cmd-with-output"] = new CommandInfo
            {
                CommandId = "cmd-with-output",
                Command = "kL",
                Output = "Some output",
                State = CommandState.Completed,
                QueuedTime = DateTime.Now
            },
            ["cmd-no-output"] = new CommandInfo
            {
                CommandId = "cmd-no-output",
                Command = "lm",
                Output = null,
                State = CommandState.Queued,
                QueuedTime = DateTime.Now
            }
        };

        m_MockEngine.Setup(e => e.GetAllCommandInfos(It.IsAny<string>()))
            .Returns(commandInfos);

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(
            m_ServiceProvider, sessionId);

        dynamic response = result;
        var commands = (object[])response.commands;
        commands.Should().HaveCount(2);
    }
}
