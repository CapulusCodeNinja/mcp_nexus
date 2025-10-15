using System.Collections.Concurrent;
using mcp_nexus.Notifications;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus.CommandQueue.Recovery
{
    /// <summary>
    /// Processes commands with resilient error handling, monitoring, and cleanup
    /// </summary>
    public class ResilientCommandProcessor
    {
        private readonly ILogger<ResilientCommandProcessor> m_Logger;
        private readonly CommandRecoveryManager m_RecoveryManager;
        private readonly ResilientQueueConfiguration m_Config;
        private readonly IMcpNotificationService? m_NotificationService;

        // Command tracking and metrics
        private readonly ConcurrentDictionary<string, QueuedCommand> m_ActiveCommands = new();
        private volatile QueuedCommand? m_CurrentCommand;
        private long m_CommandsProcessed = 0;
        private long m_CommandsFailed = 0;
        private long m_CommandsCancelled = 0;
        private DateTime m_LastStatsLog = DateTime.Now;

        // Cleanup management
        private readonly Timer m_CleanupTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilientCommandProcessor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording processing operations.</param>
        /// <param name="recoveryManager">The recovery manager for handling command failures.</param>
        /// <param name="config">The resilient queue configuration settings.</param>
        /// <param name="notificationService">Optional notification service for publishing processing events.</param>
        public ResilientCommandProcessor(
            ILogger<ResilientCommandProcessor> logger,
            CommandRecoveryManager recoveryManager,
            ResilientQueueConfiguration config,
            IMcpNotificationService? notificationService = null)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_RecoveryManager = recoveryManager ?? throw new ArgumentNullException(nameof(recoveryManager));
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_NotificationService = notificationService;

            // Initialize cleanup timer
            m_CleanupTimer = new Timer(CleanupCompletedCommands, null, m_Config.CleanupInterval, m_Config.CleanupInterval);
        }

        /// <summary>
        /// Processes the command queue continuously
        /// </summary>
        /// <param name="commandQueue">The command queue to process</param>
        /// <param name="cancellationToken">Cancellation token for shutdown</param>
        /// <returns>Task representing the processing operation</returns>
        public async Task ProcessCommandQueueAsync(BlockingCollection<QueuedCommand> commandQueue, CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("🚀 Starting resilient command processor");

            try
            {
                foreach (var queuedCommand in commandQueue.GetConsumingEnumerable(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await ProcessSingleCommandAsync(queuedCommand, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("🛑 Command processor stopped due to cancellation");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "💥 Fatal error in command processor");
                throw;
            }
            finally
            {
                m_Logger.LogInformation("🏁 Command processor shutdown complete");
            }
        }

        /// <summary>
        /// Processes a single command with comprehensive error handling
        /// </summary>
        /// <param name="queuedCommand">The command to process</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ProcessSingleCommandAsync(QueuedCommand queuedCommand, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;

            try
            {
                // Set as current command
                m_CurrentCommand = queuedCommand;

                // Update the command in active commands (it should already be there from queueing)
                m_ActiveCommands[queuedCommand.Id ?? string.Empty] = queuedCommand;

                // Update command state
                UpdateCommandState(queuedCommand, CommandState.Executing);

                m_Logger.LogDebug("⚡ Processing command {CommandId}: {Command}", queuedCommand.Id, queuedCommand.Command);

                // Execute command with recovery
                var result = await m_RecoveryManager.ExecuteCommandWithRecoveryAsync(queuedCommand, cancellationToken);

                // Complete successfully
                CompleteCommand(queuedCommand, result, CommandState.Completed);
                Interlocked.Increment(ref m_CommandsProcessed);

                var completionTime = DateTime.Now;
                var elapsed = completionTime - startTime;

                // Log detailed command statistics
                LogCommandStatistics(queuedCommand, startTime, completionTime, CommandState.Completed);

                m_Logger.LogDebug("✅ Command {CommandId} completed in {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);

                // Send completion notification
                if (m_NotificationService != null)
                {
                    await m_NotificationService.NotifyCommandStatusAsync(queuedCommand.Id ?? string.Empty, queuedCommand.Command ?? string.Empty, "completed",
                        result: result, error: string.Empty, queuePosition: 0, message: $"Completed in {elapsed.TotalMilliseconds:F0}ms");
                }
            }
            catch (OperationCanceledException) when (queuedCommand.CancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                // Command was specifically cancelled
                CompleteCommand(queuedCommand, "Command was cancelled", CommandState.Cancelled);
                Interlocked.Increment(ref m_CommandsCancelled);

                var completionTime = DateTime.Now;
                var elapsed = completionTime - startTime;

                // Log detailed command statistics
                LogCommandStatistics(queuedCommand, startTime, completionTime, CommandState.Cancelled);

                m_Logger.LogWarning("🚫 Command {CommandId} was cancelled after {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Service shutdown
                CompleteCommand(queuedCommand, "Service is shutting down", CommandState.Cancelled);

                var completionTime = DateTime.Now;
                LogCommandStatistics(queuedCommand, startTime, completionTime, CommandState.Cancelled);

                m_Logger.LogWarning("🛑 Command {CommandId} cancelled due to service shutdown", queuedCommand.Id);
            }
            catch (Exception ex)
            {
                // Command failed
                var errorMessage = $"Command execution failed: {ex.Message}";
                CompleteCommand(queuedCommand, errorMessage, CommandState.Failed);
                Interlocked.Increment(ref m_CommandsFailed);

                var completionTime = DateTime.Now;
                var elapsed = completionTime - startTime;

                // Log detailed command statistics
                LogCommandStatistics(queuedCommand, startTime, completionTime, CommandState.Failed);

                m_Logger.LogError(ex, "❌ Command {CommandId} failed after {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);

                // Send failure notification
                if (m_NotificationService != null)
                {
                    await m_NotificationService.NotifyCommandStatusAsync(queuedCommand.Id ?? string.Empty, queuedCommand.Command ?? string.Empty, "failed",
                        result: string.Empty, error: ex.Message, queuePosition: 0, message: $"Failed after {elapsed.TotalMilliseconds:F0}ms");
                }
            }
            finally
            {
                // Clear current command
                if (m_CurrentCommand == queuedCommand)
                    m_CurrentCommand = null;

                // Log periodic statistics
                LogPeriodicStatistics();
            }
        }

        /// <summary>
        /// Completes a command with the given result and state
        /// </summary>
        /// <param name="command">The command to complete</param>
        /// <param name="result">The result string of the command execution</param>
        /// <param name="state">The final state of the command</param>
        private void CompleteCommand(QueuedCommand command, string result, CommandState state)
        {
            try
            {
                UpdateCommandState(command, state);

                if (command.CompletionSource?.Task.IsCompleted == false)
                {
                    if (state == CommandState.Completed)
                        command.CompletionSource.SetResult(result);
                    else if (state == CommandState.Cancelled)
                        command.CompletionSource.SetCanceled();
                    else
                        command.CompletionSource.SetResult(result); // Return error message as result, not exception
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error completing command {CommandId}", command.Id);
            }
        }

        /// <summary>
        /// Updates the state of a command.
        /// </summary>
        /// <param name="command">The command to update the state for.</param>
        /// <param name="newState">The new state to set for the command.</param>
        private void UpdateCommandState(QueuedCommand command, CommandState newState)
        {
            try
            {
                // Update the command record (assuming QueuedCommand is mutable or we track state separately)
                var updatedCommand = command.WithState(newState);
                m_ActiveCommands[command.Id ?? string.Empty] = updatedCommand;

                m_Logger.LogTrace("🔄 Command {CommandId} state changed to {State}", command.Id, newState);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error updating command state for {CommandId}", command.Id);
            }
        }

        /// <summary>
        /// Gets the cached result with metadata for a command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>The cached result with metadata, or null if not found.</returns>
        public CachedCommandResult? GetCachedResultWithMetadata(string commandId)
        {
            // This implementation doesn't have a cache, so return null
            return null;
        }

        /// <summary>
        /// Cancels a specific command
        /// </summary>
        /// <param name="commandId">The ID of the command to cancel</param>
        /// <param name="reason">The reason for cancellation</param>
        /// <returns>True if the command was found and cancelled</returns>
        public bool CancelCommand(string commandId, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return false;

            if (!m_ActiveCommands.TryGetValue(commandId, out var command))
            {
                m_Logger.LogWarning("Cannot cancel command {CommandId} - command not found", commandId);
                return false;
            }

            try
            {
                command.CancellationTokenSource?.Cancel();
                m_RecoveryManager.CancelCommand(commandId, reason);

                m_Logger.LogInformation("🚫 Cancelled command {CommandId}: {Reason}", commandId, reason ?? "User requested");
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error cancelling command {CommandId}", commandId);
                return false;
            }
        }

        /// <summary>
        /// Cancels all active commands
        /// </summary>
        /// <param name="reason">The reason for cancellation</param>
        /// <returns>The number of commands cancelled</returns>
        public int CancelAllCommands(string? reason = null)
        {
            var cancelledCount = 0;
            // Enumerate snapshot of keys from ConcurrentDictionary without materializing a list
            foreach (var commandId in m_ActiveCommands.Keys)
            {
                if (CancelCommand(commandId, reason))
                    cancelledCount++;
            }

            m_Logger.LogInformation("🚫 Cancelled {Count} commands: {Reason}", cancelledCount, reason ?? "Bulk cancellation");
            return cancelledCount;
        }

        /// <summary>
        /// Registers a queued command for tracking
        /// </summary>
        /// <param name="queuedCommand">The command to register</param>
        public void RegisterQueuedCommand(QueuedCommand queuedCommand)
        {
            m_ActiveCommands[queuedCommand.Id ?? string.Empty] = queuedCommand;
            m_Logger.LogTrace("📝 Registered queued command {CommandId} for tracking", queuedCommand.Id);
        }

        /// <summary>
        /// Gets the currently executing command
        /// </summary>
        /// <returns>The current command, or null if none is executing</returns>
        public QueuedCommand? GetCurrentCommand()
        {
            return m_CurrentCommand;
        }

        /// <summary>
        /// Gets the result of a specific command
        /// </summary>
        /// <param name="commandId">Command ID to get result for</param>
        /// <returns>The command result</returns>
        public async Task<string> GetCommandResult(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return "Command ID cannot be null or empty";

            if (!m_ActiveCommands.TryGetValue(commandId, out var queuedCommand))
                return $"Command not found: {commandId}";

            // Check if command is still executing
            if (queuedCommand.CompletionSource?.Task.IsCompleted == false)
            {
                var elapsed = DateTime.Now - queuedCommand.QueueTime;
                return $"Command is still executing (elapsed: {elapsed:mm\\:ss}, command: {commandId})";
            }

            try
            {
                var result = await (queuedCommand.CompletionSource?.Task ?? Task.FromResult(string.Empty));
                return result;
            }
            catch (Exception ex)
            {
                return $"Command execution failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the state of a specific command
        /// </summary>
        /// <param name="commandId">Command ID to get state for</param>
        /// <returns>The command state, or null if not found</returns>
        public CommandState? GetCommandState(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            if (!m_ActiveCommands.TryGetValue(commandId, out var queuedCommand))
                return null;

            return queuedCommand.State;
        }

        /// <summary>
        /// Gets detailed information about a specific command
        /// </summary>
        /// <param name="commandId">Command ID to get info for</param>
        /// <returns>The command info, or null if not found</returns>
        public CommandInfo? GetCommandInfo(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            if (!m_ActiveCommands.TryGetValue(commandId, out var queuedCommand))
                return null;

            var elapsed = DateTime.Now - queuedCommand.QueueTime;
            var isCompleted = queuedCommand.CompletionSource?.Task.IsCompleted == true;

            return new CommandInfo(
                queuedCommand.Id ?? string.Empty,
                queuedCommand.Command ?? string.Empty,
                queuedCommand.State,
                queuedCommand.QueueTime,
                0
            )
            {
                Elapsed = elapsed,
                Remaining = TimeSpan.Zero, // Not applicable for resilient queue
                IsCompleted = isCompleted
            };
        }

        /// <summary>
        /// Gets the status of all commands in the queue
        /// </summary>
        /// <returns>Collection of command status information</returns>
        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            var results = new List<(string, string, DateTime, string)>();

            // Add current command
            var current = m_CurrentCommand;
            if (current != null)
            {
                results.Add((current.Id ?? string.Empty, current.Command ?? string.Empty, current.QueueTime, "Executing"));
            }

            // Add other active commands
            foreach (var kvp in m_ActiveCommands)
            {
                var command = kvp.Value;
                if (command != current)
                {
                    var status = command.State switch
                    {
                        CommandState.Queued => "Queued",
                        CommandState.Executing => "Executing",
                        CommandState.Completed => "Completed",
                        CommandState.Cancelled => "Cancelled",
                        CommandState.Failed => "Failed",
                        _ => "Unknown"
                    };
                    results.Add((command.Id ?? string.Empty, command.Command ?? string.Empty, command.QueueTime, status));
                }
            }

            return results;
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        /// <returns>Tuple containing processed, failed, and cancelled command counts</returns>
        public (long Processed, long Failed, long Cancelled) GetPerformanceStats()
        {
            return (
                Interlocked.Read(ref m_CommandsProcessed),
                Interlocked.Read(ref m_CommandsFailed),
                Interlocked.Read(ref m_CommandsCancelled)
            );
        }

        /// <summary>
        /// Logs periodic statistics about command processing
        /// </summary>
        private void LogPeriodicStatistics()
        {
            var now = DateTime.Now;
            if (now - m_LastStatsLog >= TimeSpan.FromMinutes(5))
            {
                var (Processed, Failed, Cancelled) = GetPerformanceStats();
                m_Logger.LogDebug("📊 Command stats - Processed: {Processed}, Failed: {Failed}, Cancelled: {Cancelled}, Active: {Active}",
                    Processed, Failed, Cancelled, m_ActiveCommands.Count);
                m_LastStatsLog = now;
            }
        }

        /// <summary>
        /// Cleans up completed commands periodically
        /// </summary>
        /// <param name="state">The state object passed to the timer callback (unused)</param>
        private void CleanupCompletedCommands(object? state)
        {
            try
            {
                var cutoffTime = DateTime.Now - m_Config.CommandRetentionTime;
                var commandsToRemove = new List<string>();

                foreach (var kvp in m_ActiveCommands)
                {
                    var command = kvp.Value;
                    if (command.State is CommandState.Completed or CommandState.Failed or CommandState.Cancelled &&
                        command.QueueTime < cutoffTime)
                    {
                        commandsToRemove.Add(kvp.Key);
                    }
                }

                foreach (var commandId in commandsToRemove)
                {
                    m_ActiveCommands.TryRemove(commandId, out _);
                }

                if (commandsToRemove.Count > 0)
                {
                    m_Logger.LogDebug("🧹 Cleaned up {Count} completed commands", commandsToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during command cleanup");
            }
        }

        /// <summary>
        /// Logs detailed command execution statistics
        /// </summary>
        /// <param name="command">The command that was executed</param>
        /// <param name="executionStartTime">The time when the command started execution</param>
        /// <param name="completionTime">The time when the command completed</param>
        /// <param name="finalState">The final state of the command</param>
        private void LogCommandStatistics(QueuedCommand command, DateTime executionStartTime, DateTime completionTime, CommandState finalState)
        {
            try
            {
                var timeInQueue = executionStartTime - command.QueueTime;
                var timeExecution = completionTime - executionStartTime;
                var totalDuration = completionTime - command.QueueTime;

                // Structured log for easy parsing - all times in milliseconds
                m_Logger.LogDebug(
                    "📊 COMMAND_STATS | SessionId: {SessionId} | CommandId: {CommandId} | Command: {Command} | " +
                    "State: {State} | QueuedAt: {QueuedAt:yyyy-MM-dd HH:mm:ss.fff} | " +
                    "StartedAt: {StartedAt:yyyy-MM-dd HH:mm:ss.fff} | CompletedAt: {CompletedAt:yyyy-MM-dd HH:mm:ss.fff} | " +
                    "TimeInQueue: {TimeInQueueMs}ms | TimeExecution: {TimeExecutionMs}ms | TotalDuration: {TotalDurationMs}ms",
                    m_Config.SessionId,
                    command.Id,
                    command.Command,
                    finalState,
                    command.QueueTime,
                    executionStartTime,
                    completionTime,
                    timeInQueue.TotalMilliseconds,
                    timeExecution.TotalMilliseconds,
                    totalDuration.TotalMilliseconds
                );
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error logging command statistics for {CommandId}", command.Id);
            }
        }

        /// <summary>
        /// Disposes of the processor and its resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                m_CleanupTimer?.Dispose();
                m_RecoveryManager?.Cleanup();

                // Cancel any remaining commands
                CancelAllCommands("Service shutdown");

                m_Logger.LogInformation("🧹 Resilient command processor disposed");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error disposing resilient command processor");
            }
        }
    }
}
