using FluentAssertions;
using Nexus.Protocol.Utilities;

namespace Nexus.Protocol.Unittests.Utilities;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable IDE0005 // Using directive is unnecessary
#pragma warning disable IDE0005

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
        result.Should().Be("## Test Title\n\n");
    }

    [Fact]
    public void CreateHeader_WithTitleAndSubtitle_ReturnsFormattedHeader()
    {
        // Act
        var result = MarkdownFormatter.CreateHeader("Test Title", "Test Subtitle");

        // Assert
        result.Should().Be("## Test Title\n\nTest Subtitle\n\n");
    }

    [Fact]
    public void CreateKeyValue_WithCodeFormat_ReturnsFormattedKeyValue()
    {
        // Act
        var result = MarkdownFormatter.CreateKeyValue("Test Key", "Test Value", true);

        // Assert
        result.Should().Be("**Test Key:** `Test Value`");
    }

    [Fact]
    public void CreateKeyValue_WithoutCodeFormat_ReturnsFormattedKeyValue()
    {
        // Act
        var result = MarkdownFormatter.CreateKeyValue("Test Key", "Test Value", false);

        // Assert
        result.Should().Be("**Test Key:** Test Value");
    }

    [Fact]
    public void CreateKeyValue_WithNullValue_ReturnsN_A()
    {
        // Act
        var result = MarkdownFormatter.CreateKeyValue("Test Key", null, false);

        // Assert
        result.Should().Be("**Test Key:** N/A");
    }

    [Fact]
    public void CreateCodeBlock_WithContent_ReturnsFormattedCodeBlock()
    {
        // Act
        var result = MarkdownFormatter.CreateCodeBlock("test content");

        // Assert
        result.Should().Be("```\ntest content\n```\n");
    }

    [Fact]
    public void CreateCodeBlock_WithTitle_ReturnsFormattedCodeBlockWithTitle()
    {
        // Act
        var result = MarkdownFormatter.CreateCodeBlock("test content", "Test Title");

        // Assert
        result.Should().Be("### Test Title\n\n```\ntest content\n```\n");
    }

    [Fact]
    public void CreateCodeBlock_WithEmptyContent_ReturnsEmptyString()
    {
        // Act
        var result = MarkdownFormatter.CreateCodeBlock("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CreateSuccessMessage_ReturnsFormattedMessage()
    {
        // Act
        var result = MarkdownFormatter.CreateSuccessMessage("Operation completed");

        // Assert
        result.Should().Be("✓ Operation completed");
    }

    [Fact]
    public void CreateWarningMessage_ReturnsFormattedMessage()
    {
        // Act
        var result = MarkdownFormatter.CreateWarningMessage("Warning message");

        // Assert
        result.Should().Be("⚠ Warning message");
    }

    [Fact]
    public void CreateErrorMessage_ReturnsFormattedMessage()
    {
        // Act
        var result = MarkdownFormatter.CreateErrorMessage("Error message");

        // Assert
        result.Should().Be("❌ Error message");
    }

    [Fact]
    public void CreateTable_WithHeadersAndRows_ReturnsFormattedTable()
    {
        // Arrange
        var headers = new[] { "ID", "Name", "Status" };
        var rows = new[]
        {
            new[] { "1", "Test1", "Active" },
            new[] { "2", "Test2", "Inactive" }
        };

        // Act
        var result = MarkdownFormatter.CreateTable(headers, rows);

        // Assert
        var expected = "| ID | Name | Status |\n" +
                      "| --- | --- | --- |\n" +
                      "| 1 | Test1 | Active |\n" +
                      "| 2 | Test2 | Inactive |\n";
        result.Should().Be(expected);
    }

    [Fact]
    public void CreateTable_WithEmptyHeaders_ReturnsEmptyString()
    {
        // Act
        var result = MarkdownFormatter.CreateTable(Array.Empty<string>(), Array.Empty<string[]>());

        // Assert
        result.Should().BeEmpty();
    }

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
        result.Should().Contain("## Command Result");
        result.Should().Contain("**Command ID:** `cmd-123`");
        result.Should().Contain("**Session ID:** `sess-456`");
        result.Should().Contain("**Command:** `!analyze`");
        result.Should().Contain("**State:** Completed");
        result.Should().Contain("**Success:** True");
        result.Should().Contain("**Queued Time:** 2024-01-01 10:00:00");
        result.Should().Contain("**Start Time:** 2024-01-01 10:01:00");
        result.Should().Contain("**End Time:** 2024-01-01 10:02:00");
        result.Should().Contain("**Execution Time:** 60.00s");
        result.Should().Contain("**Total Time:** 120.00s");
    }

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
        result.Should().Contain("## Command Result");
        result.Should().Contain("**Command ID:** `cmd-123`");
        result.Should().Contain("**Session ID:** `sess-456`");
        result.Should().Contain("**Command:** `!analyze`");
        result.Should().Contain("**State:** Completed");
        result.Should().Contain("**Success:** True");
        result.Should().Contain("**Queued Time:** 2024-01-01 10:00:00");
        result.Should().NotContain("**Start Time:**");
        result.Should().NotContain("**End Time:**");
        result.Should().NotContain("**Execution Time:**");
        result.Should().NotContain("**Total Time:**");
    }

    [Fact]
    public void CreateSessionResult_WithAllFields_ReturnsFormattedResult()
    {
        // Act
        var result = MarkdownFormatter.CreateSessionResult(
            "sess-123", "dump.dmp", "Success", "C:\\symbols", "Session created");

        // Assert
        result.Should().Contain("## Session Creation");
        result.Should().Contain("**Session ID:** `sess-123`");
        result.Should().Contain("**Dump File:** `dump.dmp`");
        result.Should().Contain("**Status:** Success");
        result.Should().Contain("**Symbols Path:** `C:\\symbols`");
        result.Should().Contain("✓ Session created");
    }

    [Fact]
    public void CreateSessionResult_WithMinimalFields_ReturnsFormattedResult()
    {
        // Act
        var result = MarkdownFormatter.CreateSessionResult(
            "sess-123", "dump.dmp", "Success");

        // Assert
        result.Should().Contain("## Session Creation");
        result.Should().Contain("**Session ID:** `sess-123`");
        result.Should().Contain("**Dump File:** `dump.dmp`");
        result.Should().Contain("**Status:** Success");
        result.Should().NotContain("**Symbols Path:**");
    }

    [Fact]
    public void CreateCommandStatusSummary_WithCommands_ReturnsFormattedSummary()
    {
        // Arrange
        var sessionId = "sess-123";
        var commands = new object[]
        {
            new { commandId = "cmd-1", command = "!analyze", state = "Completed", isSuccess = true, executionTime = TimeSpan.FromSeconds(30) },
            new { commandId = "cmd-2", command = "kL", state = "Running", isSuccess = (bool?)null, executionTime = (TimeSpan?)null }
        };

        // Act
        var result = MarkdownFormatter.CreateCommandStatusSummary(sessionId, commands);

        // Assert
        result.Should().Contain("## Command Status Summary");
        result.Should().Contain("**Session ID:** `sess-123`");
        result.Should().Contain("**Total Commands:** 2");
        result.Should().Contain("### Commands");
        result.Should().Contain("| Command ID | Command | State | Success | Execution Time |");
        result.Should().Contain("| cmd-1 | !analyze | Completed | True | 30.00s |");
        result.Should().Contain("| cmd-2 | kL | Running | N/A | N/A |");
    }

    [Fact]
    public void CreateCommandStatusSummary_WithNoCommands_ReturnsFormattedSummary()
    {
        // Act
        var result = MarkdownFormatter.CreateCommandStatusSummary("sess-123", Array.Empty<object>());

        // Assert
        result.Should().Contain("## Command Status Summary");
        result.Should().Contain("**Session ID:** `sess-123`");
        result.Should().Contain("**Total Commands:** 0");
        result.Should().Contain("No commands found.");
    }

    [Fact]
    public void CreateOperationResult_WithSuccess_ReturnsFormattedResult()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            { "Command ID", "cmd-123" },
            { "Session ID", "sess-456" },
            { "Status", "Queued" }
        };

        // Act
        var result = MarkdownFormatter.CreateOperationResult(
            "Command Enqueued", keyValues, "Command queued successfully", true);

        // Assert
        result.Should().Contain("## Command Enqueued");
        result.Should().Contain("**Command ID:** `cmd-123`");
        result.Should().Contain("**Session ID:** `sess-456`");
        result.Should().Contain("**Status:** Queued");
        result.Should().Contain("✓ Command queued successfully");
    }

    [Fact]
    public void CreateOperationResult_WithFailure_ReturnsFormattedResult()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            { "Command ID", "N/A" },
            { "Session ID", "sess-456" },
            { "Status", "Failed" }
        };

        // Act
        var result = MarkdownFormatter.CreateOperationResult(
            "Command Enqueue Failed", keyValues, "Invalid command", false);

        // Assert
        result.Should().Contain("## Command Enqueue Failed");
        result.Should().Contain("**Command ID:** N/A");
        result.Should().Contain("**Session ID:** `sess-456`");
        result.Should().Contain("**Status:** Failed");
        result.Should().Contain("❌ Invalid command");
    }

    [Fact]
    public void CreateOperationResult_WithoutMessage_ReturnsFormattedResult()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            { "Session ID", "sess-456" },
            { "Status", "Success" }
        };

        // Act
        var result = MarkdownFormatter.CreateOperationResult(
            "Session Closed", keyValues, null, true);

        // Assert
        result.Should().Contain("## Session Closed");
        result.Should().Contain("**Session ID:** `sess-456`");
        result.Should().Contain("**Status:** Success");
        result.Should().NotContain("✓");
        result.Should().NotContain("❌");
    }

    [Fact]
    public void CreateTable_WithShortRows_PadsWithEmptyStrings()
    {
        // Arrange
        var headers = new[] { "ID", "Name", "Status", "Extra" };
        var rows = new[]
        {
            new[] { "1", "Test1" }, // Missing Status and Extra
            new[] { "2", "Test2", "Active" } // Missing Extra
        };

        // Act
        var result = MarkdownFormatter.CreateTable(headers, rows);

        // Assert
        result.Should().Contain("| 1 | Test1 |  |  |");
        result.Should().Contain("| 2 | Test2 | Active |  |");
    }

    [Fact]
    public void CreateTable_WithLongRows_TruncatesToHeaderLength()
    {
        // Arrange
        var headers = new[] { "ID", "Name" };
        var rows = new[]
        {
            new[] { "1", "Test1", "Extra1", "Extra2" } // More values than headers
        };

        // Act
        var result = MarkdownFormatter.CreateTable(headers, rows);

        // Assert
        result.Should().Contain("| 1 | Test1 |");
        result.Should().NotContain("Extra1");
        result.Should().NotContain("Extra2");
    }
}
