using FluentAssertions;

using Nexus.Protocol.Tools;

using Xunit;

namespace Nexus.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for MCP Protocol Tools.
/// Tests error handling, validation, and response formatting for all tools.
/// </summary>
public class ProtocolToolsTests
{
    #region EnqueueAsyncDumpAnalyzeCommandTool Tests

    /// <summary>
    /// Verifies that enqueue tool handles empty sessionId correctly.
    /// </summary>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command("", "test command");

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
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithEmptyCommand_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command("session-123", "");

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
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithWhitespaceSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command("   ", "test command");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
    }

    /// <summary>
    /// Verifies that enqueue tool handles whitespace-only command correctly.
    /// </summary>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithWhitespaceCommand_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command("session-123", "   ");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
    }

    /// <summary>
    /// Verifies that enqueue tool handles invalid sessionId correctly.
    /// </summary>
    [Fact]
    public async Task EnqueueDumpAnalyzeCommand_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncDumpAnalyzeCommandTool.nexus_enqueue_async_dump_analyze_command("invalid-session-999", "test command");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        // Should contain error indicator or failure status
        _ = (markdown.Contains("Failed") || markdown.Contains("Error") || markdown.Contains("❌")).Should().BeTrue();
    }

    #endregion

    #region EnqueueAsyncExtensionCommandTool Tests

    /// <summary>
    /// Verifies that extension enqueue tool handles empty sessionId correctly.
    /// </summary>
    [Fact]
    public async Task EnqueueExtensionCommand_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncExtensionCommandTool.nexus_enqueue_async_extension_command("", "testExtension", null);

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
    [Fact]
    public async Task EnqueueExtensionCommand_WithEmptyExtensionName_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncExtensionCommandTool.nexus_enqueue_async_extension_command("session-123", "", null);

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
    [Fact]
    public async Task EnqueueExtensionCommand_WithWhitespaceSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncExtensionCommandTool.nexus_enqueue_async_extension_command("   ", "testExtension", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles whitespace extension name correctly.
    /// </summary>
    [Fact]
    public async Task EnqueueExtensionCommand_WithWhitespaceExtensionName_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncExtensionCommandTool.nexus_enqueue_async_extension_command("session-123", "   ", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Failed");
    }

    /// <summary>
    /// Verifies that extension enqueue tool handles invalid sessionId correctly.
    /// </summary>
    [Fact]
    public async Task EnqueueExtensionCommand_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await EnqueueAsyncExtensionCommandTool.nexus_enqueue_async_extension_command("invalid-session-999", "testExtension", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = (markdown.Contains("Failed") || markdown.Contains("Error") || markdown.Contains("❌")).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that extension enqueue tool accepts null parameters.
    /// </summary>
    [Fact]
    public async Task EnqueueExtensionCommand_WithNullParameters_HandlesGracefully()
    {
        // Act
        var result = await EnqueueAsyncExtensionCommandTool.nexus_enqueue_async_extension_command("invalid-session-999", "testExtension", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that extension enqueue tool accepts parameters object.
    /// </summary>
    [Fact]
    public async Task EnqueueExtensionCommand_WithParameters_HandlesGracefully()
    {
        // Act
        var parameters = new { param1 = "value1", param2 = 123 };
        var result = await EnqueueAsyncExtensionCommandTool.nexus_enqueue_async_extension_command("invalid-session-999", "testExtension", parameters);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetDumpAnalyzeCommandsStatusTool Tests

    /// <summary>
    /// Verifies that get commands status tool handles invalid sessionId correctly.
    /// </summary>
    [Fact]
    public async Task GetCommandsStatus_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status("invalid-session-999");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Command Status Summary");
    }

    /// <summary>
    /// Verifies that get commands status tool handles empty sessionId correctly.
    /// </summary>
    [Fact]
    public async Task GetCommandsStatus_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Act
        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status("");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that get commands status tool returns valid markdown.
    /// </summary>
    [Fact]
    public async Task GetCommandsStatus_ReturnsValidMarkdown()
    {
        // Act
        var result = await GetDumpAnalyzeCommandsStatusTool.nexus_get_dump_analyze_commands_status("test-session");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("##");
    }

    #endregion

    #region CancelCommandTool Tests

    /// <summary>
    /// Verifies that cancel command tool handles invalid sessionId correctly.
    /// </summary>
    [Fact]
    public async Task CancelCommand_WithInvalidSessionId_ReturnsResponse()
    {
        // Act
        var result = await CancelCommandTool.nexus_cancel_dump_analyze_command("invalid-session-999", "cmd-123");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("Cancellation");
    }

    /// <summary>
    /// Verifies that cancel command tool handles invalid commandId correctly.
    /// </summary>
    [Fact]
    public async Task CancelCommand_WithInvalidCommandId_ReturnsResponse()
    {
        // Act
        var result = await CancelCommandTool.nexus_cancel_dump_analyze_command("session-123", "invalid-cmd-999");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that cancel command tool returns valid markdown.
    /// </summary>
    [Fact]
    public async Task CancelCommand_ReturnsValidMarkdown()
    {
        // Act
        var result = await CancelCommandTool.nexus_cancel_dump_analyze_command("test-session", "test-command");

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().NotBeNullOrEmpty();
        _ = markdown.Should().Contain("##");
    }

    #endregion
}

