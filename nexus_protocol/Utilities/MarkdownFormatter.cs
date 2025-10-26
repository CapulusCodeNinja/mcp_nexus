using System.Text;

namespace Nexus.Protocol.Utilities;

/// <summary>
/// Utility class for formatting MCP tool responses as Markdown.
/// Provides consistent formatting patterns and reduces code duplication.
/// </summary>
internal static class MarkdownFormatter
{
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
                paddedRow[i] = i < row.Length ? row[i] : "";
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
            _ = markdown.AppendLine(CreateKeyValue("Execution Time", $"{executionTime.Value.TotalSeconds:F2}s"));
        }

        if (totalTime.HasValue)
        {
            _ = markdown.AppendLine(CreateKeyValue("Total Time", $"{totalTime.Value.TotalSeconds:F2}s"));
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
    /// <param name="symbolsPath">Optional symbols path.</param>
    /// <param name="message">Optional status message.</param>
    /// <returns>Formatted session creation result.</returns>
    public static string CreateSessionResult(
        string sessionId,
        string dumpFile,
        string status,
        string? symbolsPath = null,
        string? message = null)
    {
        var markdown = new StringBuilder();
        _ = markdown.AppendLine("## Session Creation");
        _ = markdown.AppendLine();
        _ = markdown.AppendLine(CreateKeyValue("Session ID", sessionId, true));
        _ = markdown.AppendLine(CreateKeyValue("Dump File", dumpFile, true));
        _ = markdown.AppendLine(CreateKeyValue("Status", status));

        if (!string.IsNullOrEmpty(symbolsPath))
        {
            _ = markdown.AppendLine(CreateKeyValue("Symbols Path", symbolsPath, true));
        }

        _ = markdown.AppendLine();

        if (!string.IsNullOrEmpty(message))
        {
            _ = status.Equals("Success", StringComparison.OrdinalIgnoreCase)
                ? markdown.AppendLine(CreateSuccessMessage(message))
                : markdown.AppendLine(CreateErrorMessage(message));
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
                GetPropertyValue(cmd, "commandId")?.ToString() ?? "",
                TruncateString(GetPropertyValue(cmd, "command")?.ToString() ?? "", 50),
                GetPropertyValue(cmd, "state")?.ToString() ?? "",
                GetPropertyValue(cmd, "isSuccess")?.ToString() ?? "N/A",
                FormatExecutionTime(GetPropertyValue(cmd, "executionTime"))
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
    /// Helper method to get property value from anonymous object.
    /// </summary>
    private static object? GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Helper method to determine if a key should be code-formatted.
    /// </summary>
    private static bool ShouldCodeFormat(string key)
    {
        return key.Contains("ID") || key.Contains("Command") || key.Contains("Path") || key.Contains("File");
    }

    /// <summary>
    /// Helper method to format execution time.
    /// </summary>
    private static string FormatExecutionTime(object? executionTime)
    {
        return executionTime is TimeSpan ts ? $"{ts.TotalSeconds:F2}s" : "N/A";
    }

    /// <summary>
    /// Helper method to truncate long strings.
    /// </summary>
    private static string TruncateString(string value, int maxLength)
    {
        return string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}
