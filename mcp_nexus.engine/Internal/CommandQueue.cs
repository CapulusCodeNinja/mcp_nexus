using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using mcp_nexus.Engine.Configuration;
using mcp_nexus.Engine.Events;
using mcp_nexus.Engine.Models;

namespace mcp_nexus.Engine.Internal;

/// <summary>
/// Internal command queue that manages command execution with batching support.
/// </summary>
internal class CommandQueue : IDisposable
{
    private readonly ILogger<CommandQueue> m_Logger;
    private readonly string m_SessionId;
    private readonly DebugEngineConfiguration m_Configuration;
    private readonly Channel<QueuedCommand> m_CommandChannel;
    private readonly ConcurrentDictionary<string, QueuedCommand> m_ActiveCommands = new();
    private readonly ConcurrentDictionary<string, CommandInfo> m_ResultCache = new();
    private readonly object m_StatisticsLock = new();
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
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandQueue(string sessionId, DebugEngineConfiguration configuration, ILogger<CommandQueue> logger)
    {
        m_SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
        
        m_Logger.LogDebug("Starting command queue for session {SessionId}", m_SessionId);

        // Start the processing task
        m_ProcessingTask = Task.Run(() => ProcessCommandsAsync(cancellationToken), cancellationToken);

        m_Logger.LogDebug("Command queue started for session {SessionId}", m_SessionId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the command queue processing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (m_Disposed)
            return;

        m_Logger.LogDebug("Stopping command queue for session {SessionId}", m_SessionId);

        // Signal shutdown
        m_CancellationTokenSource.Cancel();
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
                m_Logger.LogError(ex, "Error waiting for command processing to complete in session {SessionId}", m_SessionId);
            }
        }

        m_Logger.LogDebug("Command queue stopped for session {SessionId}", m_SessionId);
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
            throw new ArgumentException("Command cannot be null or empty", nameof(command));

        var commandId = GenerateCommandId();
        var queuedCommand = new QueuedCommand
        {
            Id = commandId,
            Command = command,
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
            m_ActiveCommands.TryRemove(commandId, out _);
            throw new InvalidOperationException("Command queue is not accepting new commands");
        }


        // Notify state change
        NotifyCommandStateChanged(commandId, CommandState.Queued, CommandState.Queued, command);

        m_Logger.LogDebug("Command {CommandId} enqueued: {Command}", commandId, command);
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
            throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

        // Check cache first
        if (m_ResultCache.TryGetValue(commandId, out var cachedResult))
        {
            return cachedResult;
        }

        // Check if command is still active
        if (m_ActiveCommands.TryGetValue(commandId, out var command))
        {
            // Wait for completion
            await command.CompletionSource.Task.WaitAsync(cancellationToken);
            
            // Get result from cache
            if (m_ResultCache.TryGetValue(commandId, out var result))
            {
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
            return null;

        // Check cache first
        if (m_ResultCache.TryGetValue(commandId, out var cachedResult))
        {
            return cachedResult;
        }

        // Check if command is still active
        if (m_ActiveCommands.TryGetValue(commandId, out var command))
        {
            return new CommandInfo
            {
                CommandId = command.Id,
                Command = command.Command,
                State = command.State,
                QueuedTime = command.QueuedTime,
                StartTime = null, // We don't track this in QueuedCommand anymore
                EndTime = null
            };
        }

        return null;
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
            infos[command.Id] = new CommandInfo
            {
                CommandId = command.Id,
                Command = command.Command,
                State = command.State,
                QueuedTime = command.QueuedTime,
                StartTime = null, // We don't track this in QueuedCommand anymore
                EndTime = null
            };
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
            return false;

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


        m_Logger.LogInformation("Cancelled {Count} commands in session {SessionId}. Reason: {Reason}", 
            count, m_SessionId, reason ?? "No reason specified");

        return count;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Logger.LogDebug("Disposing command queue for session {SessionId}", m_SessionId);

        try
        {
            // Cancel all commands
            CancelAllCommands("Queue disposal");

            // Stop processing
            StopAsync().GetAwaiter().GetResult();

            // Dispose resources
            m_CancellationTokenSource.Dispose();
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error disposing command queue for session {SessionId}", m_SessionId);
        }
        finally
        {
            m_Disposed = true;
        }
    }

    private async Task ProcessCommandsAsync(CancellationToken cancellationToken)
    {
        m_Logger.LogDebug("Starting command processing for session {SessionId}", m_SessionId);

        try
        {
            await foreach (var command in m_CommandChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await ProcessCommandAsync(command, cancellationToken);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error processing command {CommandId} in session {SessionId}", 
                        command.Id, m_SessionId);
                    
                    // Mark command as failed
                    UpdateCommandState(command, CommandState.Failed);
                    var endTime = DateTime.Now;
                    var commandInfo = CommandInfo.Completed(
                        command.Id,
                        command.Command,
                        command.QueuedTime,
                        command.QueuedTime, // Use queued time as start time if not available
                        endTime,
                        string.Empty,
                        false,
                        $"Command processing failed: {ex.Message}");
                    
                    SetCommandResult(command, commandInfo);
                }
            }
        }
        catch (OperationCanceledException)
        {
            m_Logger.LogDebug("Command processing cancelled for session {SessionId}", m_SessionId);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Fatal error in command processing for session {SessionId}", m_SessionId);
        }
        finally
        {
            m_Logger.LogDebug("Command processing ended for session {SessionId}", m_SessionId);
        }
    }

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
            throw new InvalidOperationException("CDB session not initialized");
    }

    /// <summary>
    /// Logs the start of command processing.
    /// </summary>
    /// <param name="command">The command being processed.</param>
    protected virtual void LogCommandProcessing(QueuedCommand command)
    {
        m_Logger.LogDebug("Processing command {CommandId}: {Command}", command.Id, command.Command);
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

        var commandInfo = CommandInfo.Completed(
            command.Id,
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            result,
            true);

        SetCommandResult(command, commandInfo);

        var executionTime = endTime - startTime;
        m_Logger.LogDebug("Command {CommandId} completed successfully in {Elapsed}ms", 
            command.Id, executionTime.TotalMilliseconds);

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
            command.Id,
            command.Command,
            command.QueuedTime,
            startTime,
            endTime);

        SetCommandResult(command, commandInfo);

        m_Logger.LogDebug("Command {CommandId} was cancelled", command.Id);

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
            command.Id,
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            $"Command timed out: {ex.Message}");

        SetCommandResult(command, commandInfo);

        m_Logger.LogWarning("Command {CommandId} timed out", command.Id);

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
            command.Id,
            command.Command,
            command.QueuedTime,
            startTime,
            endTime,
            string.Empty,
            false,
            $"Command failed: {ex.Message}");

        SetCommandResult(command, commandInfo);

        m_Logger.LogError(ex, "Command {CommandId} failed", command.Id);

        return Task.CompletedTask;
    }

    protected void UpdateCommandState(QueuedCommand command, CommandState newState)
    {
        var oldState = command.State;
        command.State = newState;

        NotifyCommandStateChanged(command.Id, oldState, newState, command.Command);
    }

    protected void SetCommandResult(QueuedCommand command, CommandInfo result)
    {
        // Cache the result
        m_ResultCache.TryAdd(command.Id, result);

        // Complete the task
        command.CompletionSource.SetResult(result);

        // Remove from active commands
        m_ActiveCommands.TryRemove(command.Id, out _);
    }

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

    private static string GenerateCommandId()
    {
        return $"cmd-{Guid.NewGuid():N}";
    }

    private void ThrowIfDisposed()
    {
        if (m_Disposed)
            throw new ObjectDisposedException(nameof(CommandQueue));
    }
}
