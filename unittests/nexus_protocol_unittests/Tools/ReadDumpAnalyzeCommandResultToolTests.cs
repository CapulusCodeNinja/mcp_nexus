using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using nexus.engine;
using nexus.engine.Models;
using nexus.protocol.Tools;
using Moq;

namespace nexus.protocol.unittests.Tools;

/// <summary>
/// Unit tests for ReadDumpAnalyzeCommandResultTool class.
/// Tests reading command results with various scenarios and error conditions.
/// </summary>
public class ReadDumpAnalyzeCommandResultToolTests
{
    private readonly Mock<IDebugEngine> m_MockEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the ReadDumpAnalyzeCommandResultToolTests class.
    /// </summary>
    public ReadDumpAnalyzeCommandResultToolTests()
    {
        m_MockEngine = new Mock<IDebugEngine>();
        
        var services = new ServiceCollection();
        services.AddSingleton<IDebugEngine>(m_MockEngine.Object);
        services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that nexus_read_dump_analyze_command_result returns result with completed command.
    /// </summary>
    [Fact]
    public async Task nexus_read_dump_analyze_command_result_WithCompletedCommand_ReturnsResult()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-456";
        var commandInfo = new CommandInfo
        {
            CommandId = commandId,
            Command = "kL",
            State = CommandState.Completed,
            Output = "Stack output",
            IsSuccess = true,
            QueuedTime = DateTime.Now
        };
        
        m_MockEngine.Setup(e => e.GetCommandInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandInfo);

        var result = await ReadDumpAnalyzeCommandResultTool.nexus_read_dump_analyze_command_result(
            m_ServiceProvider, sessionId, commandId);

        dynamic response = result;
        ((string)response.commandId).Should().Be(commandId);
        ((string)response.state).Should().Be("Completed");
        ((string)response.output).Should().Be("Stack output");
        ((bool)response.isSuccess).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that nexus_read_dump_analyze_command_result returns failed result with ArgumentException.
    /// </summary>
    [Fact]
    public async Task nexus_read_dump_analyze_command_result_WithArgumentException_ReturnsFailedResult()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-456";
        
        m_MockEngine.Setup(e => e.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid session"));

        var result = await ReadDumpAnalyzeCommandResultTool.nexus_read_dump_analyze_command_result(
            m_ServiceProvider, sessionId, commandId);

        dynamic response = result;
        ((string)response.state).Should().Be("Failed");
        ((bool)response.isSuccess).Should().BeFalse();
        ((string)response.errorMessage).Should().Be("Invalid session");
    }

    /// <summary>
    /// Verifies that nexus_read_dump_analyze_command_result returns NotFound result with KeyNotFoundException.
    /// </summary>
    [Fact]
    public async Task nexus_read_dump_analyze_command_result_WithKeyNotFoundException_ReturnsNotFoundResult()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-missing";
        
        m_MockEngine.Setup(e => e.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Command not found"));

        var result = await ReadDumpAnalyzeCommandResultTool.nexus_read_dump_analyze_command_result(
            m_ServiceProvider, sessionId, commandId);

        dynamic response = result;
        ((string)response.state).Should().Be("NotFound");
        ((bool)response.isSuccess).Should().BeFalse();
        ((string)response.errorMessage).Should().Be("Command not found");
    }

    /// <summary>
    /// Verifies that nexus_read_dump_analyze_command_result returns failed result with unexpected exception.
    /// </summary>
    [Fact]
    public async Task nexus_read_dump_analyze_command_result_WithUnexpectedException_ReturnsFailedResult()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-456";
        
        m_MockEngine.Setup(e => e.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var result = await ReadDumpAnalyzeCommandResultTool.nexus_read_dump_analyze_command_result(
            m_ServiceProvider, sessionId, commandId);

        dynamic response = result;
        ((string)response.state).Should().Be("Failed");
        ((bool)response.isSuccess).Should().BeFalse();
        ((string)response.errorMessage).Should().Contain("Unexpected error");
    }

    /// <summary>
    /// Verifies that nexus_read_dump_analyze_command_result includes usage field in response.
    /// </summary>
    [Fact]
    public async Task nexus_read_dump_analyze_command_result_IncludesUsageField()
    {
        const string sessionId = "sess-123";
        const string commandId = "cmd-789";
        var commandInfo = new CommandInfo
        {
            CommandId = commandId,
            Command = "lm",
            State = CommandState.Executing,
            QueuedTime = DateTime.Now
        };
        
        m_MockEngine.Setup(e => e.GetCommandInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandInfo);

        var result = await ReadDumpAnalyzeCommandResultTool.nexus_read_dump_analyze_command_result(
            m_ServiceProvider, sessionId, commandId);

        result.Should().NotBeNull();
        var resultType = result.GetType();
        var usageProperty = resultType.GetProperty("usage");
        usageProperty.Should().NotBeNull();
        var usageValue = usageProperty!.GetValue(result);
        usageValue.Should().NotBeNull();
    }
}
