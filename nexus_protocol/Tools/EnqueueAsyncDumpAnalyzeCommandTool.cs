using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Engine;
using Nexus.Protocol.Utilities;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for enqueuing debugging commands for asynchronous execution.
/// </summary>
[McpServerToolType]
internal static class EnqueueAsyncDumpAnalyzeCommandTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Enqueues a debugging command for asynchronous execution in the specified session.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="command">WinDbg/CDB command to execute.</param>
    /// <returns>Command enqueue result with commandId.</returns>
    [McpServerTool, Description("Enqueues a debugging command for asynchronous execution. Returns commandId for tracking.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static Task<object> nexus_enqueue_async_dump_analyze_command(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("WinDbg/CDB command to execute (e.g., 'k', '!analyze -v', 'lm')")] string command)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EnqueueAsyncDumpAnalyzeCommandTool");

        logger.LogInformation("Enqueuing command in session {SessionId}: {Command}", sessionId, command);

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

            logger.LogInformation("Command enqueued: {CommandId} in session {SessionId}", commandId, sessionId);

            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Queued" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Enqueued",
                keyValues,
                $"Command {commandId} queued successfully",
                true);

            return Task.FromResult<object>(markdown);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Enqueue Failed",
                keyValues,
                ex.Message,
                false);

            return Task.FromResult<object>(markdown);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Cannot enqueue command: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Enqueue Failed",
                keyValues,
                ex.Message,
                false);

            return Task.FromResult<object>(markdown);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error enqueuing command");
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Command", command },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Enqueue Failed",
                keyValues,
                $"Unexpected error: {ex.Message}",
                false);

            return Task.FromResult<object>(markdown);
        }
    }
}

