using NLog;

using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for canceling a queued or executing command.
/// </summary>
internal class CancelCommandTool
{
    /// <summary>
    /// Cancels a queued or executing command in a session.
    /// </summary>
    /// <param name="sessionId">Session ID from winaidbg_open_dump_analyze_session.</param>
    /// <param name="commandId">Command ID from winaidbg_enqueue_async_dump_analyze_command.</param>
    /// <returns>Command cancellation result.</returns>
    public Task<object> Execute(
        string sessionId,
        string commandId)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Cancelling command {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            ToolInputValidator.EnsureNonEmpty(commandId, "commandId");
            _ = ToolInputValidator.EnsureSessionExists(sessionId);

            var cancelled = EngineService.Get().CancelCommand(sessionId, commandId);

            logger.Info("Command {CommandId} cancellation: {Result}", commandId, cancelled ? "Success" : "NotFound");

            if (!cancelled)
            {
                throw new McpToolUserInputException(
                    $"Invalid `commandId`: `{commandId}` was not found for session `{sessionId}` (or already completed). Use `winaidbg_get_dump_analyze_commands_status` to list known commandIds.");
            }

            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Cancelled", cancelled },
                { "Status", cancelled ? "Cancelled" : "NotFound" },
            };

            var message = cancelled
                ? $"Command {commandId} cancelled successfully"
                : $"Command {commandId} not found or already completed";

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Cancellation",
                keyValues,
                message,
                cancelled);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return Task.FromResult<object>(markdown);
        }
        catch (McpToolUserInputException ex)
        {
            logger.Warn(ex, "Invalid inputs for command cancellation");
            throw;
        }
        catch (ArgumentException ex)
        {
            var message = string.Format("Invalid argument: {Message}", ex.Message);
            logger.Error(ex, message);
            throw new McpToolUserInputException(message, ex);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error cancelling command");
            throw;
        }
    }
}
