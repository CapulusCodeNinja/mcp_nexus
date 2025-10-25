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
internal static class EnqueueAsyncExtensionCommandTool
{
    /// <summary>
    /// Enqueues a debugging command for asynchronous execution in the specified session.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="command">WinDbg/CDB command to execute.</param>
    /// <returns>Command enqueue result with commandId.</returns>
    [McpServerTool, Description("Enqueues a debugging command for asynchronous execution. Returns commandId for tracking.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static Task<object> nexus_enqueue_async_extension_command(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("WinDbg/CDB command to execute (e.g., 'k', '!analyze -v', 'lm')")] string command)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Enqueuing extension script in session {SessionId}: {Command}", sessionId, command);

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

            var commandId = DebugEngine.Instance.EnqueueExtensionScript(sessionId, command);

            logger.Info("Extension Script enqueued: {CommandId} in session {SessionId}", commandId, sessionId);

            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Queued" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueued",
                keyValues,
                $"Extension Script {commandId} queued successfully",
                true);

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
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueue Failed",
                keyValues,
                ex.Message,
                false);

            return Task.FromResult<object>(markdown);
        }
        catch (InvalidOperationException ex)
        {
            logger.Error(ex, "Cannot enqueue  extension script: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueue Failed",
                keyValues,
                ex.Message,
                false);

            return Task.FromResult<object>(markdown);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error enqueuing extension script ");
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueue Failed",
                keyValues,
                $"Unexpected error: {ex.Message}",
                false);

            return Task.FromResult<object>(markdown);
        }
    }
}

