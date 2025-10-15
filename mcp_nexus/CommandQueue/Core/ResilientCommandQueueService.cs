using System.Collections.Concurrent;
using mcp_nexus.Debugger;
using mcp_nexus.Notifications;
using mcp_nexus.CommandQueue.Recovery;

namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Refactored resilient command queue service that orchestrates focused components
    /// Enhanced with automated recovery for unattended server operation
    /// </summary>
    public class ResilientCommandQueueService : IDisposable, ICommandQueueService
    {
        private readonly ILogger<ResilientCommandQueueService> m_Logger;
        private readonly BlockingCollection<QueuedCommand> m_CommandQueue;
        private readonly CancellationTokenSource m_ServiceCts = new();
        private readonly Task m_ProcessingTask;
        private bool m_Disposed;

        // Focused components
        private readonly ResilientQueueConfiguration m_Config;
        private readonly CommandRecoveryManager m_RecoveryManager;
        private readonly ResilientCommandProcessor m_Processor;
        private readonly IMcpNotificationService? m_NotificationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilientCommandQueueService"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording queue operations.</param>
        /// <param name="loggerFactory">The logger factory for creating component loggers.</param>
        /// <param name="timeoutService">The timeout service for managing command timeouts.</param>
        /// <param name="recoveryService">The recovery service for session recovery operations.</param>
        /// <param name="notificationService">Optional notification service for publishing queue events.</param>
        /// <param name="sessionId">Optional session identifier for the queue service.</param>
        public ResilientCommandQueueService(
            ICdbSession cdbSession,
            ILogger<ResilientCommandQueueService> logger,
            ILoggerFactory loggerFactory,
            ICommandTimeoutService timeoutService,
            ICdbSessionRecoveryService recoveryService,
            IMcpNotificationService? notificationService = null,
            string? sessionId = null)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_NotificationService = notificationService;

            // Create focused components
            m_Config = new ResilientQueueConfiguration(sessionId: sessionId ?? "unknown");
            m_RecoveryManager = new CommandRecoveryManager(
                cdbSession, logger, timeoutService, recoveryService, m_Config, notificationService);
            m_Processor = new ResilientCommandProcessor(
                loggerFactory.CreateLogger<ResilientCommandProcessor>(), m_RecoveryManager, m_Config, notificationService);

            // Initialize command queue
            m_CommandQueue = [];

            m_Logger.LogInformation("🚀 Starting resilient command queue with automated recovery");

            // Start the background processing task
            try
            {
                m_ProcessingTask = Task.Run(() => m_Processor.ProcessCommandQueueAsync(m_CommandQueue, m_ServiceCts.Token), m_ServiceCts.Token);
                m_Logger.LogDebug("✅ Resilient command queue background task started successfully");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "💥 Failed to start resilient command queue background task");
                throw;
            }
        }

        /// <summary>
        /// Queues a command for execution with resilience features.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The unique command ID</returns>
        public string QueueCommand(string command)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var commandId = Guid.NewGuid().ToString();
            var queuedCommand = new QueuedCommand(
                commandId,
                command,
                DateTime.Now,
                new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously),
                new CancellationTokenSource()
            );

            m_CommandQueue.Add(queuedCommand);

            // Register command for tracking immediately
            m_Processor.RegisterQueuedCommand(queuedCommand);

            m_Logger.LogInformation("📝 Queued resilient command {CommandId}: {Command}", commandId, command);
            m_Logger.LogDebug("📊 Queue depth: {QueueDepth}", m_CommandQueue.Count);

            // Send queued notification
            if (m_NotificationService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_NotificationService.NotifyCommandStatusAsync(commandId, command, "queued", 0, "Command queued for execution", string.Empty, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogWarning(ex, "Failed to send queued notification for command {CommandId}", commandId);
                    }
                });
            }

            return commandId;
        }

        /// <summary>
        /// Gets the result of a command
        /// </summary>
        /// <param name="commandId">The command ID</param>
        /// <returns>The command result</returns>
        public async Task<string> GetCommandResult(string commandId)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

            m_Logger.LogTrace("⏳ Waiting for resilient command {CommandId} result", commandId);

            return await m_Processor.GetCommandResult(commandId);
        }

        /// <summary>
        /// Gets the cached result with metadata for a command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>A task that represents the asynchronous operation and contains the cached result with metadata.</returns>
        public async Task<CachedCommandResult?> GetCachedResultWithMetadata(string commandId)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            try
            {
                return await Task.FromResult(m_Processor.GetCachedResultWithMetadata(commandId));
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting cached result with metadata for command {CommandId}", commandId);
                return null;
            }
        }

        /// <summary>
        /// Cancels a specific command by ID.
        /// </summary>
        /// <param name="commandId">The command ID to cancel</param>
        /// <returns>True if the command was cancelled</returns>
        public bool CancelCommand(string commandId)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            return m_Processor.CancelCommand(commandId, "User requested cancellation");
        }

        /// <summary>
        /// Cancels all queued commands.
        /// </summary>
        /// <param name="reason">The reason for cancellation</param>
        /// <returns>The number of commands cancelled</returns>
        public int CancelAllCommands(string? reason = null)
        {
            if (m_Disposed)
                return 0;

            return m_Processor.CancelAllCommands(reason ?? "Bulk cancellation");
        }

        /// <summary>
        /// Gets the currently executing command
        /// </summary>
        /// <returns>The current command, or null if none is executing</returns>
        public QueuedCommand? GetCurrentCommand()
        {
            if (m_Disposed)
                return null;

            return m_Processor.GetCurrentCommand();
        }

        /// <summary>
        /// Gets the status of all commands in the queue
        /// </summary>
        /// <returns>Collection of command status information</returns>
        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_Disposed)
                return [];

            return m_Processor.GetQueueStatus();
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        /// <returns>Performance statistics tuple</returns>
        public (long Processed, long Failed, long Cancelled) GetPerformanceStats()
        {
            if (m_Disposed)
                return (0, 0, 0);

            return m_Processor.GetPerformanceStats();
        }

        /// <summary>
        /// Gets the state of a specific command
        /// </summary>
        /// <param name="commandId">The command ID</param>
        /// <returns>The command state, or null if not found</returns>
        public CommandState? GetCommandState(string commandId)
        {
            if (m_Disposed || string.IsNullOrWhiteSpace(commandId))
                return null;

            return m_Processor.GetCommandState(commandId);
        }

        /// <summary>
        /// Gets detailed information about a specific command
        /// </summary>
        /// <param name="commandId">The command ID</param>
        /// <returns>Command information, or null if not found</returns>
        public CommandInfo? GetCommandInfo(string commandId)
        {
            if (m_Disposed || string.IsNullOrWhiteSpace(commandId))
                return null;

            return m_Processor.GetCommandInfo(commandId);
        }

        /// <summary>
        /// Disposes the service and all resources.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            try
            {
                m_Logger.LogInformation("🛑 Shutting down resilient command queue service");

                // Signal shutdown
                m_ServiceCts.Cancel();

                // Complete the queue to stop accepting new commands
                m_CommandQueue.CompleteAdding();

                // Wait for processing task to complete
                if (m_ProcessingTask != null && !m_ProcessingTask.IsCompleted)
                {
                    try
                    {
                        m_ProcessingTask.Wait(TimeSpan.FromSeconds(10));
                    }
                    catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                    {
                        // Expected during shutdown
                        m_Logger.LogDebug("Processing task cancelled during shutdown (expected)");
                    }
                }

                // Dispose components
                m_Processor?.Dispose();
                m_RecoveryManager?.Cleanup();

                // Dispose resources
                m_CommandQueue?.Dispose();
                m_ServiceCts?.Dispose();

                m_Logger.LogInformation("✅ Resilient command queue service shutdown complete");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "💥 Error during resilient command queue service disposal");
            }
        }
    }
}
