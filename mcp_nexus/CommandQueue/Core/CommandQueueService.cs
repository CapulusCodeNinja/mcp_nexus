using System.Collections.Concurrent;
using mcp_nexus.Debugger;

namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Refactored basic command queue service that orchestrates focused components
    /// </summary>
    public class CommandQueueService : IDisposable, ICommandQueueService
    {
        private readonly ILogger<CommandQueueService> m_Logger;
        private readonly BlockingCollection<QueuedCommand> m_CommandQueue;
        private readonly CancellationTokenSource m_ServiceCts = new();
        private readonly Task m_ProcessingTask;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_ActiveCommands = new();
        private bool m_Disposed;

        // Focused components
        private readonly BasicQueueConfiguration m_Config;
        private readonly BasicCommandProcessor m_Processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandQueueService"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording queue operations.</param>
        /// <param name="loggerFactory">The logger factory for creating component loggers.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public CommandQueueService(ICdbSession cdbSession, ILogger<CommandQueueService> logger, ILoggerFactory loggerFactory)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create focused components
            m_Config = new BasicQueueConfiguration();
            m_Processor = new BasicCommandProcessor(cdbSession, loggerFactory.CreateLogger<BasicCommandProcessor>(), m_Config, m_ActiveCommands);

            // Initialize command queue
            m_CommandQueue = [];

            m_Logger.LogInformation("🚀 CommandQueueService initializing with focused components");

            // Start the background processing task
            try
            {
                m_ProcessingTask = Task.Run(() => m_Processor.ProcessCommandQueueAsync(m_CommandQueue, m_ServiceCts.Token), m_ServiceCts.Token);
                m_Logger.LogInformation("✅ CommandQueueService background task started successfully");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "❌ Failed to start CommandQueueService background task");
                throw;
            }

            m_Logger.LogInformation("🎯 CommandQueueService fully initialized with focused components");
        }

        /// <summary>
        /// Queues a command for execution
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The unique command ID</returns>
        public string QueueCommand(string command)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var commandId = Guid.NewGuid().ToString();
            m_Logger.LogInformation("🔄 QueueCommand START: {CommandId} for command: {Command}", commandId, command);

            var tcs = new TaskCompletionSource<string>();
            var cts = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, DateTime.Now, tcs, cts);

            m_Logger.LogInformation("📝 Adding to activeCommands dictionary: {CommandId}", commandId);
            m_ActiveCommands[commandId] = queuedCommand;

            m_Logger.LogInformation("📋 Adding command to queue: {CommandId}", commandId);
            m_CommandQueue.Add(queuedCommand);

            m_Logger.LogDebug("📊 Queue depth: {QueueDepth}", m_CommandQueue.Count);
            m_Logger.LogInformation("✅ QueueCommand END: {CommandId}", commandId);

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
                throw new ObjectDisposedException(nameof(CommandQueueService));

            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

            if (!m_ActiveCommands.TryGetValue(commandId, out var queuedCommand))
                return $"Command not found: {commandId}";

            m_Logger.LogTrace("⏳ Waiting for command {CommandId} result", commandId);

            try
            {
                var result = await (queuedCommand.CompletionSource?.Task ?? Task.FromResult(string.Empty));
                m_Logger.LogTrace("✅ Command {CommandId} result retrieved", commandId);
                return result;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "❌ Error getting result for command {CommandId}", commandId);
                throw;
            }
        }

        /// <summary>
        /// Gets the cached result with metadata for a command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>A task that represents the asynchronous operation and contains the cached result with metadata.</returns>
        public async Task<CachedCommandResult?> GetCachedResultWithMetadata(string commandId)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));

            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            // This implementation doesn't have a cache, so return null
            return await Task.FromResult<CachedCommandResult?>(null);
        }

        /// <summary>
        /// Cancels a specific command
        /// </summary>
        /// <param name="commandId">The command ID to cancel</param>
        /// <returns>True if the command was cancelled</returns>
        public bool CancelCommand(string commandId)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));

            if (string.IsNullOrWhiteSpace(commandId))
                return false;

            if (!m_ActiveCommands.TryGetValue(commandId, out var command))
            {
                m_Logger.LogWarning("Cannot cancel command {CommandId} - command not found", commandId);
                return false;
            }

            try
            {
                command.CancellationTokenSource?.Cancel();
                m_Logger.LogInformation("🚫 Cancelled command {CommandId}", commandId);
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error cancelling command {CommandId}", commandId);
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
            if (m_Disposed)
                return 0;

            var cancelledCount = 0;
            // Enumerate snapshot of keys from ConcurrentDictionary without materializing a list
            foreach (var commandId in m_ActiveCommands.Keys)
            {
                if (CancelCommand(commandId))
                    cancelledCount++;
            }

            m_Logger.LogInformation("🚫 Cancelled {Count} commands: {Reason}", cancelledCount, reason ?? "Bulk cancellation");
            return cancelledCount;
        }

        /// <summary>
        /// Gets the status of all commands in the queue
        /// </summary>
        /// <returns>Collection of command status information</returns>
        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_Disposed)
                return [];

            var results = new List<(string, string, DateTime, string)>();

            // Add current command
            var current = m_Processor.GetCurrentCommand();
            if (current != null)
            {
                results.Add((current.Id ?? string.Empty, current.Command ?? string.Empty, current.QueueTime, "Executing"));
            }

            // Add other active commands
            foreach (var kvp in m_ActiveCommands)
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
                    results.Add((command.Id ?? string.Empty, command.Command ?? string.Empty, command.QueueTime, status));
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
            if (m_Disposed)
                return null;

            return m_Processor.GetCurrentCommand();
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

            if (m_ActiveCommands.TryGetValue(commandId, out var command))
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
            if (m_Disposed || string.IsNullOrWhiteSpace(commandId))
                return null;

            if (m_ActiveCommands.TryGetValue(commandId, out var command))
            {
                return new CommandInfo(
                    command.Id ?? string.Empty,
                    command.Command ?? string.Empty,
                    command.State,
                    command.QueueTime,
                    0
                )
                {
                    Elapsed = DateTime.Now - command.QueueTime,
                    Remaining = TimeSpan.Zero, // Not applicable for basic queue
                    IsCompleted = command.State is CommandState.Completed or CommandState.Failed or CommandState.Cancelled
                };
            }

            return null;
        }

        /// <summary>
        /// Triggers cleanup of completed commands (for testing purposes).
        /// </summary>
        public void TriggerCleanup()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CommandQueueService));

            m_Processor.TriggerCleanup();
        }

        /// <summary>
        /// Disposes the service and all resources
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            try
            {
                m_Logger.LogInformation("🛑 Shutting down CommandQueueService");

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

                // Dispose resources
                m_CommandQueue?.Dispose();
                m_ServiceCts?.Dispose();

                m_Logger.LogInformation("✅ CommandQueueService shutdown complete");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "💥 Error during CommandQueueService disposal");
            }
        }
    }
}
