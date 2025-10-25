using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Engine;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for reading the result of a previously enqueued command.
/// </summary>
[McpServerToolType]
internal static class ReadDumpAnalyzeCommandResultTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Reads the result of a previously enqueued command. Waits for command completion.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="commandId">Command ID from nexus_enqueue_async_dump_analyze_command.</param>
    /// <returns>Command result with output and status.</returns>
    [McpServerTool, Description("Reads the result of a command. Waits for completion if still executing.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static async Task<object> nexus_read_dump_analyze_command_result(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("Command ID from nexus_enqueue_async_dump_analyze_command")] string commandId)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ReadDumpAnalyzeCommandResultTool");

        logger.LogInformation("Reading command result: {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            var commandInfo = await DebugEngine.Instance.GetCommandInfoAsync(sessionId, commandId);

            logger.LogInformation("Command {CommandId} result retrieved: State={State}", commandId, commandInfo.State);

            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Result");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** `{commandInfo.CommandId}`");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Command:** `{commandInfo.Command}`");
            markdown.AppendLine($"**State:** {commandInfo.State}");
            markdown.AppendLine($"**Success:** {commandInfo.IsSuccess}");
            markdown.AppendLine($"**Queued Time:** {commandInfo.QueuedTime:yyyy-MM-dd HH:mm:ss}");
            if (commandInfo.StartTime.HasValue)
                markdown.AppendLine($"**Start Time:** {commandInfo.StartTime:yyyy-MM-dd HH:mm:ss}");
            if (commandInfo.EndTime.HasValue)
                markdown.AppendLine($"**End Time:** {commandInfo.EndTime:yyyy-MM-dd HH:mm:ss}");
            if (commandInfo.ExecutionTime.HasValue)
                markdown.AppendLine($"**Execution Time:** {commandInfo.ExecutionTime.Value.TotalSeconds:F2}s");
            if (commandInfo.TotalTime.HasValue)
                markdown.AppendLine($"**Total Time:** {commandInfo.TotalTime.Value.TotalSeconds:F2}s");
            markdown.AppendLine();

            if (!string.IsNullOrEmpty(commandInfo.Output))
            {
                markdown.AppendLine("### Output");
                markdown.AppendLine("```");
                markdown.AppendLine(commandInfo.Output);
                markdown.AppendLine("```");
            }

            if (!string.IsNullOrEmpty(commandInfo.ErrorMessage))
            {
                markdown.AppendLine();
                markdown.AppendLine("### Error");
                markdown.AppendLine("```");
                markdown.AppendLine(commandInfo.ErrorMessage);
                markdown.AppendLine("```");
            }

            return markdown.ToString();
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Result");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** `{commandId}`");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Command:** N/A");
            markdown.AppendLine($"**State:** Failed");
            markdown.AppendLine($"**Success:** False");
            markdown.AppendLine();
            markdown.AppendLine("### Error");
            markdown.AppendLine("```");
            markdown.AppendLine(ex.Message);
            markdown.AppendLine("```");
            return markdown.ToString();
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogError(ex, "Command not found: {CommandId}", commandId);
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Result");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** `{commandId}`");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Command:** N/A");
            markdown.AppendLine($"**State:** NotFound");
            markdown.AppendLine($"**Success:** False");
            markdown.AppendLine();
            markdown.AppendLine("### Error");
            markdown.AppendLine("```");
            markdown.AppendLine("Command not found");
            markdown.AppendLine("```");
            return markdown.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error reading command result");
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Result");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** `{commandId}`");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Command:** N/A");
            markdown.AppendLine($"**State:** Failed");
            markdown.AppendLine($"**Success:** False");
            markdown.AppendLine();
            markdown.AppendLine("### Error");
            markdown.AppendLine("```");
            markdown.AppendLine($"Unexpected error: {ex.Message}");
            markdown.AppendLine("```");
            return markdown.ToString();
        }
    }
}

