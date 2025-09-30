namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Configuration settings for command queue operations
    /// </summary>
    public class CommandQueueConfiguration
    {
        public TimeSpan DefaultCommandTimeout { get; }
        public TimeSpan HeartbeatInterval { get; }
        public TimeSpan ShutdownTimeout { get; }
        public TimeSpan ForceShutdownTimeout { get; }
        public string SessionId { get; }

        public CommandQueueConfiguration(
            string sessionId,
            TimeSpan? defaultCommandTimeout = null,
            TimeSpan? heartbeatInterval = null,
            TimeSpan? shutdownTimeout = null,
            TimeSpan? forceShutdownTimeout = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId), "Session ID cannot be null or empty");

            SessionId = sessionId;
            DefaultCommandTimeout = defaultCommandTimeout ?? TimeSpan.FromMinutes(10);
            HeartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(30);
            ShutdownTimeout = shutdownTimeout ?? TimeSpan.FromSeconds(5);
            ForceShutdownTimeout = forceShutdownTimeout ?? TimeSpan.FromSeconds(2);

            ValidateConfiguration();
        }

        /// <summary>
        /// Validates the configuration parameters
        /// </summary>
        private void ValidateConfiguration()
        {
            if (DefaultCommandTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(DefaultCommandTimeout), "Default command timeout must be positive");

            if (HeartbeatInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(HeartbeatInterval), "Heartbeat interval must be positive");

            if (ShutdownTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ShutdownTimeout), "Shutdown timeout must be positive");

            if (ForceShutdownTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ForceShutdownTimeout), "Force shutdown timeout must be positive");

            if (ForceShutdownTimeout >= ShutdownTimeout)
                throw new ArgumentException("Force shutdown timeout must be less than shutdown timeout");
        }

        /// <summary>
        /// Generates a unique command ID for the session
        /// </summary>
        public string GenerateCommandId(long commandNumber)
        {
            return $"cmd-{SessionId}-{commandNumber:D4}";
        }

        /// <summary>
        /// Calculates progress percentage based on queue position and elapsed time
        /// </summary>
        public int CalculateProgressPercentage(int queuePosition, TimeSpan elapsed)
        {
            if (queuePosition <= 0)
                return 95; // Currently executing

            // Base progress calculation
            var baseProgress = Math.Max(5, 100 - (queuePosition * 15));

            // Time-based adjustment (more time = higher progress)
            var timeBonus = Math.Min(10, (int)(elapsed.TotalMinutes * 2));

            return Math.Min(90, baseProgress + timeBonus); // Cap at 90% for queued commands
        }

        /// <summary>
        /// Gets the base status message for a queued command
        /// </summary>
        public string GetBaseMessage(int queuePosition, TimeSpan elapsed)
        {
            if (queuePosition <= 0)
                return "Executing command...";

            var elapsedMinutes = (int)elapsed.TotalMinutes;
            var elapsedSeconds = elapsed.Seconds;

            return queuePosition switch
            {
                1 => $"Next in queue (waited {elapsedMinutes}m {elapsedSeconds}s)",
                2 => $"2nd in queue (waited {elapsedMinutes}m {elapsedSeconds}s)",
                3 => $"3rd in queue (waited {elapsedMinutes}m {elapsedSeconds}s)",
                _ => $"{queuePosition}th in queue (waited {elapsedMinutes}m {elapsedSeconds}s)"
            };
        }

        /// <summary>
        /// Gets the detailed status message for a queued command
        /// </summary>
        public string GetQueuedStatusMessage(int queuePosition, TimeSpan elapsed, int remainingMinutes, int remainingSeconds)
        {
            var baseMessage = GetBaseMessage(queuePosition, elapsed);

            if (queuePosition <= 0)
                return baseMessage;

            return $"{baseMessage} - Check again in {remainingMinutes}-{remainingSeconds} seconds (next in queue)";
        }
    }
}

