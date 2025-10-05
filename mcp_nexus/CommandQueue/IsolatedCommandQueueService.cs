using System.Collections.Concurrent;
using mcp_nexus.Debugger;
using mcp_nexus.Notifications;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Refactored thread-safe, isolated command queue service for a single debugging session
    /// Uses focused components for better maintainability and testability
    /// </summary>
    public class IsolatedCommandQueueService : ICommandQueueService, IDisposable
    {
        private readonly ICdbSession m_CdbSession;
        private readonly ILogger<IsolatedCommandQueueService> m_Logger;
        private readonly IMcpNotificationService m_NotificationService;

        // Focused components
        private readonly CommandQueueConfiguration m_Config;
        private readonly CommandTracker m_Tracker;
        private readonly CommandProcessor m_Processor;
        private readonly CommandNotificationManager m_NotificationManager;

        // Core infrastructure
        private readonly BlockingCollection<QueuedCommand> m_CommandQueue;
        private readonly CancellationTokenSource m_ProcessingCts = new();
        private readonly Task m_ProcessingTask;
        private bool m_Disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolatedCommandQueueService"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording queue operations.</param>
        /// <param name="notificationService">The notification service for sending notifications.</param>
        /// <param name="sessionId">The unique identifier for the debugging session.</param>
        /// <param name="resultCache">Optional session command result cache for storing results.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public IsolatedCommandQueueService(
            ICdbSession cdbSession,
            ILogger<IsolatedCommandQueueService> logger,
            IMcpNotificationService notificationService,
            string sessionId,
            SessionCommandResultCache? resultCache = null)
        {
            m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            // Create configuration
            m_Config = new CommandQueueConfiguration(sessionId);

            // Create command queue
            m_CommandQueue = new BlockingCollection<QueuedCommand>();

            // Create focused components
            m_Tracker = new CommandTracker(m_Logger, m_Config, m_CommandQueue);
            m_Processor = new CommandProcessor(m_CdbSession, m_Logger, m_Config, m_Tracker, m_CommandQueue, m_ProcessingCts, resultCache);
            m_NotificationManager = new CommandNotificationManager(m_NotificationService, m_Logger, m_Config);

            m_Logger.LogInformation("🚀 IsolatedCommandQueueService initializing for session {SessionId}", sessionId);

            // Start processing task
            m_Logger.LogTrace("🔄 Starting background processing task for session {SessionId}", sessionId);
            m_ProcessingTask = Task.Run(m_Processor.ProcessCommandQueueAsync, m_ProcessingCts.Token);
            m_Logger.LogTrace("✅ Background processing task started for session {SessionId}, Task ID: {TaskId}", sessionId, m_ProcessingTask.Id);

            // Notify startup
            m_NotificationManager.NotifyServiceStartup();

            m_Logger.LogInformation("✅ IsolatedCommandQueueService created for session {SessionId} (background task initializing)", sessionId);
        }

        /// <summary>
        /// Checks if the command queue is ready to accept commands
        /// </summary>
        /// <returns>True if ready, false otherwise</returns>
        public bool IsReady()
        {
            return !m_Disposed && m_ProcessingTask != null && !m_ProcessingTask.IsFaulted && !m_ProcessingCts.Token.IsCancellationRequested;
        }

        /// <summary>
        /// Queues a command for execution in the isolated queue.
        /// </summary>
        /// <param name="command">The command to queue for execution.</param>
        /// <returns>The unique command ID for tracking the queued command.</returns>
        /// <exception cref="ArgumentException">Thrown when the command is null or empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public string QueueCommand(string command)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            // Generate unique command ID
            var commandId = m_Tracker.GenerateCommandId();

            m_Logger.LogTrace("🔄 Queueing command {CommandId} in session {SessionId}: {Command}",
                commandId, m_Config.SessionId, command);

            // Create command object
            var queuedCommand = new QueuedCommand(
                commandId,
                command,
                DateTime.UtcNow,
                new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously),
                new CancellationTokenSource(),
                CommandState.Queued
            );

            // Add to tracking dictionary first
            m_Logger.LogTrace("🔄 Adding command {CommandId} to active commands dictionary for session {SessionId}", commandId, m_Config.SessionId);
            if (!m_Tracker.TryAddCommand(commandId, queuedCommand))
            {
                queuedCommand.CancellationTokenSource?.Dispose();
                throw new InvalidOperationException($"Command ID conflict: {commandId}");
            }
            m_Logger.LogTrace("✅ Successfully added command {CommandId} to active commands dictionary for session {SessionId}", commandId, m_Config.SessionId);

            try
            {
                // Add to processing queue
                m_Logger.LogTrace("🔄 Adding command {CommandId} to processing queue for session {SessionId}", commandId, m_Config.SessionId);
                m_CommandQueue.Add(queuedCommand, m_ProcessingCts.Token);
                m_Logger.LogTrace("✅ Successfully added command {CommandId} to processing queue for session {SessionId}", commandId, m_Config.SessionId);

                // Notify command queued
                var queuePosition = m_Tracker.GetQueuePosition(commandId);
                var statusMessage = m_NotificationManager.CreateQueuedStatusMessage(queuePosition, TimeSpan.Zero);
                var progress = m_NotificationManager.CalculateQueueProgress(queuePosition, TimeSpan.Zero);

                m_NotificationManager.NotifyCommandStatusFireAndForget(commandId, command, statusMessage, null, progress);

                m_Logger.LogInformation("✅ Command {CommandId} queued successfully for session {SessionId} (position: {Position})",
                    commandId, m_Config.SessionId, queuePosition);

                return commandId;
            }
            catch (Exception ex)
            {
                // Clean up on failure
                m_Tracker.TryRemoveCommand(commandId, out _);
                queuedCommand.CancellationTokenSource?.Dispose();

                m_Logger.LogError(ex, "❌ Failed to queue command {CommandId} for session {SessionId}", commandId, m_Config.SessionId);
                throw;
            }
        }

        /// <summary>
        /// Gets the result of a completed command by its ID.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>
        /// The result of the command if it has completed successfully; otherwise, 
        /// an error message indicating the command status or that it was not found.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public async Task<string> GetCommandResult(string commandId)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(commandId))
                return "Command ID cannot be null or empty";

            // First, try to get result from cache (for completed commands that may have been cleaned up)
            var cachedResult = m_Processor.GetCommandResult(commandId);
            if (cachedResult != null)
            {
                m_Logger.LogTrace("✅ Command {CommandId} result retrieved from cache for session {SessionId}", commandId, m_Config.SessionId);
                return cachedResult.IsSuccess ? cachedResult.Output : $"Command failed: {cachedResult.ErrorMessage}";
            }

            // If not in cache, check if command is still active in tracker
            var command = m_Tracker.GetCommand(commandId);
            if (command == null)
            {
                m_Logger.LogWarning("Command {CommandId} not found in tracker or cache for session {SessionId}", commandId, m_Config.SessionId);
                return $"Command not found: {commandId}";
            }

            try
            {
                m_Logger.LogTrace("⏳ Waiting for command {CommandId} result in session {SessionId}", commandId, m_Config.SessionId);
                var result = await (command.CompletionSource?.Task ?? Task.FromResult(string.Empty));
                m_Logger.LogTrace("✅ Command {CommandId} result received in session {SessionId}", commandId, m_Config.SessionId);
                return result;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "❌ Error getting result for command {CommandId} in session {SessionId}", commandId, m_Config.SessionId);
                return $"Error getting command result: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the cached result with metadata for a command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>A task that represents the asynchronous operation and contains the cached result with metadata.</returns>
        public async Task<CachedCommandResult?> GetCachedResultWithMetadata(string commandId)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            try
            {
                // Get the cached result with metadata from the processor
                return await Task.FromResult(m_Processor.GetCachedResultWithMetadata(commandId));
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting cached result with metadata for command {CommandId} in session {SessionId}", commandId, m_Config.SessionId);
                return null;
            }
        }

        public CommandState? GetCommandState(string commandId)
        {
            ThrowIfDisposed();
            return m_Tracker.GetCommandState(commandId);
        }

        public CommandInfo? GetCommandInfo(string commandId)
        {
            ThrowIfDisposed();
            return m_Tracker.GetCommandInfo(commandId);
        }

        /// <summary>
        /// Cancels a queued command by its ID.
        /// </summary>
        /// <param name="commandId">The ID of the command to cancel.</param>
        /// <returns><c>true</c> if the command was found and cancelled; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the command ID is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public bool CancelCommand(string commandId)
        {
            if (m_Disposed)
                return false;

            var success = m_Processor.CancelCommand(commandId);
            if (success)
            {
                m_NotificationManager.NotifyCommandCancellation(commandId, "User requested cancellation");
            }
            return success;
        }

        /// <summary>
        /// Cancels all queued commands.
        /// </summary>
        /// <param name="reason">Optional reason for cancelling all commands.</param>
        /// <returns>The number of commands that were cancelled.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public int CancelAllCommands(string? reason = null)
        {
            if (m_Disposed)
                return 0;

            var count = m_Tracker.CancelAllCommands(reason);
            if (count > 0)
            {
                m_NotificationManager.NotifyBulkCommandCancellation(count, reason ?? "Service shutdown");
            }
            return count;
        }

        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_Disposed)
                return Enumerable.Empty<(string, string, DateTime, string)>();

            return m_Tracker.GetQueueStatus();
        }

        public QueuedCommand? GetCurrentCommand()
        {
            if (m_Disposed)
                return null;

            return m_Tracker.GetCurrentCommand();
        }

        /// <summary>
        /// Forces immediate shutdown of the command queue service.
        /// This method cancels all pending commands and stops processing immediately.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public void ForceShutdownImmediate()
        {
            m_Logger.LogWarning("🚨 Force shutdown requested for session {SessionId}", m_Config.SessionId);

            try
            {
                m_ProcessingCts.Cancel();
                // Stop accepting more commands and unblock consumers
                try { m_CommandQueue.CompleteAdding(); } catch { }
                m_NotificationManager.NotifyServiceShutdown("Force shutdown requested");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during force shutdown for session {SessionId}", m_Config.SessionId);
            }
        }

        public (long Total, long Completed, long Failed, long Cancelled) GetPerformanceStats()
        {
            if (m_Disposed)
                return (0, 0, 0, 0);

            return m_Tracker.GetPerformanceStats();
        }

        /// <summary>
        /// Throws an ObjectDisposedException if the service has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(IsolatedCommandQueueService));
        }

        /// <summary>
        /// Disposes the isolated command queue service and all its resources.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Logger.LogInformation("🧹 Disposing IsolatedCommandQueueService for session {SessionId}", m_Config.SessionId);

            try
            {
                // Cancel all commands
                var cancelledCount = CancelAllCommands("Service disposal");
                m_Logger.LogInformation("Cancelled {Count} commands during disposal", cancelledCount);

                // Signal shutdown
                m_ProcessingCts.Cancel();
                m_NotificationManager.NotifyServiceShutdown("Service disposed");

                // Complete adding to unblock processing loop immediately
                try { m_CommandQueue.CompleteAdding(); } catch { }

                // Wait for processing to complete
                try
                {
                    if (!m_ProcessingTask.Wait(m_Config.ShutdownTimeout))
                    {
                        m_Logger.LogWarning("Processing task did not complete within {Timeout}ms, forcing shutdown",
                            m_Config.ShutdownTimeout.TotalMilliseconds);
                    }
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                {
                    m_Logger.LogDebug("Processing task was cancelled during shutdown (expected)");
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Error waiting for processing task to complete during disposal");
                }

                // Dispose resources
                m_CommandQueue.Dispose();
                m_ProcessingCts.Dispose();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during disposal of IsolatedCommandQueueService for session {SessionId}", m_Config.SessionId);
            }
            finally
            {
                m_Disposed = true;
                m_Logger.LogInformation("✅ IsolatedCommandQueueService disposed for session {SessionId}", m_Config.SessionId);
            }
        }

        /// <summary>
        /// Gets the cached result of a specific command from the session cache
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <returns>The cached command result, or null if not found</returns>
        public ICommandResult? GetCachedCommandResult(string commandId)
        {
            return m_Processor.GetCommandResult(commandId);
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        /// <returns>Cache statistics, or null if no cache is available</returns>
        public CacheStatistics? GetCacheStatistics()
        {
            return m_Processor.GetCacheStatistics();
        }
    }
}
