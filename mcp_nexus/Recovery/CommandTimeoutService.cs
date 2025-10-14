using System.Collections.Concurrent;

namespace mcp_nexus.Recovery
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
        private readonly ILogger<CommandTimeoutService> m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentDictionary<string, TimeoutInfo> m_timeouts = new();
        private bool m_disposed;

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
            if (m_disposed) return;

            // Validate parameters
            ArgumentNullException.ThrowIfNull(commandId);
            if (commandId.Length == 0)
                throw new ArgumentException("Command ID cannot be empty", nameof(commandId));
            if (timeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout cannot be negative");
            ArgumentNullException.ThrowIfNull(onTimeout);

            // Cancel existing timeout if it exists
            if (m_timeouts.TryRemove(commandId, out var existingInfo))
            {
                existingInfo.CancellationTokenSource.Cancel();
                // Don't dispose immediately - let the task handle its own cleanup
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

                    // Check if we're still the active timeout
                    if (!m_timeouts.TryGetValue(commandId, out var currentInfo) || currentInfo.CancellationTokenSource != cts)
                    {
                        m_logger.LogTrace("Timeout superseded for command {CommandId}", commandId);
                        return;
                    }

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

            if (m_timeouts.TryRemove(commandId, out var timeoutInfo))
            {
                m_logger.LogTrace("Cancelling timeout for command {CommandId}", commandId);
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

            if (m_timeouts.TryRemove(commandId, out var existingInfo))
            {
                var originalHandler = existingInfo.OnTimeout;
                var totalElapsed = DateTime.UtcNow - existingInfo.StartTime;

                m_logger.LogDebug("Extending timeout for command {CommandId} by {AdditionalMinutes:F1} minutes (already running for {ElapsedMinutes:F1} minutes)",
                    commandId, additionalTime.TotalMinutes, totalElapsed.TotalMinutes);

                // Cancel existing timeout immediately to prevent it from firing
                m_logger.LogDebug("Cancelling existing timeout for command {CommandId}", commandId);
                existingInfo.CancellationTokenSource.Cancel();
                m_logger.LogDebug("Existing timeout cancelled for command {CommandId}", commandId);

                // Note: We don't dispose the CancellationTokenSource here to avoid race conditions
                // The original task will dispose it in its finally block

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

        /// <summary>
        /// Disposes the command timeout service and cancels all active timeouts.
        /// </summary>
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

        /// <summary>
        /// Asynchronously disposes the command timeout service and cancels all active timeouts.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
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

