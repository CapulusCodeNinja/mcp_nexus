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
/// Unit tests for <see cref="EnqueueAsyncDumpAnalyzeCommandTool"/>.
/// </summary>
[Collection("EngineService")]
public class EnqueueAsyncDumpAnalyzeCommandToolTests
{
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
            ["random"] = JsonSerializer.SerializeToElement(new { x = 1 }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_dump_analyze_command",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Missing required parameter(s)");
        _ = GetText(result).Should().Contain("sessionId");
        _ = GetText(result).Should().Contain("command");
    }

    /// <summary>
    /// Verifies wrong-typed known argument values are rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithWrongTypedCommand_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            ["command"] = JsonSerializer.SerializeToElement(new[] { "k", "!analyze -v" }),
            ["randomFlag"] = JsonSerializer.SerializeToElement(true),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_dump_analyze_command",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Invalid type for parameter `command`");
    }

    /// <summary>
    /// Verifies whitespace-only command is rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithWhitespaceCommand_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            ["command"] = JsonSerializer.SerializeToElement("   "),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_dump_analyze_command",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Invalid `command`");
    }

    /// <summary>
    /// Verifies that empty sessionId is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithEmptySessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement(string.Empty),
                ["command"] = JsonSerializer.SerializeToElement("test command"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that empty command is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithEmptyCommand_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                ["command"] = JsonSerializer.SerializeToElement(string.Empty),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("command");
    }

    /// <summary>
    /// Verifies that whitespace-only sessionId is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithWhitespaceSessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("   "),
                ["command"] = JsonSerializer.SerializeToElement("test command"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that invalid sessionId is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithInvalidSessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
                ["command"] = JsonSerializer.SerializeToElement("test command"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that a valid enqueue request returns a success markdown payload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithValidInputs_ReturnsSuccessMarkdown()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            const string command = "!analyze -v";
            const string commandId = "cmd-123";

            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Active);
            _ = engine.Setup(e => e.EnqueueCommand(sessionId, command)).Returns(commandId);

            SetEngineForTesting(engine.Object);

            var sut = new EnqueueAsyncDumpAnalyzeCommandTool();

            // Act
            var result = await sut.Execute(sessionId, command);
            var markdown = result.ToString() ?? string.Empty;

            // Assert
            _ = markdown.Should().Contain("## Command Enqueued");
            _ = markdown.Should().Contain(commandId);
            _ = markdown.Should().Contain(sessionId);

            engine.Verify(e => e.GetSessionState(sessionId), Times.Once);
            engine.Verify(e => e.EnqueueCommand(sessionId, command), Times.Once);
        }
        finally
        {
            EngineService.Shutdown();
        }
    }

    /// <summary>
    /// Verifies that empty commands are rejected as actionable user input errors.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithWhitespaceCommand_ThrowsMcpToolUserInputException()
    {
        // Arrange
        var sut = new EnqueueAsyncDumpAnalyzeCommandTool();

        // Act
        var act = async () => await sut.Execute("sess-test", "   ");

        // Assert
        _ = await act.Should()
            .ThrowAsync<McpToolUserInputException>()
            .WithMessage("*Invalid `command`*");
    }

    /// <summary>
    /// Verifies that inactive sessions are rejected as actionable user input errors.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithInactiveSession_ThrowsMcpToolUserInputException()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Closed);

            SetEngineForTesting(engine.Object);

            var sut = new EnqueueAsyncDumpAnalyzeCommandTool();

            // Act
            var act = async () => await sut.Execute(sessionId, "k");

            // Assert
            _ = await act.Should()
                .ThrowAsync<McpToolUserInputException>()
                .WithMessage("*not active*");
        }
        finally
        {
            EngineService.Shutdown();
        }
    }

    /// <summary>
    /// Verifies that engine failures are wrapped into actionable user input errors.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WhenEngineThrowsInvalidOperation_ThrowsMcpToolUserInputException()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            const string command = ".reload /f";

            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Active);
            _ = engine.Setup(e => e.EnqueueCommand(sessionId, command)).Throws(new InvalidOperationException("engine busy"));

            SetEngineForTesting(engine.Object);

            var sut = new EnqueueAsyncDumpAnalyzeCommandTool();

            // Act
            var act = async () => await sut.Execute(sessionId, command);

            // Assert
            _ = await act.Should()
                .ThrowAsync<McpToolUserInputException>()
                .WithMessage("*Cannot enqueue command*");
        }
        finally
        {
            EngineService.Shutdown();
        }
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

