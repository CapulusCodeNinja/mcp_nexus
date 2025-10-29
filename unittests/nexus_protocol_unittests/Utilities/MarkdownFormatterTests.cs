using FluentAssertions;

using Nexus.Protocol.Utilities;

using Xunit;

namespace Nexus.Protocol.Unittests.Utilities;

/// <summary>
/// Unit tests for MarkdownFormatter utility class.
/// </summary>
public class MarkdownFormatterTests
{
    /// <summary>
    /// Tests that CreateHeader with title returns formatted header.
    /// </summary>
    [Fact]
    public void CreateHeader_WithTitle_ReturnsFormattedHeader()
    {
        // Act
        var result = MarkdownFormatter.CreateHeader("Test Title");

        // Assert
        _ = result.Should().Be($"## Test Title{Environment.NewLine}{Environment.NewLine}");
    }

    /// <summary>
    /// Tests that CreateHeader with title and subtitle returns formatted header.
    /// </summary>
    [Fact]
    public void CreateHeader_WithTitleAndSubtitle_ReturnsFormattedHeader()
    {
        // Act
        var result = MarkdownFormatter.CreateHeader("Test Title", "Test Subtitle");

        // Assert
        _ = result.Should().Be($"## Test Title{Environment.NewLine}{Environment.NewLine}Test Subtitle{Environment.NewLine}{Environment.NewLine}");
    }

    /// <summary>
    /// Tests that CreateKeyValue with code format returns formatted key-value pair.
    /// </summary>
    [Fact]
    public void CreateKeyValue_WithCodeFormat_ReturnsFormattedKeyValue()
    {
        // Act
        var result = MarkdownFormatter.CreateKeyValue("Test Key", "Test Value", true);

        // Assert
        _ = result.Should().Be("**Test Key:** `Test Value`");
    }

    /// <summary>
    /// Tests that CreateKeyValue without code format returns formatted key-value pair.
    /// </summary>
    [Fact]
    public void CreateKeyValue_WithoutCodeFormat_ReturnsFormattedKeyValue()
    {
        // Act
        var result = MarkdownFormatter.CreateKeyValue("Test Key", "Test Value", false);

        // Assert
        _ = result.Should().Be("**Test Key:** Test Value");
    }

    /// <summary>
    /// Tests that CreateKeyValue with null value returns N/A.
    /// </summary>
    [Fact]
    public void CreateKeyValue_WithNullValue_ReturnsN_A()
    {
        // Act
        var result = MarkdownFormatter.CreateKeyValue("Test Key", null, false);

        // Assert
        _ = result.Should().Be("**Test Key:** N/A");
    }

    /// <summary>
    /// Tests that CreateCodeBlock with content returns formatted code block.
    /// </summary>
    [Fact]
    public void CreateCodeBlock_WithContent_ReturnsFormattedCodeBlock()
    {
        // Act
        var result = MarkdownFormatter.CreateCodeBlock("test content");

        // Assert
        _ = result.Should().Be($"```{Environment.NewLine}test content{Environment.NewLine}```{Environment.NewLine}");
    }

    /// <summary>
    /// Tests that CreateCodeBlock with title returns formatted code block with title.
    /// </summary>
    [Fact]
    public void CreateCodeBlock_WithTitle_ReturnsFormattedCodeBlockWithTitle()
    {
        // Act
        var result = MarkdownFormatter.CreateCodeBlock("test content", "Test Title");

        // Assert
        _ = result.Should().Be($"### Test Title{Environment.NewLine}{Environment.NewLine}```{Environment.NewLine}test content{Environment.NewLine}```{Environment.NewLine}");
    }

    /// <summary>
    /// Tests that CreateCodeBlock with empty content returns empty string.
    /// </summary>
    [Fact]
    public void CreateCodeBlock_WithEmptyContent_ReturnsEmptyString()
    {
        // Act
        var result = MarkdownFormatter.CreateCodeBlock(string.Empty);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that CreateSuccessMessage returns formatted success message.
    /// </summary>
    [Fact]
    public void CreateSuccessMessage_ReturnsFormattedMessage()
    {
        // Act
        var result = MarkdownFormatter.CreateSuccessMessage("Operation completed");

        // Assert
        _ = result.Should().Be("✓ Operation completed");
    }

    /// <summary>
    /// Tests that CreateWarningMessage returns formatted warning message.
    /// </summary>
    [Fact]
    public void CreateWarningMessage_ReturnsFormattedMessage()
    {
        // Act
        var result = MarkdownFormatter.CreateWarningMessage("Warning message");

        // Assert
        _ = result.Should().Be("⚠ Warning message");
    }

    /// <summary>
    /// Tests that CreateErrorMessage returns formatted error message.
    /// </summary>
    [Fact]
    public void CreateErrorMessage_ReturnsFormattedMessage()
    {
        // Act
        var result = MarkdownFormatter.CreateErrorMessage("Error message");

        // Assert
        _ = result.Should().Be("❌ Error message");
    }

    /// <summary>
    /// Tests that CreateTable with headers and rows returns formatted table.
    /// </summary>
    [Fact]
    public void CreateTable_WithHeadersAndRows_ReturnsFormattedTable()
    {
        // Arrange
        var headers = new[] { "ID", "Name", "Status" };
        var rows = new[]
        {
            new[] { "1", "Test1", "Active" },
            new[] { "2", "Test2", "Inactive" },
        };

        // Act
        var result = MarkdownFormatter.CreateTable(headers, rows);

        // Assert
        var expected = $"| ID | Name | Status |{Environment.NewLine}" +
                      $"| --- | --- | --- |{Environment.NewLine}" +
                      $"| 1 | Test1 | Active |{Environment.NewLine}" +
                      $"| 2 | Test2 | Inactive |{Environment.NewLine}";
        _ = result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that CreateTable with empty headers returns empty string.
    /// </summary>
    [Fact]
    public void CreateTable_WithEmptyHeaders_ReturnsEmptyString()
    {
        // Act
        var result = MarkdownFormatter.CreateTable(Array.Empty<string>(), Array.Empty<string[]>());

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that CreateCommandResult with all fields returns formatted result.
    /// </summary>
    [Fact]
    public void CreateCommandResult_WithAllFields_ReturnsFormattedResult()
    {
        // Arrange
        var commandId = "cmd-123";
        var sessionId = "sess-456";
        var command = "!analyze";
        var state = "Completed";
        var isSuccess = true;
        var queuedTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var startTime = new DateTime(2024, 1, 1, 10, 1, 0);
        var endTime = new DateTime(2024, 1, 1, 10, 2, 0);
        var executionTime = TimeSpan.FromSeconds(60);
        var totalTime = TimeSpan.FromSeconds(120);

        // Act
        var result = MarkdownFormatter.CreateCommandResult(
            commandId, sessionId, command, state, isSuccess, queuedTime,
            startTime, endTime, executionTime, totalTime);

        // Assert
        _ = result.Should().Contain("## Command Result");
        _ = result.Should().Contain("**Command ID:** `cmd-123`");
        _ = result.Should().Contain("**Session ID:** `sess-456`");
        _ = result.Should().Contain("**Command:** `!analyze`");
        _ = result.Should().Contain("**State:** Completed");
        _ = result.Should().Contain("**Success:** True");
        _ = result.Should().Contain("**Queued Time:** 2024-01-01 10:00:00");
        _ = result.Should().Contain("**Start Time:** 2024-01-01 10:01:00");
        _ = result.Should().Contain("**End Time:** 2024-01-01 10:02:00");
        _ = result.Should().Contain("**Execution Time:**");
        _ = result.Should().Contain(executionTime.ToString());
        _ = result.Should().Contain("**Total Time:**");
        _ = result.Should().Contain(totalTime.ToString());
    }

    /// <summary>
    /// Tests that CreateCommandResult with minimal fields returns formatted result.
    /// </summary>
    [Fact]
    public void CreateCommandResult_WithMinimalFields_ReturnsFormattedResult()
    {
        // Arrange
        var commandId = "cmd-123";
        var sessionId = "sess-456";
        var command = "!analyze";
        var state = "Completed";
        var isSuccess = true;
        var queuedTime = new DateTime(2024, 1, 1, 10, 0, 0);

        // Act
        var result = MarkdownFormatter.CreateCommandResult(
            commandId, sessionId, command, state, isSuccess, queuedTime);

        // Assert
        _ = result.Should().Contain("## Command Result");
        _ = result.Should().Contain("**Command ID:** `cmd-123`");
        _ = result.Should().Contain("**Session ID:** `sess-456`");
        _ = result.Should().Contain("**Command:** `!analyze`");
        _ = result.Should().Contain("**State:** Completed");
        _ = result.Should().Contain("**Success:** True");
        _ = result.Should().Contain("**Queued Time:** 2024-01-01 10:00:00");
        _ = result.Should().NotContain("**Start Time:**");
        _ = result.Should().NotContain("**End Time:**");
        _ = result.Should().NotContain("**Execution Time:**");
        _ = result.Should().NotContain("**Total Time:**");
    }

    /// <summary>
    /// Tests that CreateSessionResult with all fields returns formatted result.
    /// </summary>
    [Fact]
    public void CreateSessionResult_WithAllFields_ReturnsFormattedResult()
    {
        // Act
        var result = MarkdownFormatter.CreateSessionResult(
            "sess-123", "dump.dmp", "Success", "C:\\symbols", "Session created");

        // Assert
        _ = result.Should().Contain("## Session Creation");
        _ = result.Should().Contain("**Session ID:** `sess-123`");
        _ = result.Should().Contain("**Dump File:** `dump.dmp`");
        _ = result.Should().Contain("**Status:** Success");
        _ = result.Should().Contain("**Symbols Path:** `C:\\symbols`");
        _ = result.Should().Contain("✓ Session created");
    }

    /// <summary>
    /// Tests that CreateSessionResult with minimal fields returns formatted result.
    /// </summary>
    [Fact]
    public void CreateSessionResult_WithMinimalFields_ReturnsFormattedResult()
    {
        // Act
        var result = MarkdownFormatter.CreateSessionResult(
            "sess-123", "dump.dmp", "Success");

        // Assert
        _ = result.Should().Contain("## Session Creation");
        _ = result.Should().Contain("**Session ID:** `sess-123`");
        _ = result.Should().Contain("**Dump File:** `dump.dmp`");
        _ = result.Should().Contain("**Status:** Success");
        _ = result.Should().NotContain("**Symbols Path:**");
    }

    /// <summary>
    /// Tests that CreateCommandStatusSummary with commands returns formatted summary.
    /// </summary>
    [Fact]
    public void CreateCommandStatusSummary_WithCommands_ReturnsFormattedSummary()
    {
        // Arrange
        var sessionId = "sess-123";
        var commands = new object[]
        {
            new { commandId = "cmd-1", command = "!analyze", state = "Completed", isSuccess = true, executionTime = TimeSpan.FromSeconds(30) },
            new { commandId = "cmd-2", command = "kL", state = "Running", isSuccess = (bool?)null, executionTime = (TimeSpan?)null },
        };

        // Act
        var result = MarkdownFormatter.CreateCommandStatusSummary(sessionId, commands);

        // Assert
        _ = result.Should().Contain("## Command Status Summary");
        _ = result.Should().Contain("**Session ID:** `sess-123`");
        _ = result.Should().Contain("**Total Commands:** 2");
        _ = result.Should().Contain("### Commands");
        _ = result.Should().Contain("| Command ID | Command | State | Success | Execution Time |");
        _ = result.Should().Contain("| cmd-1 | !analyze | Completed | True |");
        _ = result.Should().Contain("30"); // Culture-invariant check
        _ = result.Should().Contain("| cmd-2 | kL | Running | N/A | N/A |");
    }

    /// <summary>
    /// Tests that CreateCommandStatusSummary with no commands returns formatted summary.
    /// </summary>
    [Fact]
    public void CreateCommandStatusSummary_WithNoCommands_ReturnsFormattedSummary()
    {
        // Act
        var result = MarkdownFormatter.CreateCommandStatusSummary("sess-123", Array.Empty<object>());

        // Assert
        _ = result.Should().Contain("## Command Status Summary");
        _ = result.Should().Contain("**Session ID:** `sess-123`");
        _ = result.Should().Contain("**Total Commands:** 0");
        _ = result.Should().Contain("No commands found.");
    }

    /// <summary>
    /// Tests that CreateOperationResult with success returns formatted result.
    /// </summary>
    [Fact]
    public void CreateOperationResult_WithSuccess_ReturnsFormattedResult()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            { "Command ID", "cmd-123" },
            { "Session ID", "sess-456" },
            { "Status", "Queued" },
        };

        // Act
        var result = MarkdownFormatter.CreateOperationResult(
            "Command Enqueued", keyValues, "Command queued successfully", true);

        // Assert
        _ = result.Should().Contain("## Command Enqueued");
        _ = result.Should().Contain("**Command ID:** `cmd-123`");
        _ = result.Should().Contain("**Session ID:** `sess-456`");
        _ = result.Should().Contain("**Status:** Queued");
        _ = result.Should().Contain("✓ Command queued successfully");
    }

    /// <summary>
    /// Tests that CreateOperationResult with failure returns formatted result.
    /// </summary>
    [Fact]
    public void CreateOperationResult_WithFailure_ReturnsFormattedResult()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            { "Command ID", "N/A" },
            { "Session ID", "sess-456" },
            { "Status", "Failed" },
        };

        // Act
        var result = MarkdownFormatter.CreateOperationResult(
            "Command Enqueue Failed", keyValues, "Invalid command", false);

        // Assert
        _ = result.Should().Contain("## Command Enqueue Failed");
        _ = result.Should().Contain("**Command ID:** `N/A`");
        _ = result.Should().Contain("**Session ID:** `sess-456`");
        _ = result.Should().Contain("**Status:** Failed");
        _ = result.Should().Contain("❌ Invalid command");
    }

    /// <summary>
    /// Tests that CreateOperationResult without message returns formatted result.
    /// </summary>
    [Fact]
    public void CreateOperationResult_WithoutMessage_ReturnsFormattedResult()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            { "Session ID", "sess-456" },
            { "Status", "Success" },
        };

        // Act
        var result = MarkdownFormatter.CreateOperationResult(
            "Session Closed", keyValues, null, true);

        // Assert
        _ = result.Should().Contain("## Session Closed");
        _ = result.Should().Contain("**Session ID:** `sess-456`");
        _ = result.Should().Contain("**Status:** Success");
        _ = result.Should().NotContain("✓");
        _ = result.Should().NotContain("❌");
    }

    /// <summary>
    /// Tests that CreateTable with short rows pads with empty strings.
    /// </summary>
    [Fact]
    public void CreateTable_WithShortRows_PadsWithEmptyStrings()
    {
        // Arrange
        var headers = new[] { "ID", "Name", "Status", "Extra" };
        var rows = new[]
        {
            new[] { "1", "Test1" }, // Missing Status and Extra
            new[] { "2", "Test2", "Active" }, // Missing Extra
        };

        // Act
        var result = MarkdownFormatter.CreateTable(headers, rows);

        // Assert
        _ = result.Should().Contain("| 1 | Test1 |  |  |");
        _ = result.Should().Contain("| 2 | Test2 | Active |  |");
    }

    /// <summary>
    /// Tests that CreateTable with long rows truncates to header length.
    /// </summary>
    [Fact]
    public void CreateTable_WithLongRows_TruncatesToHeaderLength()
    {
        // Arrange
        var headers = new[] { "ID", "Name" };
        var rows = new[]
        {
            new[] { "1", "Test1", "Extra1", "Extra2" }, // More values than headers
        };

        // Act
        var result = MarkdownFormatter.CreateTable(headers, rows);

        // Assert
        _ = result.Should().Contain("| 1 | Test1 |");
        _ = result.Should().NotContain("Extra1");
        _ = result.Should().NotContain("Extra2");
    }
}
