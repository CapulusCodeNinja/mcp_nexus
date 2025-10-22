using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using nexus.engine;

namespace nexus.protocol.Tools;

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
    public static Task<object> nexus_cancel_command(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("Command ID to cancel")] string commandId)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CancelCommandTool");
        var debugEngine = serviceProvider.GetRequiredService<IDebugEngine>();

        logger.LogInformation("Cancelling command {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            var cancelled = debugEngine.CancelCommand(sessionId, commandId);

            logger.LogInformation("Command {CommandId} cancellation: {Result}", commandId, cancelled ? "Success" : "NotFound");

            return Task.FromResult<object>(new
            {
                commandId,
                sessionId,
                cancelled,
                status = cancelled ? "Cancelled" : "NotFound",
                operation = "nexus_cancel_command",
                message = cancelled ? $"Command {commandId} cancelled successfully" : $"Command {commandId} not found or already completed",
                usage = UsageField
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            return Task.FromResult<object>(new
            {
                commandId,
                sessionId,
                cancelled = false,
                status = "Failed",
                operation = "nexus_cancel_command",
                message = ex.Message,
                usage = UsageField
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error cancelling command");
            return Task.FromResult<object>(new
            {
                commandId,
                sessionId,
                cancelled = false,
                status = "Failed",
                operation = "nexus_cancel_command",
                message = $"Unexpected error: {ex.Message}",
                usage = UsageField
            });
        }
    }
}

