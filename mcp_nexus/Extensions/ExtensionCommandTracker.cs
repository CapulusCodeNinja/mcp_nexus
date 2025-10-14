using System.Collections.Concurrent;
using mcp_nexus.CommandQueue;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Tracks extension command executions similar to regular command tracking.
    /// </summary>
    public interface IExtensionCommandTracker
    {
        /// <summary>
        /// Tracks a new extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="extensionName">The name of the extension.</param>
        /// <param name="parameters">Parameters passed to the extension.</param>
        void TrackExtension(string commandId, string sessionId, string extensionName, object? parameters);

        /// <summary>
        /// Updates the state of an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="state">The new state.</param>
        void UpdateState(string commandId, CommandState state);

        /// <summary>
        /// Updates the progress message for an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="progressMessage">The progress message.</param>
        void UpdateProgress(string commandId, string progressMessage);

        /// <summary>
        /// Increments the callback count for an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        void IncrementCallbackCount(string commandId);

        /// <summary>
        /// Stores the result of an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="result">The extension result.</param>
        void StoreResult(string commandId, ExtensionResult result);

        /// <summary>
        /// Gets information about an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <returns>Extension command information, or null if not found.</returns>
        ExtensionCommandInfo? GetCommandInfo(string commandId);

        /// <summary>
        /// Gets the result of an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <returns>The command result, or null if not found.</returns>
        ICommandResult? GetCommandResult(string commandId);

        /// <summary>
        /// Gets all extension commands for a session.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <returns>Collection of extension command information.</returns>
        IEnumerable<ExtensionCommandInfo> GetSessionCommands(string sessionId);

        /// <summary>
        /// Removes tracking information for a command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        void RemoveCommand(string commandId);

        /// <summary>
        /// Removes all tracking information for a session.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        void RemoveSessionCommands(string sessionId);
    }

    /// <summary>
    /// Implementation of extension command tracker.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ExtensionCommandTracker"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public class ExtensionCommandTracker(ILogger<ExtensionCommandTracker> logger) : IExtensionCommandTracker
    {
        private readonly ILogger<ExtensionCommandTracker> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentDictionary<string, ExtensionCommandInfo> m_Commands = new();
        private readonly ConcurrentDictionary<string, ICommandResult> m_Results = new();

        /// <summary>
        /// Tracks a new extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="extensionName">The name of the extension.</param>
        /// <param name="parameters">Parameters passed to the extension.</param>
        public void TrackExtension(string commandId, string sessionId, string extensionName, object? parameters)
        {
            var info = new ExtensionCommandInfo
            {
                Id = commandId,
                SessionId = sessionId,
                Command = extensionName,
                ExtensionName = extensionName,
                Parameters = parameters,
                State = CommandState.Queued,
                QueuedAt = DateTime.UtcNow,
                QueuePosition = -1,
                CallbackCount = 0,
                Elapsed = TimeSpan.Zero,
                Remaining = TimeSpan.Zero,
                IsCompleted = false
            };

            m_Commands[commandId] = info;
            m_Logger.LogDebug("Tracking extension command {CommandId} for session {SessionId}", commandId, sessionId);
        }

        /// <summary>
        /// Updates the state of an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="state">The new state.</param>
        public void UpdateState(string commandId, CommandState state)
        {
            if (m_Commands.TryGetValue(commandId, out var info))
            {
                info.State = state;

                if (state == CommandState.Executing && !info.StartedAt.HasValue)
                {
                    info.StartedAt = DateTime.UtcNow;
                }
                else if (state == CommandState.Completed || state == CommandState.Failed || state == CommandState.Cancelled)
                {
                    info.CompletedAt = DateTime.UtcNow;
                    info.IsCompleted = true;
                }

                // Update timing
                info.Elapsed = DateTime.UtcNow - info.QueuedAt;

                m_Logger.LogDebug("Updated extension command {CommandId} state to {State}", commandId, state);
            }
        }

        /// <summary>
        /// Updates the progress message for an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="progressMessage">The progress message.</param>
        public void UpdateProgress(string commandId, string progressMessage)
        {
            if (m_Commands.TryGetValue(commandId, out var info))
            {
                info.ProgressMessage = progressMessage;
                m_Logger.LogDebug("Updated extension command {CommandId} progress: {Progress}",
                    commandId, progressMessage);
            }
        }

        /// <summary>
        /// Increments the callback count for an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        public void IncrementCallbackCount(string commandId)
        {
            if (m_Commands.TryGetValue(commandId, out var info))
            {
                info.CallbackCount++;
                m_Logger.LogDebug("Extension command {CommandId} callback count: {Count}",
                    commandId, info.CallbackCount);
            }
        }

        /// <summary>
        /// Stores the result of an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="result">The extension result.</param>
        public void StoreResult(string commandId, ExtensionResult result)
        {
            var commandResult = new CommandResult(
                isSuccess: result.Success,
                output: result.Output ?? string.Empty,
                errorMessage: result.Error);

            m_Results[commandId] = commandResult;
            UpdateState(commandId, result.Success ? CommandState.Completed : CommandState.Failed);

            m_Logger.LogInformation("Stored result for extension command {CommandId}, success: {Success}",
                commandId, result.Success);
        }

        /// <summary>
        /// Gets information about an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <returns>Extension command information, or null if not found.</returns>
        public ExtensionCommandInfo? GetCommandInfo(string commandId)
        {
            return m_Commands.TryGetValue(commandId, out var info) ? info : null;
        }

        /// <summary>
        /// Gets the result of an extension command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <returns>The command result, or null if not found.</returns>
        public ICommandResult? GetCommandResult(string commandId)
        {
            return m_Results.TryGetValue(commandId, out var result) ? result : null;
        }

        /// <summary>
        /// Gets all extension commands for a session.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <returns>Collection of extension command information.</returns>
        public IEnumerable<ExtensionCommandInfo> GetSessionCommands(string sessionId)
        {
            return m_Commands.Values.Where(c => c.SessionId == sessionId).ToList();
        }

        /// <summary>
        /// Removes tracking information for a command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        public void RemoveCommand(string commandId)
        {
            m_Commands.TryRemove(commandId, out _);
            m_Results.TryRemove(commandId, out _);
            m_Logger.LogDebug("Removed extension command {CommandId}", commandId);
        }

        /// <summary>
        /// Removes all tracking information for a session.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        public void RemoveSessionCommands(string sessionId)
        {
            var sessionCommands = m_Commands.Values.Where(c => c.SessionId == sessionId).ToList();

            foreach (var cmd in sessionCommands)
            {
                m_Commands.TryRemove(cmd.Id, out _);
                m_Results.TryRemove(cmd.Id, out _);
            }

            m_Logger.LogInformation("Removed {Count} extension commands for session {SessionId}",
                sessionCommands.Count, sessionId);
        }
    }

    /// <summary>
    /// Information about an extension command execution.
    /// </summary>
    public class ExtensionCommandInfo : ICommandInfo
    {
        /// <summary>
        /// Unique command ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Unique command ID (ICommandInfo compatibility).
        /// </summary>
        public string CommandId => Id;

        /// <summary>
        /// Session ID this command belongs to.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The command string (extension name).
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Current state of the command.
        /// </summary>
        public CommandState State { get; set; }

        /// <summary>
        /// When the command was queued.
        /// </summary>
        public DateTime QueuedAt { get; set; }

        /// <summary>
        /// When the command was queued (ICommandInfo compatibility).
        /// </summary>
        public DateTime QueueTime => QueuedAt;

        /// <summary>
        /// When the command started executing.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the command completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Queue position (always -1 for extensions as they don't use queue).
        /// </summary>
        public int QueuePosition { get; set; }

        /// <summary>
        /// Elapsed time since queuing.
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// Estimated remaining time (not used for extensions).
        /// </summary>
        public TimeSpan Remaining { get; set; }

        /// <summary>
        /// Whether the command is completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Extension name.
        /// </summary>
        public string ExtensionName { get; set; } = string.Empty;

        /// <summary>
        /// Parameters passed to the extension.
        /// </summary>
        public object? Parameters { get; set; }

        /// <summary>
        /// Number of callbacks made by the extension.
        /// </summary>
        public int CallbackCount { get; set; }

        /// <summary>
        /// Current progress message.
        /// </summary>
        public string? ProgressMessage { get; set; }

        /// <summary>
        /// Updates timing information (ICommandInfo compatibility).
        /// </summary>
        public void UpdateTiming(TimeSpan elapsed, TimeSpan remaining)
        {
            Elapsed = elapsed;
            Remaining = remaining;
        }

        /// <summary>
        /// Marks the command as completed (ICommandInfo compatibility).
        /// </summary>
        public void MarkCompleted()
        {
            IsCompleted = true;
            CompletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the queue position (ICommandInfo compatibility).
        /// </summary>
        public void UpdateQueuePosition(int position)
        {
            QueuePosition = position;
        }
    }
}

