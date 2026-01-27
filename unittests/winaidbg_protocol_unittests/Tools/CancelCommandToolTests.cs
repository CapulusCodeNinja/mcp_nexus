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
/// Unit tests for <see cref="CancelCommandTool"/>.
/// </summary>
[Collection("EngineService")]
public class CancelCommandToolTests
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

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_cancel_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["randomObject"] = JsonSerializer.SerializeToElement(new { a = 1 }),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Missing required parameter(s)");
        _ = GetText(result).Should().Contain("sessionId");
        _ = GetText(result).Should().Contain("commandId");
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

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_cancel_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                ["commandId"] = JsonSerializer.SerializeToElement(new[] { "cmd-1" }),
                ["random"] = JsonSerializer.SerializeToElement(true),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Invalid type for parameter `commandId`");
    }

    /// <summary>
    /// Verifies invalid sessionId is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Arrange
        var sut = CreateToolCallService();

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_cancel_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
                ["commandId"] = JsonSerializer.SerializeToElement("cmd-123"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies invalid commandId is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithInvalidCommandId_ReturnsErrorResponse()
    {
        // Arrange
        var sut = CreateToolCallService();

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_cancel_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                ["commandId"] = JsonSerializer.SerializeToElement("invalid-cmd-999"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a successful cancel returns a success markdown payload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WhenCancelled_ReturnsSuccessMarkdown()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            const string commandId = "cmd-1";

            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Active);
            _ = engine.Setup(e => e.CancelCommand(sessionId, commandId)).Returns(true);

            SetEngineForTesting(engine.Object);

            var sut = new CancelCommandTool();

            // Act
            var result = await sut.Execute(sessionId, commandId);
            var markdown = result.ToString() ?? string.Empty;

            // Assert
            _ = markdown.Should().Contain("## Command Cancellation");
            _ = markdown.Should().Contain(sessionId);
            _ = markdown.Should().Contain(commandId);
            _ = markdown.Should().Contain("Cancelled");

            engine.Verify(e => e.GetSessionState(sessionId), Times.Once);
            engine.Verify(e => e.CancelCommand(sessionId, commandId), Times.Once);
        }
        finally
        {
            EngineService.Shutdown();
        }
    }

    /// <summary>
    /// Verifies that attempting to cancel an unknown command returns an actionable user input error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WhenCommandNotFound_ThrowsMcpToolUserInputException()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            const string commandId = "cmd-unknown";

            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Active);
            _ = engine.Setup(e => e.CancelCommand(sessionId, commandId)).Returns(false);

            SetEngineForTesting(engine.Object);

            var sut = new CancelCommandTool();

            // Act
            var act = async () => await sut.Execute(sessionId, commandId);

            // Assert
            _ = await act.Should()
                .ThrowAsync<McpToolUserInputException>()
                .WithMessage("*not found*");
        }
        finally
        {
            EngineService.Shutdown();
        }
    }

    /// <summary>
    /// Verifies that invalid arguments are rejected as actionable user input errors.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithWhitespaceCommandId_ThrowsMcpToolUserInputException()
    {
        // Arrange
        var sut = new CancelCommandTool();

        // Act
        var act = async () => await sut.Execute("sess-test", "   ");

        // Assert
        _ = await act.Should()
            .ThrowAsync<McpToolUserInputException>()
            .WithMessage("*Invalid `commandId`*");
    }

    /// <summary>
    /// Verifies that argument exceptions from the engine are wrapped into actionable user input errors.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WhenEngineThrowsArgumentException_ThrowsMcpToolUserInputException()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            const string commandId = "cmd-1";

            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Active);
            _ = engine.Setup(e => e.CancelCommand(sessionId, commandId)).Throws(new ArgumentException("bad command id"));

            SetEngineForTesting(engine.Object);

            var sut = new CancelCommandTool();

            // Act
            var act = async () => await sut.Execute(sessionId, commandId);

            // Assert
            _ = await act.Should()
                .ThrowAsync<McpToolUserInputException>()
                .WithMessage("*Invalid argument*");
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

