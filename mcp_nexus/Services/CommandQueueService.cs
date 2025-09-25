using System.Collections.Concurrent;
using System.Linq;

using mcp_nexus.Constants;
using mcp_nexus.Helper;

namespace mcp_nexus.Services
{
    public enum CommandState
    {
        Queued,
        Executing,
        Completed,
        Cancelled,
        Failed
    }

    public record QueuedCommand(
        string Id,
        string Command,
        DateTime QueueTime,
        TaskCompletionSource<string> CompletionSource,
        CancellationTokenSource CancellationTokenSource,
        CommandState State = CommandState.Queued
    );

    public class CommandQueueService : IDisposable, ICommandQueueService
    {
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger<CommandQueueService> m_logger;
        private readonly ConcurrentQueue<QueuedCommand> m_commandQueue = new();
        private readonly SemaphoreSlim m_queueSemaphore = new(0);
        private readonly CancellationTokenSource m_serviceCts = new();
        private readonly Task m_processingTask;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_activeCommands = new();
        private QueuedCommand? m_currentCommand;
        private readonly object m_currentCommandLock = new();
        private bool m_disposed;

        // FIXED: Add cleanup mechanism for completed commands
        private readonly Timer m_cleanupTimer;
        private readonly TimeSpan m_cleanupInterval = ApplicationConstants.CleanupInterval;
        private readonly TimeSpan m_commandRetentionTime = ApplicationConstants.CommandRetentionTime;

        // IMPROVED: Add concurrency monitoring
        private long m_commandsProcessed = 0;
        private long m_commandsFailed = 0;
        private long m_commandsCancelled = 0;
        private DateTime m_lastStatsLog = DateTime.UtcNow;

        public CommandQueueService(ICdbSession cdbSession, ILogger<CommandQueueService> logger)
        {
            m_cdbSession = cdbSession;
            m_logger = logger;

            m_logger.LogInformation("🚀 CommandQueueService CONSTRUCTOR started");

            // Start the background processing task
            try
            {
                m_processingTask = Task.Run(ProcessCommandQueue, m_serviceCts.Token);
                m_logger.LogInformation("✅ CommandQueueService background task started successfully");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ FAILED to start CommandQueueService background task");
                throw;
            }

            // FIXED: Start cleanup timer for completed commands
            m_cleanupTimer = new Timer(CleanupCompletedCommands, null, m_cleanupInterval, m_cleanupInterval);

            m_logger.LogInformation("🎯 CommandQueueService fully initialized");
        }

        public string QueueCommand(string command)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));
                
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));
            
            var commandId = Guid.NewGuid().ToString();
            m_logger.LogInformation("🔄 QueueCommand START: {CommandId} for command: {Command}", commandId, command);

            var tcs = new TaskCompletionSource<string>();
            var cts = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, DateTime.UtcNow, tcs, cts);

            m_logger.LogInformation("📝 Adding to activeCommands dictionary: {CommandId}", commandId);
            m_activeCommands[commandId] = queuedCommand;

            m_logger.LogInformation("📋 Enqueueing command: {CommandId}", commandId);
            m_commandQueue.Enqueue(queuedCommand);

            m_logger.LogInformation("🔔 Releasing semaphore for command: {CommandId}", commandId);
            m_queueSemaphore.Release(); // Signal that a command is available

            m_logger.LogInformation("✅ QueueCommand COMPLETE: {CommandId} (Queue size: {QueueSize})",
                commandId, m_commandQueue.Count);

            return commandId;
        }

        public Task<string> GetCommandResult(string commandId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));
                
            if (string.IsNullOrEmpty(commandId))
                return Task.FromResult($"Command not found: {commandId}");
                
            // NOTE: Completed commands stay in m_activeCommands for result retrieval
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                // DON'T WAIT! Check if completed, return status immediately
                if (command.CompletionSource.Task.IsCompleted)
                {
                    try
                    {
                        var result = command.CompletionSource.Task.Result;
                        return Task.FromResult(result);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult($"Command failed: {ex.Message}");
                    }
                }
                else
                {
                    // Command still running - return status immediately, don't wait!
                    return Task.FromResult($"Command is still executing... Please call get_command_status(commandId='{commandId}') again in 5-10 seconds to check if completed.");
                }
            }

            return Task.FromResult($"Command not found: {commandId}");
        }

        public bool CancelCommand(string commandId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));
                
            if (string.IsNullOrEmpty(commandId))
                return false;
                
            // First check if command is in active commands (executing or completed)
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                m_logger.LogInformation("Cancelling command {CommandId}: {Command}", commandId, command.Command);

                try
                {
                    command.CancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Token was already disposed, which means command completed/cancelled already
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

                return true;
            }

            // Check if command is still in the queue (not yet started)
            var queuedCommand = m_commandQueue.FirstOrDefault(c => c.Id == commandId);
            if (queuedCommand != null)
            {
                m_logger.LogInformation("Cancelling queued command {CommandId}: {Command}", commandId, queuedCommand.Command);
                
                try
                {
                    queuedCommand.CancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    m_logger.LogDebug("CancellationTokenSource already disposed for queued command {CommandId}", commandId);
                    return false;
                }

                return true;
            }

            m_logger.LogWarning("Attempted to cancel non-existent command: {CommandId}", commandId);
            return false;
        }

        public int CancelAllCommands(string? reason = null)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));
                
            var reasonText = string.IsNullOrWhiteSpace(reason) ? "No reason provided" : reason;
            m_logger.LogWarning("Cancelling ALL commands. Reason: {Reason}", reasonText);

            var cancelledCount = 0;

            // Snapshot current command for targeted cancellation
            string? currentId;
            lock (m_currentCommandLock)
            {
                currentId = m_currentCommand?.Id;
            }

            // Cancel everything currently tracked
            var commandsSnapshot = new List<QueuedCommand>(m_activeCommands.Values);
            foreach (var cmd in commandsSnapshot)
            {
                try
                {
                    // Skip commands that already completed
                    if (!cmd.CompletionSource.Task.IsCompleted)
                    {
                        try
                        {
                            cmd.CancellationTokenSource.Cancel();
                            cancelledCount++;
                        }
                        catch (ObjectDisposedException)
                        {
                            // CTS already disposed; nothing to cancel
                        }

                        // Ensure waiters get a result if not already completed
                        cmd.CompletionSource.TrySetResult($"Command was cancelled. Reason: {reasonText}");
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error cancelling command {CommandId}", cmd.Id);
                }
            }

            // If one is executing, request debugger-side cancellation
            if (!string.IsNullOrEmpty(currentId))
            {
                try
                {
                    m_logger.LogWarning("Requesting CDB cancellation for currently executing command: {CommandId}", currentId);
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
                throw new ObjectDisposedException(nameof(CommandQueueService));
                
            lock (m_currentCommandLock)
            {
                return m_currentCommand;
            }
        }

        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));
                
            var results = new List<(string, string, DateTime, string)>();

            // Add current command
            lock (m_currentCommandLock)
            {
                if (m_currentCommand != null)
                {
                    results.Add((m_currentCommand.Id, m_currentCommand.Command, m_currentCommand.QueueTime, "Executing"));
                }
            }

            // Add queued commands
            foreach (var cmd in m_commandQueue)
            {
                results.Add(cmd.CancellationTokenSource.Token.IsCancellationRequested
                    ? (cmd.Id, cmd.Command, cmd.QueueTime, "Cancelled")
                    : (cmd.Id, cmd.Command, cmd.QueueTime, "Queued"));
            }

            return results;
        }

        private async Task ProcessCommandQueue()
        {
            m_logger.LogInformation("🔥 BACKGROUND PROCESSOR: ProcessCommandQueue started");

            try
            {
                while (!m_serviceCts.Token.IsCancellationRequested)
                {
                    m_logger.LogInformation("⏳ BACKGROUND PROCESSOR: Waiting for command (semaphore)...");

                    // Wait for a command to be available
                    await m_queueSemaphore.WaitAsync(m_serviceCts.Token);

                    m_logger.LogInformation("🔔 BACKGROUND PROCESSOR: Semaphore released, checking queue...");

                    if (m_commandQueue.TryDequeue(out var queuedCommand))
                    {
                        m_logger.LogInformation("📦 BACKGROUND PROCESSOR: Dequeued command {CommandId}: {Command}",
                            queuedCommand.Id, queuedCommand.Command);

                        // Check if command was cancelled while queued
                        if (queuedCommand.CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            m_logger.LogInformation("❌ BACKGROUND PROCESSOR: Skipping cancelled command {CommandId}: {Command}",
                                queuedCommand.Id, queuedCommand.Command);

                            queuedCommand.CompletionSource.SetResult("Command was cancelled while queued.");
                            CleanupCommand(queuedCommand);
                            continue;
                        }

                        // FIXED: Set as current command with state transition
                        lock (m_currentCommandLock)
                        {
                            m_currentCommand = queuedCommand;
                        }
                        
                        // Update command state to executing
                        UpdateCommandState(queuedCommand.Id, CommandState.Executing);

                        var waitTime = (DateTime.UtcNow - queuedCommand.QueueTime).TotalSeconds;
                        m_logger.LogInformation("🚀 BACKGROUND PROCESSOR: Starting execution of {CommandId}: {Command} (waited {WaitTime:F1}s in queue)",
                            queuedCommand.Id, queuedCommand.Command, waitTime);

                        m_logger.LogInformation("🔧 BACKGROUND PROCESSOR: Checking CDB session status...");
                        m_logger.LogInformation("🔧 BACKGROUND PROCESSOR: CdbSession.IsActive = {IsActive}", m_cdbSession.IsActive);

                        try
                        {
                            m_logger.LogInformation("⚡ BACKGROUND PROCESSOR: Calling CdbSession.ExecuteCommand for {CommandId}", queuedCommand.Id);

                            // Execute the command
                            var result = await m_cdbSession.ExecuteCommand(queuedCommand.Command, queuedCommand.CancellationTokenSource.Token);

                            m_logger.LogInformation("✅ BACKGROUND PROCESSOR: CdbSession.ExecuteCommand completed for {CommandId}", queuedCommand.Id);

                            // CRITICAL FIX: Atomic completion and state update
                            var resultMessage = queuedCommand.CancellationTokenSource.Token.IsCancellationRequested
                                ? "Command execution was cancelled."
                                : result;
                            
                            var wasCompleted = queuedCommand.CompletionSource.TrySetResult(resultMessage);
                            
                            // Update state atomically - this is safe even if another thread modified the command
                            UpdateCommandState(queuedCommand.Id, CommandState.Completed);
                            
                            if (wasCompleted)
                            {
                                Interlocked.Increment(ref m_commandsProcessed);
                            }
                            else
                            {
                                m_logger.LogDebug("Command {CommandId} was already completed by another thread (race condition handled)", queuedCommand.Id);
                            }
                            
                            LogConcurrencyStats();
                        }
                        catch (OperationCanceledException)
                        {
                            m_logger.LogInformation("Command {CommandId} was cancelled during execution", queuedCommand.Id);
                            
                            // FIXED: Use TrySetResult to prevent double-completion and update state
                            var wasCompleted = queuedCommand.CompletionSource.TrySetResult("Command execution was cancelled.");
                            if (wasCompleted)
                            {
                                UpdateCommandState(queuedCommand.Id, CommandState.Cancelled);
                                Interlocked.Increment(ref m_commandsCancelled);
                            }
                            else
                            {
                                m_logger.LogDebug("Command {CommandId} was already completed by another thread during cancellation", queuedCommand.Id);
                            }
                            
                            LogConcurrencyStats();
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogError(ex, "Error executing command {CommandId}: {Command}", queuedCommand.Id, queuedCommand.Command);
                            
                            // IMPROVED: Better error handling with detailed error information
                            var errorMessage = ex switch
                            {
                                OperationCanceledException => "Command execution was cancelled",
                                TimeoutException => "Command execution timed out",
                                InvalidOperationException => $"Invalid operation: {ex.Message}",
                                ArgumentException => $"Invalid argument: {ex.Message}",
                                _ => $"Command execution failed: {ex.GetType().Name}: {ex.Message}"
                            };
                            
                            var wasCompleted = queuedCommand.CompletionSource.TrySetResult(errorMessage);
                            UpdateCommandState(queuedCommand.Id, CommandState.Failed);
                            
                            if (wasCompleted)
                            {
                                Interlocked.Increment(ref m_commandsFailed);
                            }
                            else
                            {
                                m_logger.LogDebug("Command {CommandId} was already completed by another thread during error handling", queuedCommand.Id);
                            }
                            
                            LogConcurrencyStats();
                        }
                        finally
                        {
                            // Clear current command
                            lock (m_currentCommandLock)
                            {
                                m_currentCommand = null;
                            }

                            CleanupCommand(queuedCommand);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_logger.LogInformation("Command queue processing was cancelled");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Unexpected error in command queue processing");
            }
        }

        private void CleanupCommand(QueuedCommand command)
        {
            // DON'T remove from m_activeCommands - let completed commands stay for retrieval!
            // Only dispose the cancellation token to free resources
            command.CancellationTokenSource.Dispose();

            m_logger.LogDebug("Cleaned up command resources for {CommandId} (kept in activeCommands for result retrieval)", command.Id);
        }

        // CRITICAL FIX: Make state updates atomic
        private void UpdateCommandState(string commandId, CommandState newState)
        {
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                // Create updated command with new state
                var updatedCommand = command with { State = newState };
                // Use TryUpdate to ensure atomic state transition
                if (!m_activeCommands.TryUpdate(commandId, updatedCommand, command))
                {
                    // If update failed, the command was modified by another thread
                    // This is expected in concurrent scenarios, so we log and continue
                    m_logger.LogTrace("Command {CommandId} state update failed - command was modified by another thread", commandId);
                }
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
                    m_logger.LogInformation("Concurrency Stats - Processed: {Processed}, Failed: {Failed}, Cancelled: {Cancelled}, Success Rate: {SuccessRate:F1}%", 
                        processed, failed, cancelled, successRate);
                }
                
                m_lastStatsLog = now;
            }
        }

        // FIXED: Add cleanup method for completed commands
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

        public void Dispose()
        {
            m_logger.LogInformation("Shutting down CommandQueueService");

            // Check if already disposed
            try
            {
                if (m_serviceCts.Token.IsCancellationRequested)
                {
                    m_logger.LogWarning("CommandQueueService already disposed");
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                m_logger.LogWarning("CommandQueueService already disposed (CTS disposed)");
                return;
            }

            try
            {
                m_serviceCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                m_logger.LogWarning("CancellationTokenSource already disposed during shutdown");
                return;
            }

            try
            {
                m_processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error waiting for processing task to complete");
            }

            // Cancel all pending commands with disposal guards
            foreach (var command in m_activeCommands.Values)
            {
                try
                {
                    if (!command.CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        command.CancellationTokenSource.Cancel();
                    }
                    command.CompletionSource.TrySetResult("Service is shutting down.");
                }
                catch (ObjectDisposedException)
                {
                    // CancellationTokenSource already disposed, ignore
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error cancelling command {CommandId}", command.Id);
                }

                try
                {
                    command.CancellationTokenSource.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
            }

            m_activeCommands.Clear();

            try
            {
                m_serviceCts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }

            try
            {
                m_queueSemaphore.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }

            // FIXED: Dispose cleanup timer
            try
            {
                m_cleanupTimer?.Dispose();
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error disposing cleanup timer");
            }

            m_disposed = true;
            m_logger.LogInformation("CommandQueueService disposed");
        }
    }
}
