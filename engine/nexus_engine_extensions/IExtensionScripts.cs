using Nexus.Engine.Share.Models;

namespace Nexus.Engine.Extensions;

/// <summary>
/// Interface for managing extension scripts.
/// </summary>
public interface IExtensionScripts
{
    /// <summary>
    /// Enqueues an extension script for execution and returns a command ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="extensionName">The name of the extension to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the extension.</param>
    /// <returns>The command ID for tracking this execution.</returns>
    string EnqueueExtensionScript(string sessionId, string extensionName, object? parameters = null);

    /// <summary>
    /// Gets the status of a specific extension command.
    /// </summary>
    /// <param name="commandId">The command ID to query.</param>
    /// <returns>The command info if found, null otherwise.</returns>
    CommandInfo? GetCommandStatus(string commandId);

    /// <summary>
    /// Gets all extension commands for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to query.</param>
    /// <returns>List of command info for all extension commands in the session.</returns>
    List<CommandInfo> GetSessionCommands(string sessionId);

    /// <summary>
    /// Cancels a running extension command.
    /// </summary>
    /// <param name="commandId">The command ID to cancel.</param>
    /// <returns>True if the command was cancelled, false if not found or already completed.</returns>
    bool CancelCommand(string commandId);

    /// <summary>
    /// Closes all extension scripts for a session.
    /// </summary>
    /// <param name="sessionId">The session ID to close extensions for.</param>
    void CloseSession(string sessionId);
}
