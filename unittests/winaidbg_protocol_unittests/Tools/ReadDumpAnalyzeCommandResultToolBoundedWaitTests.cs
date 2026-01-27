using System.Reflection;

using FluentAssertions;

using Moq;

using WinAiDbg.Engine.Share;
using WinAiDbg.Engine.Share.Models;
using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Tools;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for bounded-wait behavior in <see cref="ReadDumpAnalyzeCommandResultTool"/>.
/// </summary>
[Collection("EngineService")]
public class ReadDumpAnalyzeCommandResultToolBoundedWaitTests
{
    /// <summary>
    /// Verifies that when the wait budget expires, the tool returns the current command state rather than blocking indefinitely.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_CommandStillRunning_ReturnsCurrentStateWithNote()
    {
        // Arrange
        EngineService.Shutdown();

        const string sessionId = "sess-test";
        const string commandId = "cmd-sess-test-1";

        var tcs = new TaskCompletionSource<CommandInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var engine = new Mock<IDebugEngine>(MockBehavior.Strict);

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

    /// <summary>
    /// Verifies that when the command completes within the wait budget, the tool returns the completed output and does not emit the in-progress note.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_CommandCompletedWithinWait_ReturnsOutputWithoutNote()
    {
        // Arrange
        EngineService.Shutdown();

        const string sessionId = "sess-test";
        const string commandId = "cmd-sess-test-2";

        var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
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
}

