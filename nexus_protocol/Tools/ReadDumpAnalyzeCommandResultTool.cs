using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using nexus.engine;

namespace nexus.protocol.Tools;

/// <summary>
/// MCP tool for reading the result of a previously enqueued command.
/// </summary>
[McpServerToolType]
internal static class ReadDumpAnalyzeCommandResultTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Reads the result of a previously enqueued command. Waits for command completion.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="commandId">Command ID from nexus_enqueue_async_dump_analyze_command.</param>
    /// <returns>Command result with output and status.</returns>
    [McpServerTool, Description("Reads the result of a command. Waits for completion if still executing.")]
    public static async Task<object> nexus_read_dump_analyze_command_result(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("Command ID from nexus_enqueue_async_dump_analyze_command")] string commandId)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ReadDumpAnalyzeCommandResultTool");
        var debugEngine = serviceProvider.GetRequiredService<IDebugEngine>();

        logger.LogInformation("Reading command result: {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            var commandInfo = await debugEngine.GetCommandInfoAsync(sessionId, commandId);

            logger.LogInformation("Command {CommandId} result retrieved: State={State}", commandId, commandInfo.State);

            return new
            {
                commandId = commandInfo.CommandId,
                sessionId,
                command = commandInfo.Command,
                state = commandInfo.State.ToString(),
                output = commandInfo.Output,
                isSuccess = commandInfo.IsSuccess,
                errorMessage = commandInfo.ErrorMessage,
                queuedTime = commandInfo.QueuedTime,
                startTime = commandInfo.StartTime,
                endTime = commandInfo.EndTime,
                executionTime = commandInfo.ExecutionTime,
                totalTime = commandInfo.TotalTime,
                operation = "nexus_read_dump_analyze_command_result",
                usage = UsageField
            };
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            return new
            {
                commandId,
                sessionId,
                command = (string?)null,
                state = "Failed",
                output = (string?)null,
                isSuccess = false,
                errorMessage = ex.Message,
                operation = "nexus_read_dump_analyze_command_result",
                usage = UsageField
            };
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogError(ex, "Command not found: {CommandId}", commandId);
            return new
            {
                commandId,
                sessionId,
                command = (string?)null,
                state = "NotFound",
                output = (string?)null,
                isSuccess = false,
                errorMessage = "Command not found",
                operation = "nexus_read_dump_analyze_command_result",
                usage = UsageField
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error reading command result");
            return new
            {
                commandId,
                sessionId,
                command = (string?)null,
                state = "Failed",
                output = (string?)null,
                isSuccess = false,
                errorMessage = $"Unexpected error: {ex.Message}",
                operation = "nexus_read_dump_analyze_command_result",
                usage = UsageField
            };
        }
    }
}

