using System.Collections.Concurrent;
using mcp_nexus.Debugger;
using mcp_nexus.Notifications;
using mcp_nexus.Recovery;
using mcp_nexus.Constants;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Enhanced CommandQueueService with automated recovery for unattended server operation
    /// </summary>
    public class ResilientCommandQueueService : IDisposable, ICommandQueueService
    {
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger<ResilientCommandQueueService> m_logger;
        private readonly ICommandTimeoutService m_timeoutService;
        private readonly ICdbSessionRecoveryService m_recoveryService;
        private readonly IMcpNotificationService? m_notificationService;

        private readonly ConcurrentQueue<QueuedCommand> m_commandQueue = new();
        private readonly SemaphoreSlim m_queueSemaphore = new(0);
        private readonly CancellationTokenSource m_serviceCts = new();
        private readonly Task m_processingTask;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_activeCommands = new();
        private QueuedCommand? m_currentCommand;
        private readonly object m_currentCommandLock = new();
        private bool m_disposed;

        // Configuration for automated recovery
        private readonly TimeSpan m_defaultCommandTimeout = ApplicationConstants.DefaultCommandTimeout;
        private readonly TimeSpan m_complexCommandTimeout = ApplicationConstants.MaxCommandTimeout;
        private readonly TimeSpan m_maxCommandTimeout = ApplicationConstants.LongRunningCommandTimeout;

        // IMPROVED: Add cleanup mechanism and monitoring like CommandQueueService
        private readonly Timer m_cleanupTimer;
        private readonly TimeSpan m_cleanupInterval = ApplicationConstants.CleanupInterval;
        private readonly TimeSpan m_commandRetentionTime = ApplicationConstants.CommandRetentionTime;
        private long m_commandsProcessed = 0;
        private long m_commandsFailed = 0;
        private long m_commandsCancelled = 0;
        private DateTime m_lastStatsLog = DateTime.UtcNow;

        public ResilientCommandQueueService(
            ICdbSession cdbSession,
            ILogger<ResilientCommandQueueService> logger,
            ICommandTimeoutService timeoutService,
            ICdbSessionRecoveryService recoveryService,
            IMcpNotificationService? notificationService = null)
        {
            m_cdbSession = cdbSession;
            m_logger = logger;
            m_timeoutService = timeoutService;
            m_recoveryService = recoveryService;
            m_notificationService = notificationService;

            m_logger.LogInformation("Starting resilient command queue with automated recovery");

            // Start the background processing task
            try
            {
                m_processingTask = Task.Run(ProcessCommandQueue, m_serviceCts.Token);
                m_cleanupTimer = new Timer(CleanupCompletedCommands, null, m_cleanupInterval, m_cleanupInterval);
                m_logger.LogDebug("Resilient command queue background task started successfully");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to start resilient command queue background task");
                throw;
            }
        }

        public string QueueCommand(string command)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var commandId = Guid.NewGuid().ToString();
            var queuedCommand = new QueuedCommand(
                commandId,
                command,
                DateTime.UtcNow,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource()
            );

            m_activeCommands[commandId] = queuedCommand;
            m_commandQueue.Enqueue(queuedCommand);
            m_queueSemaphore.Release();

            m_logger.LogInformation("Queued command {CommandId}: {Command}", commandId, command);
            m_logger.LogDebug("Queue depth: {QueueDepth}", m_commandQueue.Count);

            // IMPROVED: Better fire-and-forget with proper task tracking
            if (m_notificationService != null)
            {
                var notificationTask = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService.NotifyCommandStatusAsync(
                            commandId, command, "queued", 0, "Command queued for execution");
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send queued notification for command {CommandId}", commandId);
                    }
                }, CancellationToken.None);

                // Track the task to prevent unobserved exceptions
                _ = notificationTask.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        m_logger.LogError(t.Exception?.GetBaseException(), "Notification task failed for command {CommandId}", commandId);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            return commandId;
        }

        public async Task<string> GetCommandResult(string commandId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            if (string.IsNullOrEmpty(commandId))
                return $"Command not found: {commandId}";

            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                if (command.CompletionSource.Task.IsCompleted)
                {
                    try
                    {
                        // FIXED: Use await instead of blocking .Result
                        var result = await command.CompletionSource.Task.ConfigureAwait(false);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        return $"Command failed: {ex.Message}";
                    }
                }
                else
                {
                    return $"Command is still executing... Please call get_command_status(commandId='{commandId}') again in 5-10 seconds to check if completed.";
                }
            }

            return $"Command not found: {commandId}";
        }

        public bool CancelCommand(string commandId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            if (string.IsNullOrEmpty(commandId))
                return false;

            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                m_logger.LogInformation("Cancelling command {CommandId}: {Command}", commandId, command.Command);

                // Cancel timeout first
                m_timeoutService.CancelCommandTimeout(commandId);

                try
                {
                    command.CancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    m_logger.LogDebug("CancellationTokenSource already disposed for command {CommandId}", commandId);
                    return false;
                }

                // If this is the currently executing command, also cancel the CDB operation
                lock (m_currentCommandLock)
                {
                    if (m_currentCommand?.Id == commandId)
                    {
                        m_logger.LogWarning("Cancelling currently executing command {CommandId}", commandId);
                        m_cdbSession.CancelCurrentOperation();
                    }
                }

                // Remove from queue if still queued (prevents "Cancelled" status showing)
                RemoveCancelledCommandFromQueue(commandId);

                return true;
            }

            m_logger.LogDebug("Attempted to cancel non-existent command: {CommandId}", commandId);
            return false;
        }

        public int CancelAllCommands(string? reason = null)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            var reasonText = string.IsNullOrWhiteSpace(reason) ? "No reason provided" : reason;
            m_logger.LogWarning("Cancelling ALL commands. Reason: {Reason}", reasonText);

            var cancelledCount = 0;
            string? currentId;

            lock (m_currentCommandLock)
            {
                currentId = m_currentCommand?.Id;
            }

            var commandsSnapshot = new List<QueuedCommand>(m_activeCommands.Values);
            foreach (var cmd in commandsSnapshot)
            {
                try
                {
                    if (!cmd.CompletionSource.Task.IsCompleted)
                    {
                        // Cancel timeout
                        m_timeoutService.CancelCommandTimeout(cmd.Id);

                        try
                        {
                            cmd.CancellationTokenSource.Cancel();
                            cancelledCount++;
                        }
                        catch (ObjectDisposedException)
                        {
                            // Already disposed
                        }

                        cmd.CompletionSource.TrySetResult($"Command was cancelled. Reason: {reasonText}");
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error cancelling command {CommandId}", cmd.Id);
                }
            }

            // Clear the entire queue of pending commands
            ClearQueue();

            if (!string.IsNullOrEmpty(currentId))
            {
                try
                {
                    m_logger.LogDebug("Requesting CDB cancellation for currently executing command: {CommandId}", currentId);
                    m_cdbSession.CancelCurrentOperation();
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error requesting CDB cancellation for current command: {CommandId}", currentId);
                }
            }

            m_logger.LogInformation("Cancelled {Count} command(s)", cancelledCount);
            return cancelledCount;
        }

        public QueuedCommand? GetCurrentCommand()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            lock (m_currentCommandLock)
            {
                return m_currentCommand;
            }
        }

        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            var results = new List<(string, string, DateTime, string)>();

            // Add current command
            lock (m_currentCommandLock)
            {
                if (m_currentCommand != null)
                {
                    results.Add((m_currentCommand.Id, m_currentCommand.Command, m_currentCommand.QueueTime, "Executing"));
                }
            }

            // Add queued commands (excluding cancelled ones)
            foreach (var cmd in m_commandQueue)
            {
                if (!cmd.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    results.Add((cmd.Id, cmd.Command, cmd.QueueTime, "Queued"));
                }
            }

            return results;
        }

        private async Task ProcessCommandQueue()
        {
            m_logger.LogDebug("Resilient command processor started");

            try
            {
                while (!m_serviceCts.Token.IsCancellationRequested)
                {
                    await m_queueSemaphore.WaitAsync(m_serviceCts.Token);

                    if (m_commandQueue.TryDequeue(out var queuedCommand))
                    {
                        // Skip cancelled commands
                        if (queuedCommand.CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            m_logger.LogDebug("Skipping cancelled command {CommandId}", queuedCommand.Id);
                            queuedCommand.CompletionSource.TrySetResult("Command was cancelled while queued.");
                            CleanupCommand(queuedCommand);
                            continue;
                        }

                        await ProcessSingleCommand(queuedCommand);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_logger.LogDebug("Command queue processing was cancelled");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Unexpected error in command queue processing - attempting recovery");

                // FIXED: Proper fire-and-forget with error handling
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_recoveryService.RecoverStuckSession("Queue processor failure");
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Recovery attempt failed after queue processor error");
                    }
                }, CancellationToken.None);
            }
        }

        private async Task ProcessSingleCommand(QueuedCommand queuedCommand)
        {
            lock (m_currentCommandLock)
            {
                m_currentCommand = queuedCommand;
            }

            // FIXED: Update command state to executing
            UpdateCommandState(queuedCommand.Id, CommandState.Executing);

            var waitTime = (DateTime.UtcNow - queuedCommand.QueueTime).TotalSeconds;
            m_logger.LogInformation("Executing command {CommandId}: {Command}", queuedCommand.Id, queuedCommand.Command);
            m_logger.LogDebug("Command wait time: {WaitTime:F1}s", waitTime);

            // FIXED: Proper fire-and-forget with error handling
            _ = Task.Run(async () =>
            {
                try
                {
                    if (m_notificationService != null)
                    {
                        await m_notificationService.NotifyCommandStatusAsync(
                            queuedCommand.Id, queuedCommand.Command, "executing", 10, "Command started execution");
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Failed to send executing notification for command {CommandId}", queuedCommand.Id);
                }
            }, CancellationToken.None);

            // Determine timeout based on command complexity
            var timeout = DetermineCommandTimeout(queuedCommand.Command);

            // Start automated timeout monitoring
            m_timeoutService.StartCommandTimeout(queuedCommand.Id, timeout, async () =>
            {
                m_logger.LogError("Command {CommandId} timed out after {Minutes:F1} minutes",
                    queuedCommand.Id, timeout.TotalMinutes);

                // First try gentle recovery
                await m_recoveryService.RecoverStuckSession($"Command timeout: {queuedCommand.Command}");

                // Set timeout result
                queuedCommand.CompletionSource.TrySetResult(
                    $"Command timed out after {timeout.TotalMinutes:F1} minutes and session was recovered. " +
                    "This may indicate a complex analysis that requires breaking into smaller commands.");
            });

            // Start heartbeat notifications for long-running commands
            var heartbeatCts = new CancellationTokenSource();
            var commandStartTime = DateTime.UtcNow;
            var heartbeatInterval = TimeSpan.FromSeconds(30); // Send heartbeat every 30 seconds

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!heartbeatCts.Token.IsCancellationRequested && !queuedCommand.CompletionSource.Task.IsCompleted)
                    {
                        await Task.Delay(heartbeatInterval, heartbeatCts.Token);

                        if (!heartbeatCts.Token.IsCancellationRequested && !queuedCommand.CompletionSource.Task.IsCompleted)
                        {
                            var elapsed = DateTime.UtcNow - commandStartTime;
                            var details = DetermineHeartbeatDetails(queuedCommand.Command, elapsed);

                            if (m_notificationService != null)
                            {
                                await m_notificationService.NotifyCommandHeartbeatAsync(
                                    queuedCommand.Id, queuedCommand.Command, elapsed, details);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, ignore
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Error in heartbeat sender for command {CommandId}", queuedCommand.Id);
                }
            }, heartbeatCts.Token);

            try
            {
                // Check session health before executing
                if (!m_recoveryService.IsSessionHealthy())
                {
                    m_logger.LogWarning("Session unhealthy, attempting recovery before command execution");
                    var recovered = await m_recoveryService.RecoverStuckSession("Pre-execution health check failed");

                    if (!recovered)
                    {
                        queuedCommand.CompletionSource.TrySetResult("Session recovery failed - command could not be executed");
                        return;
                    }
                }

                // Execute the command
                var result = await m_cdbSession.ExecuteCommand(queuedCommand.Command, queuedCommand.CancellationTokenSource.Token);

                // Cancel timeout on successful completion
                m_timeoutService.CancelCommandTimeout(queuedCommand.Id);

                m_logger.LogInformation("Command {CommandId} completed successfully", queuedCommand.Id);
                var wasCompleted = queuedCommand.CompletionSource.TrySetResult(result);
                if (wasCompleted)
                {
                    UpdateCommandState(queuedCommand.Id, CommandState.Completed);
                    Interlocked.Increment(ref m_commandsProcessed);
                }

                LogConcurrencyStats();

                // Send completion notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (m_notificationService != null)
                        {
                            await m_notificationService.NotifyCommandStatusAsync(
                                queuedCommand.Id, queuedCommand.Command, "completed", 100, "Command completed successfully", result);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send completion notification for command {CommandId}", queuedCommand.Id);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                m_timeoutService.CancelCommandTimeout(queuedCommand.Id);
                m_logger.LogInformation("Command {CommandId} was cancelled", queuedCommand.Id);
                var wasCompleted = queuedCommand.CompletionSource.TrySetResult("Command execution was cancelled.");
                if (wasCompleted)
                {
                    UpdateCommandState(queuedCommand.Id, CommandState.Cancelled);
                    Interlocked.Increment(ref m_commandsCancelled);
                }

                LogConcurrencyStats();

                // Send cancellation notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (m_notificationService != null)
                        {
                            await m_notificationService.NotifyCommandStatusAsync(
                                queuedCommand.Id, queuedCommand.Command, "cancelled", progress: null,
                                message: "Command execution was cancelled", result: null, error: null);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send cancellation notification for command {CommandId}", queuedCommand.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                m_timeoutService.CancelCommandTimeout(queuedCommand.Id);
                m_logger.LogError(ex, "Command {CommandId} execution failed", queuedCommand.Id);

                // On execution failure, trigger recovery
                _ = Task.Run(async () => await m_recoveryService.RecoverStuckSession($"Command execution failed: {ex.Message}"));

                var wasCompleted = queuedCommand.CompletionSource.TrySetResult($"Command execution failed: {ex.Message}");
                if (wasCompleted)
                {
                    UpdateCommandState(queuedCommand.Id, CommandState.Failed);
                    Interlocked.Increment(ref m_commandsFailed);
                }

                LogConcurrencyStats();

                // Send error notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (m_notificationService != null)
                        {
                            await m_notificationService.NotifyCommandStatusAsync(
                                queuedCommand.Id, queuedCommand.Command, "failed", null, "Command execution failed", null, ex.Message);
                        }
                    }
                    catch (Exception notificationEx)
                    {
                        m_logger.LogWarning(notificationEx, "Failed to send error notification for command {CommandId}", queuedCommand.Id);
                    }
                });
            }
            finally
            {
                // Stop heartbeat notifications
                heartbeatCts?.Cancel();
                heartbeatCts?.Dispose();

                lock (m_currentCommandLock)
                {
                    m_currentCommand = null;
                }
                CleanupCommand(queuedCommand);
            }
        }

        private TimeSpan DetermineCommandTimeout(string command)
        {
            var lowerCommand = command.ToLowerInvariant();

            // Complex analysis commands get longer timeouts
            if (lowerCommand.Contains("!analyze") ||
                lowerCommand.Contains("!heap") ||
                lowerCommand.Contains("!locks") ||
                lowerCommand.Contains("!handle") ||
                lowerCommand.Contains("!process 0 0"))
            {
                return m_complexCommandTimeout;
            }

            // Very simple commands get shorter timeouts
            if (lowerCommand.Length < 10 &&
                (lowerCommand.StartsWith("k") ||
                 lowerCommand.StartsWith("lm") ||
                 lowerCommand.StartsWith("r") ||
                 lowerCommand == "version"))
            {
                return TimeSpan.FromMinutes(2);
            }

            return m_defaultCommandTimeout;
        }

        private static string DetermineHeartbeatDetails(string command, TimeSpan elapsed)
        {
            // PERFORMANCE: Avoid ToLowerInvariant() allocation - use StringComparison instead

            // Provide context-specific details based on command type
            if (command.Contains("!analyze", StringComparison.OrdinalIgnoreCase))
            {
                if (elapsed.TotalMinutes < 2)
                    return "Initializing crash analysis engine...";
                else if (elapsed.TotalMinutes < 5)
                    return "Analyzing memory dumps and stack traces...";
                else if (elapsed.TotalMinutes < 10)
                    return "Performing deep symbol resolution...";
                else
                    return "Processing complex crash analysis (this may take several more minutes)...";
            }

            if (command.Contains("!heap", StringComparison.OrdinalIgnoreCase))
            {
                if (elapsed.TotalMinutes < 1)
                    return "Scanning heap structures...";
                else if (elapsed.TotalMinutes < 3)
                    return "Analyzing heap allocations and free blocks...";
                else
                    return "Processing large heap dump (this is normal for applications with high memory usage)...";
            }

            if (command.Contains("!process 0 0", StringComparison.OrdinalIgnoreCase) ||
                command.Contains("!process", StringComparison.OrdinalIgnoreCase))
            {
                if (elapsed.TotalMinutes < 1)
                    return "Enumerating system processes...";
                else if (elapsed.TotalMinutes < 3)
                    return "Gathering detailed process information...";
                else
                    return "Processing extensive process data...";
            }

            if (command.Contains("!locks", StringComparison.OrdinalIgnoreCase) ||
                command.Contains("!handle", StringComparison.OrdinalIgnoreCase))
            {
                return elapsed.TotalMinutes < 2
                    ? "Scanning kernel synchronization objects..."
                    : "Analyzing complex lock dependencies...";
            }

            // Generic progress messages for other commands
            if (elapsed.TotalMinutes < 1)
                return "Command executing...";
            else if (elapsed.TotalMinutes < 5)
                return "Processing... (complex operations can take several minutes)";
            else
                return "Still working... (this operation is taking longer than usual but is still active)";
        }

        private void RemoveCancelledCommandFromQueue(string commandId)
        {
            // This is a limitation of ConcurrentQueue - we can't easily remove items
            // But we handle it in GetQueueStatus by filtering cancelled commands
            m_logger.LogDebug("Command {CommandId} cancelled - will be filtered from queue status", commandId);
        }

        private void ClearQueue()
        {
            while (m_commandQueue.TryDequeue(out _)) { }
            m_logger.LogDebug("Cleared all queued commands");
        }

        private void CleanupCommand(QueuedCommand command)
        {
            // Keep completed commands for result retrieval, just dispose the cancellation token
            command.CancellationTokenSource.Dispose();
            m_logger.LogDebug("🧹 Cleaned up resources for command {CommandId}", command.Id);
        }

        // FIXED: Add state management methods (same as CommandQueueService)
        private void UpdateCommandState(string commandId, CommandState newState)
        {
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                // Create updated command with new state
                var updatedCommand = command with { State = newState };
                if (!m_activeCommands.TryUpdate(commandId, updatedCommand, command))
                {
                    m_logger.LogTrace("Command {CommandId} state update failed - command was modified by another thread", commandId);
                }
            }
        }

        // IMPROVED: Add cleanup method for completed commands
        private void CleanupCompletedCommands(object? state)
        {
            try
            {
                if (m_disposed) return;

                var cutoffTime = DateTime.UtcNow - m_commandRetentionTime;
                // PERFORMANCE: Use Span<List<string>> to avoid repeated allocations
                var commandsToRemove = new List<string>();

                // PERFORMANCE: Use ValueTuple to avoid boxing in foreach
                foreach (var (key, command) in m_activeCommands)
                {
                    if (command.State == CommandState.Completed ||
                        command.State == CommandState.Cancelled ||
                        command.State == CommandState.Failed)
                    {
                        if (command.QueueTime < cutoffTime)
                        {
                            commandsToRemove.Add(key);
                        }
                    }
                }

                // PERFORMANCE: Batch removal to reduce dictionary operations
                foreach (var commandId in commandsToRemove)
                {
                    if (m_activeCommands.TryRemove(commandId, out var command))
                    {
                        command.CancellationTokenSource.Dispose();
                        m_logger.LogDebug("Cleaned up old completed command: {CommandId}", commandId);
                    }
                }

                if (commandsToRemove.Count > 0)
                {
                    m_logger.LogDebug("Cleaned up {Count} old completed commands", commandsToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during command cleanup");
            }
        }

        // IMPROVED: Add concurrency statistics logging
        private void LogConcurrencyStats()
        {
            var now = DateTime.UtcNow;
            if ((now - m_lastStatsLog) >= ApplicationConstants.StatsLogInterval)
            {
                var processed = Interlocked.Read(ref m_commandsProcessed);
                var failed = Interlocked.Read(ref m_commandsFailed);
                var cancelled = Interlocked.Read(ref m_commandsCancelled);
                var total = processed + failed + cancelled;

                if (total > 0)
                {
                    var successRate = total > 0 ? (double)processed / total * 100 : 0;
                    m_logger.LogInformation("Resilient Concurrency Stats - Processed: {Processed}, Failed: {Failed}, Cancelled: {Cancelled}, Success Rate: {SuccessRate:F1}%",
                        processed, failed, cancelled, successRate);
                }

                m_lastStatsLog = now;
            }
        }

        public CommandState? GetCommandState(string commandId)
        {
            if (string.IsNullOrEmpty(commandId))
                return null;

            if (!m_activeCommands.TryGetValue(commandId, out var command))
                return null;

            return command.State;
        }

        public CommandInfo? GetCommandInfo(string commandId)
        {
            if (string.IsNullOrEmpty(commandId))
                return null;

            if (!m_activeCommands.TryGetValue(commandId, out var command))
                return null;

            var elapsed = DateTime.UtcNow - command.QueueTime;
            var remaining = m_defaultCommandTimeout - elapsed;
            var queuePosition = GetQueuePosition(commandId);

            return new CommandInfo
            {
                CommandId = commandId,
                Command = command.Command,
                State = command.State,
                QueueTime = command.QueueTime,
                Elapsed = elapsed,
                Remaining = remaining,
                QueuePosition = queuePosition,
                IsCompleted = command.CompletionSource.Task.IsCompleted
            };
        }

        private int GetQueuePosition(string commandId)
        {
            if (string.IsNullOrEmpty(commandId))
                return -1;

            // If there's a current command executing, all queued commands are behind it
            lock (m_currentCommandLock)
            {
                if (m_currentCommand != null)
                {
                    // Count commands in queue + 1 for the current command
                    var queueCount = m_commandQueue.Count;
                    return queueCount + 1; // +1 because current command is executing
                }
            }

            // No current command, so count how many commands are ahead in the queue
            var position = 0;
            var queueArray = m_commandQueue.ToArray();

            for (int i = 0; i < queueArray.Length; i++)
            {
                if (queueArray[i].Id == commandId)
                {
                    return position;
                }
                position++;
            }

            // Command not found in queue
            return -1;
        }

        public void Dispose()
        {
            if (m_disposed) return;

            m_logger.LogInformation("Disposing ResilientCommandQueueService");

            // Cancel all commands and cleanup BEFORE setting disposed flag
            try
            {
                CancelAllCommands("Service shutdown");
                m_disposed = true;
                m_serviceCts.Cancel();
                m_cleanupTimer?.Dispose();

                if (!m_processingTask.Wait(ApplicationConstants.ServiceShutdownTimeout))
                {
                    m_logger.LogWarning("Processing task did not complete within {TimeoutSeconds} seconds during shutdown", ApplicationConstants.ServiceShutdownTimeout.TotalSeconds);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during disposal");
            }

            m_activeCommands.Clear();

            try { m_serviceCts.Dispose(); } catch (ObjectDisposedException) { }
            try { m_queueSemaphore.Dispose(); } catch (ObjectDisposedException) { }

            m_logger.LogDebug("ResilientCommandQueueService disposed");
        }
    }
}

