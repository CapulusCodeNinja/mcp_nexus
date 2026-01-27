using WinAiDbg.Engine.Share.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.Protocol.Services;

namespace WinAiDbg.Protocol.Utilities;

/// <summary>
/// Provides shared, tool-level input validation helpers for producing actionable MCP errors.
/// </summary>
internal static class ToolInputValidator
{
    /// <summary>
    /// Ensures that a required string argument is non-empty.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="argumentName">The argument name.</param>
    /// <exception cref="McpToolUserInputException">Thrown when the value is null/empty/whitespace.</exception>
    public static void EnsureNonEmpty(string value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new McpToolUserInputException($"Invalid `{argumentName}`: expected a non-empty string.");
        }
    }

    /// <summary>
    /// Ensures that a dump file path exists on the server.
    /// </summary>
    /// <param name="dumpPath">The dump file path.</param>
    /// <param name="fileSystem">File system abstraction.</param>
    /// <exception cref="McpToolUserInputException">Thrown when the file does not exist.</exception>
    public static void EnsureDumpFileExists(string dumpPath, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        if (!fileSystem.FileExists(dumpPath))
        {
            throw new McpToolUserInputException(
                $"Invalid `dumpPath`: file not found at `{dumpPath}`. Ensure the path exists on the server and is accessible.");
        }
    }

    /// <summary>
    /// Ensures that a session exists and returns its current state.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The current session state.</returns>
    /// <exception cref="McpToolUserInputException">Thrown when the session does not exist or the engine is unavailable.</exception>
    public static SessionState EnsureSessionExists(string sessionId)
    {
        EnsureNonEmpty(sessionId, "sessionId");

        try
        {
            var engine = EngineService.Get();
            var state = engine.GetSessionState(sessionId);
            return state ?? throw new McpToolUserInputException(
                $"Invalid `sessionId`: `{sessionId}` was not found. Call `winaidbg_open_dump_analyze_session` and use the returned sessionId.");
        }
        catch (NullReferenceException ex)
        {
            throw new McpToolUserInputException(
                "Server debug engine is not initialized. Ensure the WinAiDbg service is started and configured.",
                ex);
        }
    }

    /// <summary>
    /// Ensures that a session exists and is currently active.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <exception cref="McpToolUserInputException">Thrown when the session does not exist or is not active.</exception>
    public static void EnsureSessionIsActive(string sessionId)
    {
        var state = EnsureSessionExists(sessionId);
        if (state != SessionState.Active)
        {
            throw new McpToolUserInputException(
                $"Invalid `sessionId`: `{sessionId}` is not active (current state: `{state}`). Open a new session and retry.");
        }
    }
}

