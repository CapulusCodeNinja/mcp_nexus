namespace mcp_nexus.Recovery
{
    /// <summary>
    /// Configuration settings for CDB session recovery operations
    /// </summary>
    public class RecoveryConfiguration
    {
        public TimeSpan CancellationTimeout { get; }
        public TimeSpan RestartDelay { get; }
        public TimeSpan HealthCheckInterval { get; }
        public int MaxRecoveryAttempts { get; }
        public TimeSpan RecoveryAttemptCooldown { get; }

        public RecoveryConfiguration(
            TimeSpan? cancellationTimeout = null,
            TimeSpan? restartDelay = null,
            TimeSpan? healthCheckInterval = null,
            int maxRecoveryAttempts = 3,
            TimeSpan? recoveryAttemptCooldown = null)
        {
            CancellationTimeout = cancellationTimeout ?? TimeSpan.FromSeconds(5);
            RestartDelay = restartDelay ?? TimeSpan.FromSeconds(2);
            HealthCheckInterval = healthCheckInterval ?? TimeSpan.FromMinutes(1);
            MaxRecoveryAttempts = maxRecoveryAttempts;
            RecoveryAttemptCooldown = recoveryAttemptCooldown ?? TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Determines if recovery should be attempted based on attempt count and timing
        /// </summary>
        /// <param name="attemptCount">Current recovery attempt count</param>
        /// <param name="lastAttemptTime">Time of last recovery attempt</param>
        /// <returns>True if recovery should be attempted</returns>
        public bool ShouldAttemptRecovery(int attemptCount, DateTime lastAttemptTime)
        {
            if (attemptCount >= MaxRecoveryAttempts)
                return false;

            var timeSinceLastAttempt = DateTime.UtcNow - lastAttemptTime;
            return timeSinceLastAttempt >= RecoveryAttemptCooldown;
        }

        /// <summary>
        /// Gets the delay before attempting to restart a session
        /// </summary>
        /// <param name="attemptNumber">The attempt number (1-based)</param>
        /// <returns>Delay before restart</returns>
        public TimeSpan GetRestartDelay(int attemptNumber)
        {
            // Exponential backoff: base delay * 2^(attempt-1)
            var multiplier = Math.Pow(2, attemptNumber - 1);
            return TimeSpan.FromMilliseconds(RestartDelay.TotalMilliseconds * multiplier);
        }
    }
}
