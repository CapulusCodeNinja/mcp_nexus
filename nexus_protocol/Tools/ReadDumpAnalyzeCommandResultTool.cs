using System.ComponentModel;

using ModelContextProtocol.Server;

using Nexus.Engine;
using Nexus.Protocol.Utilities;

using NLog;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for reading the result of a previously enqueued command.
/// </summary>
[McpServerToolType]
internal static class ReadDumpAnalyzeCommandResultTool
{
    /// <summary>
    /// Reads the result of a previously enqueued command. Waits for command completion.
    /// </summary>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="commandId">Command ID from nexus_enqueue_async_dump_analyze_command.</param>
    /// <returns>Command result with output and status.</returns>
    [McpServerTool, Description("Reads the result of a command. Waits for completion if still executing.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static async Task<object> nexus_read_dump_analyze_command_result(
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("Command ID from nexus_enqueue_async_dump_analyze_command")] string commandId)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Reading command result: {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            var commandInfo = await DebugEngine.Instance.GetCommandInfoAsync(sessionId, commandId);

            logger.Info("Command {CommandId} result retrieved: State={State}", commandId, commandInfo.State);

            var markdown = MarkdownFormatter.CreateCommandResult(
                commandInfo.CommandId,
                sessionId,
                commandInfo.Command,
                commandInfo.State.ToString(),
                commandInfo.IsSuccess ?? false,
                commandInfo.QueuedTime,
                commandInfo.StartTime,
                commandInfo.EndTime,
                commandInfo.ExecutionTime,
                commandInfo.TotalTime);

            if (!string.IsNullOrEmpty(commandInfo.AggregatedOutput))
            {
                markdown += MarkdownFormatter.CreateCodeBlock(commandInfo.AggregatedOutput, "Output");
            }

            if (!string.IsNullOrEmpty(commandInfo.ErrorMessage))
            {
                markdown += MarkdownFormatter.CreateCodeBlock(commandInfo.ErrorMessage, "Error");
            }

            return markdown;
        }
        catch (ArgumentException ex)
        {
            logger.Error(ex, "Invalid argument: {Message}", ex.Message);
            var markdown = MarkdownFormatter.CreateCommandResult(
                commandId,
                sessionId,
                "N/A",
                "Failed",
                false,
                DateTime.Now);

            markdown += MarkdownFormatter.CreateCodeBlock(ex.Message, "Error");
            return markdown;
        }
        catch (KeyNotFoundException ex)
        {
            logger.Error(ex, "Command not found: {CommandId}", commandId);
            var markdown = MarkdownFormatter.CreateCommandResult(
                commandId,
                sessionId,
                "N/A",
                "NotFound",
                false,
                DateTime.Now);

            markdown += MarkdownFormatter.CreateCodeBlock("Command not found", "Error");
            return markdown;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error reading command result");
            var markdown = MarkdownFormatter.CreateCommandResult(
                commandId,
                sessionId,
                "N/A",
                "Failed",
                false,
                DateTime.Now);

            markdown += MarkdownFormatter.CreateCodeBlock($"Unexpected error: {ex.Message}", "Error");
            return markdown;
        }
    }
}

