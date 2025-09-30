using mcp_nexus.Constants;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Configuration settings and timeout management for resilient command queue operations
    /// </summary>
    public class ResilientQueueConfiguration
    {
        public TimeSpan DefaultCommandTimeout { get; }
        public TimeSpan ComplexCommandTimeout { get; }
        public TimeSpan MaxCommandTimeout { get; }
        public TimeSpan CleanupInterval { get; }
        public TimeSpan CommandRetentionTime { get; }
        public TimeSpan HeartbeatInterval { get; }
        public TimeSpan RecoveryCheckInterval { get; }

        public ResilientQueueConfiguration(
            TimeSpan? defaultCommandTimeout = null,
            TimeSpan? complexCommandTimeout = null,
            TimeSpan? maxCommandTimeout = null,
            TimeSpan? cleanupInterval = null,
            TimeSpan? commandRetentionTime = null,
            TimeSpan? heartbeatInterval = null,
            TimeSpan? recoveryCheckInterval = null)
        {
            DefaultCommandTimeout = defaultCommandTimeout ?? ApplicationConstants.DefaultCommandTimeout;
            ComplexCommandTimeout = complexCommandTimeout ?? ApplicationConstants.MaxCommandTimeout;
            MaxCommandTimeout = maxCommandTimeout ?? ApplicationConstants.LongRunningCommandTimeout;
            CleanupInterval = cleanupInterval ?? ApplicationConstants.CleanupInterval;
            CommandRetentionTime = commandRetentionTime ?? ApplicationConstants.CommandRetentionTime;
            HeartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(30);
            RecoveryCheckInterval = recoveryCheckInterval ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Determines the appropriate timeout for a command based on its complexity
        /// </summary>
        /// <param name="command">The command to analyze</param>
        /// <returns>The recommended timeout for the command</returns>
        public TimeSpan DetermineCommandTimeout(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return DefaultCommandTimeout;

            var lowerCommand = command.ToLowerInvariant().Trim();

            // Long-running analysis commands
            if (IsLongRunningCommand(lowerCommand))
                return MaxCommandTimeout;

            // Complex commands that may take longer
            if (IsComplexCommand(lowerCommand))
                return ComplexCommandTimeout;

            // Simple command timeout for basic commands like "version"
            return ApplicationConstants.SimpleCommandTimeout;
        }

        /// <summary>
        /// Determines if a command is considered long-running
        /// </summary>
        private static bool IsLongRunningCommand(string command)
        {
            var longRunningPatterns = new[]
            {
                "!analyze", "!heap", "!poolused", "!verifier",
                "!locks", "!deadlock", "!process", "!thread",
                "!handle", "!vm", "!vadump", "!memusage"
            };

            return longRunningPatterns.Any(pattern => command.Contains(pattern));
        }

        /// <summary>
        /// Determines if a command is considered complex
        /// </summary>
        private static bool IsComplexCommand(string command)
        {
            var complexPatterns = new[]
            {
                "!stack", "!clrstack", "!dumpheap", "!gcroot",
                "!finalizequeue", "!syncblk", "!threads",
                "!runaway", "!address", "!peb", "!teb"
            };

            return complexPatterns.Any(pattern => command.Contains(pattern));
        }

        /// <summary>
        /// Generates heartbeat details based on command and elapsed time
        /// </summary>
        /// <param name="command">The command being executed</param>
        /// <param name="elapsed">Time elapsed since command started</param>
        /// <returns>Descriptive heartbeat message</returns>
        public static string GenerateHeartbeatDetails(string command, TimeSpan elapsed)
        {
            var lowerCommand = command.ToLowerInvariant().Trim();

            if (lowerCommand.Contains("!analyze"))
            {
                if (elapsed < TimeSpan.FromMinutes(2))
                    return "Initializing crash analysis...";
                else if (elapsed < TimeSpan.FromMinutes(5))
                    return "Analyzing crash dump structure...";
                else if (elapsed < TimeSpan.FromMinutes(10))
                    return "Processing stack traces and modules...";
                else
                    return "Performing deep analysis (this may take several more minutes)...";
            }

            if (lowerCommand.Contains("!heap"))
            {
                if (elapsed < TimeSpan.FromSeconds(30))
                    return "Scanning heap structures...";
                else if (elapsed < TimeSpan.FromMinutes(2))
                    return "Analyzing heap allocations...";
                else
                    return "Processing large heap data (please wait)...";
            }

            if (lowerCommand.Contains("!dumpheap"))
            {
                if (elapsed < TimeSpan.FromSeconds(15))
                    return "Enumerating managed objects...";
                else if (elapsed < TimeSpan.FromMinutes(1))
                    return "Collecting object statistics...";
                else
                    return "Processing large object heap...";
            }

            // Default heartbeat for other commands
            if (elapsed < TimeSpan.FromSeconds(30))
                return "Executing command...";
            else if (elapsed < TimeSpan.FromMinutes(2))
                return "Processing command (complex operation)...";
            else
                return "Long-running operation in progress...";
        }
    }
}
