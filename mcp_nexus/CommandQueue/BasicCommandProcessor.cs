using System.Collections.Concurrent;
using mcp_nexus.Debugger;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Basic command processor for simple command queue operations
    /// </summary>
    public class BasicCommandProcessor
    {
        private readonly ICdbSession m_CdbSession;
        private readonly ILogger<BasicCommandProcessor> m_Logger;
        private readonly BasicQueueConfiguration m_Config;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_ActiveCommands;
        private readonly SessionCommandResultCache? m_ResultCache;
        private volatile QueuedCommand? m_CurrentCommand;

        // Performance counters
        private long m_CommandsProcessed = 0;
        private long m_CommandsFailed = 0;
        private long m_CommandsCancelled = 0;
        private DateTime m_LastStatsLog = DateTime.UtcNow;

        // Cleanup timer
        private readonly Timer m_CleanupTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicCommandProcessor"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording processing operations.</param>
        /// <param name="config">The basic queue configuration settings.</param>
        /// <param name="activeCommands">The concurrent dictionary for tracking active commands.</param>
        /// <param name="resultCache">Optional session command result cache for storing results.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public BasicCommandProcessor(
            ICdbSession cdbSession,
            ILogger<BasicCommandProcessor> logger,
            BasicQueueConfiguration config,
            ConcurrentDictionary<string, QueuedCommand> activeCommands,
            SessionCommandResultCache? resultCache = null)
        {
            m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_ActiveCommands = activeCommands ?? throw new ArgumentNullException(nameof(activeCommands));

            // Use provided cache or create a default one if memory optimization is enabled
            m_ResultCache = resultCache ?? (m_Config.EnableMemoryOptimization 
                ? new SessionCommandResultCache(
                    m_Config.MaxCommandMemoryBytes,
                    m_Config.MaxCommandsInMemory,
                    m_Config.MemoryPressureThreshold,
                    null) // Use null logger for now to avoid type mismatch
                : null);

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
            m_Logger.LogInformation("🚀 Starting basic command processor");

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
                m_Logger.LogInformation("🛑 Command processor stopped due to cancellation");
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
                m_CurrentCommand = queuedCommand;
                UpdateCommandState(queuedCommand.Id ?? string.Empty, CommandState.Executing);

                m_Logger.LogInformation("⚡ Processing command {CommandId}: {Command}", queuedCommand.Id, queuedCommand.Command);

                // Execute command
                var result = await m_CdbSession.ExecuteCommand(queuedCommand.Command ?? string.Empty, cancellationToken);

                // Store result in cache for session persistence
                var commandResult = CommandResult.Success(result, DateTime.UtcNow - startTime);
                m_ResultCache?.StoreResult(queuedCommand.Id ?? string.Empty, commandResult);

                // Complete successfully
                CompleteCommand(queuedCommand, result, CommandState.Completed);
                Interlocked.Increment(ref m_CommandsProcessed);

                var elapsed = DateTime.UtcNow - startTime;
                m_Logger.LogInformation("✅ Command {CommandId} completed in {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (queuedCommand.CancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                // Command was specifically cancelled
                var errorMessage = "Command was cancelled";
                var elapsed = DateTime.UtcNow - startTime;
                
                // Store cancelled result in cache
                var cancelledResult = CommandResult.Failure(errorMessage, elapsed);
                m_ResultCache?.StoreResult(queuedCommand.Id ?? string.Empty, cancelledResult);
                
                CompleteCommand(queuedCommand, errorMessage, CommandState.Cancelled);
                Interlocked.Increment(ref m_CommandsCancelled);

                m_Logger.LogWarning("🚫 Command {CommandId} was cancelled after {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Service shutdown
                var errorMessage = "Service is shutting down";
                var elapsed = DateTime.UtcNow - startTime;
                
                // Store cancelled result in cache
                var cancelledResult = CommandResult.Failure(errorMessage, elapsed);
                m_ResultCache?.StoreResult(queuedCommand.Id ?? string.Empty, cancelledResult);
                
                CompleteCommand(queuedCommand, errorMessage, CommandState.Cancelled);
                m_Logger.LogInformation("🛑 Command {CommandId} cancelled due to service shutdown", queuedCommand.Id);
            }
            catch (Exception ex)
            {
                // Command failed
                var errorMessage = $"Command execution failed: {ex.Message}";
                var elapsed = DateTime.UtcNow - startTime;
                
                // Store failed result in cache
                var failedResult = CommandResult.Failure(errorMessage, elapsed);
                m_ResultCache?.StoreResult(queuedCommand.Id ?? string.Empty, failedResult);
                
                CompleteCommand(queuedCommand, errorMessage, CommandState.Failed);
                Interlocked.Increment(ref m_CommandsFailed);

                m_Logger.LogError(ex, "❌ Command {CommandId} failed after {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);
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
                m_Logger.LogError(ex, "Error completing command {CommandId}", command.Id);
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
                if (m_ActiveCommands.TryGetValue(commandId, out var command))
                {
                    var updatedCommand = command.WithState(newState);
                    m_ActiveCommands[commandId] = updatedCommand;

                    m_Logger.LogTrace("🔄 Command {CommandId} state changed to {State}", commandId, newState);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error updating command state for {CommandId}", commandId);
            }
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
        /// Gets performance statistics
        /// </summary>
        /// <returns>Performance statistics tuple</returns>
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
            var now = DateTime.UtcNow;
            if (now - m_LastStatsLog >= m_Config.StatsLogInterval)
            {
                var stats = GetPerformanceStats();
                m_Logger.LogInformation("📊 Command stats - Processed: {Processed}, Failed: {Failed}, Cancelled: {Cancelled}, Active: {Active}",
                    stats.Processed, stats.Failed, stats.Cancelled, m_ActiveCommands.Count);
                m_LastStatsLog = now;
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
                var cutoffTime = DateTime.UtcNow - m_Config.CommandRetentionTime;
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
        /// Gets the result of a specific command from the cache
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <returns>The cached command result, or null if not found</returns>
        public ICommandResult? GetCommandResult(string commandId)
        {
            return m_ResultCache?.GetResult(commandId);
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        /// <returns>Cache statistics, or null if no cache is available</returns>
        public CacheStatistics? GetCacheStatistics()
        {
            return m_ResultCache?.GetStatistics();
        }

        /// <summary>
        /// Disposes of the processor and its resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                m_CleanupTimer?.Dispose();
                // Only dispose cache if it was created by this processor (not injected)
                if (m_ResultCache != null && m_Config.EnableMemoryOptimization)
                {
                    m_ResultCache.Dispose();
                }
                m_Logger.LogInformation("🧹 Basic command processor disposed");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error disposing basic command processor");
            }
        }
    }
}
