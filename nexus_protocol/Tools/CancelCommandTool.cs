using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Engine;
using Nexus.Protocol.Utilities;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for canceling a queued or executing command.
/// </summary>
[McpServerToolType]
internal static class CancelCommandTool
{
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

            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Cancelled", cancelled },
                { "Status", cancelled ? "Cancelled" : "NotFound" }
            };

            var message = cancelled 
                ? $"Command {commandId} cancelled successfully"
                : $"Command {commandId} not found or already completed";

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Command Cancellation",
                keyValues,
                message,
                cancelled);

            return Task.FromResult<object>(markdown);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Cancelled", false },
                { "Status", "Failed" }
            };

            return Task.FromResult<object>(MarkdownFormatter.CreateOperationResult(
                "Command Cancellation Failed",
                keyValues,
                ex.Message,
                false));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error cancelling command");
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Cancelled", false },
                { "Status", "Failed" }
            };

            return Task.FromResult<object>(MarkdownFormatter.CreateOperationResult(
                "Command Cancellation Failed",
                keyValues,
                $"Unexpected error: {ex.Message}",
                false));
        }
    }
}

