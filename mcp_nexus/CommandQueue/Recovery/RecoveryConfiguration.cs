namespace mcp_nexus.CommandQueue.Recovery
{
    /// <summary>
    /// Configuration settings for CDB session recovery operations
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RecoveryConfiguration"/> class.
    /// </remarks>
    /// <param name="cancellationTimeout">The timeout for command cancellation operations.</param>
    /// <param name="restartDelay">The delay before restarting a CDB session.</param>
    /// <param name="healthCheckInterval">The interval between health checks.</param>
    /// <param name="maxRecoveryAttempts">The maximum number of recovery attempts.</param>
    /// <param name="recoveryAttemptCooldown">The cooldown period between recovery attempts.</param>
    public class RecoveryConfiguration(
        TimeSpan? cancellationTimeout = null,
        TimeSpan? restartDelay = null,
        TimeSpan? healthCheckInterval = null,
        int maxRecoveryAttempts = 3,
        TimeSpan? recoveryAttemptCooldown = null)
    {
        /// <summary>
        /// Gets the timeout for command cancellation operations.
        /// </summary>
        public TimeSpan CancellationTimeout { get; } = cancellationTimeout ?? TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets the delay before restarting a CDB session.
        /// </summary>
        public TimeSpan RestartDelay { get; } = restartDelay ?? TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets the interval between health checks.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; } = healthCheckInterval ?? TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets the maximum number of recovery attempts.
        /// </summary>
        public int MaxRecoveryAttempts { get; } = maxRecoveryAttempts;

        /// <summary>
        /// Gets the cooldown period between recovery attempts.
        /// </summary>
        public TimeSpan RecoveryAttemptCooldown { get; } = recoveryAttemptCooldown ?? TimeSpan.FromMinutes(5);

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

            var timeSinceLastAttempt = DateTime.Now - lastAttemptTime;
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
