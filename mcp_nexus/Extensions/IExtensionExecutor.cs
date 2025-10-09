namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Interface for executing extension scripts.
    /// </summary>
    public interface IExtensionExecutor
    {
        /// <summary>
        /// Executes an extension script asynchronously.
        /// </summary>
        /// <param name="extensionName">The name of the extension to execute.</param>
        /// <param name="sessionId">The session ID this extension is running for.</param>
        /// <param name="parameters">Parameters to pass to the extension (will be serialized as JSON).</param>
        /// <param name="commandId">The command ID for tracking this extension execution.</param>
        /// <param name="progressCallback">Optional callback for progress updates.</param>
        /// <param name="cancellationToken">Cancellation token to stop execution.</param>
        /// <returns>The extension execution result.</returns>
        Task<ExtensionResult> ExecuteAsync(
            string extensionName,
            string sessionId,
            object? parameters,
            string commandId,
            Action<string>? progressCallback = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kills a running extension by command ID.
        /// </summary>
        /// <param name="commandId">The command ID of the extension to kill.</param>
        /// <returns>True if the extension was killed, false if not found or already completed.</returns>
        bool KillExtension(string commandId);

        /// <summary>
        /// Gets information about a running extension.
        /// </summary>
        /// <param name="commandId">The command ID of the extension.</param>
        /// <returns>Extension process information, or null if not found.</returns>
        ExtensionProcessInfo? GetExtensionInfo(string commandId);
    }

    /// <summary>
    /// Information about a running extension process.
    /// </summary>
    public class ExtensionProcessInfo
    {
        /// <summary>
        /// Command ID of the extension.
        /// </summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Extension name.
        /// </summary>
        public string ExtensionName { get; set; } = string.Empty;

        /// <summary>
        /// Session ID the extension is running for.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Process ID of the extension.
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// When the extension started.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Whether the extension is still running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Number of callbacks made so far.
        /// </summary>
        public int CallbackCount { get; set; }
    }
}

