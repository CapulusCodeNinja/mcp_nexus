using System.Text;

using WinAiDbg.Engine.Share.Models;

namespace WinAiDbg.Protocol.Utilities;

/// <summary>
/// Utility class for formatting MCP tool responses as Markdown.
/// Provides consistent formatting patterns and reduces code duplication.
/// </summary>
internal static class MarkdownFormatter
{
    /// <summary>
    /// Gets the standardized Usage Guide in Markdown format to append to AI client responses.
    /// </summary>
    /// <returns>Usage guide markdown.</returns>
    public static string GetUsageGuideMarkdown()
    {
        var md = new StringBuilder();
        _ = md.AppendLine();
        _ = md.AppendLine("## Usage Guide");
        _ = md.AppendLine();
        _ = md.AppendLine("### Overview");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Description**: Complete guide for using the WinAiDbg MCP server tools and resources.");
        _ = md.AppendLine();
        _ = md.AppendLine("### MCP Tools");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Description**: Core debugging tools for crash dump analysis");
        _ = md.AppendLine("- **General notes**:");
        _ = md.AppendLine("- **TOOLS**: Use `tools/call` to execute operations (open session, run commands, close session)");
        _ = md.AppendLine("- **RESOURCES**: Use `resources/read` to access data (command lists, session lists, documentation, metrics)");
        _ = md.AppendLine("- **Async execution**: After opening an analyze session, WinDBG commands can be executed asynchronously");
        _ = md.AppendLine("- **Command status (get)**: Use `winaidbg_get_dump_analyze_commands_status` to poll ALL command statuses in a session (efficient for polling)");
        _ = md.AppendLine("- **Command results (read)**: Use `winaidbg_read_dump_analyze_command_result` to retrieve the FULL output for a single command; use the `commands` resource to browse summaries");
        _ = md.AppendLine("- **Sessions**: Opening a session does nothing by itself. Only open a session if you will enqueue commands.");
        _ = md.AppendLine("- **Commands**: After enqueueing, either poll with `winaidbg_get_dump_analyze_commands_status` (get = statuses) or fetch the full output with `winaidbg_read_dump_analyze_command_result` (read = full result). Enqueueing without monitoring or reading results is not useful.");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Tooling - Open Session");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Tool name**: `winaidbg_open_dump_analyze_session`");
        _ = md.AppendLine("- **Action**: Open the analyze session for the dump file");
        _ = md.AppendLine("- **Input**:");
        _ = md.AppendLine("    * **dumpPath**: string (required)");
        _ = md.AppendLine("- **Output**:");
        _ = md.AppendLine("    * **sessionId**: string");
        _ = md.AppendLine("- **Note**: This EXACT `sessionId` IS REQUIRED for all following commands in the session");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Tooling - Exec Command");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Tool name**: `winaidbg_enqueue_async_dump_analyze_command`");
        _ = md.AppendLine("- **Action**: Start asynchronous execution of a WinDBG/CDB command");
        _ = md.AppendLine("- **Input**:");
        _ = md.AppendLine("    * **command**: string (required)");
        _ = md.AppendLine("    * **sessionId**: string (required)");
        _ = md.AppendLine("- **Output**:");
        _ = md.AppendLine("    * **commandId**: string");
        _ = md.AppendLine("- **Note**: This EXACT `commandId` IS REQUIRED for `winaidbg_read_dump_analyze_command_result`");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Tooling - Close Session");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Tool name**: `winaidbg_close_dump_analyze_session`");
        _ = md.AppendLine("- **Action**: Close the analyze session after commands complete or when no longer needed");
        _ = md.AppendLine("- **Input**:");
        _ = md.AppendLine("    * **sessionId**: string (required)");
        _ = md.AppendLine("- **Output**: none");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Tooling - Get Commands Status (Polling)");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Tool name**: `winaidbg_get_dump_analyze_commands_status`");
        _ = md.AppendLine("- **Action**: Bulk-poll the status of all queued/executing/completed commands in a session");
        _ = md.AppendLine("- **Input**:");
        _ = md.AppendLine("    * **sessionId**: string (required)");
        _ = md.AppendLine("- **Output**:");
        _ = md.AppendLine("    * **commands**: array of command status objects ({ commandId, command, state, queuedTime, startTime, endTime, executionTime })");
        _ = md.AppendLine("- **Note**: Use this to efficiently monitor progress (get = poll statuses). For the full output of an individual command, use `winaidbg_read_dump_analyze_command_result` (read = full result)");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Tooling - Get Command Result");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Tool name**: `winaidbg_read_dump_analyze_command_result`");
        _ = md.AppendLine("- **Action**: Get status and results of a previously queued async command");
        _ = md.AppendLine("- **Input**:");
        _ = md.AppendLine("    * **sessionId**: string (required)");
        _ = md.AppendLine("    * **commandId**: string (required)");
        _ = md.AppendLine("- **Output**:");
        _ = md.AppendLine("    * **commandStatus**: object");
        _ = md.AppendLine("    * **commandResult**: object");
        _ = md.AppendLine("- **Note**: Use for results from `winaidbg_enqueue_async_dump_analyze_command` or `winaidbg_enqueue_async_extension_command`");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Tooling - Cancel Command");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Tool name**: `winaidbg_cancel_dump_analyze_command`");
        _ = md.AppendLine("- **Action**: Cancel a queued or executing command in a session");
        _ = md.AppendLine("- **Input**:");
        _ = md.AppendLine("    * **sessionId**: string (required)");
        _ = md.AppendLine("    * **commandId**: string (required)");
        _ = md.AppendLine("- **Output**:  ");
        _ = md.AppendLine("    * **cancellation result**: string (Cancelled/NotFound)");
        _ = md.AppendLine("- **Note**: No effect if the command already completed; returns NotFound in that case");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Tooling - Queue Extension");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Tool name**: `winaidbg_enqueue_async_extension_command`");
        _ = md.AppendLine("- **Action**: Queue an extension script for complex workflows (may run multiple commands)");
        _ = md.AppendLine("- **Input**:");
        _ = md.AppendLine("    * **sessionId**: string (required)");
        _ = md.AppendLine("    * **extensionName**: string (required)");
        _ = md.AppendLine("    * **parameters**: object (optional)");
        _ = md.AppendLine("- **Output**:");
        _ = md.AppendLine("    * **commandId**: string");
        _ = md.AppendLine("- **Note**: If an invalid extension name is provided, the error lists available extensions. Use `winaidbg_read_dump_analyze_command_result` to get results. Extensions may take several minutes.");
        _ = md.AppendLine();
        _ = md.AppendLine("### MCP Resources");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Description**: Access data and results using `resources/read` (NOT `tools/call`)");
        _ = md.AppendLine("- **Usage notes**:");
        _ = md.AppendLine("- **Access**: Use `resources/read` to access these resources");
        _ = md.AppendLine("- **Separation**: Resources provide data access; tools perform actions");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Resource: `sessions`");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Name**: List Sessions");
        _ = md.AppendLine("- **Description**: List all debugging sessions with status and activity information");
        _ = md.AppendLine("- **Input**: none");
        _ = md.AppendLine("- **Note**: Use `sessions` resource (no parameters; returns all sessions)");
        _ = md.AppendLine();
        _ = md.AppendLine("#### Resource: `commands`");
        _ = md.AppendLine();
        _ = md.AppendLine("- **Name**: List Commands");
        _ = md.AppendLine("- **Description**: List async commands from all sessions with status and timing information");
        _ = md.AppendLine("- **Input**: none");
        _ = md.AppendLine("- **Note**: Use `commands` resource (no parameters; returns all commands)");
        _ = md.AppendLine();

        return md.ToString();
    }

    /// <summary>
    /// Creates a header section with title and optional subtitle.
    /// </summary>
    /// <param name="title">The main title.</param>
    /// <param name="subtitle">Optional subtitle.</param>
    /// <returns>Formatted header section.</returns>
    public static string CreateHeader(string title, string? subtitle = null)
    {
        var markdown = new StringBuilder();
        _ = markdown.AppendLine($"## {title}");
        _ = markdown.AppendLine();

        if (!string.IsNullOrEmpty(subtitle))
        {
            _ = markdown.AppendLine(subtitle);
            _ = markdown.AppendLine();
        }

        return markdown.ToString();
    }

    /// <summary>
    /// Creates a key-value pair line with consistent formatting.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="value">The value.</param>
    /// <param name="codeFormat">Whether to wrap the value in backticks.</param>
    /// <returns>Formatted key-value line.</returns>
    public static string CreateKeyValue(string key, object? value, bool codeFormat = false)
    {
        if (value == null)
        {
            return $"**{key}:** N/A";
        }

        var formattedValue = codeFormat ? $"`{value}`" : value.ToString();
        return $"**{key}:** {formattedValue}";
    }

    /// <summary>
    /// Creates a code block section with optional title.
    /// </summary>
    /// <param name="content">The content to wrap in code blocks.</param>
    /// <param name="title">Optional title for the section.</param>
    /// <returns>Formatted code block section.</returns>
    public static string CreateCodeBlock(string content, string? title = null)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var markdown = new StringBuilder();

        if (!string.IsNullOrEmpty(title))
        {
            _ = markdown.AppendLine($"### {title}");
            _ = markdown.AppendLine();
        }

        _ = markdown.AppendLine("```");
        _ = markdown.AppendLine(content);
        _ = markdown.AppendLine("```");

        return markdown.ToString();
    }

    /// <summary>
    /// Creates a note section in Markdown.
    /// </summary>
    /// <param name="message">The note message.</param>
    /// <returns>Formatted note section.</returns>
    public static string CreateNoteBlock(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var markdown = new StringBuilder();
        _ = markdown.AppendLine("### Note");
        _ = markdown.AppendLine();
        _ = markdown.AppendLine(message);
        _ = markdown.AppendLine();
        return markdown.ToString();
    }

    /// <summary>
    /// Creates a success message with checkmark.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>Formatted success message.</returns>
    public static string CreateSuccessMessage(string message)
    {
        return $"✓ {message}";
    }

    /// <summary>
    /// Creates a warning message with warning symbol.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <returns>Formatted warning message.</returns>
    public static string CreateWarningMessage(string message)
    {
        return $"⚠ {message}";
    }

    /// <summary>
    /// Creates an error message with error symbol.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>Formatted error message.</returns>
    public static string CreateErrorMessage(string message)
    {
        return $"❌ {message}";
    }

    /// <summary>
    /// Creates a table with headers and rows.
    /// </summary>
    /// <param name="headers">The table headers.</param>
    /// <param name="rows">The table rows (each row is an array of values).</param>
    /// <returns>Formatted Markdown table.</returns>
    public static string CreateTable(string[] headers, string[][] rows)
    {
        if (headers.Length == 0)
        {
            return string.Empty;
        }

        var markdown = new StringBuilder();

        // Header row
        _ = markdown.AppendLine("| " + string.Join(" | ", headers) + " |");

        // Separator row
        var separators = headers.Select(_ => "---").ToArray();
        _ = markdown.AppendLine("| " + string.Join(" | ", separators) + " |");

        // Data rows
        foreach (var row in rows)
        {
            var paddedRow = new string[headers.Length];
            for (var i = 0; i < headers.Length; i++)
            {
                paddedRow[i] = i < row.Length ? row[i] : string.Empty;
            }

            _ = markdown.AppendLine("| " + string.Join(" | ", paddedRow) + " |");
        }

        return markdown.ToString();
    }

    /// <summary>
    /// Creates a command result section with all common fields.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="command">The command text.</param>
    /// <param name="state">The command state.</param>
    /// <param name="isSuccess">Whether the command was successful.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    /// <param name="startTime">When the command started (optional).</param>
    /// <param name="endTime">When the command ended (optional).</param>
    /// <param name="executionTime">How long the command took to execute (optional).</param>
    /// <param name="totalTime">Total time including queuing (optional).</param>
    /// <returns>Formatted command result section.</returns>
    public static string CreateCommandResult(
        string commandId,
        string sessionId,
        string command,
        string state,
        bool isSuccess,
        DateTime queuedTime,
        DateTime? startTime = null,
        DateTime? endTime = null,
        TimeSpan? executionTime = null,
        TimeSpan? totalTime = null)
    {
        var markdown = new StringBuilder();
        _ = markdown.AppendLine("## Command Result");
        _ = markdown.AppendLine();
        _ = markdown.AppendLine(CreateKeyValue("Command ID", commandId, true));
        _ = markdown.AppendLine(CreateKeyValue("Session ID", sessionId, true));
        _ = markdown.AppendLine(CreateKeyValue("Command", command, true));
        _ = markdown.AppendLine(CreateKeyValue("State", state));
        _ = markdown.AppendLine(CreateKeyValue("Success", isSuccess));
        _ = markdown.AppendLine(CreateKeyValue("Queued Time", queuedTime.ToString("yyyy-MM-dd HH:mm:ss")));

        if (startTime.HasValue)
        {
            _ = markdown.AppendLine(CreateKeyValue("Start Time", startTime.Value.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        if (endTime.HasValue)
        {
            _ = markdown.AppendLine(CreateKeyValue("End Time", endTime.Value.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        if (executionTime.HasValue)
        {
            _ = markdown.AppendLine(CreateKeyValue("Execution Time", $"{executionTime}"));
        }

        if (totalTime.HasValue)
        {
            _ = markdown.AppendLine(CreateKeyValue("Total Time", $"{totalTime}"));
        }

        _ = markdown.AppendLine();

        return markdown.ToString();
    }

    /// <summary>
    /// Creates a session creation result section.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="dumpFile">The dump file name.</param>
    /// <param name="status">The creation status.</param>
    /// <param name="dumpCheck">The result of the dump check.</param>
    /// <param name="message">Optional status message.</param>
    /// <returns>Formatted session creation result.</returns>
    public static string CreateSessionResult(
        string sessionId,
        string dumpFile,
        string status,
        DumpCheckResult dumpCheck,
        string? message = null)
    {
        var markdown = new StringBuilder();
        _ = markdown.AppendLine("## Session Creation");
        _ = markdown.AppendLine();
        _ = markdown.AppendLine(CreateKeyValue("Session ID", sessionId, true));
        _ = markdown.AppendLine(CreateKeyValue("Dump File", dumpFile, true));
        _ = markdown.AppendLine(CreateKeyValue("Status", status));

        _ = markdown.AppendLine();

        if (!string.IsNullOrEmpty(message))
        {
            _ = status.Equals("Success", StringComparison.OrdinalIgnoreCase)
                ? markdown.AppendLine(CreateSuccessMessage(message))
                : markdown.AppendLine(CreateErrorMessage(message));
        }

        if (dumpCheck.IsEnabled && dumpCheck.WasExecuted)
        {
            _ = markdown.AppendLine();

            if (dumpCheck.TimedOut)
            {
                _ = markdown.AppendLine("## Dump File Validation (dumpchk)");
                _ = markdown.AppendLine();
                _ = markdown.AppendLine(CreateWarningMessage("Validation timed out - session creation continued successfully"));
                _ = markdown.AppendLine();
                _ = markdown.AppendLine(CreateKeyValue("Note", dumpCheck.Message));
            }
            else
            {
                _ = markdown.AppendLine("## Dump file validation result (dumpchk.exe)");
                _ = markdown.AppendLine();
                _ = markdown.AppendLine(CreateKeyValue("Exitcode", dumpCheck.ExitCode));
                _ = markdown.AppendLine(CreateKeyValue("Output", dumpCheck.Message, true));
            }
        }

        return markdown.ToString();
    }

    /// <summary>
    /// Creates a command status summary with table.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commands">Array of command status objects.</param>
    /// <returns>Formatted command status summary.</returns>
    public static string CreateCommandStatusSummary(string sessionId, object[] commands)
    {
        var markdown = new StringBuilder();
        _ = markdown.AppendLine("## Command Status Summary");
        _ = markdown.AppendLine();
        _ = markdown.AppendLine(CreateKeyValue("Session ID", sessionId, true));
        _ = markdown.AppendLine(CreateKeyValue("Total Commands", commands.Length));
        _ = markdown.AppendLine();

        if (commands.Length > 0)
        {
            _ = markdown.AppendLine("### Commands");
            _ = markdown.AppendLine();

            var headers = new[] { "Command ID", "Command", "State", "Success", "Execution Time" };
            var rows = commands.Select(cmd => new string[]
            {
                GetPropertyValue(cmd, "commandId")?.ToString() ?? string.Empty,
                TruncateString(GetPropertyValue(cmd, "command")?.ToString() ?? string.Empty, 50),
                GetPropertyValue(cmd, "state")?.ToString() ?? string.Empty,
                GetPropertyValue(cmd, "isSuccess")?.ToString() ?? "N/A",
                FormatExecutionTime(GetPropertyValue(cmd, "executionTime")),
            }).ToArray();

            _ = markdown.AppendLine(CreateTable(headers, rows));
        }
        else
        {
            _ = markdown.AppendLine("No commands found.");
        }

        return markdown.ToString();
    }

    /// <summary>
    /// Creates a simple operation result (for enqueue, cancel, close operations).
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="keyValues">Key-value pairs to include.</param>
    /// <param name="message">Optional status message.</param>
    /// <param name="isSuccess">Whether the operation was successful.</param>
    /// <returns>Formatted operation result.</returns>
    public static string CreateOperationResult(
        string operation,
        Dictionary<string, object?> keyValues,
        string? message = null,
        bool isSuccess = true)
    {
        var markdown = new StringBuilder();
        _ = markdown.AppendLine($"## {operation}");
        _ = markdown.AppendLine();

        foreach (var kvp in keyValues)
        {
            _ = markdown.AppendLine(CreateKeyValue(kvp.Key, kvp.Value, ShouldCodeFormat(kvp.Key)));
        }

        _ = markdown.AppendLine();

        if (!string.IsNullOrEmpty(message))
        {
            _ = isSuccess ? markdown.AppendLine(CreateSuccessMessage(message)) : markdown.AppendLine(CreateErrorMessage(message));
        }

        return markdown.ToString();
    }

    /// <summary>
    /// Appends command output according to its origin. For extension commands (prefix "Extension: "),
    /// returns the output verbatim (expected to be Markdown). For all other commands, wraps output
    /// in a code block with an optional title.
    /// </summary>
    /// <param name="command">The command label as stored in command info.</param>
    /// <param name="output">The raw output to append.</param>
    /// <param name="titleForNonExtension">Optional title when wrapping non-extension output.</param>
    /// <returns>Markdown to append for the given output.</returns>
    public static string AppendOutputForCommand(string? command, string? output, string? titleForNonExtension = "Output")
    {
        return string.IsNullOrEmpty(output)
            ? string.Empty
            : !string.IsNullOrEmpty(command) && command.StartsWith("Extension: ", StringComparison.Ordinal)
            ? output
            : CreateCodeBlock(output, titleForNonExtension);
    }

    /// <summary>
    /// Helper method to get a property value from an object via reflection.
    /// </summary>
    /// <param name="obj">The object that contains the property.</param>
    /// <param name="propertyName">The name of the property to read.</param>
    /// <returns>The value of the property, or <c>null</c> if the property is not found.</returns>
    private static object? GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Helper method to determine if a key should be formatted as inline code.
    /// </summary>
    /// <param name="key">The key to evaluate.</param>
    /// <returns><c>true</c> if the key indicates code formatting; otherwise, <c>false</c>.</returns>
    private static bool ShouldCodeFormat(string key)
    {
        return key.Contains("ID") || key.Contains("Command") || key.Contains("Path") || key.Contains("File");
    }

    /// <summary>
    /// Helper method to format an execution time value.
    /// </summary>
    /// <param name="executionTime">A value that may be a <see cref="TimeSpan"/>.</param>
    /// <returns>The formatted time if a <see cref="TimeSpan"/> is provided; otherwise, "N/A".</returns>
    private static string FormatExecutionTime(object? executionTime)
    {
        return executionTime is TimeSpan ts ? $"{ts}" : "N/A";
    }

    /// <summary>
    /// Helper method to truncate long strings.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum allowed length including the ellipsis.</param>
    /// <returns>
    /// The original string if it is shorter than or equal to <paramref name="maxLength"/>;
    /// otherwise, a truncated version ending with "...".
    /// </returns>
    private static string TruncateString(string value, int maxLength)
    {
        return string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}
