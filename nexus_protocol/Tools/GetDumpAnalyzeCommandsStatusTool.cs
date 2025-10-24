using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using nexus.engine;

namespace nexus.protocol.Tools;

/// <summary>
/// MCP tool for getting the status of all commands in a session.
/// </summary>
[McpServerToolType]
internal static class GetDumpAnalyzeCommandsStatusTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Gets the status of all commands in a session. Efficient for monitoring multiple commands.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <returns>Array of command status information.</returns>
    [McpServerTool, Description("Gets status of all commands in a session. Use for efficient bulk monitoring.")]
    public static Task<object> nexus_get_dump_analyze_commands_status(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("GetDumpAnalyzeCommandsStatusTool");
        var debugEngine = Loader.EngineLoader.GetDebugEngine();

        logger.LogInformation("Getting all command statuses for session: {SessionId}", sessionId);

        try
        {
            var allCommands = debugEngine.GetAllCommandInfos(sessionId);

            var commandStatuses = allCommands.Values.Select(cmd => new
            {
                commandId = cmd.CommandId,
                command = cmd.Command,
                state = cmd.State.ToString(),
                queuedTime = cmd.QueuedTime,
                startTime = cmd.StartTime,
                endTime = cmd.EndTime,
                executionTime = cmd.ExecutionTime,
                totalTime = cmd.TotalTime,
                isSuccess = cmd.IsSuccess,
                hasOutput = !string.IsNullOrEmpty(cmd.Output)
            }).ToArray();

            logger.LogInformation("Retrieved status for {Count} commands in session {SessionId}", commandStatuses.Length, sessionId);

            return Task.FromResult<object>(new
            {
                commands = commandStatuses,
                count = commandStatuses.Length,
                sessionId,
                operation = "nexus_get_dump_analyze_commands_status",
                usage = UsageField
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid session ID: {Message}", ex.Message);
            return Task.FromResult<object>(new
            {
                commands = Array.Empty<object>(),
                count = 0,
                sessionId,
                operation = "nexus_get_dump_analyze_commands_status",
                error = ex.Message,
                usage = UsageField
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error getting command statuses");
            return Task.FromResult<object>(new
            {
                commands = Array.Empty<object>(),
                count = 0,
                sessionId,
                operation = "nexus_get_dump_analyze_commands_status",
                error = $"Unexpected error: {ex.Message}",
                usage = UsageField
            });
        }
    }
}

