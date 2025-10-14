using System.Collections.Concurrent;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Manages command tracking, status, and performance metrics
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CommandTracker"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance for recording tracking operations.</param>
    /// <param name="config">The command queue configuration settings.</param>
    /// <param name="commandQueue">The blocking collection for queued commands.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
    public class CommandTracker(ILogger logger, CommandQueueConfiguration config, BlockingCollection<QueuedCommand> commandQueue)
    {
        private readonly ILogger m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly CommandQueueConfiguration m_config = config ?? throw new ArgumentNullException(nameof(config));

        // Thread-safe collections and counters
        private readonly ConcurrentDictionary<string, QueuedCommand> m_activeCommands = new();
        private readonly BlockingCollection<QueuedCommand> m_commandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
        private volatile QueuedCommand? m_currentCommand;

        // Performance counters
        private long m_commandCounter = 0;
        private long m_completedCommands = 0;
        private long m_failedCommands = 0;
        private long m_cancelledCommands = 0;

        /// <summary>
        /// Gets the current command being executed
        /// </summary>
        public QueuedCommand? GetCurrentCommand() => m_currentCommand;

        /// <summary>
        /// Sets the current command being executed
        /// </summary>
        public void SetCurrentCommand(QueuedCommand? command) => m_currentCommand = command;

        /// <summary>
        /// Generates a unique command ID and increments the counter.
        /// </summary>
        /// <returns>A unique command ID string.</returns>
        public string GenerateCommandId()
        {
            var commandNumber = Interlocked.Increment(ref m_commandCounter);
            return m_config.GenerateCommandId(commandNumber);
        }

        /// <summary>
        /// Adds a command to the tracking dictionary.
        /// </summary>
        /// <param name="commandId">The unique command ID.</param>
        /// <param name="command">The command to track.</param>
        /// <returns>True if the command was added successfully; otherwise, false.</returns>
        public bool TryAddCommand(string commandId, QueuedCommand command)
        {
            return m_activeCommands.TryAdd(commandId, command);
        }

        /// <summary>
        /// Updates the state of an existing command in a thread-safe way.
        /// </summary>
        /// <param name="commandId">The unique command ID.</param>
        /// <param name="newState">The new state to set for the command.</param>
        public void UpdateState(string commandId, CommandState newState)
        {
            if (string.IsNullOrWhiteSpace(commandId)) return;

            if (m_activeCommands.TryGetValue(commandId, out var existing))
            {
                var updated = existing.WithState(newState);
                m_activeCommands[commandId] = updated;
                m_logger.LogTrace("Command {CommandId} state updated to {State}", commandId, newState);
            }
        }

        /// <summary>
        /// Removes a command from the tracking dictionary.
        /// </summary>
        /// <param name="commandId">The unique command ID.</param>
        /// <param name="command">The removed command, if found.</param>
        /// <returns>True if the command was removed successfully; otherwise, false.</returns>
        public bool TryRemoveCommand(string commandId, out QueuedCommand? command)
        {
            return m_activeCommands.TryRemove(commandId, out command);
        }

        /// <summary>
        /// Gets a command by ID
        /// </summary>
        public QueuedCommand? GetCommand(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            return m_activeCommands.TryGetValue(commandId, out var command) ? command : null;
        }

        /// <summary>
        /// Gets the state of a command
        /// </summary>
        public CommandState? GetCommandState(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            return GetCommand(commandId)?.State;
        }

        /// <summary>
        /// Gets detailed command information
        /// </summary>
        public CommandInfo? GetCommandInfo(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            var command = GetCommand(commandId);
            if (command == null) return null;

            var elapsed = DateTime.UtcNow - command.QueueTime;
            var queuePosition = GetQueuePosition(commandId);
            var remaining = CalculateRemainingTime(queuePosition);

            return new CommandInfo(
                command.Id ?? string.Empty,
                command.Command ?? string.Empty,
                command.State,
                command.QueueTime,
                queuePosition
            )
            {
                Elapsed = elapsed,
                Remaining = remaining,
                IsCompleted = IsCommandCompleted(command.State)
            };
        }

        /// <summary>
        /// Gets the position of a command in the queue.
        /// </summary>
        /// <param name="commandId">The unique command ID.</param>
        /// <returns>The queue position of the command, or -1 if not found.</returns>
        public int GetQueuePosition(string commandId)
        {
            try
            {
                var currentCmd = m_currentCommand;
                if (currentCmd?.Id == commandId)
                    return 0; // Currently executing

                // Check if command is in queue
                var queuedCommands = m_commandQueue.ToArray();
                for (int i = 0; i < queuedCommands.Length; i++)
                {
                    if (queuedCommands[i].Id == commandId)
                        return i + 1; // Position in queue (1-based)
                }

                // Command not found in queue - might be completed or cancelled
                return -1;
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error calculating queue position for command {CommandId}", commandId);
                return -1;
            }
        }

        /// <summary>
        /// Gets the current queue status
        /// </summary>
        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            var results = new List<(string, string, DateTime, string)>();

            // Add current command
            var current = m_currentCommand;
            if (current != null)
            {
                results.Add((current.Id ?? string.Empty, current.Command ?? string.Empty, current.QueueTime, "Executing"));
            }

            // Add queued commands
            try
            {
                var queuedCommands = m_commandQueue.ToArray();
                for (int i = 0; i < queuedCommands.Length; i++)
                {
                    var cmd = queuedCommands[i];
                    results.Add((cmd.Id ?? string.Empty, cmd.Command ?? string.Empty, cmd.QueueTime, $"Queued (position {i + 1})"));
                }
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error getting queue status");
            }

            return results;
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        public (long Total, long Completed, long Failed, long Cancelled) GetPerformanceStats()
        {
            var completed = Interlocked.Read(ref m_completedCommands);
            var failed = Interlocked.Read(ref m_failedCommands);
            var cancelled = Interlocked.Read(ref m_cancelledCommands);

            return (
                Total: completed + failed + cancelled, // Total = sum of completed operations, not queued
                Completed: completed,
                Failed: failed,
                Cancelled: cancelled
            );
        }

        /// <summary>
        /// Increments the completed commands counter
        /// </summary>
        public void IncrementCompleted() => Interlocked.Increment(ref m_completedCommands);

        /// <summary>
        /// Increments the failed commands counter
        /// </summary>
        public void IncrementFailed() => Interlocked.Increment(ref m_failedCommands);

        /// <summary>
        /// Increments the cancelled commands counter
        /// </summary>
        public void IncrementCancelled() => Interlocked.Increment(ref m_cancelledCommands);

        /// <summary>
        /// Cancels all commands with an optional reason.
        /// </summary>
        /// <param name="reason">The reason for cancellation.</param>
        /// <returns>The number of commands that were cancelled.</returns>
        public int CancelAllCommands(string? reason = null)
        {
            var cancelledCount = 0;
            var reasonText = reason ?? "Service shutdown";

            m_logger.LogWarning("Cancelling ALL commands. Reason: {Reason}", reasonText);

            // Cancel all active commands
            foreach (var kvp in m_activeCommands.ToArray())
            {
                var command = kvp.Value;
                try
                {
                    if (command.CancellationTokenSource?.Token.IsCancellationRequested == false)
                    {
                        command.CancellationTokenSource.Cancel();
                        command.CompletionSource?.TrySetResult($"Command cancelled: {reasonText}");
                        // update state to cancelled
                        UpdateState(command.Id ?? string.Empty, CommandState.Cancelled);
                        cancelledCount++;
                        IncrementCancelled();
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Error cancelling command {CommandId}", command.Id);
                }
            }

            m_logger.LogInformation("Cancelled {Count} command(s)", cancelledCount);
            return cancelledCount;
        }

        /// <summary>
        /// Calculates the estimated remaining time for a command based on its queue position.
        /// </summary>
        /// <param name="queuePosition">The position of the command in the queue.</param>
        /// <returns>An estimated time remaining for the command to be processed.</returns>
        private static TimeSpan CalculateRemainingTime(int queuePosition)
        {
            if (queuePosition <= 0) return TimeSpan.Zero;

            // Estimate based on position and average command time
            var estimatedMinutesPerCommand = 2; // Conservative estimate
            return TimeSpan.FromMinutes(queuePosition * estimatedMinutesPerCommand);
        }

        /// <summary>
        /// Determines if a command is in a completed state.
        /// </summary>
        /// <param name="state">The command state to check.</param>
        /// <returns><c>true</c> if the command is completed, failed, or cancelled; otherwise, <c>false</c>.</returns>
        private static bool IsCommandCompleted(CommandState state)
        {
            return state is CommandState.Completed or CommandState.Failed or CommandState.Cancelled;
        }
    }
}

