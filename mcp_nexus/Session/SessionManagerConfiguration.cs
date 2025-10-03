using Microsoft.Extensions.Options;
using mcp_nexus.Session.Models;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Configuration settings and validation for session management
    /// </summary>
    public class SessionManagerConfiguration
    {
        /// <summary>
        /// Gets the session configuration settings.
        /// </summary>
        public SessionConfiguration Config { get; }
        
        /// <summary>
        /// Gets the CDB session options.
        /// </summary>
        public CdbSessionOptions CdbOptions { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionManagerConfiguration"/> class.
        /// </summary>
        /// <param name="config">Optional session configuration options.</param>
        /// <param name="cdbOptions">Optional CDB session options.</param>
        public SessionManagerConfiguration(
            IOptions<SessionConfiguration>? config = null,
            IOptions<CdbSessionOptions>? cdbOptions = null)
        {
            Config = config?.Value ?? new SessionConfiguration();
            CdbOptions = cdbOptions?.Value ?? new CdbSessionOptions();
        }

        /// <summary>
        /// Validates session creation parameters
        /// </summary>
        /// <param name="dumpPath">Path to the dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        /// <returns>Validation result</returns>
        public (bool IsValid, string? ErrorMessage) ValidateSessionCreation(string dumpPath, string? symbolsPath = null)
        {
            if (dumpPath == null)
                return (false, "Dump path cannot be null");

            if (string.IsNullOrWhiteSpace(dumpPath))
                return (false, "Dump path cannot be empty or whitespace");

            if (!File.Exists(dumpPath))
                return (false, $"Dump file not found: {dumpPath}");

            // Optional: Validate symbols path if provided
            if (!string.IsNullOrWhiteSpace(symbolsPath) && !Directory.Exists(symbolsPath))
                return (false, $"Symbols directory not found: {symbolsPath}");

            return (true, null);
        }

        /// <summary>
        /// Generates a unique session ID with enhanced entropy
        /// </summary>
        /// <param name="sessionCounter">Atomic session counter</param>
        /// <returns>Unique session ID</returns>
        public static string GenerateSessionId(long sessionCounter)
        {
            var guid = Guid.NewGuid().ToString("N");
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var processId = Environment.ProcessId;
            return $"sess-{sessionCounter:D6}-{guid[..8]}-{timestamp:X8}-{processId:X4}";
        }

        /// <summary>
        /// Constructs the CDB target string from dump and symbols paths
        /// </summary>
        /// <param name="dumpPath">Path to the dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        /// <returns>CDB target string</returns>
        public string ConstructCdbTarget(string dumpPath, string? symbolsPath = null)
        {
            var target = $"-z \"{dumpPath}\"";

            if (!string.IsNullOrWhiteSpace(symbolsPath))
            {
                target += $" -y \"{symbolsPath}\"";
            }

            return target;
        }

        /// <summary>
        /// Determines if a session should be considered expired
        /// </summary>
        /// <param name="lastActivity">Last activity timestamp</param>
        /// <returns>True if the session is expired</returns>
        public bool IsSessionExpired(DateTime lastActivity)
        {
            return DateTime.UtcNow - lastActivity > Config.SessionTimeout;
        }

        /// <summary>
        /// Checks if the session limit would be exceeded
        /// </summary>
        /// <param name="currentSessionCount">Current number of sessions</param>
        /// <returns>True if limit would be exceeded</returns>
        public bool WouldExceedSessionLimit(int currentSessionCount)
        {
            return currentSessionCount >= Config.MaxConcurrentSessions;
        }

        /// <summary>
        /// Gets the cleanup interval for expired sessions
        /// </summary>
        /// <returns>Cleanup interval</returns>
        public TimeSpan GetCleanupInterval()
        {
            return Config.CleanupInterval;
        }

        /// <summary>
        /// Gets the session timeout duration
        /// </summary>
        /// <returns>Session timeout</returns>
        public TimeSpan GetSessionTimeout()
        {
            return Config.SessionTimeout;
        }
    }
}
