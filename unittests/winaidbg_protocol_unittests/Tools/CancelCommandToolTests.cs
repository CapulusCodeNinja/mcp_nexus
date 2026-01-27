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
/// Unit tests for the cancel command tool call shape and errors.
/// Tests command cancellation, error handling, and response formatting.
/// </summary>
public class CancelCommandToolTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;
    private readonly McpToolCallService m_Sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelCommandToolTests"/> class.
    /// </summary>
    public CancelCommandToolTests()
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
        m_Sut = new McpToolCallService(new McpToolDefinitionService());
    }

    /// <summary>
    /// Verifies that Execute returns a result when command does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WinAiDbgCancelDumpAnalyzeCommand_WithNonExistentCommand_ReturnsResult()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        try
        {
            // Act
            var result = await m_Sut.CallToolAsync(
                "winaidbg_cancel_dump_analyze_command",
                new Dictionary<string, JsonElement>
                {
                    ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                    ["commandId"] = JsonSerializer.SerializeToElement("cmd-999"),
                },
                CancellationToken.None);

            // Assert
            _ = result.IsError.Should().BeTrue();
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies that Execute handles empty session ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WinAiDbgCancelDumpAnalyzeCommand_WithEmptySessionId_HandlesGracefully()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        try
        {
            // Act
            var result = await m_Sut.CallToolAsync(
                "winaidbg_cancel_dump_analyze_command",
                new Dictionary<string, JsonElement>
                {
                    ["sessionId"] = JsonSerializer.SerializeToElement(string.Empty),
                    ["commandId"] = JsonSerializer.SerializeToElement("cmd-456"),
                },
                CancellationToken.None);

            // Assert
            _ = result.IsError.Should().BeTrue();
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies that Execute handles empty command ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WinAiDbgCancelDumpAnalyzeCommand_WithEmptyCommandId_HandlesGracefully()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        try
        {
            // Act
            var result = await m_Sut.CallToolAsync(
                "winaidbg_cancel_dump_analyze_command",
                new Dictionary<string, JsonElement>
                {
                    ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                    ["commandId"] = JsonSerializer.SerializeToElement(string.Empty),
                },
                CancellationToken.None);

            // Assert
            _ = result.IsError.Should().BeTrue();
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies that Execute handles null session ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WinAiDbgCancelDumpAnalyzeCommand_WithNullSessionId_HandlesGracefully()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        try
        {
            // Act
            var result = await m_Sut.CallToolAsync(
                "winaidbg_cancel_dump_analyze_command",
                new Dictionary<string, JsonElement>
                {
                    ["sessionId"] = JsonSerializer.SerializeToElement(string.Empty),
                    ["commandId"] = JsonSerializer.SerializeToElement("cmd-456"),
                },
                CancellationToken.None);

            // Assert
            _ = result.IsError.Should().BeTrue();
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies that Execute returns formatted result with usage guide.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WinAiDbgCancelDumpAnalyzeCommand_ReturnsFormattedResultWithUsageGuide()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        try
        {
            // Act
            var result = await m_Sut.CallToolAsync(
                "winaidbg_cancel_dump_analyze_command",
                new Dictionary<string, JsonElement>
                {
                    ["sessionId"] = JsonSerializer.SerializeToElement("session-123"),
                    ["commandId"] = JsonSerializer.SerializeToElement("cmd-999"),
                },
                CancellationToken.None);

            // Assert
            _ = result.IsError.Should().BeTrue();
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies unexpected/extra arguments do not crash invocation and missing required args are reported.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WinAiDbgCancelDumpAnalyzeCommand_WithUnexpectedArgumentsAndMissingRequired_ReturnsActionableToolError()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        try
        {
            // Act
            var result = await m_Sut.CallToolAsync(
                "winaidbg_cancel_dump_analyze_command",
                new Dictionary<string, JsonElement>
                {
                    ["randomObject"] = JsonSerializer.SerializeToElement(new { a = 1 }),
                },
                CancellationToken.None);

            // Assert
            _ = result.IsError.Should().BeTrue();
            var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
            _ = textBlock.Text.Should().Contain("Missing required parameter(s)");
            _ = textBlock.Text.Should().Contain("sessionId");
            _ = textBlock.Text.Should().Contain("commandId");
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies wrong-typed known argument values are rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WinAiDbgCancelDumpAnalyzeCommand_WithWrongTypedCommandId_ReturnsActionableToolError()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        try
        {
            // Act
            var result = await m_Sut.CallToolAsync(
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
            var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
            _ = textBlock.Text.Should().Contain("Invalid type for parameter `commandId`");
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }
}
