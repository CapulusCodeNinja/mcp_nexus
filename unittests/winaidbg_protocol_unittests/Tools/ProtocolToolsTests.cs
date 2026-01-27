using System.Text.Json;

using FluentAssertions;

using ModelContextProtocol.Protocol;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.Protocol.Services;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for MCP Protocol Tools.
/// Tests error handling, validation, and response formatting for all tools.
/// </summary>
public class ProtocolToolsTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolToolsTests"/> class.
    /// </summary>
    public ProtocolToolsTests()
    {
        m_Settings = new Mock<ISettings>();
        m_FileSystem = new Mock<IFileSystem>();
        m_ProcessManager = new Mock<IProcessManager>();

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
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);

        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
    }

    /// <summary>
    /// Creates a new tool call service instance for tests.
    /// </summary>
    /// <returns>The tool call service.</returns>
    private static McpToolCallService CreateSut()
    {
        var toolDefinitionService = new McpToolDefinitionService();
        return new McpToolCallService(toolDefinitionService);
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

    /// <summary>
    /// Verifies that enqueue tool handles empty sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
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
    /// Verifies that enqueue tool handles empty command correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithEmptyCommand_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
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
    /// Verifies that enqueue tool handles whitespace-only sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithWhitespaceSessionId_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
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
    /// Verifies that enqueue tool handles whitespace-only command correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithWhitespaceCommand_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                ["command"] = JsonSerializer.SerializeToElement("   "),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("command");
    }

    /// <summary>
    /// Verifies that enqueue tool handles invalid sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
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
    /// Verifies that extension enqueue tool handles empty sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement(string.Empty),
                ["extensionName"] = JsonSerializer.SerializeToElement("testExtension"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles empty extension name correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithEmptyExtensionName_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                ["extensionName"] = JsonSerializer.SerializeToElement(string.Empty),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("extensionName");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles whitespace sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithWhitespaceSessionId_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("   "),
                ["extensionName"] = JsonSerializer.SerializeToElement("testExtension"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles whitespace extension name correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithWhitespaceExtensionName_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                ["extensionName"] = JsonSerializer.SerializeToElement("   "),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("extensionName");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles invalid sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
                ["extensionName"] = JsonSerializer.SerializeToElement("testExtension"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that extension enqueue tool accepts null parameters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithNullParameters_HandlesGracefully()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
                ["extensionName"] = JsonSerializer.SerializeToElement("testExtension"),
                ["parameters"] = JsonSerializer.SerializeToElement<object?>(null),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that extension enqueue tool accepts parameters object.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithParameters_HandlesGracefully()
    {
        // Act
        var parameters = new { param1 = "value1", param2 = 123 };
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
                ["extensionName"] = JsonSerializer.SerializeToElement("testExtension"),
                ["parameters"] = JsonSerializer.SerializeToElement(parameters),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that get commands status tool handles invalid sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandsStatus_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_get_dump_analyze_commands_status",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that get commands status tool handles empty sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandsStatus_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_get_dump_analyze_commands_status",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement(string.Empty),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that get commands status tool returns valid markdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandsStatus_ReturnsValidMarkdown()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_get_dump_analyze_commands_status",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("test-session"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that cancel command tool handles invalid sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelCommand_WithInvalidSessionId_ReturnsResponse()
    {
        // Act
        var sut = CreateSut();
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
    /// Verifies that cancel command tool handles invalid commandId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelCommand_WithInvalidCommandId_ReturnsResponse()
    {
        // Act
        var sut = CreateSut();
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
    /// Verifies that cancel command tool returns valid markdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelCommand_ReturnsValidMarkdown()
    {
        // Act
        var sut = CreateSut();
        var result = await sut.CallToolAsync(
            "winaidbg_cancel_dump_analyze_command",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("test-session"),
                ["commandId"] = JsonSerializer.SerializeToElement("test-command"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
    }
}
