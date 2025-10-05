using System.Collections.Concurrent;
using System.Diagnostics;
using mcp_nexus.Debugger;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Handles the core command processing logic for the isolated command queue
    /// </summary>
    public class CommandProcessor
    {
        private readonly ICdbSession m_CdbSession;
        private readonly ILogger m_Logger;
        private readonly CommandQueueConfiguration m_Config;
        private readonly CommandTracker m_Tracker;
        private readonly BlockingCollection<QueuedCommand> m_CommandQueue;
        private readonly CancellationTokenSource m_ProcessingCts;
        private readonly SessionCommandResultCache? m_ResultCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandProcessor"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording processing operations.</param>
        /// <param name="config">The command queue configuration settings.</param>
        /// <param name="tracker">The command tracker for monitoring command status.</param>
        /// <param name="commandQueue">The blocking collection for queued commands.</param>
        /// <param name="processingCts">The cancellation token source for processing operations.</param>
        /// <param name="resultCache">Optional session command result cache for storing results.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public CommandProcessor(
            ICdbSession cdbSession,
            ILogger logger,
            CommandQueueConfiguration config,
            CommandTracker tracker,
            BlockingCollection<QueuedCommand> commandQueue,
            CancellationTokenSource processingCts,
            SessionCommandResultCache? resultCache = null)
        {
            m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_Tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            m_CommandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
            m_ProcessingCts = processingCts ?? throw new ArgumentNullException(nameof(processingCts));
            m_ResultCache = resultCache;
        }

        /// <summary>
        /// Main processing loop for the command queue
        /// </summary>
        public async Task ProcessCommandQueueAsync()
        {
            m_Logger.LogInformation("🔄 Starting command processing loop for session {SessionId}", m_Config.SessionId);

            try
            {
                // Process commands until cancellation or disposal
                foreach (var command in m_CommandQueue.GetConsumingEnumerable(m_ProcessingCts.Token))
                {
                    try
                    {
                        // Check for cancellation before processing
                        m_ProcessingCts.Token.ThrowIfCancellationRequested();

                        m_Logger.LogInformation("🎯 Processing command {CommandId}: {Command}", command.Id, command.Command);

                        // Set as current command
                        m_Tracker.SetCurrentCommand(command);

                        // Execute the command
                        await ExecuteCommandSafely(command);
                    }
                    catch (ObjectDisposedException)
                    {
                        m_Logger.LogDebug("Command queue disposed during enumeration - stopping processing");
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        m_Logger.LogDebug("Command processing cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex, "Unexpected error in command processing loop");
                        // Continue processing other commands
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                m_Logger.LogDebug("Command queue disposed - processing loop ended");
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInformation("Command processing cancelled for session {SessionId}", m_Config.SessionId);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Fatal error in command processing loop for session {SessionId}", m_Config.SessionId);
            }
            finally
            {
                m_Tracker.SetCurrentCommand(null);
                m_Logger.LogInformation("✅ Command processing loop ended for session {SessionId}", m_Config.SessionId);
            }
        }

        /// <summary>
        /// Executes a single command with proper error handling and timeout
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ExecuteCommandSafely(QueuedCommand command)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                m_Logger.LogTrace("🔄 Starting execution of command {CommandId}", command.Id);
                m_Logger.LogTrace("Debugger session IsActive={IsActive} for {SessionId}", m_CdbSession.IsActive, m_Config.SessionId);

                // Update command state to executing
                var updatedCommand = command.WithState(CommandState.Executing);
                m_Tracker.UpdateState(command.Id ?? string.Empty, CommandState.Executing);

                // Start heartbeat for long-running commands
                using var heartbeatCts = new CancellationTokenSource();
                var heartbeatTask = StartHeartbeatAsync(updatedCommand, stopwatch, heartbeatCts.Token);

                try
                {
                    // Execute the command with timeout
                    using var timeoutCts = new CancellationTokenSource(m_Config.DefaultCommandTimeout);
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        command.CancellationTokenSource?.Token ?? CancellationToken.None,
                        timeoutCts.Token,
                        m_ProcessingCts.Token);

                    var result = await m_CdbSession.ExecuteCommand(command.Command ?? string.Empty, combinedCts.Token);

                    // Stop heartbeat
                    heartbeatCts.Cancel();
                    try { await heartbeatTask; } catch { /* Ignore heartbeat cancellation */ }

                    // Store result in cache with complete metadata for session persistence
                    var commandResult = CommandResult.Success(result ?? string.Empty, stopwatch.Elapsed);
                    var endTime = DateTime.UtcNow;
                    var startTime = endTime.Add(-stopwatch.Elapsed);
                    m_ResultCache?.StoreResult(command.Id ?? string.Empty, commandResult, 
                        command.Command, command.QueueTime, startTime, endTime);

                    // Complete successfully
                    // Log small preview of result for diagnostics
                    try
                    {
                        var preview = result?.Length > 300 ? result.Substring(0, 300) + "..." : result;
                        m_Logger.LogTrace("Command {CommandId} result preview: {Preview}", command.Id, preview);
                    }
                    catch { }

                    CompleteCommandSafely(command, result ?? string.Empty, CommandState.Completed);
                    m_Tracker.UpdateState(command.Id ?? string.Empty, CommandState.Completed);

                    m_Tracker.IncrementCompleted();

                    m_Logger.LogInformation("✅ Command {CommandId} completed successfully in {Elapsed}ms",
                        command.Id, stopwatch.ElapsedMilliseconds);

                    // Results are now cached, so we can clean up immediately
                    m_Tracker.TryRemoveCommand(command.Id ?? string.Empty, out _);
                    m_Logger.LogTrace("Removed completed command {CommandId} from tracker (result cached)", command.Id);
                }
                catch (OperationCanceledException) when (command.CancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    // Command was explicitly cancelled
                    var errorMessage = "Command was cancelled by user request";
                    var cancelledResult = CommandResult.Failure(errorMessage, stopwatch.Elapsed);
                    var endTime = DateTime.UtcNow;
                    var startTime = endTime.Add(-stopwatch.Elapsed);
                    m_ResultCache?.StoreResult(command.Id ?? string.Empty, cancelledResult, 
                        command.Command, command.QueueTime, startTime, endTime);
                    
                    CompleteCommandSafely(command, errorMessage, CommandState.Cancelled);
                    m_Tracker.UpdateState(command.Id ?? string.Empty, CommandState.Cancelled);
                    m_Tracker.IncrementCancelled();
                    m_Logger.LogWarning("⚠️ Command {CommandId} was cancelled by user in {Elapsed}ms",
                        command.Id, stopwatch.ElapsedMilliseconds);

                    // Results are now cached, so we can clean up immediately
                    m_Tracker.TryRemoveCommand(command.Id ?? string.Empty, out _);
                    m_Logger.LogTrace("Removed cancelled command {CommandId} from tracker (result cached)", command.Id);
                }
                catch (OperationCanceledException)
                {
                    // Timeout or service shutdown
                    var message = stopwatch.Elapsed >= m_Config.DefaultCommandTimeout
                        ? $"Command timed out after {m_Config.DefaultCommandTimeout.TotalMinutes:F1} minutes"
                        : "Command cancelled due to service shutdown";

                    var failedResult = CommandResult.Failure(message, stopwatch.Elapsed);
                    var endTime = DateTime.UtcNow;
                    var startTime = endTime.Add(-stopwatch.Elapsed);
                    m_ResultCache?.StoreResult(command.Id ?? string.Empty, failedResult, 
                        command.Command, command.QueueTime, startTime, endTime);

                    CompleteCommandSafely(command, message, CommandState.Failed);
                    m_Tracker.UpdateState(command.Id ?? string.Empty, CommandState.Failed);
                    m_Tracker.IncrementFailed();
                    m_Logger.LogWarning("⏰ Command {CommandId} timed out or was cancelled in {Elapsed}ms",
                        command.Id, stopwatch.ElapsedMilliseconds);

                    // Results are now cached, so we can clean up immediately
                    m_Tracker.TryRemoveCommand(command.Id ?? string.Empty, out _);
                    m_Logger.LogTrace("Removed failed command {CommandId} from tracker (result cached)", command.Id);
                }
                finally
                {
                    // Ensure heartbeat is stopped
                    heartbeatCts.Cancel();
                    try { await heartbeatTask; } catch { /* Ignore */ }
                }
            }
            catch (Exception ex)
            {
                // Unexpected error during execution
                var errorMessage = $"Command execution failed: {ex.Message}";
                var failedResult = CommandResult.Failure(errorMessage, stopwatch.Elapsed);
                var endTime = DateTime.UtcNow;
                var startTime = endTime.Add(-stopwatch.Elapsed);
                m_ResultCache?.StoreResult(command.Id ?? string.Empty, failedResult, 
                    command.Command, command.QueueTime, startTime, endTime);
                
                CompleteCommandSafely(command, errorMessage, CommandState.Failed);
                m_Tracker.UpdateState(command.Id ?? string.Empty, CommandState.Failed);
                m_Tracker.IncrementFailed();
                m_Logger.LogError(ex, "❌ Command {CommandId} failed with exception in {Elapsed}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);

                // Results are now cached, so we can clean up immediately
                m_Tracker.TryRemoveCommand(command.Id ?? string.Empty, out _);
                m_Logger.LogTrace("Removed failed command {CommandId} from tracker (result cached)", command.Id);
            }
            finally
            {
                stopwatch.Stop();

                // Don't remove from active commands immediately - let them stay for status checking
                // They can be cleaned up later by a cleanup mechanism if needed

                m_Logger.LogTrace("🧹 Command {CommandId} processing finished after {Elapsed}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Starts a heartbeat task for long-running commands
        /// </summary>
        /// <param name="command">The command to monitor</param>
        /// <param name="stopwatch">The stopwatch tracking command execution time</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A task representing the heartbeat operation</returns>
        private async Task StartHeartbeatAsync(QueuedCommand command, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(m_Config.HeartbeatInterval, cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var elapsed = stopwatch.Elapsed;
                        m_Logger.LogTrace("💓 Heartbeat for command {CommandId} - elapsed: {Elapsed}",
                            command.Id, elapsed);

                        // Notify about command progress (this could be extended to send notifications)
                        // For now, just log the heartbeat
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when heartbeat is cancelled
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error in heartbeat for command {CommandId}", command.Id);
            }
        }

        /// <summary>
        /// Safely completes a command with the given result and state.
        /// </summary>
        /// <param name="command">The command to complete.</param>
        /// <param name="result">The result of the command execution.</param>
        /// <param name="state">The final state of the command.</param>
        private void CompleteCommandSafely(QueuedCommand command, string result, CommandState state)
        {
            try
            {
                // Update the command state (this is a record, so we create a new instance)
                var completedCommand = command.WithState(state);

                // Set the result
                command.CompletionSource?.TrySetResult(result);

                m_Logger.LogTrace("✅ Command {CommandId} completed with state {State}", command.Id, state);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error completing command {CommandId}", command.Id);

                // Ensure the completion source is set even if there's an error
                try
                {
                    command.CompletionSource?.TrySetResult($"Command completed with errors: {ex.Message}");
                }
                catch
                {
                    // Last resort - ignore if we can't even set the completion source
                }
            }
        }

        /// <summary>
        /// Cancels a specific command by ID.
        /// </summary>
        /// <param name="commandId">The ID of the command to cancel.</param>
        /// <returns><c>true</c> if the command was found and cancelled; otherwise, <c>false</c>.</returns>
        public bool CancelCommand(string commandId)
        {
            if (commandId == null)
                throw new ArgumentNullException(nameof(commandId), "Command ID cannot be null or empty");

            if (string.IsNullOrWhiteSpace(commandId))
                return false; // Empty string returns false, null throws

            var command = m_Tracker.GetCommand(commandId);
            if (command == null)
            {
                m_Logger.LogWarning("Cannot cancel command {CommandId} - command not found", commandId);
                return false;
            }

            try
            {
                if (command.CancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    m_Logger.LogDebug("Command {CommandId} is already cancelled", commandId);
                    return true;
                }

                m_Logger.LogInformation("🚫 Cancelling command {CommandId}", commandId);
                command.CancellationTokenSource?.Cancel();

                // If it's not currently executing, complete it immediately
                var currentCommand = m_Tracker.GetCurrentCommand();
                if (currentCommand?.Id != commandId)
                {
                    CompleteCommandSafely(command, "Command was cancelled before execution", CommandState.Cancelled);
                    m_Tracker.IncrementCancelled();
                    m_Tracker.TryRemoveCommand(commandId, out _);
                }

                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error cancelling command {CommandId}", commandId);
                return false;
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
        /// Gets the cached result with metadata for a command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>The cached result with metadata, or null if not found.</returns>
        public CachedCommandResult? GetCachedResultWithMetadata(string commandId)
        {
            return m_ResultCache?.GetCachedResultWithMetadata(commandId);
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        /// <returns>Cache statistics, or null if no cache is available</returns>
        public CacheStatistics? GetCacheStatistics()
        {
            return m_ResultCache?.GetStatistics();
        }
    }
}
