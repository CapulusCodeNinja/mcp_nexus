using NLog;

using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for reading the result of a previously enqueued command.
/// </summary>
internal class ReadDumpAnalyzeCommandResultTool
{
    /// <summary>
    /// Reads the result of a previously enqueued command. Waits for command completion.
    /// </summary>
    /// <param name="sessionId">Session ID from winaidbg_open_dump_analyze_session.</param>
    /// <param name="commandId">Command ID from winaidbg_enqueue_async_dump_analyze_command.</param>
    /// <returns>Command result with output and status.</returns>
    public async Task<object> Execute(
        string sessionId,
        string commandId)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Reading command result: {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            var commandInfo = await EngineService.Get().GetCommandInfoAsync(sessionId, commandId);

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
                markdown += MarkdownFormatter.AppendOutputForCommand(commandInfo.Command, commandInfo.AggregatedOutput, "Output");
            }

            if (!string.IsNullOrEmpty(commandInfo.ErrorMessage))
            {
                markdown += MarkdownFormatter.CreateCodeBlock(commandInfo.ErrorMessage, "Error");
            }

            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
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
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
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
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
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
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
    }
}
