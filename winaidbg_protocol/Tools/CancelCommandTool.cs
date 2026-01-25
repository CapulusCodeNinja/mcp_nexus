using System.ComponentModel;

using ModelContextProtocol.Server;

using NLog;

using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for canceling a queued or executing command.
/// </summary>
[McpServerToolType]
internal static class CancelCommandTool
{
    /// <summary>
    /// Cancels a queued or executing command in a session.
    /// </summary>
    /// <param name="sessionId">Session ID from winaidbg_open_dump_analyze_session.</param>
    /// <param name="commandId">Command ID from winaidbg_enqueue_async_dump_analyze_command.</param>
    /// <returns>Command cancellation result.</returns>
    [McpServerTool]
    [Description("Cancels a queued or executing command.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Required for interoperability with external system")]
    public static Task<object> winaidbg_cancel_dump_analyze_command(
        [Description("Session ID from winaidbg_open_dump_analyze_session")] string sessionId,
        [Description("Command ID to cancel")] string commandId)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Cancelling command {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            var cancelled = EngineService.Get().CancelCommand(sessionId, commandId);

            logger.Info("Command {CommandId} cancellation: {Result}", commandId, cancelled ? "Success" : "NotFound");

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
        catch (ArgumentException ex)
        {
            logger.Error(ex, "Invalid argument: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Cancelled", false },
                { "Status", "Failed" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Cancellation Failed",
                keyValues,
                ex.Message,
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return Task.FromResult<object>(markdown);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error cancelling command");
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Cancelled", false },
                { "Status", "Failed" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Cancellation Failed",
                keyValues,
                $"Unexpected error: {ex.Message}",
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return Task.FromResult<object>(markdown);
        }
    }
}
