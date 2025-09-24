using System.Collections.Concurrent;
using mcp_nexus.Helper;

namespace mcp_nexus.Services
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
        private readonly TimeSpan m_defaultCommandTimeout = TimeSpan.FromMinutes(10); // 10 minute default
        private readonly TimeSpan m_complexCommandTimeout = TimeSpan.FromMinutes(30); // 30 minutes for complex analysis
        private readonly TimeSpan m_maxCommandTimeout = TimeSpan.FromHours(1); // 1 hour absolute max

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

            m_logger.LogInformation("🚀 ResilientCommandQueueService starting with automated recovery");

            // Start the background processing task
            try
            {
                m_processingTask = Task.Run(ProcessCommandQueue, m_serviceCts.Token);
                m_logger.LogInformation("✅ Resilient command queue started successfully");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ FAILED to start resilient command queue");
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

            m_logger.LogInformation("📋 Queued command {CommandId}: {Command} (queue depth: {QueueDepth})", 
                commandId, command, m_commandQueue.Count);

            // Send notification about command being queued
            _ = Task.Run(async () =>
            {
                try
                {
                    await m_notificationService?.NotifyCommandStatusAsync(
                        commandId, command, "queued", 0, "Command queued for execution")!;
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Failed to send queued notification for command {CommandId}", commandId);
                }
            });

            return commandId;
        }

        public Task<string> GetCommandResult(string commandId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));
                
            if (string.IsNullOrEmpty(commandId))
                return Task.FromResult($"Command not found: {commandId}");
                
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
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
                    return Task.FromResult($"Command is still executing... Please call get_command_status(commandId='{commandId}') again in 5-10 seconds to check if completed.");
                }
            }

            return Task.FromResult($"Command not found: {commandId}");
        }

        public bool CancelCommand(string commandId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));
                
            if (string.IsNullOrEmpty(commandId))
                return false;
                
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                m_logger.LogInformation("⏹️ Cancelling command {CommandId}: {Command}", commandId, command.Command);

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
                        m_logger.LogWarning("🚨 Cancelling currently executing command {CommandId}", commandId);
                        m_cdbSession.CancelCurrentOperation();
                    }
                }

                // Remove from queue if still queued (prevents "Cancelled" status showing)
                RemoveCancelledCommandFromQueue(commandId);

                return true;
            }

            m_logger.LogWarning("❌ Attempted to cancel non-existent command: {CommandId}", commandId);
            return false;
        }

        public int CancelAllCommands(string? reason = null)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));
                
            var reasonText = string.IsNullOrWhiteSpace(reason) ? "No reason provided" : reason;
            m_logger.LogWarning("🚨 Cancelling ALL commands. Reason: {Reason}", reasonText);

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
                    m_logger.LogWarning("🚨 Requesting CDB cancellation for currently executing command: {CommandId}", currentId);
                    m_cdbSession.CancelCurrentOperation();
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error requesting CDB cancellation for current command: {CommandId}", currentId);
                }
            }

            m_logger.LogInformation("✅ Cancelled {Count} command(s)", cancelledCount);
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
            m_logger.LogInformation("🚀 Resilient command processor started");

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
                            m_logger.LogInformation("⏭️ Skipping cancelled command {CommandId}", queuedCommand.Id);
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
                m_logger.LogInformation("⏹️ Command queue processing was cancelled");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Unexpected error in command queue processing - attempting recovery");
                
                // Trigger recovery for queue processor failure
                _ = Task.Run(async () => await m_recoveryService.RecoverStuckSession("Queue processor failure"));
            }
        }

        private async Task ProcessSingleCommand(QueuedCommand queuedCommand)
        {
            lock (m_currentCommandLock)
            {
                m_currentCommand = queuedCommand;
            }

            var waitTime = (DateTime.UtcNow - queuedCommand.QueueTime).TotalSeconds;
            m_logger.LogInformation("🚀 Starting command {CommandId}: {Command} (waited {WaitTime:F1}s)", 
                queuedCommand.Id, queuedCommand.Command, waitTime);

            // Send notification about command starting execution
            _ = Task.Run(async () =>
            {
                try
                {
                    await m_notificationService?.NotifyCommandStatusAsync(
                        queuedCommand.Id, queuedCommand.Command, "executing", 10, "Command started execution")!;
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Failed to send executing notification for command {CommandId}", queuedCommand.Id);
                }
            });

            // Determine timeout based on command complexity
            var timeout = DetermineCommandTimeout(queuedCommand.Command);
            
            // Start automated timeout monitoring
            m_timeoutService.StartCommandTimeout(queuedCommand.Id, timeout, async () =>
            {
                m_logger.LogError("⏰ TIMEOUT: Command {CommandId} exceeded {Minutes:F1} minute limit", 
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
                            
                            await m_notificationService?.NotifyCommandHeartbeatAsync(
                                queuedCommand.Id, queuedCommand.Command, elapsed, details)!;
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
                    m_logger.LogWarning("⚠️ Session unhealthy, attempting recovery before command execution");
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

                m_logger.LogInformation("✅ Command {CommandId} completed successfully", queuedCommand.Id);
                queuedCommand.CompletionSource.TrySetResult(result);
                
                // Send completion notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService?.NotifyCommandStatusAsync(
                            queuedCommand.Id, queuedCommand.Command, "completed", 100, "Command completed successfully", result)!;
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
                m_logger.LogInformation("⏹️ Command {CommandId} was cancelled", queuedCommand.Id);
                queuedCommand.CompletionSource.TrySetResult("Command execution was cancelled.");
                
                // Send cancellation notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService?.NotifyCommandStatusAsync(
                            queuedCommand.Id, queuedCommand.Command, "cancelled", null, "Command execution was cancelled")!;
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
                m_logger.LogError(ex, "💥 Command {CommandId} failed: {Error}", queuedCommand.Id, ex.Message);
                
                // On execution failure, trigger recovery
                _ = Task.Run(async () => await m_recoveryService.RecoverStuckSession($"Command execution failed: {ex.Message}"));
                
                queuedCommand.CompletionSource.TrySetResult($"Command execution failed: {ex.Message}");
                
                // Send error notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService?.NotifyCommandStatusAsync(
                            queuedCommand.Id, queuedCommand.Command, "failed", null, "Command execution failed", null, ex.Message)!;
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
            var lowerCommand = command.ToLowerInvariant();
            
            // Provide context-specific details based on command type
            if (lowerCommand.Contains("!analyze"))
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
            
            if (lowerCommand.Contains("!heap"))
            {
                if (elapsed.TotalMinutes < 1)
                    return "Scanning heap structures...";
                else if (elapsed.TotalMinutes < 3)
                    return "Analyzing heap allocations and free blocks...";
                else
                    return "Processing large heap dump (this is normal for applications with high memory usage)...";
            }
            
            if (lowerCommand.Contains("!process 0 0") || lowerCommand.Contains("!process"))
            {
                if (elapsed.TotalMinutes < 1)
                    return "Enumerating system processes...";
                else if (elapsed.TotalMinutes < 3)
                    return "Gathering detailed process information...";
                else
                    return "Processing extensive process data...";
            }
            
            if (lowerCommand.Contains("!locks") || lowerCommand.Contains("!handle"))
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
            m_logger.LogInformation("🧹 Cleared all queued commands");
        }

        private void CleanupCommand(QueuedCommand command)
        {
            // Keep completed commands for result retrieval, just dispose the cancellation token
            command.CancellationTokenSource.Dispose();
            m_logger.LogDebug("🧹 Cleaned up resources for command {CommandId}", command.Id);
        }

        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            m_logger.LogInformation("🛑 Disposing ResilientCommandQueueService");

            // Cancel all commands and cleanup
            try
            {
                CancelAllCommands("Service shutdown");
                m_serviceCts.Cancel();
                
                if (!m_processingTask.Wait(5000))
                {
                    m_logger.LogWarning("⚠️ Processing task did not complete within 5 seconds");
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during disposal");
            }

            m_activeCommands.Clear();

            try { m_serviceCts.Dispose(); } catch (ObjectDisposedException) { }
            try { m_queueSemaphore.Dispose(); } catch (ObjectDisposedException) { }

            m_logger.LogInformation("✅ ResilientCommandQueueService disposed");
        }
    }
}
