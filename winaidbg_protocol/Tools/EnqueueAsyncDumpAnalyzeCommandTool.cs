using NLog;

using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for enqueuing debugging commands for asynchronous execution.
/// </summary>
internal class EnqueueAsyncDumpAnalyzeCommandTool
{
    /// <summary>
    /// Enqueues a debugging command for asynchronous execution in the specified session.
    /// </summary>
    /// <param name="sessionId">Session ID from winaidbg_open_dump_analyze_session.</param>
    /// <param name="command">WinDbg/CDB command to execute.</param>
    /// <returns>Command enqueue result with commandId.</returns>
    public Task<object> Execute(
        string sessionId,
        string command)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Enqueuing command in session {SessionId}: {Command}", sessionId, command);

        try
        {
            ToolInputValidator.EnsureNonEmpty(command, "command");
            ToolInputValidator.EnsureSessionIsActive(sessionId);

            var commandId = EngineService.Get().EnqueueCommand(sessionId, command);

            logger.Info("Command enqueued: {CommandId} in session {SessionId}", commandId, sessionId);

            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Queued" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Enqueued",
                keyValues,
                $"Command {commandId} queued successfully",
                true);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return Task.FromResult<object>(markdown);
        }
        catch (McpToolUserInputException ex)
        {
            logger.Warn(ex, "Invalid inputs for command enqueue");
            throw;
        }
        catch (ArgumentException ex)
        {
            var message = string.Format("Invalid argument: {Message}", ex.Message);
            logger.Error(ex, message);
            throw new McpToolUserInputException(message, ex);
        }
        catch (InvalidOperationException ex)
        {
            var message = string.Format("Cannot enqueue command: {Message}", ex.Message);
            logger.Error(ex, message);
            throw new McpToolUserInputException(message, ex);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error enqueuing command");
            throw;
        }
    }
}
