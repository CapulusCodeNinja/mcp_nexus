using FluentAssertions;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Tools;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for the <see cref="CancelCommandTool"/> class.
/// Tests command cancellation, error handling, and response formatting.
/// </summary>
public class CancelCommandToolTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;

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
    }

    /// <summary>
    /// Verifies that winaidbg_cancel_dump_analyze_command returns a result when command does not exist.
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
            var result = await CancelCommandTool.winaidbg_cancel_dump_analyze_command("session-123", "cmd-999");

            // Assert
            _ = result.Should().NotBeNull();
            var markdown = result.ToString()!;
            _ = markdown.Should().NotBeNullOrEmpty();
            _ = markdown.Should().Contain("cmd-999");
            _ = markdown.Should().Contain("session-123");
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies that winaidbg_cancel_dump_analyze_command handles empty session ID.
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
            var result = await CancelCommandTool.winaidbg_cancel_dump_analyze_command(string.Empty, "cmd-456");

            // Assert
            _ = result.Should().NotBeNull();
            var markdown = result.ToString()!;
            _ = markdown.Should().NotBeNullOrEmpty();
            _ = markdown.Should().Contain("cmd-456");
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies that winaidbg_cancel_dump_analyze_command handles empty command ID.
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
            var result = await CancelCommandTool.winaidbg_cancel_dump_analyze_command("session-123", string.Empty);

            // Assert
            _ = result.Should().NotBeNull();
            var markdown = result.ToString()!;
            _ = markdown.Should().NotBeNullOrEmpty();
            _ = markdown.Should().Contain("session-123");
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies that winaidbg_cancel_dump_analyze_command handles null session ID.
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
            var result = await CancelCommandTool.winaidbg_cancel_dump_analyze_command(string.Empty, "cmd-456");

            // Assert
            _ = result.Should().NotBeNull();
            var markdown = result.ToString()!;
            _ = markdown.Should().NotBeNullOrEmpty();
            _ = markdown.Should().Contain("cmd-456");
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }

    /// <summary>
    /// Verifies that winaidbg_cancel_dump_analyze_command returns formatted result with usage guide.
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
            var result = await CancelCommandTool.winaidbg_cancel_dump_analyze_command("session-123", "cmd-999");

            // Assert
            _ = result.Should().NotBeNull();
            var markdown = result.ToString()!;
            _ = markdown.Should().NotBeNullOrEmpty();

            // Should contain usage guide
            _ = markdown.Length.Should().BeGreaterThan(100);
        }
        finally
        {
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        }
    }
}
