using Microsoft.Extensions.Options;
using mcp_nexus.Session.Models;
using NLog;

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
        /// Generates a session-specific CDB log file path based on the current log configuration and service mode.
        /// </summary>
        /// <param name="sessionId">The unique session identifier for the CDB log file.</param>
        /// <returns>The full path to the session-specific CDB log file.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the NLog file target is not found, the log directory is not set, or the session ID is not provided.
        /// </exception>
        private string GetCdbSessionBasedLogPath(string sessionId)
        {
            var fileTarget = LogManager.Configuration?.FindTargetByName("mainFile") as NLog.Targets.FileTarget;
            if (fileTarget == null)
            {
                throw new InvalidOperationException("File target not found in NLog configuration");
            }

            var logEventInfo = new LogEventInfo(NLog.LogLevel.Info, "", "");
            var originalPath = fileTarget.FileName.Render(logEventInfo);

            string? directory = Path.GetDirectoryName(originalPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);

            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Directory is not set");
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                throw new InvalidOperationException("Session ID is not set");
            }

            string sessionsDirectory;
            if (Config.ServiceMode)
            {
                // Service mode: C:\ProgramData\MCP-Nexus\Sessions\
                sessionsDirectory = Path.Combine(Path.GetDirectoryName(directory)!, "Sessions");
            }
            else
            {
                // Other modes: C:\ProgramData\MCP-Nexus\Logs\Sessions\
                sessionsDirectory = Path.Combine(directory, "Sessions");
            }

            // Ensure the Sessions directory exists
            Directory.CreateDirectory(sessionsDirectory);

            var newFileNameWithoutExtension = $"cdb_{sessionId}";
            var newFileName = Path.ChangeExtension(newFileNameWithoutExtension, ".log");
            var newPath = Path.Combine(sessionsDirectory, newFileName);

            return newPath;
        }

        /// <summary>
        /// Constructs the CDB target string from dump and symbols paths
        /// </summary>
        /// <param name="dumpPath">Path to the dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        /// <returns>CDB target string</returns>
        public string ConstructCdbTarget(string sessionId, string dumpPath, string? symbolsPath = null)
        {
            var target = $"-z \"{dumpPath}\"";

            if (!string.IsNullOrWhiteSpace(symbolsPath))
            {
                target += $" -y \"{symbolsPath}\"";
            }
            
            var cdbLogFilePath = GetCdbSessionBasedLogPath(sessionId);
            target += $" -lines -logau  \"{cdbLogFilePath}\"";

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
