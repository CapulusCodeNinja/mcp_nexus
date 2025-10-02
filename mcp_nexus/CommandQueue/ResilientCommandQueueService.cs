using System.Collections.Concurrent;
using mcp_nexus.Debugger;
using mcp_nexus.Notifications;
using mcp_nexus.Recovery;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Refactored resilient command queue service that orchestrates focused components
    /// Enhanced with automated recovery for unattended server operation
    /// </summary>
    public class ResilientCommandQueueService : IDisposable, ICommandQueueService
    {
        private readonly ILogger<ResilientCommandQueueService> m_logger;
        private readonly BlockingCollection<QueuedCommand> m_commandQueue;
        private readonly CancellationTokenSource m_serviceCts = new();
        private readonly Task m_processingTask;
        private bool m_disposed;

        // Focused components
        private readonly ResilientQueueConfiguration m_config;
        private readonly CommandRecoveryManager m_recoveryManager;
        private readonly ResilientCommandProcessor m_processor;
        private readonly IMcpNotificationService? m_notificationService;

        public ResilientCommandQueueService(
            ICdbSession cdbSession,
            ILogger<ResilientCommandQueueService> logger,
            ILoggerFactory loggerFactory,
            ICommandTimeoutService timeoutService,
            ICdbSessionRecoveryService recoveryService,
            IMcpNotificationService? notificationService = null,
            string? sessionId = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_notificationService = notificationService;

            // Create focused components
            m_config = new ResilientQueueConfiguration(sessionId: sessionId ?? "unknown");
            m_recoveryManager = new CommandRecoveryManager(
                cdbSession, logger, timeoutService, recoveryService, m_config, notificationService);
            m_processor = new ResilientCommandProcessor(
                loggerFactory.CreateLogger<ResilientCommandProcessor>(), m_recoveryManager, m_config, notificationService);

            // Initialize command queue
            m_commandQueue = new BlockingCollection<QueuedCommand>();

            m_logger.LogInformation("🚀 Starting resilient command queue with automated recovery");

            // Start the background processing task
            try
            {
                m_processingTask = Task.Run(() => m_processor.ProcessCommandQueueAsync(m_commandQueue, m_serviceCts.Token), m_serviceCts.Token);
                m_logger.LogDebug("✅ Resilient command queue background task started successfully");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Failed to start resilient command queue background task");
                throw;
            }
        }

        /// <summary>
        /// Queues a command for execution
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The unique command ID</returns>
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

            m_commandQueue.Add(queuedCommand);

            // Register command for tracking immediately
            m_processor.RegisterQueuedCommand(queuedCommand);

            m_logger.LogInformation("📝 Queued resilient command {CommandId}: {Command}", commandId, command);
            m_logger.LogDebug("📊 Queue depth: {QueueDepth}", m_commandQueue.Count);

            // Send queued notification
            if (m_notificationService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService.NotifyCommandStatusAsync(commandId, command, "queued", 0, "Command queued for execution", string.Empty, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send queued notification for command {CommandId}", commandId);
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
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

            m_logger.LogTrace("⏳ Waiting for resilient command {CommandId} result", commandId);

            return await m_processor.GetCommandResult(commandId);
        }

        /// <summary>
        /// Cancels a specific command
        /// </summary>
        /// <param name="commandId">The command ID to cancel</param>
        /// <returns>True if the command was cancelled</returns>
        public bool CancelCommand(string commandId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ResilientCommandQueueService));

            return m_processor.CancelCommand(commandId, "User requested cancellation");
        }

        /// <summary>
        /// Cancels all commands
        /// </summary>
        /// <param name="reason">The reason for cancellation</param>
        /// <returns>The number of commands cancelled</returns>
        public int CancelAllCommands(string? reason = null)
        {
            if (m_disposed)
                return 0;

            return m_processor.CancelAllCommands(reason ?? "Bulk cancellation");
        }

        /// <summary>
        /// Gets the currently executing command
        /// </summary>
        /// <returns>The current command, or null if none is executing</returns>
        public QueuedCommand? GetCurrentCommand()
        {
            if (m_disposed)
                return null;

            return m_processor.GetCurrentCommand();
        }

        /// <summary>
        /// Gets the status of all commands in the queue
        /// </summary>
        /// <returns>Collection of command status information</returns>
        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_disposed)
                return Enumerable.Empty<(string, string, DateTime, string)>();

            return m_processor.GetQueueStatus();
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        /// <returns>Performance statistics tuple</returns>
        public (long Processed, long Failed, long Cancelled) GetPerformanceStats()
        {
            if (m_disposed)
                return (0, 0, 0);

            return m_processor.GetPerformanceStats();
        }

        /// <summary>
        /// Gets the state of a specific command
        /// </summary>
        /// <param name="commandId">The command ID</param>
        /// <returns>The command state, or null if not found</returns>
        public CommandState? GetCommandState(string commandId)
        {
            if (m_disposed || string.IsNullOrWhiteSpace(commandId))
                return null;

            return m_processor.GetCommandState(commandId);
        }

        /// <summary>
        /// Gets detailed information about a specific command
        /// </summary>
        /// <param name="commandId">The command ID</param>
        /// <returns>Command information, or null if not found</returns>
        public CommandInfo? GetCommandInfo(string commandId)
        {
            if (m_disposed || string.IsNullOrWhiteSpace(commandId))
                return null;

            return m_processor.GetCommandInfo(commandId);
        }

        /// <summary>
        /// Disposes the service and all resources
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            try
            {
                m_logger.LogInformation("🛑 Shutting down resilient command queue service");

                // Signal shutdown
                m_serviceCts.Cancel();

                // Complete the queue to stop accepting new commands
                m_commandQueue.CompleteAdding();

                // Wait for processing task to complete
                if (m_processingTask != null && !m_processingTask.IsCompleted)
                {
                    try
                    {
                        m_processingTask.Wait(TimeSpan.FromSeconds(10));
                    }
                    catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                    {
                        // Expected during shutdown
                        m_logger.LogDebug("Processing task cancelled during shutdown (expected)");
                    }
                }

                // Dispose components
                m_processor?.Dispose();
                m_recoveryManager?.Cleanup();

                // Dispose resources
                m_commandQueue?.Dispose();
                m_serviceCts?.Dispose();

                m_logger.LogInformation("✅ Resilient command queue service shutdown complete");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Error during resilient command queue service disposal");
            }
        }
    }
}
