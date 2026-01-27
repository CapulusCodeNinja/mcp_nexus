using FluentAssertions;

using WinAiDbg.Protocol.Tools;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for MCP Protocol Tools.
/// Tests error handling, validation, and response formatting for all tools.
/// </summary>
public class ProtocolToolsTests
{
    /// <summary>
    /// Verifies that enqueue tool handles empty sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncDumpAnalyzeCommandTool().Execute(string.Empty, "test command");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
        _ = markdown.Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that enqueue tool handles empty command correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithEmptyCommand_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncDumpAnalyzeCommandTool().Execute("session-123", string.Empty);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
        _ = markdown.Should().Contain("command");
    }

    /// <summary>
    /// Verifies that enqueue tool handles whitespace-only sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithWhitespaceSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncDumpAnalyzeCommandTool().Execute("   ", "test command");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
    }

    /// <summary>
    /// Verifies that enqueue tool handles whitespace-only command correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithWhitespaceCommand_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncDumpAnalyzeCommandTool().Execute("session-123", "   ");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
    }

    /// <summary>
    /// Verifies that enqueue tool handles invalid sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncDumpAnalyzeCommandTool().Execute("invalid-session-999", "test command");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();

        // Should contain error indicator or failure status
        _ = (markdown.Contains("Failed") || markdown.Contains("Error") || markdown.Contains("❌")).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles empty sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncExtensionCommandTool().Execute(string.Empty, "testExtension", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
        _ = markdown.Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles empty extension name correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithEmptyExtensionName_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncExtensionCommandTool().Execute("session-123", string.Empty, null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
        _ = markdown.Should().Contain("extensionName");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles whitespace sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithWhitespaceSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncExtensionCommandTool().Execute("   ", "testExtension", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles whitespace extension name correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithWhitespaceExtensionName_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncExtensionCommandTool().Execute("session-123", "   ", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles invalid sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await new EnqueueAsyncExtensionCommandTool().Execute("invalid-session-999", "testExtension", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = (markdown.Contains("Failed") || markdown.Contains("Error") || markdown.Contains("❌")).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that extension enqueue tool accepts null parameters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task EnqueueExtensionCommand_WithNullParameters_HandlesGracefully()
    {
        // Act
        var result = await new EnqueueAsyncExtensionCommandTool().Execute("invalid-session-999", "testExtension", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
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
        var result = await new EnqueueAsyncExtensionCommandTool().Execute("invalid-session-999", "testExtension", parameters);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that get commands status tool handles invalid sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandsStatus_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await new GetDumpAnalyzeCommandsStatusTool().Execute("invalid-session-999");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Command Status Summary");
    }

    /// <summary>
    /// Verifies that get commands status tool handles empty sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandsStatus_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await new GetDumpAnalyzeCommandsStatusTool().Execute(string.Empty);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that get commands status tool returns valid markdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandsStatus_ReturnsValidMarkdown()
    {
        // Act
        var result = await new GetDumpAnalyzeCommandsStatusTool().Execute("test-session");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("##");
    }

    /// <summary>
    /// Verifies that cancel command tool handles invalid sessionId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelCommand_WithInvalidSessionId_ReturnsResponse()
    {
        // Act
        var result = await new CancelCommandTool().Execute("invalid-session-999", "cmd-123");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Cancellation");
    }

    /// <summary>
    /// Verifies that cancel command tool handles invalid commandId correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelCommand_WithInvalidCommandId_ReturnsResponse()
    {
        // Act
        var result = await new CancelCommandTool().Execute("session-123", "invalid-cmd-999");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that cancel command tool returns valid markdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelCommand_ReturnsValidMarkdown()
    {
        // Act
        var result = await new CancelCommandTool().Execute("test-session", "test-command");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("##");
    }
}
