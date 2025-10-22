using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using mcp_nexus.Engine;
using mcp_nexus.Engine.Models;
using mcp_nexus.Protocol.Tools;

namespace mcp_nexus.Protocol.Tests.Tools;

/// <summary>
/// Unit tests for MCP tools.
/// Tests all MCP tools with mocked IDebugEngine dependencies.
/// </summary>
public class McpNexusToolsTests
{
    private readonly Mock<IDebugEngine> m_MockDebugEngine;
    private readonly IServiceProvider m_ServiceProvider;

    public McpNexusToolsTests()
    {
        m_MockDebugEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(m_MockDebugEngine.Object);
        m_ServiceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task nexus_open_dump_analyze_session_Success_ReturnsSessionId()
    {
        var dumpPath = Path.Combine(Path.GetTempPath(), "test.dmp");
        var expectedSessionId = "sess-00001-abc123";

        m_MockDebugEngine
            .Setup(x => x.CreateSessionAsync(dumpPath, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSessionId);

        File.WriteAllText(dumpPath, "dummy");
        try
        {
            var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath, null);

            result.Should().NotBeNull();
            var resultDict = result as dynamic;
            ((string)resultDict.sessionId).Should().Be(expectedSessionId);
            ((string)resultDict.status).Should().Be("Success");
            ((string)resultDict.operation).Should().Be("nexus_open_dump_analyze_session");
        }
        finally
        {
            if (File.Exists(dumpPath)) File.Delete(dumpPath);
        }
    }

    [Fact]
    public async Task nexus_open_dump_analyze_session_FileNotFound_ReturnsFailure()
    {
        var dumpPath = "C:\\nonexistent\\test.dmp";

        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath, null);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.status).Should().Be("Failed");
        ((string)resultDict.sessionId).Should().BeNull();
    }

    [Fact]
    public async Task nexus_open_dump_analyze_session_EngineThrowsInvalidOperation_ReturnsFailure()
    {
        var dumpPath = Path.Combine(Path.GetTempPath(), "test2.dmp");

        m_MockDebugEngine
            .Setup(x => x.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Maximum sessions exceeded"));

        File.WriteAllText(dumpPath, "dummy");
        try
        {
            var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath, null);

            result.Should().NotBeNull();
            var resultDict = result as dynamic;
            ((string)resultDict.status).Should().Be("Failed");
            ((string)resultDict.sessionId).Should().BeNull();
            ((string)resultDict.message).Should().Contain("Maximum sessions exceeded");
        }
        finally
        {
            if (File.Exists(dumpPath)) File.Delete(dumpPath);
        }
    }

    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_Success_ReturnsCommandId()
    {
        var sessionId = "sess-00001-abc123";
        var command = "k";
        var expectedCommandId = "cmd-123";

        m_MockDebugEngine
            .Setup(x => x.EnqueueCommand(sessionId, command))
            .Returns(expectedCommandId);

        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(m_ServiceProvider, sessionId, command);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.commandId).Should().Be(expectedCommandId);
        ((string)resultDict.status).Should().Be("Queued");
        ((string)resultDict.sessionId).Should().Be(sessionId);
    }

    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_EmptySessionId_ReturnsFailure()
    {
        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(m_ServiceProvider, "", "k");

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.status).Should().Be("Failed");
        ((string)resultDict.commandId).Should().BeNull();
    }

    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_EmptyCommand_ReturnsFailure()
    {
        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(m_ServiceProvider, "sess-123", "");

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.status).Should().Be("Failed");
        ((string)resultDict.commandId).Should().BeNull();
    }

    [Fact]
    public async Task nexus_enqueue_async_dump_analyze_command_EngineThrowsInvalidOperation_ReturnsFailure()
    {
        m_MockDebugEngine
            .Setup(x => x.EnqueueCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Session not found"));

        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command(m_ServiceProvider, "sess-999", "k");

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.status).Should().Be("Failed");
    }

    [Fact]
    public async Task nexus_read_dump_analyze_command_result_CompletedCommand_ReturnsResult()
    {
        var sessionId = "sess-00001";
        var commandId = "cmd-123";
        var commandInfo = CommandInfo.Completed(
            commandId, "k", DateTime.Now.AddSeconds(-10), DateTime.Now.AddSeconds(-5),
            DateTime.Now, "Stack trace output", true);

        m_MockDebugEngine
            .Setup(x => x.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandInfo);

        var result = await ReadDumpAnalyzeCommandResultTool.nexus_read_dump_analyze_command_result(m_ServiceProvider, sessionId, commandId);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.commandId).Should().Be(commandId);
        ((string)resultDict.state).Should().Be("Completed");
        ((string)resultDict.output).Should().Be("Stack trace output");
        ((bool)resultDict.isSuccess).Should().BeTrue();
    }

    [Fact]
    public async Task nexus_read_dump_analyze_command_result_ExecutingCommand_ReturnsExecuting()
    {
        var sessionId = "sess-00001";
        var commandId = "cmd-123";
        var commandInfo = CommandInfo.Executing(commandId, "!analyze -v", DateTime.Now.AddSeconds(-30), DateTime.Now.AddSeconds(-25));

        m_MockDebugEngine
            .Setup(x => x.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandInfo);

        var result = await ReadDumpAnalyzeCommandResultTool.nexus_read_dump_analyze_command_result(m_ServiceProvider, sessionId, commandId);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.state).Should().Be("Executing");
    }

    [Fact]
    public async Task nexus_read_dump_analyze_command_result_CommandNotFound_ReturnsNotFound()
    {
        m_MockDebugEngine
            .Setup(x => x.GetCommandInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Command not found"));

        var result = await ReadDumpAnalyzeCommandResultTool.nexus_read_dump_analyze_command_result(m_ServiceProvider, "sess-999", "cmd-999");

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.state).Should().Be("NotFound");
    }

    [Fact]
    public async Task nexus_get_dump_analyze_commands_status_MultipleCommands_ReturnsAllStatuses()
    {
        var sessionId = "sess-00001";
        var commands = new Dictionary<string, CommandInfo>
        {
            ["cmd-1"] = CommandInfo.Queued("cmd-1", "k", DateTime.Now),
            ["cmd-2"] = CommandInfo.Executing("cmd-2", "lm", DateTime.Now.AddSeconds(-10), DateTime.Now.AddSeconds(-5)),
            ["cmd-3"] = CommandInfo.Completed("cmd-3", "!peb", DateTime.Now.AddSeconds(-20), DateTime.Now.AddSeconds(-15), DateTime.Now, "PEB output", true)
        };

        m_MockDebugEngine
            .Setup(x => x.GetAllCommandInfos(sessionId))
            .Returns(commands);

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(m_ServiceProvider, sessionId);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((int)resultDict.count).Should().Be(3);
        ((string)resultDict.sessionId).Should().Be(sessionId);
    }

    [Fact]
    public async Task nexus_get_dump_analyze_commands_status_EmptySession_ReturnsEmptyList()
    {
        var sessionId = "sess-00001";

        m_MockDebugEngine
            .Setup(x => x.GetAllCommandInfos(sessionId))
            .Returns(new Dictionary<string, CommandInfo>());

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(m_ServiceProvider, sessionId);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((int)resultDict.count).Should().Be(0);
    }

    [Fact]
    public async Task nexus_get_dump_analyze_commands_status_InvalidSession_ReturnsError()
    {
        m_MockDebugEngine
            .Setup(x => x.GetAllCommandInfos(It.IsAny<string>()))
            .Throws(new ArgumentException("Invalid session"));

        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status(m_ServiceProvider, "invalid");

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((int)resultDict.count).Should().Be(0);
    }

    [Fact]
    public async Task nexus_close_dump_analyze_session_Success_ReturnsSuccess()
    {
        var sessionId = "sess-00001";

        m_MockDebugEngine
            .Setup(x => x.CloseSessionAsync(sessionId))
            .Returns(Task.CompletedTask);

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, sessionId);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.status).Should().Be("Success");
        ((string)resultDict.sessionId).Should().Be(sessionId);
    }

    [Fact]
    public async Task nexus_close_dump_analyze_session_InvalidSession_ReturnsFailure()
    {
        m_MockDebugEngine
            .Setup(x => x.CloseSessionAsync(It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Session not found"));

        var result = await CloseDumpAnalyzeSessionTool.nexus_close_dump_analyze_session(m_ServiceProvider, "invalid");

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.status).Should().Be("Failed");
    }

    [Fact]
    public async Task nexus_cancel_command_Success_ReturnsCancelled()
    {
        var sessionId = "sess-00001";
        var commandId = "cmd-123";

        m_MockDebugEngine
            .Setup(x => x.CancelCommand(sessionId, commandId))
            .Returns(true);

        var result = await CancelCommandTool.nexus_cancel_command(m_ServiceProvider, sessionId, commandId);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((bool)resultDict.cancelled).Should().BeTrue();
        ((string)resultDict.status).Should().Be("Cancelled");
    }

    [Fact]
    public async Task nexus_cancel_command_CommandNotFound_ReturnsNotFound()
    {
        var sessionId = "sess-00001";
        var commandId = "cmd-999";

        m_MockDebugEngine
            .Setup(x => x.CancelCommand(sessionId, commandId))
            .Returns(false);

        var result = await CancelCommandTool.nexus_cancel_command(m_ServiceProvider, sessionId, commandId);

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((bool)resultDict.cancelled).Should().BeFalse();
        ((string)resultDict.status).Should().Be("NotFound");
    }

    [Fact]
    public async Task nexus_cancel_command_InvalidArgument_ReturnsFailure()
    {
        m_MockDebugEngine
            .Setup(x => x.CancelCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new ArgumentException("Invalid command ID"));

        var result = await CancelCommandTool.nexus_cancel_command(m_ServiceProvider, "sess-00001", "invalid");

        result.Should().NotBeNull();
        var resultDict = result as dynamic;
        ((string)resultDict.status).Should().Be("Failed");
    }
}

