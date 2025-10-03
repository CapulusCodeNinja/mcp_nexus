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
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger<IsolatedCommandQueueService> m_logger;
        private readonly IMcpNotificationService m_notificationService;

        // Focused components
        private readonly CommandQueueConfiguration m_config;
        private readonly CommandTracker m_tracker;
        private readonly CommandProcessor m_processor;
        private readonly CommandNotificationManager m_notificationManager;

        // Core infrastructure
        private readonly BlockingCollection<QueuedCommand> m_commandQueue;
        private readonly CancellationTokenSource m_processingCts = new();
        private readonly Task m_processingTask;
        private bool m_disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolatedCommandQueueService"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording queue operations.</param>
        /// <param name="notificationService">The notification service for sending notifications.</param>
        /// <param name="sessionId">The unique identifier for the debugging session.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public IsolatedCommandQueueService(
            ICdbSession cdbSession,
            ILogger<IsolatedCommandQueueService> logger,
            IMcpNotificationService notificationService,
            string sessionId)
        {
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            // Create configuration
            m_config = new CommandQueueConfiguration(sessionId);

            // Create command queue
            m_commandQueue = new BlockingCollection<QueuedCommand>();

            // Create focused components
            m_tracker = new CommandTracker(m_logger, m_config, m_commandQueue);
            m_processor = new CommandProcessor(m_cdbSession, m_logger, m_config, m_tracker, m_commandQueue, m_processingCts);
            m_notificationManager = new CommandNotificationManager(m_notificationService, m_logger, m_config);

            m_logger.LogInformation("🚀 IsolatedCommandQueueService initializing for session {SessionId}", sessionId);

            // Start processing task
            m_logger.LogTrace("🔄 Starting background processing task for session {SessionId}", sessionId);
            m_processingTask = Task.Run(m_processor.ProcessCommandQueueAsync, m_processingCts.Token);
            m_logger.LogTrace("✅ Background processing task started for session {SessionId}, Task ID: {TaskId}", sessionId, m_processingTask.Id);

            // Notify startup
            m_notificationManager.NotifyServiceStartup();

            m_logger.LogInformation("✅ IsolatedCommandQueueService created for session {SessionId} (background task initializing)", sessionId);
        }

        /// <summary>
        /// Checks if the command queue is ready to accept commands
        /// </summary>
        /// <returns>True if ready, false otherwise</returns>
        public bool IsReady()
        {
            return !m_disposed && m_processingTask != null && !m_processingTask.IsFaulted && !m_processingCts.Token.IsCancellationRequested;
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
            var commandId = m_tracker.GenerateCommandId();

            m_logger.LogTrace("🔄 Queueing command {CommandId} in session {SessionId}: {Command}",
                commandId, m_config.SessionId, command);

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
            m_logger.LogTrace("🔄 Adding command {CommandId} to active commands dictionary for session {SessionId}", commandId, m_config.SessionId);
            if (!m_tracker.TryAddCommand(commandId, queuedCommand))
            {
                queuedCommand.CancellationTokenSource?.Dispose();
                throw new InvalidOperationException($"Command ID conflict: {commandId}");
            }
            m_logger.LogTrace("✅ Successfully added command {CommandId} to active commands dictionary for session {SessionId}", commandId, m_config.SessionId);

            try
            {
                // Add to processing queue
                m_logger.LogTrace("🔄 Adding command {CommandId} to processing queue for session {SessionId}", commandId, m_config.SessionId);
                m_commandQueue.Add(queuedCommand, m_processingCts.Token);
                m_logger.LogTrace("✅ Successfully added command {CommandId} to processing queue for session {SessionId}", commandId, m_config.SessionId);

                // Notify command queued
                var queuePosition = m_tracker.GetQueuePosition(commandId);
                var statusMessage = m_notificationManager.CreateQueuedStatusMessage(queuePosition, TimeSpan.Zero);
                var progress = m_notificationManager.CalculateQueueProgress(queuePosition, TimeSpan.Zero);

                m_notificationManager.NotifyCommandStatusFireAndForget(commandId, command, statusMessage, null, progress);

                m_logger.LogInformation("✅ Command {CommandId} queued successfully for session {SessionId} (position: {Position})",
                    commandId, m_config.SessionId, queuePosition);

                return commandId;
            }
            catch (Exception ex)
            {
                // Clean up on failure
                m_tracker.TryRemoveCommand(commandId, out _);
                queuedCommand.CancellationTokenSource?.Dispose();

                m_logger.LogError(ex, "❌ Failed to queue command {CommandId} for session {SessionId}", commandId, m_config.SessionId);
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

            var command = m_tracker.GetCommand(commandId);
            if (command == null)
            {
                m_logger.LogWarning("Command {CommandId} not found for session {SessionId}", commandId, m_config.SessionId);
                return $"Command not found: {commandId}";
            }

            try
            {
                m_logger.LogTrace("⏳ Waiting for command {CommandId} result in session {SessionId}", commandId, m_config.SessionId);
                var result = await (command.CompletionSource?.Task ?? Task.FromResult(string.Empty));
                m_logger.LogTrace("✅ Command {CommandId} result received in session {SessionId}", commandId, m_config.SessionId);
                return result;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Error getting result for command {CommandId} in session {SessionId}", commandId, m_config.SessionId);
                return $"Error getting command result: {ex.Message}";
            }
        }

        public CommandState? GetCommandState(string commandId)
        {
            ThrowIfDisposed();
            return m_tracker.GetCommandState(commandId);
        }

        public CommandInfo? GetCommandInfo(string commandId)
        {
            ThrowIfDisposed();
            return m_tracker.GetCommandInfo(commandId);
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
            if (m_disposed)
                return false;

            var success = m_processor.CancelCommand(commandId);
            if (success)
            {
                m_notificationManager.NotifyCommandCancellation(commandId, "User requested cancellation");
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
            if (m_disposed)
                return 0;

            var count = m_tracker.CancelAllCommands(reason);
            if (count > 0)
            {
                m_notificationManager.NotifyBulkCommandCancellation(count, reason ?? "Service shutdown");
            }
            return count;
        }

        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_disposed)
                return Enumerable.Empty<(string, string, DateTime, string)>();

            return m_tracker.GetQueueStatus();
        }

        public QueuedCommand? GetCurrentCommand()
        {
            if (m_disposed)
                return null;

            return m_tracker.GetCurrentCommand();
        }

        /// <summary>
        /// Forces immediate shutdown of the command queue service.
        /// This method cancels all pending commands and stops processing immediately.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public void ForceShutdownImmediate()
        {
            m_logger.LogWarning("🚨 Force shutdown requested for session {SessionId}", m_config.SessionId);

            try
            {
                m_processingCts.Cancel();
                // Stop accepting more commands and unblock consumers
                try { m_commandQueue.CompleteAdding(); } catch { }
                m_notificationManager.NotifyServiceShutdown("Force shutdown requested");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during force shutdown for session {SessionId}", m_config.SessionId);
            }
        }

        public (long Total, long Completed, long Failed, long Cancelled) GetPerformanceStats()
        {
            if (m_disposed)
                return (0, 0, 0, 0);

            return m_tracker.GetPerformanceStats();
        }

        /// <summary>
        /// Throws an ObjectDisposedException if the service has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(IsolatedCommandQueueService));
        }

        /// <summary>
        /// Disposes the isolated command queue service and all its resources.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_logger.LogInformation("🧹 Disposing IsolatedCommandQueueService for session {SessionId}", m_config.SessionId);

            try
            {
                // Cancel all commands
                var cancelledCount = CancelAllCommands("Service disposal");
                m_logger.LogInformation("Cancelled {Count} commands during disposal", cancelledCount);

                // Signal shutdown
                m_processingCts.Cancel();
                m_notificationManager.NotifyServiceShutdown("Service disposed");

                // Complete adding to unblock processing loop immediately
                try { m_commandQueue.CompleteAdding(); } catch { }

                // Wait for processing to complete
                try
                {
                    if (!m_processingTask.Wait(m_config.ShutdownTimeout))
                    {
                        m_logger.LogWarning("Processing task did not complete within {Timeout}ms, forcing shutdown",
                            m_config.ShutdownTimeout.TotalMilliseconds);
                    }
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                {
                    m_logger.LogDebug("Processing task was cancelled during shutdown (expected)");
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Error waiting for processing task to complete during disposal");
                }

                // Dispose resources
                m_commandQueue.Dispose();
                m_processingCts.Dispose();
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during disposal of IsolatedCommandQueueService for session {SessionId}", m_config.SessionId);
            }
            finally
            {
                m_disposed = true;
                m_logger.LogInformation("✅ IsolatedCommandQueueService disposed for session {SessionId}", m_config.SessionId);
            }
        }
    }
}
