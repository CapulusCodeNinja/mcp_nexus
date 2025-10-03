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
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger m_logger;
        private readonly CommandQueueConfiguration m_config;
        private readonly CommandTracker m_tracker;
        private readonly BlockingCollection<QueuedCommand> m_commandQueue;
        private readonly CancellationTokenSource m_processingCts;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandProcessor"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording processing operations.</param>
        /// <param name="config">The command queue configuration settings.</param>
        /// <param name="tracker">The command tracker for monitoring command status.</param>
        /// <param name="commandQueue">The blocking collection for queued commands.</param>
        /// <param name="processingCts">The cancellation token source for processing operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public CommandProcessor(
            ICdbSession cdbSession,
            ILogger logger,
            CommandQueueConfiguration config,
            CommandTracker tracker,
            BlockingCollection<QueuedCommand> commandQueue,
            CancellationTokenSource processingCts)
        {
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            m_commandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
            m_processingCts = processingCts ?? throw new ArgumentNullException(nameof(processingCts));
        }

        /// <summary>
        /// Main processing loop for the command queue
        /// </summary>
        public async Task ProcessCommandQueueAsync()
        {
            m_logger.LogInformation("🔄 Starting command processing loop for session {SessionId}", m_config.SessionId);

            try
            {
                // Process commands until cancellation or disposal
                foreach (var command in m_commandQueue.GetConsumingEnumerable(m_processingCts.Token))
                {
                    try
                    {
                        // Check for cancellation before processing
                        m_processingCts.Token.ThrowIfCancellationRequested();

                        m_logger.LogInformation("🎯 Processing command {CommandId}: {Command}", command.Id, command.Command);

                        // Set as current command
                        m_tracker.SetCurrentCommand(command);

                        // Execute the command
                        await ExecuteCommandSafely(command);
                    }
                    catch (ObjectDisposedException)
                    {
                        m_logger.LogDebug("Command queue disposed during enumeration - stopping processing");
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        m_logger.LogDebug("Command processing cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Unexpected error in command processing loop");
                        // Continue processing other commands
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                m_logger.LogDebug("Command queue disposed - processing loop ended");
            }
            catch (OperationCanceledException)
            {
                m_logger.LogInformation("Command processing cancelled for session {SessionId}", m_config.SessionId);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Fatal error in command processing loop for session {SessionId}", m_config.SessionId);
            }
            finally
            {
                m_tracker.SetCurrentCommand(null);
                m_logger.LogInformation("✅ Command processing loop ended for session {SessionId}", m_config.SessionId);
            }
        }

        /// <summary>
        /// Executes a single command with proper error handling and timeout
        /// </summary>
        private async Task ExecuteCommandSafely(QueuedCommand command)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                m_logger.LogTrace("🔄 Starting execution of command {CommandId}", command.Id);
                m_logger.LogTrace("Debugger session IsActive={IsActive} for {SessionId}", m_cdbSession.IsActive, m_config.SessionId);

                // Update command state to executing
                var updatedCommand = command.WithState(CommandState.Executing);
                m_tracker.UpdateState(command.Id ?? string.Empty, CommandState.Executing);

                // Start heartbeat for long-running commands
                using var heartbeatCts = new CancellationTokenSource();
                var heartbeatTask = StartHeartbeatAsync(updatedCommand, stopwatch, heartbeatCts.Token);

                try
                {
                    // Execute the command with timeout
                    using var timeoutCts = new CancellationTokenSource(m_config.DefaultCommandTimeout);
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        command.CancellationTokenSource?.Token ?? CancellationToken.None,
                        timeoutCts.Token,
                        m_processingCts.Token);

                    var result = await m_cdbSession.ExecuteCommand(command.Command ?? string.Empty, combinedCts.Token);

                    // Stop heartbeat
                    heartbeatCts.Cancel();
                    try { await heartbeatTask; } catch { /* Ignore heartbeat cancellation */ }

                    // Complete successfully
                    // Log small preview of result for diagnostics
                    try
                    {
                        var preview = result?.Length > 300 ? result.Substring(0, 300) + "..." : result;
                        m_logger.LogTrace("Command {CommandId} result preview: {Preview}", command.Id, preview);
                    }
                    catch { }

                    CompleteCommandSafely(command, result ?? string.Empty, CommandState.Completed);
                    m_tracker.UpdateState(command.Id ?? string.Empty, CommandState.Completed);

                    m_tracker.IncrementCompleted();

                    m_logger.LogInformation("✅ Command {CommandId} completed successfully in {Elapsed}ms",
                        command.Id, stopwatch.ElapsedMilliseconds);

                    // CRITICAL FIX: Remove completed command from tracker after short retention to prevent memory leak
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(5)); // Keep for 5 minutes for result retrieval
                            m_tracker.TryRemoveCommand(command.Id ?? string.Empty, out _);
                            m_logger.LogTrace("Removed completed command {CommandId} from tracker", command.Id);
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogWarning(ex, "Error during delayed cleanup of command {CommandId}", command.Id);
                        }
                    });
                }
                catch (OperationCanceledException) when (command.CancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    // Command was explicitly cancelled
                    CompleteCommandSafely(command, "Command was cancelled by user request", CommandState.Cancelled);
                    m_tracker.UpdateState(command.Id ?? string.Empty, CommandState.Cancelled);
                    m_tracker.IncrementCancelled();
                    m_logger.LogWarning("⚠️ Command {CommandId} was cancelled by user in {Elapsed}ms",
                        command.Id, stopwatch.ElapsedMilliseconds);

                    // Clean up cancelled command
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(5));
                            m_tracker.TryRemoveCommand(command.Id ?? string.Empty, out _);
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogWarning(ex, "Error during delayed cleanup of cancelled command {CommandId}", command.Id);
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    // Timeout or service shutdown
                    var message = stopwatch.Elapsed >= m_config.DefaultCommandTimeout
                        ? $"Command timed out after {m_config.DefaultCommandTimeout.TotalMinutes:F1} minutes"
                        : "Command cancelled due to service shutdown";

                    CompleteCommandSafely(command, message, CommandState.Failed);
                    m_tracker.UpdateState(command.Id ?? string.Empty, CommandState.Failed);
                    m_tracker.IncrementFailed();
                    m_logger.LogWarning("⏰ Command {CommandId} timed out or was cancelled in {Elapsed}ms",
                        command.Id, stopwatch.ElapsedMilliseconds);

                    // Clean up failed/timed out command
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(5));
                            m_tracker.TryRemoveCommand(command.Id ?? string.Empty, out _);
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogWarning(ex, "Error during delayed cleanup of failed command {CommandId}", command.Id);
                        }
                    });
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
                CompleteCommandSafely(command, $"Command execution failed: {ex.Message}", CommandState.Failed);
                m_tracker.UpdateState(command.Id ?? string.Empty, CommandState.Failed);
                m_tracker.IncrementFailed();
                m_logger.LogError(ex, "❌ Command {CommandId} failed with exception in {Elapsed}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);

                // Clean up failed command
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5));
                        m_tracker.TryRemoveCommand(command.Id ?? string.Empty, out _);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Error during delayed cleanup of failed command {CommandId}", command.Id);
                    }
                });
            }
            finally
            {
                stopwatch.Stop();

                // Don't remove from active commands immediately - let them stay for status checking
                // They can be cleaned up later by a cleanup mechanism if needed

                m_logger.LogTrace("🧹 Command {CommandId} processing finished after {Elapsed}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Starts a heartbeat task for long-running commands
        /// </summary>
        private async Task StartHeartbeatAsync(QueuedCommand command, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(m_config.HeartbeatInterval, cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var elapsed = stopwatch.Elapsed;
                        m_logger.LogTrace("💓 Heartbeat for command {CommandId} - elapsed: {Elapsed}",
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
                m_logger.LogWarning(ex, "Error in heartbeat for command {CommandId}", command.Id);
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

                m_logger.LogTrace("✅ Command {CommandId} completed with state {State}", command.Id, state);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error completing command {CommandId}", command.Id);

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

            var command = m_tracker.GetCommand(commandId);
            if (command == null)
            {
                m_logger.LogWarning("Cannot cancel command {CommandId} - command not found", commandId);
                return false;
            }

            try
            {
                if (command.CancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    m_logger.LogDebug("Command {CommandId} is already cancelled", commandId);
                    return true;
                }

                m_logger.LogInformation("🚫 Cancelling command {CommandId}", commandId);
                command.CancellationTokenSource?.Cancel();

                // If it's not currently executing, complete it immediately
                var currentCommand = m_tracker.GetCurrentCommand();
                if (currentCommand?.Id != commandId)
                {
                    CompleteCommandSafely(command, "Command was cancelled before execution", CommandState.Cancelled);
                    m_tracker.IncrementCancelled();
                    m_tracker.TryRemoveCommand(commandId, out _);
                }

                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error cancelling command {CommandId}", commandId);
                return false;
            }
        }
    }
}
