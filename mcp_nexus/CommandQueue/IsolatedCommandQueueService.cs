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
        private readonly ConcurrentQueue<QueuedCommand> m_commandQueue = new();
        private readonly ConcurrentDictionary<string, QueuedCommand> m_activeCommands = new();
        
        // SYNCHRONIZATION: Semaphore for queue processing
        private readonly SemaphoreSlim m_queueSemaphore = new(0, int.MaxValue);
        
        // CURRENT COMMAND: Thread-safe current command tracking
        private volatile QueuedCommand? m_currentCommand;
        private readonly object m_currentCommandLock = new();
        
        // LIFECYCLE: Cancellation and disposal
        private readonly CancellationTokenSource m_processingCts = new();
        private readonly Task m_processingTask;
        private volatile bool m_disposed = false;
        
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

            m_logger.LogInformation("🚀 IsolatedCommandQueueService initializing for session {SessionId}", sessionId);

            // PROCESSING: Start dedicated processing task for this session
            m_processingTask = Task.Run(ProcessCommandQueueAsync, m_processingCts.Token);

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

            m_logger.LogDebug("🔄 Queueing command {CommandId} in session {SessionId}: {Command}", 
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
            if (!m_activeCommands.TryAdd(commandId, queuedCommand))
            {
                queuedCommand.CancellationTokenSource.Dispose();
                throw new InvalidOperationException($"Command ID conflict: {commandId}");
            }

            try
            {
                // QUEUE: Add to processing queue
                m_commandQueue.Enqueue(queuedCommand);

                // SIGNAL: Release semaphore to wake up processor
                m_queueSemaphore.Release();

                // NOTIFICATION: Send queued notification (async)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService.NotifyCommandStatusAsync(
                            m_sessionId, commandId, command, "queued", 
                            result: null, progress: 0);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send queued notification for command {CommandId}", commandId);
                    }
                }, CancellationToken.None);

                m_logger.LogInformation("📋 Command {CommandId} queued in session {SessionId}", commandId, m_sessionId);
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

            // Return current status for polling
            return command.State switch
            {
                CommandState.Queued => "Command is still queued for execution",
                CommandState.Executing => "Command is currently executing",
                CommandState.Cancelled => "Command was cancelled",
                CommandState.Failed => "Command execution failed",
                _ => "Command status unknown"
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
                lock (m_currentCommandLock)
                {
                    if (ReferenceEquals(m_currentCommand, command))
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
            lock (m_currentCommandLock)
            {
                return m_currentCommand;
            }
        }

        #region Private Processing Methods

        private async Task ProcessCommandQueueAsync()
        {
            m_logger.LogDebug("🔄 Command processor started for session {SessionId}", m_sessionId);

            try
            {
                while (!m_processingCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // WAIT: Wait for command to be available
                        await m_queueSemaphore.WaitAsync(m_processingCts.Token);

                        // DEQUEUE: Get next command
                        if (!m_commandQueue.TryDequeue(out var command))
                            continue;

                        // CANCELLATION: Check if command was cancelled while queued
                        if (command.CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            CompleteCommandSafely(command, "Command was cancelled while queued", CommandState.Cancelled);
                            continue;
                        }

                        // CURRENT: Set as current command atomically
                        lock (m_currentCommandLock)
                        {
                            m_currentCommand = command;
                        }

                        // EXECUTE: Process command with proper error handling
                        await ExecuteCommandSafely(command);
                    }
                    catch (OperationCanceledException)
                    {
                        // EXPECTED: Normal shutdown
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Unexpected error in command processor for session {SessionId}", m_sessionId);
                        // Continue processing other commands
                    }
                }
            }
            finally
            {
                m_logger.LogDebug("🔄 Command processor stopped for session {SessionId}", m_sessionId);
            }
        }

        private async Task ExecuteCommandSafely(QueuedCommand command)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // NOTIFICATION: Notify execution start
                await NotifyCommandStatus(command, "executing", progress: 0);

                // EXECUTION: Execute with timeout and cancellation
                using var timeoutCts = new CancellationTokenSource(m_defaultCommandTimeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    command.CancellationTokenSource.Token,
                    timeoutCts.Token,
                    m_processingCts.Token);

                // HEARTBEAT: Start heartbeat for long-running commands
                var heartbeatTask = StartHeartbeatAsync(command, stopwatch, combinedCts.Token);

                try
                {
                    var result = await m_cdbSession.ExecuteCommand(command.Command, combinedCts.Token);

                    // SUCCESS: Complete successfully
                    CompleteCommandSafely(command, result, CommandState.Completed);
                    Interlocked.Increment(ref m_completedCommands);

                    m_logger.LogInformation("✅ Command {CommandId} completed in {ElapsedMs}ms", 
                        command.Id, stopwatch.ElapsedMilliseconds);
                }
                finally
                {
                    // Stop heartbeat
                    try { await heartbeatTask; } catch { /* Ignore heartbeat errors */ }
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
                // CLEANUP: Clear current command
                lock (m_currentCommandLock)
                {
                    if (ReferenceEquals(m_currentCommand, command))
                    {
                        m_currentCommand = null;
                    }
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

                    // Send heartbeat notification
                    try
                    {
                        await m_notificationService.NotifyCommandHeartbeatAsync(
                            m_sessionId, command.Id, command.Command, stopwatch.Elapsed);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send heartbeat for command {CommandId}", command.Id);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when command completes
            }
        }

        private void CompleteCommandSafely(QueuedCommand command, string result, CommandState state)
        {
            try
            {
                // ATOMIC: Update command state
                var updatedCommand = command with { State = state };
                m_activeCommands.TryUpdate(command.Id, updatedCommand, command);

                // COMPLETION: Set result (thread-safe)
                command.CompletionSource.TrySetResult(result);

                // NOTIFICATION: Notify completion (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var progress = state == CommandState.Completed ? 100 : 0;
                        await NotifyCommandStatus(updatedCommand, state.ToString().ToLowerInvariant(), result, progress);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send completion notification for command {CommandId}", command.Id);
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error completing command {CommandId}", command.Id);
            }
        }

        private async Task NotifyCommandStatus(QueuedCommand command, string status, string? result = null, int progress = 0)
        {
            try
            {
                await m_notificationService.NotifyCommandStatusAsync(
                    m_sessionId, command.Id, command.Command, status, result, progress);
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Failed to send status notification for command {CommandId}", command.Id);
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
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
                m_queueSemaphore.Dispose();
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

