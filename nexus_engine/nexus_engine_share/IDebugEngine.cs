using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;

namespace Nexus.Engine.Share;

/// <summary>
/// Main interface for the debug engine that manages CDB sessions and command execution.
/// </summary>
public interface IDebugEngine : IDisposable
{
    /// <summary>
    /// Creates a new debug session for analyzing a dump file.
    /// </summary>
    /// <param name="dumpFilePath">The path to the dump file to analyze.</param>
    /// <param name="symbolPath">Optional symbol path for debugging symbols.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the session ID.</returns>
    /// <exception cref="ArgumentException">Thrown when dumpFilePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the dump file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of concurrent sessions is reached.</exception>
    Task<string> CreateSessionAsync(string dumpFilePath, string? symbolPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a debug session and cleans up resources.
    /// </summary>
    /// <param name="sessionId">The session ID to close.</param>
    /// <param name="closeReason">Optional reason for session closure (e.g., "IdleTimeout", "UserRequest").</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    Task CloseSessionAsync(string sessionId, string? closeReason = null);

    /// <summary>
    /// Checks if a session is currently active.
    /// </summary>
    /// <param name="sessionId">The session ID to check.</param>
    /// <returns>True if the session is active, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    bool IsSessionActive(string sessionId);

    /// <summary>
    /// Enqueues a command for execution in the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID to execute the command in.</param>
    /// <param name="command">The debug command to execute.</param>
    /// <returns>The unique command ID for tracking the command.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or command is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not active or the command queue is full.</exception>
    string EnqueueCommand(string sessionId, string command);


    /// <summary>
    /// Enqueues an extension script for execution in the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID to execute the extension script in.</param>
    /// <param name="extensionName">The name of the extension to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the extension.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unique command ID for tracking the extension execution.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or extensionName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not active or the extension is not found.</exception>
    Task<string> EnqueueExtensionScriptAsync(string sessionId, string extensionName, object? parameters = null);

    /// <summary>
    /// Gets the information about a command.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID to get the information for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command information.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or commandId is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not active.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the command is not found.</exception>
    Task<CommandInfo> GetCommandInfoAsync(string sessionId, string commandId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current information about a command without waiting for completion.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID to get the information for.</param>
    /// <returns>The command information, or null if the command is not found.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or commandId is null or empty.</exception>
    CommandInfo? GetCommandInfo(string sessionId, string commandId);

    /// <summary>
    /// Gets the information about all commands in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>A dictionary of command IDs to their information.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    Dictionary<string, CommandInfo> GetAllCommandInfos(string sessionId);

    /// <summary>
    /// Cancels a queued or executing command.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID to cancel.</param>
    /// <returns>True if the command was found and cancelled, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or commandId is null or empty.</exception>
    bool CancelCommand(string sessionId, string commandId);

    /// <summary>
    /// Cancels all commands in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="reason">Optional reason for cancellation.</param>
    /// <returns>The number of commands that were cancelled.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    int CancelAllCommands(string sessionId, string? reason = null);

    /// <summary>
    /// Gets the current state of a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The session state, or null if the session is not found.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    SessionState? GetSessionState(string sessionId);

    /// <summary>
    /// Occurs when a command's state changes.
    /// </summary>
    event EventHandler<CommandStateChangedEventArgs>? CommandStateChanged;

    /// <summary>
    /// Occurs when a session's state changes.
    /// </summary>
    event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;
}

