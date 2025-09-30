using System.Collections.Concurrent;
using mcp_nexus.Debugger;

namespace mcp_nexus.CommandQueue
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

    /// <summary>
    /// Refactored basic command queue service that orchestrates focused components
    /// </summary>
    public class CommandQueueService : IDisposable, ICommandQueueService
    {
        private readonly ILogger<CommandQueueService> m_logger;
        private readonly BlockingCollection<QueuedCommand> m_commandQueue;
        private readonly CancellationTokenSource m_serviceCts = new();
        private readonly Task m_processingTask;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_activeCommands = new();
        private bool m_disposed;

        // Focused components
        private readonly BasicQueueConfiguration m_config;
        private readonly BasicCommandProcessor m_processor;

        public CommandQueueService(ICdbSession cdbSession, ILogger<CommandQueueService> logger, ILoggerFactory loggerFactory)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create focused components
            m_config = new BasicQueueConfiguration();
            m_processor = new BasicCommandProcessor(cdbSession, loggerFactory.CreateLogger<BasicCommandProcessor>(), m_config, m_activeCommands);

            // Initialize command queue
            m_commandQueue = new BlockingCollection<QueuedCommand>();

            m_logger.LogInformation("🚀 CommandQueueService initializing with focused components");

            // Start the background processing task
            try
            {
                m_processingTask = Task.Run(() => m_processor.ProcessCommandQueueAsync(m_commandQueue, m_serviceCts.Token), m_serviceCts.Token);
                m_logger.LogInformation("✅ CommandQueueService background task started successfully");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Failed to start CommandQueueService background task");
                throw;
            }

            m_logger.LogInformation("🎯 CommandQueueService fully initialized with focused components");
        }

        /// <summary>
        /// Queues a command for execution
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The unique command ID</returns>
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

            m_logger.LogInformation("📋 Adding command to queue: {CommandId}", commandId);
            m_commandQueue.Add(queuedCommand);

            m_logger.LogDebug("📊 Queue depth: {QueueDepth}", m_commandQueue.Count);
            m_logger.LogInformation("✅ QueueCommand END: {CommandId}", commandId);

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
                throw new ObjectDisposedException(nameof(CommandQueueService));

            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

            if (!m_activeCommands.TryGetValue(commandId, out var queuedCommand))
                return $"Command not found: {commandId}";

            m_logger.LogTrace("⏳ Waiting for command {CommandId} result", commandId);

            try
            {
                var result = await queuedCommand.CompletionSource.Task;
                m_logger.LogTrace("✅ Command {CommandId} result retrieved", commandId);
                return result;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Error getting result for command {CommandId}", commandId);
                throw;
            }
        }

        /// <summary>
        /// Cancels a specific command
        /// </summary>
        /// <param name="commandId">The command ID to cancel</param>
        /// <returns>True if the command was cancelled</returns>
        public bool CancelCommand(string commandId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));

            if (string.IsNullOrWhiteSpace(commandId))
                return false;

            if (!m_activeCommands.TryGetValue(commandId, out var command))
            {
                m_logger.LogWarning("Cannot cancel command {CommandId} - command not found", commandId);
                return false;
            }

            try
            {
                command.CancellationTokenSource.Cancel();
                m_logger.LogInformation("🚫 Cancelled command {CommandId}", commandId);
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error cancelling command {CommandId}", commandId);
                return false;
            }
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

            var cancelledCount = 0;
            var commandIds = m_activeCommands.Keys.ToList();

            foreach (var commandId in commandIds)
            {
                if (CancelCommand(commandId))
                    cancelledCount++;
            }

            m_logger.LogInformation("🚫 Cancelled {Count} commands: {Reason}", cancelledCount, reason ?? "Bulk cancellation");
            return cancelledCount;
        }

        /// <summary>
        /// Gets the status of all commands in the queue
        /// </summary>
        /// <returns>Collection of command status information</returns>
        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_disposed)
                return Enumerable.Empty<(string, string, DateTime, string)>();

            var results = new List<(string, string, DateTime, string)>();

            // Add current command
            var current = m_processor.GetCurrentCommand();
            if (current != null)
            {
                results.Add((current.Id, current.Command, current.QueueTime, "Executing"));
            }

            // Add other active commands
            foreach (var kvp in m_activeCommands)
            {
                var command = kvp.Value;
                if (command != current)
                {
                    var status = command.State switch
                    {
                        CommandState.Queued => "Queued",
                        CommandState.Executing => "Executing",
                        CommandState.Completed => "Completed",
                        CommandState.Cancelled => "Cancelled",
                        CommandState.Failed => "Failed",
                        _ => "Unknown"
                    };
                    results.Add((command.Id, command.Command, command.QueueTime, status));
                }
            }

            return results;
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
        /// Gets the state of a specific command
        /// </summary>
        /// <param name="commandId">The command ID</param>
        /// <returns>The command state, or null if not found</returns>
        public CommandState? GetCommandState(string commandId)
        {
            if (m_disposed || string.IsNullOrWhiteSpace(commandId))
                return null;

            if (m_activeCommands.TryGetValue(commandId, out var command))
                return command.State;

            return null;
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

            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                return new CommandInfo
                {
                    CommandId = command.Id,
                    Command = command.Command,
                    State = command.State,
                    QueueTime = command.QueueTime,
                    Elapsed = DateTime.UtcNow - command.QueueTime,
                    Remaining = TimeSpan.Zero, // Not applicable for basic queue
                    QueuePosition = 0, // Not applicable for basic queue
                    IsCompleted = command.State is CommandState.Completed or CommandState.Failed or CommandState.Cancelled
                };
            }

            return null;
        }

        /// <summary>
        /// Triggers cleanup of completed commands (for testing purposes)
        /// </summary>
        public void TriggerCleanup()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));

            m_processor.TriggerCleanup();
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
                m_logger.LogInformation("🛑 Shutting down CommandQueueService");

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

                // Dispose resources
                m_commandQueue?.Dispose();
                m_serviceCts?.Dispose();

                m_logger.LogInformation("✅ CommandQueueService shutdown complete");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Error during CommandQueueService disposal");
            }
        }
    }
}
