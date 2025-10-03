using System.Collections.Concurrent;
using mcp_nexus.Debugger;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Basic command processor for simple command queue operations
    /// </summary>
    public class BasicCommandProcessor
    {
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger<BasicCommandProcessor> m_logger;
        private readonly BasicQueueConfiguration m_config;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_activeCommands;
        private volatile QueuedCommand? m_currentCommand;

        // Performance counters
        private long m_commandsProcessed = 0;
        private long m_commandsFailed = 0;
        private long m_commandsCancelled = 0;
        private DateTime m_lastStatsLog = DateTime.UtcNow;

        // Cleanup timer
        private readonly Timer m_cleanupTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicCommandProcessor"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording processing operations.</param>
        /// <param name="config">The basic queue configuration settings.</param>
        /// <param name="activeCommands">The concurrent dictionary for tracking active commands.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public BasicCommandProcessor(
            ICdbSession cdbSession,
            ILogger<BasicCommandProcessor> logger,
            BasicQueueConfiguration config,
            ConcurrentDictionary<string, QueuedCommand> activeCommands)
        {
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_activeCommands = activeCommands ?? throw new ArgumentNullException(nameof(activeCommands));

            // Initialize cleanup timer
            m_cleanupTimer = new Timer(CleanupCompletedCommands, null, m_config.CleanupInterval, m_config.CleanupInterval);
        }

        /// <summary>
        /// Processes the command queue continuously
        /// </summary>
        /// <param name="commandQueue">The command queue to process</param>
        /// <param name="cancellationToken">Cancellation token for shutdown</param>
        /// <returns>Task representing the processing operation</returns>
        public async Task ProcessCommandQueueAsync(BlockingCollection<QueuedCommand> commandQueue, CancellationToken cancellationToken)
        {
            m_logger.LogInformation("🚀 Starting basic command processor");

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
                m_logger.LogInformation("🛑 Command processor stopped due to cancellation");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Fatal error in command processor");
                throw;
            }
            finally
            {
                m_logger.LogInformation("🏁 Command processor shutdown complete");
            }
        }

        /// <summary>
        /// Processes a single command
        /// </summary>
        /// <param name="queuedCommand">The command to process</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ProcessSingleCommandAsync(QueuedCommand queuedCommand, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Set as current command
                m_currentCommand = queuedCommand;
                UpdateCommandState(queuedCommand.Id ?? string.Empty, CommandState.Executing);

                m_logger.LogInformation("⚡ Processing command {CommandId}: {Command}", queuedCommand.Id, queuedCommand.Command);

                // Execute command
                var result = await m_cdbSession.ExecuteCommand(queuedCommand.Command ?? string.Empty, cancellationToken);

                // Complete successfully
                CompleteCommand(queuedCommand, result, CommandState.Completed);
                Interlocked.Increment(ref m_commandsProcessed);

                var elapsed = DateTime.UtcNow - startTime;
                m_logger.LogInformation("✅ Command {CommandId} completed in {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (queuedCommand.CancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                // Command was specifically cancelled
                CompleteCommand(queuedCommand, "Command was cancelled", CommandState.Cancelled);
                Interlocked.Increment(ref m_commandsCancelled);

                var elapsed = DateTime.UtcNow - startTime;
                m_logger.LogWarning("🚫 Command {CommandId} was cancelled after {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Service shutdown
                CompleteCommand(queuedCommand, "Service is shutting down", CommandState.Cancelled);
                m_logger.LogInformation("🛑 Command {CommandId} cancelled due to service shutdown", queuedCommand.Id);
            }
            catch (Exception ex)
            {
                // Command failed
                var errorMessage = $"Command execution failed: {ex.Message}";
                CompleteCommand(queuedCommand, errorMessage, CommandState.Failed);
                Interlocked.Increment(ref m_commandsFailed);

                var elapsed = DateTime.UtcNow - startTime;
                m_logger.LogError(ex, "❌ Command {CommandId} failed after {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);
            }
            finally
            {
                // Clear current command
                if (m_currentCommand == queuedCommand)
                    m_currentCommand = null;

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
                UpdateCommandState(command.Id ?? string.Empty, state);

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
                m_logger.LogError(ex, "Error completing command {CommandId}", command.Id);
            }
        }

        /// <summary>
        /// Updates the state of a command.
        /// </summary>
        /// <param name="commandId">The ID of the command to update.</param>
        /// <param name="newState">The new state to set for the command.</param>
        private void UpdateCommandState(string commandId, CommandState newState)
        {
            try
            {
                if (m_activeCommands.TryGetValue(commandId, out var command))
                {
                    var updatedCommand = command.WithState(newState);
                    m_activeCommands[commandId] = updatedCommand;

                    m_logger.LogTrace("🔄 Command {CommandId} state changed to {State}", commandId, newState);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error updating command state for {CommandId}", commandId);
            }
        }

        /// <summary>
        /// Gets the currently executing command
        /// </summary>
        /// <returns>The current command, or null if none is executing</returns>
        public QueuedCommand? GetCurrentCommand()
        {
            return m_currentCommand;
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        /// <returns>Performance statistics tuple</returns>
        public (long Processed, long Failed, long Cancelled) GetPerformanceStats()
        {
            return (
                Interlocked.Read(ref m_commandsProcessed),
                Interlocked.Read(ref m_commandsFailed),
                Interlocked.Read(ref m_commandsCancelled)
            );
        }

        /// <summary>
        /// Logs periodic statistics about command processing
        /// </summary>
        private void LogPeriodicStatistics()
        {
            var now = DateTime.UtcNow;
            if (now - m_lastStatsLog >= m_config.StatsLogInterval)
            {
                var stats = GetPerformanceStats();
                m_logger.LogInformation("📊 Command stats - Processed: {Processed}, Failed: {Failed}, Cancelled: {Cancelled}, Active: {Active}",
                    stats.Processed, stats.Failed, stats.Cancelled, m_activeCommands.Count);
                m_lastStatsLog = now;
            }
        }

        /// <summary>
        /// Triggers immediate cleanup of completed commands (for testing)
        /// </summary>
        public void TriggerCleanup()
        {
            CleanupCompletedCommands(null);
        }

        /// <summary>
        /// Cleans up completed commands periodically
        /// </summary>
        /// <param name="state">The state object passed to the timer callback (unused)</param>
        private void CleanupCompletedCommands(object? state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - m_config.CommandRetentionTime;
                var commandsToRemove = new List<string>();

                foreach (var kvp in m_activeCommands)
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
                    m_activeCommands.TryRemove(commandId, out _);
                }

                if (commandsToRemove.Count > 0)
                {
                    m_logger.LogDebug("🧹 Cleaned up {Count} completed commands", commandsToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during command cleanup");
            }
        }

        /// <summary>
        /// Disposes of the processor and its resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                m_cleanupTimer?.Dispose();
                m_logger.LogInformation("🧹 Basic command processor disposed");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error disposing basic command processor");
            }
        }
    }
}
