using NLog;

using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for getting the status of all commands in a session.
/// </summary>
internal class GetDumpAnalyzeCommandsStatusTool
{
    /// <summary>
    /// Gets the status of all commands in a session. Efficient for monitoring multiple commands.
    /// </summary>
    /// <param name="sessionId">Session ID from winaidbg_open_dump_analyze_session.</param>
    /// <returns>Array of command status information.</returns>
    public Task<object> Execute(
        string sessionId)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Getting all command statuses for session: {SessionId}", sessionId);

        try
        {
            _ = ToolInputValidator.EnsureSessionExists(sessionId);

            var allCommands = EngineService.Get().GetAllCommandInfos(sessionId);

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
            }).ToArray();

            logger.Info("Retrieved status for {Count} commands in session {SessionId}", commandStatuses.Length, sessionId);

            var markdown = MarkdownFormatter.CreateCommandStatusSummary(sessionId, commandStatuses);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return Task.FromResult<object>(markdown);
        }
        catch (McpToolUserInputException ex)
        {
            logger.Warn(ex, "Invalid inputs for get command statuses");
            throw;
        }
        catch (ArgumentException ex)
        {
            var message = string.Format("Invalid session ID: {Message}", ex.Message);
            logger.Error(ex, message);
            throw new McpToolUserInputException(message, ex);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error getting command statuses");
            throw;
        }
    }
}
