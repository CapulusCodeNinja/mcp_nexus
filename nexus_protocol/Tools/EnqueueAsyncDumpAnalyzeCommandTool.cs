using System.ComponentModel;

using ModelContextProtocol.Server;

using Nexus.Engine;
using Nexus.Protocol.Utilities;

using NLog;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for enqueuing debugging commands for asynchronous execution.
/// </summary>
[McpServerToolType]
internal static class EnqueueAsyncDumpAnalyzeCommandTool
{
    /// <summary>
    /// Enqueues a debugging command for asynchronous execution in the specified session.
    /// </summary>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="command">WinDbg/CDB command to execute.</param>
    /// <returns>Command enqueue result with commandId.</returns>
    [McpServerTool]
    [Description("Enqueues a debugging command for asynchronous execution. Returns commandId for tracking.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static Task<object> Nexus_enqueue_async_dump_analyze_command(
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("WinDbg/CDB command to execute (e.g., 'k', '!analyze -v', 'lm')")] string command)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Enqueuing command in session {SessionId}: {Command}", sessionId, command);

        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException("sessionId cannot be empty", nameof(sessionId));
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("command cannot be empty", nameof(command));
            }

            var commandId = DebugEngine.Instance.EnqueueCommand(sessionId, command);

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
        catch (ArgumentException ex)
        {
            logger.Error(ex, "Invalid argument: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Failed" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Enqueue Failed",
                keyValues,
                ex.Message,
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return Task.FromResult<object>(markdown);
        }
        catch (InvalidOperationException ex)
        {
            logger.Error(ex, "Cannot enqueue command: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Failed" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Enqueue Failed",
                keyValues,
                ex.Message,
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return Task.FromResult<object>(markdown);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error enqueuing command");
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Failed" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Enqueue Failed",
                keyValues,
                $"Unexpected error: {ex.Message}",
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return Task.FromResult<object>(markdown);
        }
    }
}
