using NLog;

using WinAiDbg.Engine.Share;
using WinAiDbg.Engine.Share.Models;
using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for reading the result of a previously enqueued command.
/// </summary>
internal class ReadDumpAnalyzeCommandResultTool
{
    private const int MaxAllowedWaitSeconds = 30;

    /// <summary>
    /// Reads the result of a previously enqueued command.
    /// Waits up to <paramref name="maxWaitSeconds"/> for command completion, then returns the current command state.
    /// </summary>
    /// <param name="sessionId">Session ID from winaidbg_open_dump_analyze_session.</param>
    /// <param name="commandId">Command ID from winaidbg_enqueue_async_dump_analyze_command.</param>
    /// <param name="maxWaitSeconds">Maximum number of seconds to wait for completion (must be >= 1).</param>
    /// <returns>Command result with output and status.</returns>
    public async Task<object> Execute(
        string sessionId,
        string commandId,
        int maxWaitSeconds)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Reading command result: {CommandId} in session {SessionId}", commandId, sessionId);

        try
        {
            ToolInputValidator.EnsureNonEmpty(commandId, "commandId");
            _ = ToolInputValidator.EnsureSessionExists(sessionId);

            if (maxWaitSeconds is < 1 or > MaxAllowedWaitSeconds)
            {
                throw new McpToolUserInputException(
                    $"Invalid `maxWaitSeconds`: expected an integer in range 1-{MaxAllowedWaitSeconds}. For polling (0-second wait), use `winaidbg_get_dump_analyze_commands_status`.");
            }

            var engine = EngineService.Get();
            var commandInfo = await TryGetCommandInfoWithBoundedWaitAsync(engine, sessionId, commandId, TimeSpan.FromSeconds(maxWaitSeconds));

            logger.Info("Command {CommandId} result retrieved: State={State}", commandId, commandInfo.State);

            var markdown = MarkdownFormatter.CreateCommandResult(
                commandInfo.CommandId,
                sessionId,
                commandInfo.Command,
                commandInfo.State.ToString(),
                commandInfo.IsSuccess ?? false,
                commandInfo.QueuedTime,
                commandInfo.StartTime,
                commandInfo.EndTime,
                commandInfo.ExecutionTime,
                commandInfo.TotalTime);

            if (!string.IsNullOrEmpty(commandInfo.AggregatedOutput))
            {
                markdown += MarkdownFormatter.AppendOutputForCommand(commandInfo.Command, commandInfo.AggregatedOutput, "Output");
            }

            if (!string.IsNullOrEmpty(commandInfo.ErrorMessage))
            {
                markdown += MarkdownFormatter.CreateCodeBlock(commandInfo.ErrorMessage, "Error");
            }

            if (!IsTerminalState(commandInfo.State))
            {
                markdown += MarkdownFormatter.CreateNoteBlock(
                    $"Command `{commandId}` is not finished yet (current state: `{commandInfo.State}`). " +
                    $"This call waited up to {maxWaitSeconds} seconds. " +
                    "Poll `winaidbg_get_dump_analyze_commands_status` and retry (for 0-wait polling), or retry with a larger `maxWaitSeconds` (up to 30).");
            }

            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (McpToolUserInputException ex)
        {
            logger.Warn(ex, "Invalid inputs for command result read");
            throw;
        }
        catch (ArgumentException ex)
        {
            logger.Error(ex, "Invalid argument: {Message}", ex.Message);
            throw new McpToolUserInputException(ex.Message, ex);
        }
        catch (KeyNotFoundException ex)
        {
            logger.Error(ex, "Command not found: {CommandId}", commandId);
            throw new McpToolUserInputException(
                $"Invalid `commandId`: `{commandId}` was not found for session `{sessionId}`. Use `winaidbg_get_dump_analyze_commands_status` to list known commandIds.",
                ex);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error reading command result");
            throw;
        }
    }

    /// <summary>
    /// Attempts to fetch command info with a bounded wait time, falling back to a non-blocking status read.
    /// </summary>
    /// <param name="engine">The debug engine.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="maxWait">Maximum duration to wait for completion.</param>
    /// <returns>The command info, either completed or in-progress.</returns>
    private static async Task<CommandInfo> TryGetCommandInfoWithBoundedWaitAsync(
        IDebugEngine engine,
        string sessionId,
        string commandId,
        TimeSpan maxWait)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(maxWait);

        try
        {
            return await engine.GetCommandInfoAsync(sessionId, commandId, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            var current = engine.GetCommandInfo(sessionId, commandId);
            if (current != null)
            {
                return current;
            }

            throw;
        }
    }

    /// <summary>
    /// Determines whether a command state is terminal.
    /// </summary>
    /// <param name="state">The command state.</param>
    /// <returns>True if the state is terminal, otherwise false.</returns>
    private static bool IsTerminalState(CommandState state)
    {
        return state is CommandState.Completed or
               CommandState.Failed or
               CommandState.Timeout or
               CommandState.Cancelled;
    }
}
