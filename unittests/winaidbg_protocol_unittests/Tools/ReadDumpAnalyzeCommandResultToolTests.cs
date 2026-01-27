using System.Text.Json;
using System.Reflection;

using FluentAssertions;

using ModelContextProtocol.Protocol;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.Engine.Share;
using WinAiDbg.Engine.Share.Models;
using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Tools;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for <see cref="ReadDumpAnalyzeCommandResultTool"/>.
/// </summary>
[Collection("EngineService")]
public class ReadDumpAnalyzeCommandResultToolTests
{
    /// <summary>
    /// Verifies that when the wait budget expires, the tool returns the current command state rather than blocking indefinitely.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_CommandStillRunning_ReturnsCurrentStateWithNote()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            const string commandId = "cmd-sess-test-1";

            var tcs = new TaskCompletionSource<CommandInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);

            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Active);

            _ = engine.Setup(e => e.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()))
                .Returns<string, string, CancellationToken>((_, _, ct) => tcs.Task.WaitAsync(ct));

            var now = DateTime.Now;
            var current = CommandInfo.Executing(sessionId, commandId, ".reload /f", now, now, processId: 123);

            _ = engine.Setup(e => e.GetCommandInfo(sessionId, commandId)).Returns(current);

            SetEngineForTesting(engine.Object);

            var sut = new ReadDumpAnalyzeCommandResultTool();

            // Act
            var result = await sut.Execute(sessionId, commandId, maxWaitSeconds: 1);
            var markdown = result.ToString() ?? string.Empty;

            // Assert
            _ = markdown.Should().Contain("## Command Result");
            _ = markdown.Should().Contain("**State:** Executing");
            _ = markdown.Should().Contain("Command `cmd-sess-test-1` is not finished yet");
            _ = markdown.Should().Contain("waited up to 1 seconds");

            engine.Verify(e => e.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()), Times.Once);
            engine.Verify(e => e.GetCommandInfo(sessionId, commandId), Times.Once);
        }
        finally
        {
            EngineService.Shutdown();
        }
    }

    /// <summary>
    /// Verifies that when the command completes within the wait budget, the tool returns the completed output and does not emit the in-progress note.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_CommandCompletedWithinWait_ReturnsOutputWithoutNote()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            const string commandId = "cmd-sess-test-2";

            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Active);

            var queued = DateTime.Now.AddSeconds(-2);
            var start = DateTime.Now.AddSeconds(-1);
            var end = DateTime.Now;
            var completed = CommandInfo.Completed(sessionId, commandId, "!analyze -v", queued, start, end, "OK", string.Empty, processId: 456);

            _ = engine.Setup(e => e.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(completed);

            SetEngineForTesting(engine.Object);

            var sut = new ReadDumpAnalyzeCommandResultTool();

            // Act
            var result = await sut.Execute(sessionId, commandId, maxWaitSeconds: 1);
            var markdown = result.ToString() ?? string.Empty;

            // Assert
            _ = markdown.Should().Contain("## Command Result");
            _ = markdown.Should().Contain("**State:** Completed");
            _ = markdown.Should().Contain("### Output");
            _ = markdown.Should().Contain("OK");
            _ = markdown.Should().NotContain("is not finished yet");

            engine.Verify(e => e.GetCommandInfoAsync(sessionId, commandId, It.IsAny<CancellationToken>()), Times.Once);
            engine.Verify(e => e.GetCommandInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        finally
        {
            EngineService.Shutdown();
        }
    }

    /// <summary>
    /// Verifies unexpected/extra arguments do not crash invocation and missing required args are reported.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithUnexpectedArgumentsAndMissingRequired_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["random"] = JsonSerializer.SerializeToElement(123),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_read_dump_analyze_command_result",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Missing required parameter(s)");
        _ = GetText(result).Should().Contain("sessionId");
        _ = GetText(result).Should().Contain("commandId");
        _ = GetText(result).Should().Contain("maxWaitSeconds");
    }

    /// <summary>
    /// Verifies wrong-typed known argument values are rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithWrongTypedCommandId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            ["commandId"] = JsonSerializer.SerializeToElement(new { id = "cmd-123" }),
            ["maxWaitSeconds"] = JsonSerializer.SerializeToElement(30),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_read_dump_analyze_command_result",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Invalid type for parameter `commandId`");
    }

    /// <summary>
    /// Verifies a "smart" but invalid string input still produces a descriptive error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithNullStringCommandId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            ["commandId"] = JsonSerializer.SerializeToElement("null"),
            ["maxWaitSeconds"] = JsonSerializer.SerializeToElement(30),
            ["random"] = JsonSerializer.SerializeToElement(new[] { 1, 2, 3 }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_read_dump_analyze_command_result",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Invalid `sessionId`");
    }

    /// <summary>
    /// Replaces the <see cref="EngineService"/> singleton instance for tests using reflection.
    /// </summary>
    /// <param name="engine">The engine instance to set.</param>
    private static void SetEngineForTesting(IDebugEngine engine)
    {
        var field = typeof(EngineService).GetField("m_DebugEngine", BindingFlags.NonPublic | BindingFlags.Static);
        _ = field.Should().NotBeNull();
        field!.SetValue(null, engine);
    }

    /// <summary>
    /// Creates a tool call service instance with engine initialized.
    /// </summary>
    /// <returns>The tool call service.</returns>
    private static McpToolCallService CreateToolCallService()
    {
        var settings = new Mock<ISettings>();
        var fileSystem = new Mock<IFileSystem>();
        var processManager = new Mock<IProcessManager>();

        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };

        _ = settings.Setup(s => s.Get()).Returns(sharedConfig);
        EngineService.Initialize(fileSystem.Object, processManager.Object, settings.Object);
        return new McpToolCallService(new McpToolDefinitionService());
    }

    /// <summary>
    /// Extracts the first text content block from a tool call result.
    /// </summary>
    /// <param name="result">The call result.</param>
    /// <returns>The text content.</returns>
    private static string GetText(CallToolResult result)
    {
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        return textBlock.Text;
    }
}

