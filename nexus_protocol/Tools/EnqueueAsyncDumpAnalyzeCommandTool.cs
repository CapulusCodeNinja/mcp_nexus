using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using nexus.engine;

namespace nexus.protocol.Tools;

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
    public static Task<object> nexus_enqueue_async_dump_analyze_command(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("WinDbg/CDB command to execute (e.g., 'k', '!analyze -v', 'lm')")] string command)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EnqueueAsyncDumpAnalyzeCommandTool");
        var debugEngine = Loader.EngineLoader.GetDebugEngine();

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

            var commandId = debugEngine.EnqueueCommand(sessionId, command);

            logger.LogInformation("Command enqueued: {CommandId} in session {SessionId}", commandId, sessionId);

            return Task.FromResult<object>(new
            {
                commandId,
                sessionId,
                command,
                status = "Queued",
                operation = "nexus_enqueue_async_dump_analyze_command",
                message = $"Command {commandId} queued successfully",
                usage = UsageField
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            return Task.FromResult<object>(new
            {
                commandId = (string?)null,
                sessionId,
                command,
                status = "Failed",
                operation = "nexus_enqueue_async_dump_analyze_command",
                message = ex.Message,
                usage = UsageField
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Cannot enqueue command: {Message}", ex.Message);
            return Task.FromResult<object>(new
            {
                commandId = (string?)null,
                sessionId,
                command,
                status = "Failed",
                operation = "nexus_enqueue_async_dump_analyze_command",
                message = ex.Message,
                usage = UsageField
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error enqueuing command");
            return Task.FromResult<object>(new
            {
                commandId = (string?)null,
                sessionId,
                command,
                status = "Failed",
                operation = "nexus_enqueue_async_dump_analyze_command",
                message = $"Unexpected error: {ex.Message}",
                usage = UsageField
            });
        }
    }
}

