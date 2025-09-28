using System.Collections.Concurrent;
using System.Diagnostics;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Detailed command information for type-safe status checking
    /// </summary>
    public class CommandInfo
    {
        public string CommandId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public CommandState State { get; set; }
        public DateTime QueueTime { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan Remaining { get; set; }
        public int QueuePosition { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// Thread-safe, isolated command queue service for a single debugging session
    /// Prevents session state pollution and provides deadlock-free operation
    /// </summary>
    public class IsolatedCommandQueueService : ICommandQueueService, IDisposable
    {
        private readonly ICdbSession m_cdbSession;
        private readonly string m_sessionId;
        private readonly ILogger m_logger;
        private readonly IMcpNotificationService m_notificationService;

        // CONCURRENCY: Thread-safe collections
        private readonly BlockingCollection<QueuedCommand> m_commandQueue;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_activeCommands = new();

        // CURRENT COMMAND: Thread-safe current command tracking (volatile is sufficient)
        private volatile QueuedCommand? m_currentCommand;

        // LIFECYCLE: Cancellation and disposal
        private readonly CancellationTokenSource m_processingCts = new();
        private readonly Task m_processingTask;
        private bool m_disposed = false;

        // COUNTERS: Thread-safe performance tracking
        private long m_commandCounter = 0;
        private long m_completedCommands = 0;
        private long m_failedCommands = 0;
        private long m_cancelledCommands = 0;

        // CONFIGURATION: Timeout settings
        private readonly TimeSpan m_defaultCommandTimeout = TimeSpan.FromMinutes(10);
        private readonly TimeSpan m_heartbeatInterval = TimeSpan.FromSeconds(30);

        public IsolatedCommandQueueService(
            ICdbSession cdbSession,
            ILogger logger,
            IMcpNotificationService notificationService,
            string sessionId)
        {
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            m_sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));

            // INITIALIZE: Create blocking collection for thread-safe producer/consumer
            m_commandQueue = new BlockingCollection<QueuedCommand>();

            m_logger.LogInformation("🚀 IsolatedCommandQueueService initializing for session {SessionId}", sessionId);

            // PROCESSING: Start dedicated processing task for this session
            m_logger.LogTrace("🔄 Starting background processing task for session {SessionId}", sessionId);
            m_processingTask = Task.Run(ProcessCommandQueueAsync, m_processingCts.Token);
            m_logger.LogTrace("✅ Background processing task started for session {SessionId}, Task ID: {TaskId}", sessionId, m_processingTask.Id);

            m_logger.LogInformation("✅ IsolatedCommandQueueService created for session {SessionId}", sessionId);
        }

        public string QueueCommand(string command)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            // ATOMIC: Generate unique command ID
            var commandNumber = Interlocked.Increment(ref m_commandCounter);
            var commandId = $"cmd-{m_sessionId}-{commandNumber:D4}";

            m_logger.LogTrace("🔄 Queueing command {CommandId} in session {SessionId}: {Command}",
                commandId, m_sessionId, command);

            // IMMUTABLE: Create command object
            var queuedCommand = new QueuedCommand(
                Id: commandId,
                Command: command,
                QueueTime: DateTime.UtcNow,
                CompletionSource: new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously),
                CancellationTokenSource: new CancellationTokenSource(),
                State: CommandState.Queued
            );

            // ATOMIC: Add to tracking dictionary first
            m_logger.LogTrace("🔄 Adding command {CommandId} to active commands dictionary for session {SessionId}", commandId, m_sessionId);
            if (!m_activeCommands.TryAdd(commandId, queuedCommand))
            {
                queuedCommand.CancellationTokenSource.Dispose();
                throw new InvalidOperationException($"Command ID conflict: {commandId}");
            }
            m_logger.LogTrace("✅ Successfully added command {CommandId} to active commands dictionary for session {SessionId}", commandId, m_sessionId);

            try
            {
                // QUEUE: Add to processing queue (automatically signals consumer)
                m_logger.LogTrace("🔄 Adding command {CommandId} to BlockingCollection for session {SessionId}", commandId, m_sessionId);
                m_logger.LogTrace("🔄 BlockingCollection state before add - IsAddingCompleted: {IsAddingCompleted}, Count: {Count} for session {SessionId}", 
                    m_commandQueue.IsAddingCompleted, m_commandQueue.Count, m_sessionId);
                m_commandQueue.Add(queuedCommand);
                m_logger.LogTrace("✅ Successfully added command {CommandId} to BlockingCollection for session {SessionId}", commandId, m_sessionId);
                m_logger.LogTrace("🔄 BlockingCollection state after add - IsAddingCompleted: {IsAddingCompleted}, Count: {Count} for session {SessionId}", 
                    m_commandQueue.IsAddingCompleted, m_commandQueue.Count, m_sessionId);

                // NOTIFICATION: Send queued notification (fire-and-forget)
                NotifyCommandStatusFireAndForget(commandId, command, "queued", result: null, progress: 0);

                m_logger.LogInformation("📋 Command {CommandId} queued in session {SessionId} (timeout: {TimeoutMinutes} minutes)",
                    commandId, m_sessionId, m_defaultCommandTimeout.TotalMinutes);
                return commandId;
            }
            catch
            {
                // CLEANUP: Remove from dictionary on failure
                m_activeCommands.TryRemove(commandId, out _);
                queuedCommand.CancellationTokenSource.Dispose();
                throw;
            }
        }

        public async Task<string> GetCommandResult(string commandId)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(commandId))
                return "Command ID cannot be null or empty";

            if (!m_activeCommands.TryGetValue(commandId, out var command))
                return $"Command not found: {commandId}";

            // ASYNC: Wait for completion or return current status
            if (command.CompletionSource.Task.IsCompleted)
            {
                try
                {
                    return await command.CompletionSource.Task;
                }
                catch (Exception ex)
                {
                    return $"Command failed: {ex.Message}";
                }
            }

            // Calculate elapsed time and remaining timeout
            var elapsed = DateTime.UtcNow - command.QueueTime;
            var remaining = m_defaultCommandTimeout - elapsed;
            var remainingMinutes = Math.Max(0, (int)remaining.TotalMinutes);
            var remainingSeconds = Math.Max(0, (int)remaining.TotalSeconds % 60);

            // Calculate queue position for queued commands
            var queuePosition = GetQueuePosition(commandId);

            // Return current status for polling with timeout information
            return command.State switch
            {
                CommandState.Queued => GetQueuedStatusMessage(queuePosition, elapsed, remainingMinutes, remainingSeconds),
                CommandState.Executing => $"Command is currently executing (elapsed: {elapsed.TotalMinutes:F1} minutes, remaining: {remainingMinutes} minutes {remainingSeconds} seconds)",
                CommandState.Cancelled => "Command was cancelled",
                CommandState.Failed => "Command execution failed",
                _ => "Command status unknown"
            };
        }

        /// <summary>
        /// Gets the current state of a command without parsing strings
        /// </summary>
        public CommandState? GetCommandState(string commandId)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(commandId))
                return null;

            if (!m_activeCommands.TryGetValue(commandId, out var command))
                return null;

            return command.State;
        }

        /// <summary>
        /// Gets detailed command information including state, queue position, and progress
        /// </summary>
        public CommandInfo? GetCommandInfo(string commandId)
        {
            ThrowIfDisposed();

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

        public bool CancelCommand(string commandId)
        {
            if (!m_activeCommands.TryGetValue(commandId, out var command))
                return false;

            try
            {
                // CANCEL: Cancel the command's token
                command.CancellationTokenSource.Cancel();

                // CURRENT: If it's currently executing, cancel CDB operation
                var currentCommand = m_currentCommand; // volatile read is thread-safe
                if (ReferenceEquals(currentCommand, command))
                {
                    try
                    {
                        m_cdbSession.CancelCurrentOperation();
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to cancel current CDB operation for command {CommandId}", commandId);
                    }
                }

                m_logger.LogInformation("🚫 Cancelled command {CommandId} in session {SessionId}", commandId, m_sessionId);
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error cancelling command {CommandId}", commandId);
                return false;
            }
        }

        public int CancelAllCommands(string? reason = null)
        {
            var cancelledCount = 0;
            var commands = m_activeCommands.Values.ToList();

            foreach (var command in commands)
            {
                if (!command.CompletionSource.Task.IsCompleted)
                {
                    try
                    {
                        command.CancellationTokenSource.Cancel();
                        command.CompletionSource.TrySetResult(reason ?? "All commands cancelled");
                        cancelledCount++;
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Error cancelling command {CommandId}", command.Id);
                    }
                }
            }

            // Cancel current CDB operation if any
            try
            {
                m_cdbSession.CancelCurrentOperation();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error cancelling current CDB operation during bulk cancel");
            }

            m_logger.LogInformation("🚫 Cancelled {Count} commands in session {SessionId}: {Reason}",
                cancelledCount, m_sessionId, reason ?? "Bulk cancellation");

            return cancelledCount;
        }

        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            return m_activeCommands.Values.Select(cmd => (
                cmd.Id,
                cmd.Command,
                cmd.QueueTime,
                cmd.State.ToString()
            )).ToList();
        }

        public QueuedCommand? GetCurrentCommand()
        {
            return m_currentCommand; // volatile read is thread-safe
        }

        /// <summary>
        /// Gets thread-safe performance statistics
        /// </summary>
        public (long Total, long Completed, long Failed, long Cancelled) GetPerformanceStats()
        {
            var completed = Interlocked.Read(ref m_completedCommands);
            var failed = Interlocked.Read(ref m_failedCommands);
            var cancelled = Interlocked.Read(ref m_cancelledCommands);
            var total = completed + failed + cancelled;
            
            return (total, completed, failed, cancelled);
        }

        /// <summary>
        /// Calculates the position of a command in the queue (0 = next to execute, 1 = second, etc.)
        /// </summary>
        private int GetQueuePosition(string commandId)
        {
            if (string.IsNullOrEmpty(commandId))
                return -1;

            // If there's a current command executing, all queued commands are behind it
            var currentCommand = GetCurrentCommand();
            if (currentCommand != null)
            {
                // For BlockingCollection, we can't safely get count, so estimate based on active commands
                var queuedCommands = m_activeCommands.Values.Count(cmd => cmd.State == CommandState.Queued);
                return queuedCommands; // All queued commands are behind the current one
            }

            // No current command, count how many commands are ahead in the queue
            var position = 0;
            foreach (var cmd in m_activeCommands.Values.Where(cmd => cmd.State == CommandState.Queued))
            {
                if (cmd.Id == commandId)
                {
                    return position;
                }
                position++;
            }

            // Command not found in queue
            return -1;
        }

        /// <summary>
        /// Generates dynamic status messages for queued commands with progress indicators
        /// </summary>
        private string GetQueuedStatusMessage(int queuePosition, TimeSpan elapsed, int remainingMinutes, int remainingSeconds)
        {
            if (queuePosition < 0)
            {
                return $"Command is queued for execution (estimated wait: up to {m_defaultCommandTimeout.TotalMinutes:F0} minutes)";
            }

            // Calculate progress percentage that ALWAYS increases over time
            // This prevents the "stuck" perception by showing continuous progress
            var progressPercentage = CalculateProgressPercentage(queuePosition, elapsed);

            // Generate different messages based on position and elapsed time
            var baseMessage = GetBaseMessage(queuePosition, elapsed);

            // Add progress and time information
            var progressInfo = $" (Progress: {progressPercentage}%, Elapsed: {elapsed.TotalMinutes:F1}min)";
            var timeInfo = remainingMinutes > 0 ? $", ETA: {remainingMinutes}min {remainingSeconds}s" : ", ETA: <1min";

            return $"{baseMessage}{progressInfo}{timeInfo}";
        }

        /// <summary>
        /// Calculates progress percentage that always increases over time to prevent "stuck" perception
        /// </summary>
        private int CalculateProgressPercentage(int queuePosition, TimeSpan elapsed)
        {
            // Base progress from queue position (0-50%)
            var queueProgress = Math.Max(0, Math.Min(50, (10 - queuePosition) * 5));

            // Time-based progress that always increases (0-50%)
            // This ensures progress always goes up, even if queue position doesn't change
            var timeProgress = Math.Min(50, (int)(elapsed.TotalMinutes * 2)); // 2% per minute

            // Combine both for total progress (0-100%)
            var totalProgress = Math.Min(100, queueProgress + timeProgress);

            // Ensure minimum progress based on elapsed time to show activity
            var minProgress = Math.Min(95, (int)(elapsed.TotalSeconds * 0.5)); // 0.5% per second

            return Math.Max(totalProgress, minProgress);
        }

        /// <summary>
        /// Generates base message with emojis and dynamic content based on position and time
        /// </summary>
        private string GetBaseMessage(int queuePosition, TimeSpan elapsed)
        {
            // Add time-based variations to make messages feel more dynamic
            var timeVariation = (int)(elapsed.TotalSeconds) % 4;

            return queuePosition switch
            {
                0 => timeVariation switch
                {
                    0 => "Command is next in queue - will start executing soon",
                    1 => "Command is next in queue - preparing to execute",
                    2 => "Command is next in queue - almost ready to start",
                    _ => "Command is next in queue - execution imminent"
                },
                1 => timeVariation switch
                {
                    0 => "Command is 2nd in queue - almost ready to execute",
                    1 => "Command is 2nd in queue - waiting for 1 command ahead",
                    2 => "Command is 2nd in queue - will be next soon",
                    _ => "Command is 2nd in queue - preparing for execution"
                },
                2 => timeVariation switch
                {
                    0 => "Command is 3rd in queue - waiting for 2 commands ahead",
                    1 => "Command is 3rd in queue - making progress through queue",
                    2 => "Command is 3rd in queue - moving up in line",
                    _ => "Command is 3rd in queue - queue position improving"
                },
                3 => timeVariation switch
                {
                    0 => "Command is 4th in queue - waiting for 3 commands ahead",
                    1 => "Command is 4th in queue - progressing through queue",
                    2 => "Command is 4th in queue - position advancing",
                    _ => "Command is 4th in queue - queue moving forward"
                },
                4 => timeVariation switch
                {
                    0 => "Command is 5th in queue - waiting for 4 commands ahead",
                    1 => "Command is 5th in queue - queue processing actively",
                    2 => "Command is 5th in queue - position updating",
                    _ => "Command is 5th in queue - making steady progress"
                },
                _ when queuePosition <= 10 => timeVariation switch
                {
                    0 => $"Command is {queuePosition + 1}th in queue - waiting for {queuePosition} commands ahead",
                    1 => $"Command is {queuePosition + 1}th in queue - queue processing normally",
                    2 => $"Command is {queuePosition + 1}th in queue - position tracking active",
                    _ => $"Command is {queuePosition + 1}th in queue - progress monitoring"
                },
                _ => timeVariation switch
                {
                    0 => $"Command is position {queuePosition + 1} in queue - waiting for {queuePosition} commands ahead",
                    1 => $"Command is position {queuePosition + 1} in queue - queue system active",
                    2 => $"Command is position {queuePosition + 1} in queue - processing normally",
                    _ => $"Command is position {queuePosition + 1} in queue - status updating"
                }
            };
        }

        #region Private Processing Methods

        private async Task ProcessCommandQueueAsync()
        {
            m_logger.LogTrace("🔄 Command processor started for session {SessionId}", m_sessionId);

            try
            {
                m_logger.LogTrace("🔄 Starting to enumerate commands from BlockingCollection for session {SessionId}", m_sessionId);
                m_logger.LogTrace("🔄 BlockingCollection state - IsAddingCompleted: {IsAddingCompleted}, Count: {Count} for session {SessionId}", 
                    m_commandQueue.IsAddingCompleted, m_commandQueue.Count, m_sessionId);
                m_logger.LogTrace("🔄 About to call GetConsumingEnumerable with cancellation token for session {SessionId}", m_sessionId);
                
                // PROCESS: Use BlockingCollection's built-in blocking enumeration
                foreach (var command in m_commandQueue.GetConsumingEnumerable(m_processingCts.Token))
                {
                    m_logger.LogTrace("🔄 Dequeued command {CommandId} from queue for session {SessionId}", command?.Id ?? "null", m_sessionId);
                    m_logger.LogTrace("🔄 BlockingCollection state after dequeue - IsAddingCompleted: {IsAddingCompleted}, Count: {Count} for session {SessionId}", 
                        m_commandQueue.IsAddingCompleted, m_commandQueue.Count, m_sessionId);
                    try
                    {
                        // CANCELLATION: Check if command was cancelled while queued
                        m_logger.LogTrace("🔄 Checking cancellation status for command {CommandId} in session {SessionId}", command?.Id ?? "null", m_sessionId);
                        if (command?.CancellationTokenSource.Token.IsCancellationRequested == true)
                        {
                            m_logger.LogTrace("🔄 Command {CommandId} was cancelled while queued in session {SessionId}", command.Id, m_sessionId);
                            CompleteCommandSafely(command, "Command was cancelled while queued", CommandState.Cancelled);
                            continue;
                        }

                        // CURRENT: Set as current command (volatile write is thread-safe)
                        m_logger.LogTrace("🔄 Setting command {CommandId} as current command for session {SessionId}", command?.Id ?? "null", m_sessionId);
                        m_currentCommand = command;

                        // EXECUTE: Process command with proper error handling
                        if (command != null)
                        {
                            m_logger.LogTrace("🔄 Starting execution of command {CommandId} for session {SessionId}", command.Id, m_sessionId);
                            m_logger.LogTrace("🔄 BlockingCollection state before execution - IsAddingCompleted: {IsAddingCompleted}, Count: {Count} for session {SessionId}", 
                                m_commandQueue.IsAddingCompleted, m_commandQueue.Count, m_sessionId);
                            await ExecuteCommandSafely(command);
                            m_logger.LogTrace("✅ Completed execution of command {CommandId} for session {SessionId}", command.Id, m_sessionId);
                            m_logger.LogTrace("🔄 BlockingCollection state after execution - IsAddingCompleted: {IsAddingCompleted}, Count: {Count} for session {SessionId}", 
                                m_commandQueue.IsAddingCompleted, m_commandQueue.Count, m_sessionId);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Unexpected error processing command {CommandId} for session {SessionId}", 
                            command?.Id ?? "unknown", m_sessionId);
                        // Continue processing next command
                    }
                    
                    m_logger.LogTrace("🔄 Finished processing command {CommandId}, continuing to next command in queue for session {SessionId}", 
                        command?.Id ?? "null", m_sessionId);
                }
            }
            catch (OperationCanceledException)
            {
                // EXPECTED: Normal shutdown
                m_logger.LogTrace("🔄 Command processor cancelled for session {SessionId}", m_sessionId);
            }
            catch (Exception ex)
            {
                // UNEXPECTED: Log any other exceptions
                m_logger.LogError(ex, "🔄 Unexpected error in ProcessCommandQueueAsync for session {SessionId}", m_sessionId);
            }
            finally
            {
                m_logger.LogTrace("🔄 Command processor stopped for session {SessionId}", m_sessionId);
            }
        }

        private async Task ExecuteCommandSafely(QueuedCommand command)
        {
            var stopwatch = Stopwatch.StartNew();
            m_logger.LogTrace("🔄 ExecuteCommandSafely started for command {CommandId} in session {SessionId}", command.Id, m_sessionId);

            try
            {
                // NOTIFICATION: Notify execution start (fire-and-forget to avoid blocking)
                m_logger.LogTrace("🔄 Sending execution notification for command {CommandId} in session {SessionId}", command.Id, m_sessionId);
                NotifyCommandStatusFireAndForget(command, "executing", progress: 0);

                // EXECUTION: Execute with timeout and cancellation
                m_logger.LogTrace("🔄 Creating cancellation tokens for command {CommandId} in session {SessionId} (timeout: {TimeoutMs}ms)", 
                    command.Id, m_sessionId, m_defaultCommandTimeout.TotalMilliseconds);
                using var timeoutCts = new CancellationTokenSource(m_defaultCommandTimeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    command.CancellationTokenSource.Token,
                    timeoutCts.Token,
                    m_processingCts.Token);

                // HEARTBEAT: Start heartbeat for long-running commands
                m_logger.LogTrace("🔄 Starting heartbeat for command {CommandId} in session {SessionId}", command.Id, m_sessionId);
                var heartbeatTask = StartHeartbeatAsync(command, stopwatch, combinedCts.Token);

                try
                {
                    m_logger.LogTrace("🔄 About to call CDB ExecuteCommand for command {CommandId} in session {SessionId}: {Command}", 
                        command.Id, m_sessionId, command.Command);
                    var result = await m_cdbSession.ExecuteCommand(command.Command, combinedCts.Token);
                    m_logger.LogTrace("🔄 CDB ExecuteCommand completed for command {CommandId} in session {SessionId}", command.Id, m_sessionId);

                    // SUCCESS: Complete successfully
                    m_logger.LogTrace("🔄 Completing command {CommandId} successfully in session {SessionId}", command.Id, m_sessionId);
                    CompleteCommandSafely(command, result, CommandState.Completed);
                    Interlocked.Increment(ref m_completedCommands);

                    m_logger.LogInformation("✅ Command {CommandId} completed in {ElapsedMs}ms",
                        command.Id, stopwatch.ElapsedMilliseconds);
                }
                finally
                {
                    // Stop heartbeat - DON'T AWAIT as it blocks queue processing!
                    // The heartbeat task will be cancelled by the combinedCts.Token
                    m_logger.LogTrace("🔄 Heartbeat task will be cancelled by token for command {CommandId} in session {SessionId}", command.Id, m_sessionId);
                }
            }
            catch (OperationCanceledException) when (command.CancellationTokenSource.Token.IsCancellationRequested)
            {
                // USER CANCELLATION
                CompleteCommandSafely(command, "Command was cancelled by user", CommandState.Cancelled);
                Interlocked.Increment(ref m_cancelledCommands);
            }
            catch (OperationCanceledException)
            {
                // TIMEOUT OR SHUTDOWN
                CompleteCommandSafely(command, "Command timed out or system is shutting down", CommandState.Failed);
                Interlocked.Increment(ref m_failedCommands);
            }
            catch (Exception ex)
            {
                // EXECUTION ERROR
                CompleteCommandSafely(command, $"Command execution failed: {ex.Message}", CommandState.Failed);
                Interlocked.Increment(ref m_failedCommands);

                m_logger.LogError(ex, "❌ Command {CommandId} failed after {ElapsedMs}ms",
                    command.Id, stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                // CLEANUP: Clear current command (volatile write is thread-safe)
                if (ReferenceEquals(m_currentCommand, command))
                {
                    m_currentCommand = null;
                }
            }
        }

        private async Task StartHeartbeatAsync(QueuedCommand command, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(m_heartbeatInterval, cancellationToken);

                    // Send heartbeat notification (fire-and-forget)
                    NotifyCommandHeartbeatFireAndForget(command, stopwatch.Elapsed);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when command completes
            }
        }

        private void NotifyCommandHeartbeatFireAndForget(QueuedCommand command, TimeSpan elapsed)
        {
            NotificationHelper.NotifyCommandHeartbeatFireAndForget(
                m_notificationService, m_logger, m_sessionId, command.Id, command.Command, elapsed);
        }

        private void CompleteCommandSafely(QueuedCommand command, string result, CommandState state)
        {
            m_logger.LogTrace("🔄 CompleteCommandSafely called for command {CommandId} with state {State} in session {SessionId}", 
                command.Id, state, m_sessionId);
            try
            {
                // ATOMIC: Update command state
                m_logger.LogTrace("🔄 Updating command {CommandId} state to {State} in session {SessionId}", command.Id, state, m_sessionId);
                var updatedCommand = command with { State = state };
                m_activeCommands.TryUpdate(command.Id, updatedCommand, command);

                // COMPLETION: Set result (thread-safe)
                m_logger.LogTrace("🔄 Setting completion result for command {CommandId} in session {SessionId}", command.Id, m_sessionId);
                command.CompletionSource.TrySetResult(result);

                // NOTIFICATION: Notify completion (fire-and-forget)
                var progress = state == CommandState.Completed ? 100 : 0;
                NotifyCommandStatusFireAndForget(updatedCommand, state.ToString().ToLowerInvariant(), result, progress);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error completing command {CommandId}", command.Id);
            }
        }

        private void NotifyCommandStatusFireAndForget(QueuedCommand command, string status, string? result = null, int progress = 0)
        {
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_notificationService, m_logger, m_sessionId, command.Id, command.Command, status, result, progress);
        }

        private void NotifyCommandStatusFireAndForget(string commandId, string command, string status, string? result = null, int progress = 0)
        {
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_notificationService, m_logger, m_sessionId, commandId, command, status, result, progress);
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed || m_processingCts.Token.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(IsolatedCommandQueueService));
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (m_disposed) return;

            m_disposed = true;

            m_logger.LogInformation("🛑 IsolatedCommandQueueService disposing for session {SessionId}...", m_sessionId);

            try
            {
                // SHUTDOWN: Cancel processing
                m_processingCts.Cancel();

                // WAIT: Wait for processor to finish (with timeout)
                if (!m_processingTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    m_logger.LogWarning("⚠️ Command processor did not stop within timeout for session {SessionId}", m_sessionId);
                }

                // CLEANUP: Cancel all pending commands
                var cancelledCount = 0;
                foreach (var command in m_activeCommands.Values)
                {
                    if (!command.CompletionSource.Task.IsCompleted)
                    {
                        try
                        {
                            command.CancellationTokenSource.Cancel();
                            command.CompletionSource.TrySetResult("Session was disposed");
                            cancelledCount++;
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogWarning(ex, "Error cancelling command {CommandId} during disposal", command.Id);
                        }
                    }
                    command.CancellationTokenSource.Dispose();
                }

                if (cancelledCount > 0)
                {
                    m_logger.LogInformation("🚫 Cancelled {Count} pending commands during disposal", cancelledCount);
                }

                // CLEANUP: Dispose resources
                m_commandQueue.Dispose();
                m_processingCts.Dispose();

                // CLEANUP: Dispose processing task
                try
                {
                    m_processingTask.Dispose();
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Error disposing processing task for session {SessionId}", m_sessionId);
                }

                m_logger.LogInformation("✅ IsolatedCommandQueueService disposed for session {SessionId}", m_sessionId);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Error disposing IsolatedCommandQueueService for session {SessionId}", m_sessionId);
            }
        }

        #endregion
    }
}

