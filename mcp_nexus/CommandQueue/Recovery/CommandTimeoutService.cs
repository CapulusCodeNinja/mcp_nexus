using System.Collections.Concurrent;

namespace mcp_nexus.CommandQueue.Recovery
{
    /// <summary>
    /// Interface for managing command timeouts.
    /// Provides methods for starting, cancelling, and extending command timeouts.
    /// </summary>
    public interface ICommandTimeoutService
    {
        /// <summary>
        /// Starts a timeout for a command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="onTimeout">The action to execute when the timeout occurs.</param>
        void StartCommandTimeout(string commandId, TimeSpan timeout, Func<Task> onTimeout);

        /// <summary>
        /// Cancels a command timeout.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        void CancelCommandTimeout(string commandId);

        /// <summary>
        /// Extends a command timeout by the specified additional time.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <param name="additionalTime">The additional time to add to the timeout.</param>
        void ExtendCommandTimeout(string commandId, TimeSpan additionalTime);
    }

    /// <summary>
    /// Internal record containing timeout information for a command.
    /// </summary>
    /// <param name="CancellationTokenSource">The cancellation token source for the timeout.</param>
    /// <param name="OnTimeout">The action to execute when the timeout occurs.</param>
    /// <param name="StartTime">The time when the timeout was started.</param>
    internal record TimeoutInfo(
        CancellationTokenSource CancellationTokenSource,
        Func<Task> OnTimeout,
        DateTime StartTime
    );

    /// <summary>
    /// Service for managing command timeouts.
    /// Provides functionality to start, cancel, and extend timeouts for commands.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CommandTimeoutService"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance for recording timeout operations and errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public class CommandTimeoutService(ILogger<CommandTimeoutService> logger) : ICommandTimeoutService, IDisposable, IAsyncDisposable
    {
        private readonly ILogger<CommandTimeoutService> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentDictionary<string, TimeoutInfo> m_Timeouts = new();
        private bool m_Disposed;

        /// <summary>
        /// Starts a timeout for a command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="onTimeout">The action to execute when the timeout occurs.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandId"/> or <paramref name="onTimeout"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="commandId"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is negative.</exception>
        public void StartCommandTimeout(string commandId, TimeSpan timeout, Func<Task> onTimeout)
        {
            if (m_Disposed) return;

            // Validate parameters
            ArgumentNullException.ThrowIfNull(commandId);
            if (commandId.Length == 0)
                throw new ArgumentException("Command ID cannot be empty", nameof(commandId));
            if (timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout cannot be negative");
            ArgumentNullException.ThrowIfNull(onTimeout);

            // Cancel existing timeout if it exists
            if (m_Timeouts.TryRemove(commandId, out var existingInfo))
            {
                existingInfo.CancellationTokenSource.Cancel();
                // Don't dispose immediately - let the task handle its own cleanup
            }

            var cts = new CancellationTokenSource();
            var timeoutInfo = new TimeoutInfo(cts, onTimeout, DateTime.Now);
            m_Timeouts[commandId] = timeoutInfo;

            m_Logger.LogDebug("Starting timeout for command {CommandId}: {TimeoutMinutes:F1} minutes",
                commandId, timeout.TotalMinutes);

            // Start timeout task
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeout, cts.Token);

                    // Check if we're still the active timeout
                    if (!m_Timeouts.TryGetValue(commandId, out var currentInfo) || currentInfo.CancellationTokenSource != cts)
                    {
                        m_Logger.LogTrace("Timeout superseded for command {CommandId}", commandId);
                        return;
                    }

                    m_Logger.LogError("Command {CommandId} timed out after {TimeoutMinutes:F1} minutes",
                        commandId, timeout.TotalMinutes);

                    await onTimeout();
                }
                catch (OperationCanceledException)
                {
                    m_Logger.LogTrace("Timeout cancelled for command {CommandId}", commandId);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error in timeout handler for command {CommandId}", commandId);
                }
                finally
                {
                    m_Timeouts.TryRemove(commandId, out _);
                    cts.Dispose();
                }
            });
        }

        /// <summary>
        /// Cancels a command timeout.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="commandId"/> is empty.</exception>
        public void CancelCommandTimeout(string commandId)
        {
            ArgumentNullException.ThrowIfNull(commandId);
            if (commandId.Length == 0)
                throw new ArgumentException("Command ID cannot be empty", nameof(commandId));

            if (m_Timeouts.TryRemove(commandId, out var timeoutInfo))
            {
                m_Logger.LogTrace("Cancelling timeout for command {CommandId}", commandId);
                timeoutInfo.CancellationTokenSource.Cancel();
                timeoutInfo.CancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Extends a command timeout by the specified additional time.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <param name="additionalTime">The additional time to add to the timeout.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="commandId"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="additionalTime"/> is negative.</exception>
        public void ExtendCommandTimeout(string commandId, TimeSpan additionalTime)
        {
            ArgumentNullException.ThrowIfNull(commandId);
            if (commandId.Length == 0)
                throw new ArgumentException("Command ID cannot be empty", nameof(commandId));
            if (additionalTime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(additionalTime), "Additional time cannot be negative");

            if (m_Timeouts.TryRemove(commandId, out var existingInfo))
            {
                var originalHandler = existingInfo.OnTimeout;
                var totalElapsed = DateTime.Now - existingInfo.StartTime;

                m_Logger.LogDebug("Extending timeout for command {CommandId} by {AdditionalMinutes:F1} minutes (already running for {ElapsedMinutes:F1} minutes)",
                    commandId, additionalTime.TotalMinutes, totalElapsed.TotalMinutes);

                // Cancel existing timeout immediately to prevent it from firing
                m_Logger.LogDebug("Cancelling existing timeout for command {CommandId}", commandId);
                existingInfo.CancellationTokenSource.Cancel();
                m_Logger.LogDebug("Existing timeout cancelled for command {CommandId}", commandId);

                // Note: We don't dispose the CancellationTokenSource here to avoid race conditions
                // The original task will dispose it in its finally block

                // Create new timeout with the additional time, preserving the original handler
                var newCts = new CancellationTokenSource();
                var newTimeoutInfo = new TimeoutInfo(newCts, originalHandler, existingInfo.StartTime);
                m_Timeouts[commandId] = newTimeoutInfo;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Wait for the additional time
                        await Task.Delay(additionalTime, newCts.Token);

                        if (!newCts.Token.IsCancellationRequested)
                        {
                            var finalElapsed = DateTime.Now - existingInfo.StartTime;
                            m_Logger.LogError("Command {CommandId} exceeded extended timeout after {TotalMinutes:F1} minutes",
                                commandId, finalElapsed.TotalMinutes);

                            // Now we can call the original timeout handler!
                            await originalHandler();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        m_Logger.LogTrace("Extended timeout cancelled for command {CommandId}", commandId);
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex, "Error in extended timeout handler for command {CommandId}", commandId);
                    }
                    finally
                    {
                        m_Timeouts.TryRemove(commandId, out _);
                        newCts.Dispose();
                    }
                }, newCts.Token);
            }
        }

        /// <summary>
        /// Disposes the command timeout service and cancels all active timeouts.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;

            foreach (var timeoutInfo in m_Timeouts.Values)
            {
                timeoutInfo.CancellationTokenSource.Cancel();
                timeoutInfo.CancellationTokenSource.Dispose();
            }
            m_Timeouts.Clear();
        }

        /// <summary>
        /// Asynchronously disposes the command timeout service and cancels all active timeouts.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
        public ValueTask DisposeAsync()
        {
            if (m_Disposed) return ValueTask.CompletedTask;
            m_Disposed = true;

            // Cancel all timeouts first
            foreach (var timeoutInfo in m_Timeouts.Values)
            {
                timeoutInfo.CancellationTokenSource.Cancel();
            }

            // Dispose cancellation tokens synchronously
            foreach (var timeoutInfo in m_Timeouts.Values)
            {
                try
                {
                    timeoutInfo.CancellationTokenSource.Dispose();
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error disposing cancellation token");
                }
            }

            m_Timeouts.Clear();
            return ValueTask.CompletedTask;
        }
    }
}

