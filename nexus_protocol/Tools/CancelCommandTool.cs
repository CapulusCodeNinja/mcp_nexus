using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Engine;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for canceling a queued or executing command.
/// </summary>
[McpServerToolType]
internal static class CancelCommandTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Cancels a queued or executing command in a session.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="commandId">Command ID from nexus_enqueue_async_dump_analyze_command.</param>
    /// <returns>Command cancellation result.</returns>
    [McpServerTool, Description("Cancels a queued or executing command.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static Task<object> nexus_cancel_dump_analyze_command(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("Command ID to cancel")] string commandId)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CancelCommandTool");

        logger.LogInformation("Cancelling command {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            var cancelled = DebugEngine.Instance.CancelCommand(sessionId, commandId);

            logger.LogInformation("Command {CommandId} cancellation: {Result}", commandId, cancelled ? "Success" : "NotFound");

            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Cancellation");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** `{commandId}`");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Cancelled:** {cancelled}");
            markdown.AppendLine($"**Status:** {(cancelled ? "Cancelled" : "NotFound")}");
            markdown.AppendLine();
            if (cancelled)
                markdown.AppendLine($"✓ Command {commandId} cancelled successfully");
            else
                markdown.AppendLine($"⚠ Command {commandId} not found or already completed");

            return Task.FromResult<object>(markdown.ToString());
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Cancellation Failed");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** `{commandId}`");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Cancelled:** False");
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
            logger.LogError(ex, "Unexpected error cancelling command");
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Cancellation Failed");
            markdown.AppendLine();
            markdown.AppendLine($"**Command ID:** `{commandId}`");
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Cancelled:** False");
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

