using System.Collections.Concurrent;
using System.Threading.Channels;

using Nexus.Engine.Batch;
using Nexus.Engine.Share;
using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;

using NLog;

namespace Nexus.Engine.Internal;
/// <summary>
/// Internal command queue that manages command execution with batching support.
/// </summary>
internal class CommandQueue : IDisposable
{
    private readonly Logger m_Logger;
    private readonly string m_SessionId;
    private readonly Channel<QueuedCommand> m_CommandChannel;
    private readonly ConcurrentDictionary<string, QueuedCommand> m_ActiveCommands = new();
    private readonly ConcurrentDictionary<string, CommandInfo> m_ResultCache = new();
    private readonly CancellationTokenSource m_CancellationTokenSource = new();

    private CdbSession? m_CdbSession;
    private Task? m_ProcessingTask;
    private volatile bool m_Disposed = false;


    /// <summary>
    /// Occurs when a command's state changes.
    /// </summary>
    public event EventHandler<CommandStateChangedEventArgs>? CommandStateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandQueue"/> class.
    /// </summary>
    /// <param name="sessionId">The session identifier for this command queue.</param>
    /// <exception cref="ArgumentNullException">Thrown when sessionId is null.</exception>
    public CommandQueue(string sessionId)
    {
        m_Logger = LogManager.GetCurrentClassLogger();

        m_SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));

        // Create unbounded channel for commands
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        m_CommandChannel = Channel.CreateUnbounded<QueuedCommand>(options);
    }

    /// <summary>
    /// Starts the command queue processing.
    /// </summary>
    /// <param name="cdbSession">The CDB session to use for command execution.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartAsync(CdbSession cdbSession, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));

        m_Logger.Debug("Starting command queue for session {SessionId}", m_SessionId);

        // Start the processing task
        m_ProcessingTask = Task.Run(() => ProcessCommandsAsync(cancellationToken), cancellationToken);

        m_Logger.Debug("Command queue started for session {SessionId}", m_SessionId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the command queue processing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Logger.Debug("Stopping command queue for session {SessionId}", m_SessionId);

        // Signal shutdown
        await m_CancellationTokenSource.CancelAsync();
        m_CommandChannel.Writer.Complete();

        // Wait for processing to complete
        if (m_ProcessingTask != null)
        {
            try
            {
                await m_ProcessingTask;
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Error waiting for command processing to complete in session {SessionId}", m_SessionId);
            }
        }

        m_Logger.Debug("Command queue stopped for session {SessionId}", m_SessionId);
    }

    /// <summary>
    /// Enqueues a command for execution.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The command identifier.</returns>
    public string EnqueueCommand(string command)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        var commandId = CommandIdGenerator.Instance.GenerateCommandId(m_SessionId);
        var queuedCommand = new QueuedCommand
        {
            Id = commandId,
            Command = command,
            ProcessId = null,
            QueuedTime = DateTime.Now,
            State = CommandState.Queued
        };

        // Add to active commands
        if (!m_ActiveCommands.TryAdd(commandId, queuedCommand))
        {
            throw new InvalidOperationException($"Command ID conflict: {commandId}");
        }

        // Enqueue for processing
        if (!m_CommandChannel.Writer.TryWrite(queuedCommand))
        {
            _ = m_ActiveCommands.TryRemove(commandId, out _);
            throw new InvalidOperationException("Command queue is not accepting new commands");
        }


        // Notify state change
        NotifyCommandStateChanged(commandId, CommandState.Queued, CommandState.Queued, command);

        m_Logger.Trace("Command {CommandId} got enqueued: {Command}", commandId, command);

        return commandId;
    }

    /// <summary>
    /// Gets the information about a command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command information.</returns>
    public async Task<CommandInfo> GetCommandInfoAsync(string commandId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(commandId))
        {
            throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));
        }

        // Check cache first
        if (m_ResultCache.TryGetValue(commandId, out var cachedResult))
        {
            cachedResult.ReadCount++;
            return cachedResult;
        }

        // Check if command is still active
        if (m_ActiveCommands.TryGetValue(commandId, out var command))
        {
            // Wait for completion
            _ = await command.CompletionSource.Task.WaitAsync(cancellationToken);

            // Get result from cache
            if (m_ResultCache.TryGetValue(commandId, out var result))
            {
                result.ReadCount++;
                return result;
            }
        }

        throw new KeyNotFoundException($"Command {commandId} not found");
    }

    /// <summary>
    /// Gets the current information about a command without waiting for completion.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>The command information, or null if not found.</returns>
    public CommandInfo? GetCommandInfo(string commandId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(commandId))
        {
            return null;
        }

        // Check cache first
        if (m_ResultCache.TryGetValue(commandId, out var cachedResult))
        {
            return cachedResult;
        }

        // Check if command is still active
        return m_ActiveCommands.TryGetValue(commandId, out var command)
            ? new CommandInfo(m_SessionId, command.Id, command.Command, command.State, command.QueuedTime, command.ProcessId, null, null, string.Empty, string.Empty)
            : null;
    }

    /// <summary>
    /// Gets the information about all commands in the queue.
    /// </summary>
    /// <returns>A dictionary of command IDs to their information.</returns>
    public Dictionary<string, CommandInfo> GetAllCommandInfos()
    {
        ThrowIfDisposed();

        var infos = new Dictionary<string, CommandInfo>();

        // Add cached results
        foreach (var kvp in m_ResultCache)
        {
            infos[kvp.Key] = kvp.Value;
        }

        // Add active commands
        foreach (var command in m_ActiveCommands.Values)
        {
            infos[command.Id] = new CommandInfo(m_SessionId, command.Id, command.Command, command.State, command.QueuedTime, command.ProcessId, null, null, string.Empty, string.Empty);
        }

        return infos;
    }

    /// <summary>
    /// Cancels a command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>True if the command was found and cancelled, false otherwise.</returns>
    public bool CancelCommand(string commandId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(commandId))
        {
            return false;
        }

        if (m_ActiveCommands.TryGetValue(commandId, out var command))
        {
            command.CancellationTokenSource.Cancel();
            UpdateCommandState(command, CommandState.Cancelled);


            return true;
        }

        return false;
    }

    /// <summary>
    /// Cancels all commands in the queue.
    /// </summary>
    /// <param name="reason">Optional reason for cancellation.</param>
    /// <returns>The number of commands that were cancelled.</returns>
    public int CancelAllCommands(string? reason = null)
    {
        ThrowIfDisposed();

        var count = 0;
        foreach (var command in m_ActiveCommands.Values)
        {
            command.CancellationTokenSource.Cancel();
            UpdateCommandState(command, CommandState.Cancelled);
            count++;
        }


        m_Logger.Info("Cancelled {Count} commands in session {SessionId}. Reason: {Reason}",
            count, m_SessionId, reason ?? "No reason specified");

        return count;
    }

    /// <summary>
    /// Disposes of the command queue and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Logger.Debug("Disposing command queue for session {SessionId}", m_SessionId);

        try
        {
            // Cancel all commands
            _ = CancelAllCommands("Queue disposal");

            // Stop processing
            StopAsync().GetAwaiter().GetResult();

            // Reset the command ID generator for this session
            _ = CommandIdGenerator.Instance.ResetSession(m_SessionId);

            // Clear all batch mappings for this session
            BatchProcessor.Instance.ClearSessionBatchMappings(m_SessionId);

            // Dispose resources
            m_CancellationTokenSource.Dispose();
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error disposing command queue for session {SessionId}", m_SessionId);
        }
        finally
        {
            m_Disposed = true;
        }
    }

    /// <summary>
    /// Main loop that processes commands from the queue, collecting and batching them when appropriate.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ProcessCommandsAsync(CancellationToken cancellationToken)
    {
        m_Logger.Debug("Starting command processing for session {SessionId}", m_SessionId);

        try
        {
            await foreach (var command in m_CommandChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // Collect available commands for potential batching
                    var commandsToProcess = CollectAvailableCommands(command, cancellationToken);

                    // Process commands (with or without batching)
                    await ProcessCommandsAsync(commandsToProcess, cancellationToken);
                }
                catch (Exception ex)
                {
                    m_Logger.Error(ex, "Error processing command {CommandId} in session {SessionId}",
                        command.Id, m_SessionId);

                    // Mark command as failed
                    UpdateCommandState(command, CommandState.Failed);
                    var endTime = DateTime.Now;
                    var commandInfo = new CommandInfo(m_SessionId, command.Id, command.Command, CommandState.Failed, command.QueuedTime, command.ProcessId, null, endTime, string.Empty, $"Command processing failed: {ex.Message}");
                    SetCommandResult(command, commandInfo);
                }
            }
        }
        catch (OperationCanceledException)
        {
            m_Logger.Debug("Command processing cancelled for session {SessionId}", m_SessionId);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Fatal error in command processing for session {SessionId}", m_SessionId);
        }
        finally
        {
            m_Logger.Debug("Command processing ended for session {SessionId}", m_SessionId);
        }
    }

    /// <summary>
    /// Collects available commands from the channel for potential batching.
    /// </summary>
    /// <param name="firstCommand">The first command already read from the channel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of commands to process.</returns>
    private List<QueuedCommand> CollectAvailableCommands(QueuedCommand firstCommand, CancellationToken cancellationToken)
    {
        var commands = new List<QueuedCommand> { firstCommand };

        // Short timeout to avoid blocking - collect immediately available commands
        var timeout = TimeSpan.FromMilliseconds(100);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        // Collect all immediately available commands without blocking
        while (!cts.Token.IsCancellationRequested && m_CommandChannel.Reader.TryRead(out var nextCommand))
        {
            commands.Add(nextCommand);
        }

        return commands;
    }

    /// <summary>
    /// Processes a batch of commands using the batch processor.
    /// </summary>
    /// <param name="queuedCommands">The commands to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ProcessCommandsAsync(List<QueuedCommand> queuedCommands, CancellationToken cancellationToken)
    {
        ValidateCdbSession();

        // Prepare commands for batching
        var commandsToExecute = PrepareCommandsForBatching(queuedCommands);

        // Execute commands and track timing
        var (executionResults, commandStartTimes) = await ExecuteCommandsWithTiming(commandsToExecute, queuedCommands, cancellationToken);

        // Process results and complete commands
        ProcessCommandResults(executionResults, queuedCommands, commandStartTimes);
    }

    /// <summary>
    /// Prepares queued commands for batching by converting them to batch commands and applying batching logic.
    /// </summary>
    /// <param name="queuedCommands">The original queued commands.</param>
    /// <returns>Commands ready for execution (batched or individual).</returns>
    private List<Command> PrepareCommandsForBatching(List<QueuedCommand> queuedCommands)
    {
        // Convert to batch commands
        var batchCommands = queuedCommands.Select(qc => new Command
        {
            CommandId = qc.Id,
            CommandText = qc.Command
        }).ToList();

        // Apply batching (library decides whether to batch or pass through)
        var commandsToExecute = BatchProcessor.Instance.BatchCommands(m_SessionId, batchCommands);

        m_Logger.Debug("Processing {OriginalCount} commands as {ExecutionCount} execution units",
            queuedCommands.Count, commandsToExecute.Count);

        return commandsToExecute;
    }

    /// <summary>
    /// Executes commands and tracks start times for each command.
    /// </summary>
    /// <param name="commandsToExecute">Commands to execute.</param>
    /// <param name="queuedCommands">Original queued commands for state updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple containing execution results and command start times.</returns>
    private async Task<(List<CommandResult> executionResults, Dictionary<string, DateTime> commandStartTimes)> ExecuteCommandsWithTiming(
        List<Command> commandsToExecute,
        List<QueuedCommand> queuedCommands,
        CancellationToken cancellationToken)
    {
        var commandStartTimes = new Dictionary<string, DateTime>();
        var executionResults = new List<CommandResult>();

        foreach (var cmd in commandsToExecute)
        {
            // Mark commands as executing and record start time
            var batchStartTime = DateTime.Now;
            MarkCommandsAsExecuting(cmd, queuedCommands, commandStartTimes, batchStartTime);

            // Execute the command and handle results
            var result = await ExecuteSingleCommand(cmd, cancellationToken);
            executionResults.Add(result);
        }

        return (executionResults, commandStartTimes);
    }

    /// <summary>
    /// Marks queued commands as executing and records their start times.
    /// </summary>
    /// <param name="cmd">The command being executed.</param>
    /// <param name="queuedCommands">Original queued commands.</param>
    /// <param name="commandStartTimes">Dictionary to store start times.</param>
    /// <param name="batchStartTime">The start time for this batch.</param>
    private void MarkCommandsAsExecuting(Command cmd, List<QueuedCommand> queuedCommands, Dictionary<string, DateTime> commandStartTimes, DateTime batchStartTime)
    {
        var originalCommandIds = BatchProcessor.Instance.GetOriginalCommandIds(cmd.CommandId);
        foreach (var commandId in originalCommandIds)
        {
            var queuedCommand = queuedCommands.FirstOrDefault(qc => qc.Id == commandId);
            if (queuedCommand != null)
            {
                UpdateCommandState(queuedCommand, CommandState.Executing);
                commandStartTimes[commandId] = batchStartTime;
            }
        }
    }

    /// <summary>
    /// Executes a single command and handles all exception scenarios.
    /// </summary>
    /// <param name="cmd">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result with appropriate state flags.</returns>
    private async Task<CommandResult> ExecuteSingleCommand(Command cmd, CancellationToken cancellationToken)
    {
        try
        {
            // Execute the command
            var result = await m_CdbSession!.ExecuteCommandAsync(cmd.CommandText, cancellationToken);

            return new CommandResult
            {
                CommandId = cmd.CommandId,
                ProcessId = m_CdbSession.ProcessId,
                ResultText = result
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            m_Logger.Warn("Command {CommandId} was cancelled", cmd.CommandId);

            return new CommandResult
            {
                CommandId = cmd.CommandId,
                ProcessId = m_CdbSession?.ProcessId ?? null,
                ResultText = "Command was cancelled",
                IsCancelled = true
            };
        }
        catch (TimeoutException ex)
        {
            m_Logger.Warn("Command {CommandId} timed out", cmd.CommandId);

            return new CommandResult
            {
                CommandId = cmd.CommandId,
                ProcessId = m_CdbSession?.ProcessId ?? null,
                ResultText = $"Command timed out: {ex.Message}",
                IsTimeout = true
            };
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error executing command {CommandId}", cmd.CommandId);

            return new CommandResult
            {
                CommandId = cmd.CommandId,
                ProcessId = m_CdbSession?.ProcessId ?? null,
                ResultText = $"ERROR: {ex.Message}",
                IsFailed = true
            };
        }
    }

    /// <summary>
    /// Processes command results, unbatch them, and complete the original commands with statistics.
    /// </summary>
    /// <param name="executionResults">Raw execution results.</param>
    /// <param name="queuedCommands">Original queued commands.</param>
    /// <param name="commandStartTimes">Command start times.</param>
    private void ProcessCommandResults(List<CommandResult> executionResults, List<QueuedCommand> queuedCommands, Dictionary<string, DateTime> commandStartTimes)
    {
        // Unbatch results (library decides whether to unbatch or pass through)
        var individualResults = BatchProcessor.Instance.UnbatchResults(executionResults);

        LogUnbatchingResults(executionResults.Count, individualResults.Count);

        // Complete each original command with statistics
        foreach (var result in individualResults)
        {
            CompleteCommandWithStatistics(result, queuedCommands, commandStartTimes);
        }
    }

    /// <summary>
    /// Logs unbatching results for debugging purposes.
    /// </summary>
    /// <param name="executionCount">Number of execution results.</param>
    /// <param name="individualCount">Number of individual results.</param>
    private void LogUnbatchingResults(int executionCount, int individualCount)
    {
        if (executionCount == individualCount)
        {
            m_Logger.Trace("Unbatched {ExecutionCount} execution results into {IndividualCount} individual results",
                executionCount, individualCount);
        }
        else
        {
            m_Logger.Debug("Unbatched {ExecutionCount} execution results into {IndividualCount} individual results",
                executionCount, individualCount);
        }
    }

    /// <summary>
    /// Completes a single command with appropriate state and statistics.
    /// </summary>
    /// <param name="result">The command result.</param>
    /// <param name="queuedCommands">Original queued commands.</param>
    /// <param name="commandStartTimes">Command start times.</param>
    private void CompleteCommandWithStatistics(CommandResult result, List<QueuedCommand> queuedCommands, Dictionary<string, DateTime> commandStartTimes)
    {
        var queuedCommand = queuedCommands.FirstOrDefault(qc => qc.Id == result.CommandId);
        if (queuedCommand == null)
        {
            m_Logger.Warn("No queued command found for result {CommandId}", result.CommandId);
            return;
        }

        var endTime = DateTime.Now;
        var startTime = commandStartTimes.TryGetValue(result.CommandId, out var start) ? start : queuedCommand.QueuedTime;

        // Calculate timings
        var executionTime = endTime - startTime;
        var queueTime = startTime - queuedCommand.QueuedTime;
        var totalTime = endTime - queuedCommand.QueuedTime;

        // Determine final state and create command info
        var (commandInfo, finalState) = CreateCommandInfoFromResult(result, queuedCommand, startTime, endTime);

        // Complete the command
        SetCommandResult(queuedCommand, commandInfo);
        UpdateCommandState(queuedCommand, finalState);

        // Get batch command ID efficiently using the batch processor's cache
        var batchCommandId = BatchProcessor.Instance.GetBatchCommandId(result.CommandId);

        // Emit detailed statistics
        Statistics.EmitCommandStats(
            m_Logger,
            finalState,
            m_SessionId,
            queuedCommand.Id,
            batchCommandId,
            queuedCommand.Command,
            queuedCommand.QueuedTime,
            startTime,
            endTime,
            queueTime,
            executionTime,
            totalTime);
    }

    /// <summary>
    /// Creates appropriate CommandInfo and CommandState based on the command result.
    /// </summary>
    /// <param name="result">The command result.</param>
    /// <param name="queuedCommand">The original queued command.</param>
    /// <param name="startTime">Command start time.</param>
    /// <param name="endTime">Command end time.</param>
    /// <returns>Tuple containing CommandInfo and CommandState.</returns>
    private (CommandInfo commandInfo, CommandState finalState) CreateCommandInfoFromResult(CommandResult result, QueuedCommand queuedCommand, DateTime startTime, DateTime endTime)
    {
        if (result.IsCancelled)
        {
            var commandInfo = new CommandInfo(m_SessionId, queuedCommand.Id, queuedCommand.Command, CommandState.Cancelled, queuedCommand.QueuedTime, queuedCommand.ProcessId, startTime, endTime, string.Empty, "Command was cancelled");
            return (commandInfo, CommandState.Cancelled);
        }

        if (result.IsTimeout)
        {
            var commandInfo = new CommandInfo(m_SessionId, queuedCommand.Id, queuedCommand.Command, CommandState.Timeout, queuedCommand.QueuedTime, queuedCommand.ProcessId, startTime, endTime, string.Empty, $"Command timed out: {result.ResultText}");
            return (commandInfo, CommandState.Timeout);
        }

        if (result.IsFailed)
        {
            var commandInfo = new CommandInfo(m_SessionId, queuedCommand.Id, queuedCommand.Command, CommandState.Failed, queuedCommand.QueuedTime, queuedCommand.ProcessId, startTime, endTime, string.Empty, $"Command failed: {result.ResultText}");
            return (commandInfo, CommandState.Failed);
        }

        if (!result.IsCancelled && !result.IsTimeout && !result.IsFailed)
        {
            // Explicitly check for successful completion
            var commandInfo = new CommandInfo(m_SessionId, queuedCommand.Id, queuedCommand.Command, CommandState.Completed, queuedCommand.QueuedTime, queuedCommand.ProcessId, startTime, endTime, result.ResultText, string.Empty);
            return (commandInfo, CommandState.Completed);
        }

        // Default to failed for any unexpected state - this will make missing cases obvious
        m_Logger.Error("Unexpected command result state for command {CommandId}: Cancelled={IsCancelled}, Timeout={IsTimeout}, Failed={IsFailed}",
            result.CommandId, result.IsCancelled, result.IsTimeout, result.IsFailed);

        var errorCommandInfo = new CommandInfo(m_SessionId, queuedCommand.Id, queuedCommand.Command, CommandState.Failed, queuedCommand.QueuedTime, queuedCommand.ProcessId, startTime, endTime, $"UNEXPECTED STATE: {result.ResultText}", "Command result in unexpected state");
        return (errorCommandInfo, CommandState.Failed);
    }


    /// <summary>
    /// Processes a single command through the CDB session (used when batching is not applicable).
    /// </summary>
    /// <param name="command">The command to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ProcessCommandAsync(QueuedCommand command, CancellationToken cancellationToken)
    {
        ValidateCdbSession();
        LogCommandProcessing(command);

        UpdateCommandState(command, CommandState.Executing);
        var startTime = DateTime.Now;

        try
        {
            var result = await ExecuteCommandWithCdbSession(command, cancellationToken);
            await HandleSuccessfulCommandExecution(command, startTime, result);
        }
        catch (OperationCanceledException) when (command.CancellationTokenSource.Token.IsCancellationRequested)
        {
            await HandleCancelledCommand(command, startTime);
        }
        catch (TimeoutException ex)
        {
            await HandleTimedOutCommand(command, startTime, ex);
        }
        catch (Exception ex)
        {
            await HandleFailedCommand(command, startTime, ex);
        }
    }

    /// <summary>
    /// Validates that the CDB session is initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when CDB session is not initialized.</exception>
    protected virtual void ValidateCdbSession()
    {
        if (m_CdbSession == null)
        {
            throw new InvalidOperationException("CDB session not initialized");
        }
    }

    /// <summary>
    /// Logs the start of command processing.
    /// </summary>
    /// <param name="command">The command being processed.</param>
    protected virtual void LogCommandProcessing(QueuedCommand command)
    {
        m_Logger.Debug("Processing command {CommandId}: {Command}", command.Id, command.Command);
    }

    /// <summary>
    /// Executes the command with the CDB session.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The command result.</returns>
    protected virtual async Task<string> ExecuteCommandWithCdbSession(QueuedCommand command, CancellationToken cancellationToken)
    {
        ValidateCdbSession();
        return await m_CdbSession!.ExecuteCommandAsync(command.Command, cancellationToken);
    }

    /// <summary>
    /// Handles successful command execution.
    /// </summary>
    /// <param name="command">The command that was executed.</param>
    /// <param name="startTime">The start time of execution.</param>
    /// <param name="result">The execution result.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual Task HandleSuccessfulCommandExecution(QueuedCommand command, DateTime startTime, string result)
    {
        UpdateCommandState(command, CommandState.Completed);
        var endTime = DateTime.Now;

        var commandInfo = new CommandInfo(m_SessionId, command.Id, command.Command, CommandState.Completed, command.QueuedTime, command.ProcessId, startTime, endTime, result, string.Empty);

        SetCommandResult(command, commandInfo);

        var executionTime = endTime - startTime;
        var queueTime = startTime - command.QueuedTime;
        var totalTime = endTime - command.QueuedTime;

        m_Logger.Debug("Command {CommandId} completed successfully in {Elapsed}ms",
            command.Id, executionTime.TotalMilliseconds);

        // Emit detailed statistics
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Completed,
            m_SessionId,
            command.Id,
            null, // Not part of a batch
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            queueTime,
            executionTime,
            totalTime);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles cancelled command execution.
    /// </summary>
    /// <param name="command">The command that was cancelled.</param>
    /// <param name="startTime">The start time of execution.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual Task HandleCancelledCommand(QueuedCommand command, DateTime startTime)
    {
        UpdateCommandState(command, CommandState.Cancelled);
        var endTime = DateTime.Now;

        var commandInfo = CommandInfo.Cancelled(
            m_SessionId,
            command.Id,
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            string.Empty,
            "Command was cancelled",
            command.ProcessId);

        SetCommandResult(command, commandInfo);

        var executionTime = endTime - startTime;
        var queueTime = startTime - command.QueuedTime;
        var totalTime = endTime - command.QueuedTime;

        m_Logger.Debug("Command {CommandId} was cancelled", command.Id);

        // Emit detailed statistics
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Cancelled,
            m_SessionId,
            command.Id,
            null, // Not part of a batch
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            queueTime,
            executionTime,
            totalTime);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles timed out command execution.
    /// </summary>
    /// <param name="command">The command that timed out.</param>
    /// <param name="startTime">The start time of execution.</param>
    /// <param name="ex">The timeout exception.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual Task HandleTimedOutCommand(QueuedCommand command, DateTime startTime, TimeoutException ex)
    {
        UpdateCommandState(command, CommandState.Timeout);
        var endTime = DateTime.Now;

        var commandInfo = CommandInfo.TimedOut(
            m_SessionId,
            command.Id,
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            string.Empty,
            $"Command timed out: {ex.Message}",
            command.ProcessId);

        SetCommandResult(command, commandInfo);

        var executionTime = endTime - startTime;
        var queueTime = startTime - command.QueuedTime;
        var totalTime = endTime - command.QueuedTime;

        m_Logger.Warn("Command {CommandId} timed out", command.Id);

        // Emit detailed statistics
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Timeout,
            m_SessionId,
            command.Id,
            null, // Not part of a batch
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            queueTime,
            executionTime,
            totalTime);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles failed command execution.
    /// </summary>
    /// <param name="command">The command that failed.</param>
    /// <param name="startTime">The start time of execution.</param>
    /// <param name="ex">The exception that caused the failure.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual Task HandleFailedCommand(QueuedCommand command, DateTime startTime, Exception ex)
    {
        UpdateCommandState(command, CommandState.Failed);
        var endTime = DateTime.Now;

        var commandInfo = CommandInfo.Completed(
            m_SessionId,
            command.Id,
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            string.Empty,
            $"Command failed: {ex.Message}",
            command.ProcessId);

        SetCommandResult(command, commandInfo);

        var executionTime = endTime - startTime;
        var queueTime = startTime - command.QueuedTime;
        var totalTime = endTime - command.QueuedTime;

        m_Logger.Error(ex, "Command {CommandId} failed", command.Id);

        // Emit detailed statistics
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Failed,
            m_SessionId,
            command.Id,
            null, // Not part of a batch
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            queueTime,
            executionTime,
            totalTime);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the state of a command and notifies listeners.
    /// </summary>
    /// <param name="command">The command to update.</param>
    /// <param name="newState">The new state to set.</param>
    protected void UpdateCommandState(QueuedCommand command, CommandState newState)
    {
        var oldState = command.State;
        command.State = newState;

        NotifyCommandStateChanged(command.Id, oldState, newState, command.Command);
    }

    /// <summary>
    /// Caches the command result and completes the associated task.
    /// </summary>
    /// <param name="command">The command to set the result for.</param>
    /// <param name="result">The command result to cache.</param>
    protected void SetCommandResult(QueuedCommand command, CommandInfo result)
    {
        // Cache the result
        _ = m_ResultCache.TryAdd(command.Id, result);

        // Complete the task
        command.CompletionSource.SetResult(result);

        // Remove from active commands and dispose
        if (m_ActiveCommands.TryRemove(command.Id, out var removedCommand))
        {
            removedCommand.Dispose();
        }
    }

    /// <summary>
    /// Notifies listeners that a command's state has changed.
    /// </summary>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="oldState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    /// <param name="command">The command text.</param>
    private void NotifyCommandStateChanged(string commandId, CommandState oldState, CommandState newState, string command)
    {
        var args = new CommandStateChangedEventArgs
        {
            SessionId = m_SessionId,
            CommandId = commandId,
            OldState = oldState,
            NewState = newState,
            Timestamp = DateTime.Now,
            Command = command
        };

        CommandStateChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Throws an exception if the command queue has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the queue is disposed.</exception>
    private void ThrowIfDisposed()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(CommandQueue));
        }
    }
}
