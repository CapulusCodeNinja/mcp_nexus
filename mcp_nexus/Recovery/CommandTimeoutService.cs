using System.Collections.Concurrent;

namespace mcp_nexus.Recovery
{
    public interface ICommandTimeoutService
    {
        void StartCommandTimeout(string commandId, TimeSpan timeout, Func<Task> onTimeout);
        void CancelCommandTimeout(string commandId);
        void ExtendCommandTimeout(string commandId, TimeSpan additionalTime);
    }

    internal record TimeoutInfo(
        CancellationTokenSource CancellationTokenSource,
        Func<Task> OnTimeout,
        DateTime StartTime
    );

    public class CommandTimeoutService : ICommandTimeoutService, IDisposable, IAsyncDisposable
    {
        private readonly ILogger<CommandTimeoutService> m_logger;
        private readonly ConcurrentDictionary<string, TimeoutInfo> m_timeouts = new();
        private bool m_disposed;

        public CommandTimeoutService(ILogger<CommandTimeoutService> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void StartCommandTimeout(string commandId, TimeSpan timeout, Func<Task> onTimeout)
        {
            if (m_disposed) return;
            
            // Validate parameters
            if (commandId == null)
                throw new ArgumentNullException(nameof(commandId));
            if (commandId.Length == 0)
                throw new ArgumentException("Command ID cannot be empty", nameof(commandId));
            if (timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout cannot be negative");
            if (onTimeout == null)
                throw new ArgumentNullException(nameof(onTimeout));

            // Cancel existing timeout if it exists
            if (m_timeouts.TryRemove(commandId, out var existingInfo))
            {
                existingInfo.CancellationTokenSource.Cancel();
                existingInfo.CancellationTokenSource.Dispose();
            }

            var cts = new CancellationTokenSource();
            var timeoutInfo = new TimeoutInfo(cts, onTimeout, DateTime.UtcNow);
            m_timeouts[commandId] = timeoutInfo;
            
            m_logger.LogDebug("Starting timeout for command {CommandId}: {TimeoutMinutes:F1} minutes", 
                commandId, timeout.TotalMinutes);

            // Start timeout task
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeout, cts.Token);
                    
                    // Check cancellation again after delay
                    cts.Token.ThrowIfCancellationRequested();
                    
                    m_logger.LogError("Command {CommandId} timed out after {TimeoutMinutes:F1} minutes", 
                        commandId, timeout.TotalMinutes);
                    
                    await onTimeout();
                }
                catch (OperationCanceledException)
                {
                    m_logger.LogTrace("Timeout cancelled for command {CommandId}", commandId);
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error in timeout handler for command {CommandId}", commandId);
                }
                finally
                {
                    m_timeouts.TryRemove(commandId, out _);
                    cts.Dispose();
                }
            }, cts.Token);
        }

        public void CancelCommandTimeout(string commandId)
        {
            if (commandId == null)
                throw new ArgumentNullException(nameof(commandId));
            if (commandId.Length == 0)
                throw new ArgumentException("Command ID cannot be empty", nameof(commandId));
                
            if (m_timeouts.TryRemove(commandId, out var timeoutInfo))
            {
                m_logger.LogTrace("Cancelling timeout for command {CommandId}", commandId);
                timeoutInfo.CancellationTokenSource.Cancel();
                timeoutInfo.CancellationTokenSource.Dispose();
            }
        }

        public void ExtendCommandTimeout(string commandId, TimeSpan additionalTime)
        {
            if (commandId == null)
                throw new ArgumentNullException(nameof(commandId));
            if (commandId.Length == 0)
                throw new ArgumentException("Command ID cannot be empty", nameof(commandId));
            if (additionalTime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(additionalTime), "Additional time cannot be negative");
                
            if (m_timeouts.TryRemove(commandId, out var existingInfo))
            {
                var originalHandler = existingInfo.OnTimeout;
                var totalElapsed = DateTime.UtcNow - existingInfo.StartTime;
                
                m_logger.LogDebug("Extending timeout for command {CommandId} by {AdditionalMinutes:F1} minutes (already running for {ElapsedMinutes:F1} minutes)", 
                    commandId, additionalTime.TotalMinutes, totalElapsed.TotalMinutes);
                
                // Cancel existing timeout immediately to prevent it from firing
                m_logger.LogDebug("Cancelling existing timeout for command {CommandId}", commandId);
                existingInfo.CancellationTokenSource.Cancel();
                existingInfo.CancellationTokenSource.Dispose();
                m_logger.LogDebug("Existing timeout cancelled for command {CommandId}", commandId);
                
                // Create new timeout with the additional time, preserving the original handler
                var newCts = new CancellationTokenSource();
                var newTimeoutInfo = new TimeoutInfo(newCts, originalHandler, existingInfo.StartTime);
                m_timeouts[commandId] = newTimeoutInfo;
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Wait for the additional time
                        await Task.Delay(additionalTime, newCts.Token);
                        
                        if (!newCts.Token.IsCancellationRequested)
                        {
                            var finalElapsed = DateTime.UtcNow - existingInfo.StartTime;
                            m_logger.LogError("Command {CommandId} exceeded extended timeout after {TotalMinutes:F1} minutes", 
                                commandId, finalElapsed.TotalMinutes);
                            
                            // Now we can call the original timeout handler!
                            await originalHandler();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        m_logger.LogTrace("Extended timeout cancelled for command {CommandId}", commandId);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Error in extended timeout handler for command {CommandId}", commandId);
                    }
                    finally
                    {
                        m_timeouts.TryRemove(commandId, out _);
                        newCts.Dispose();
                    }
                }, newCts.Token);
            }
        }

        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            foreach (var timeoutInfo in m_timeouts.Values)
            {
                timeoutInfo.CancellationTokenSource.Cancel();
                timeoutInfo.CancellationTokenSource.Dispose();
            }
            m_timeouts.Clear();
        }

        // IMPROVED: Simplified async disposal - Dispose() is synchronous
        public ValueTask DisposeAsync()
        {
            if (m_disposed) return ValueTask.CompletedTask;
            m_disposed = true;

            // Cancel all timeouts first
            foreach (var timeoutInfo in m_timeouts.Values)
            {
                timeoutInfo.CancellationTokenSource.Cancel();
            }

            // Dispose cancellation tokens synchronously
            foreach (var timeoutInfo in m_timeouts.Values)
            {
                try
                {
                    timeoutInfo.CancellationTokenSource.Dispose();
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error disposing cancellation token");
                }
            }

            m_timeouts.Clear();
            return ValueTask.CompletedTask;
        }
    }
}

