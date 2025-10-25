using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Engine;

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

            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Enqueued");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** `{commandId}`");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Command:** `{command}`");
            markdown.AppendLine($"**Status:** Queued");
            markdown.AppendLine();
            markdown.AppendLine($"✓ Command {commandId} queued successfully");

            return Task.FromResult<object>(markdown.ToString());
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Enqueue Failed");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** N/A");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Command:** `{command}`");
            markdown.AppendLine($"**Status:** Failed");
            markdown.AppendLine();
            markdown.AppendLine("### Error");
            markdown.AppendLine("```");
            markdown.AppendLine(ex.Message);
            markdown.AppendLine("```");
            return Task.FromResult<object>(markdown.ToString());
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Cannot enqueue command: {Message}", ex.Message);
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Enqueue Failed");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** N/A");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Command:** `{command}`");
            markdown.AppendLine($"**Status:** Failed");
            markdown.AppendLine();
            markdown.AppendLine("### Error");
            markdown.AppendLine("```");
            markdown.AppendLine(ex.Message);
            markdown.AppendLine("```");
            return Task.FromResult<object>(markdown.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error enqueuing command");
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Enqueue Failed");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** N/A");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Command:** `{command}`");
            markdown.AppendLine($"**Status:** Failed");
            markdown.AppendLine();
            markdown.AppendLine("### Error");
            markdown.AppendLine("```");
            markdown.AppendLine($"Unexpected error: {ex.Message}");
            markdown.AppendLine("```");
            return Task.FromResult<object>(markdown.ToString());
        }
    }
}

