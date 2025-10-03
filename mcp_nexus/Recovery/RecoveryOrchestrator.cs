using mcp_nexus.Debugger;
using mcp_nexus.Notifications;

namespace mcp_nexus.Recovery
{
    /// <summary>
    /// Orchestrates CDB session recovery operations with comprehensive error handling.
    /// Provides multi-step recovery strategies for stuck or unresponsive CDB sessions.
    /// </summary>
    public class RecoveryOrchestrator
    {
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger m_logger;
        private readonly Func<string, int> m_cancelAllCommandsCallback;
        private readonly IMcpNotificationService? m_notificationService;
        private readonly RecoveryConfiguration m_config;
        private readonly SessionHealthMonitor m_healthMonitor;

        // Recovery state tracking
        private volatile int m_recoveryAttempts = 0;
        private DateTime m_lastRecoveryAttempt = DateTime.MinValue;
        private readonly ReaderWriterLockSlim m_recoveryLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="RecoveryOrchestrator"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session to manage recovery for.</param>
        /// <param name="logger">The logger instance for recording recovery operations and errors.</param>
        /// <param name="cancelAllCommandsCallback">Callback function to cancel all pending commands.</param>
        /// <param name="config">The recovery configuration containing retry limits and timeouts.</param>
        /// <param name="healthMonitor">The session health monitor for checking session responsiveness.</param>
        /// <param name="notificationService">Optional notification service for sending recovery events.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public RecoveryOrchestrator(
            ICdbSession cdbSession,
            ILogger logger,
            Func<string, int> cancelAllCommandsCallback,
            RecoveryConfiguration config,
            SessionHealthMonitor healthMonitor,
            IMcpNotificationService? notificationService = null)
        {
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_cancelAllCommandsCallback = cancelAllCommandsCallback ?? throw new ArgumentNullException(nameof(cancelAllCommandsCallback));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
            m_notificationService = notificationService;
        }

        /// <summary>
        /// Attempts to recover a stuck CDB session using a multi-step approach.
        /// This method implements a comprehensive recovery strategy including command cancellation,
        /// gentle CDB cancellation, and force restart if necessary.
        /// </summary>
        /// <param name="reason">The reason for recovery (e.g., "timeout", "unresponsive").</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if recovery was successful; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or empty.</exception>
        public async Task<bool> RecoverStuckSessionAsync(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

            // Check if session is already not active
            if (!m_cdbSession.IsActive)
            {
                m_logger.LogInformation("🔧 Session is not active, no recovery needed");
                return false;
            }

            // Check recovery attempt limits
            m_recoveryLock.EnterWriteLock();
            try
            {
                if (!m_config.ShouldAttemptRecovery(m_recoveryAttempts, m_lastRecoveryAttempt))
                {
                    m_logger.LogWarning("🔧 Recovery attempt limit reached or cooldown active - skipping recovery");
                    return false;
                }

                m_recoveryAttempts++;
                m_lastRecoveryAttempt = DateTime.UtcNow;
                m_logger.LogWarning("🔧 Starting recovery attempt #{Attempt}: {Reason}", m_recoveryAttempts, reason);
            }
            finally
            {
                m_recoveryLock.ExitWriteLock();
            }

            // Send recovery start notification
            await SendRecoveryNotificationAsync(reason, "Recovery Started", false, $"Starting recovery attempt #{m_recoveryAttempts}");

            try
            {
                // Step 1: Cancel all pending commands
                m_logger.LogInformation("🔧 Recovery Step 1: Cancelling all pending commands");
                var cancelledCount = m_cancelAllCommandsCallback($"Recovery: {reason}");
                m_logger.LogDebug("Cancelled {Count} pending commands", cancelledCount);

                // Step 2: Try gentle CDB cancellation first
                m_logger.LogInformation("🔧 Recovery Step 2: Attempting CDB cancellation");
                try
                {
                    m_cdbSession.CancelCurrentOperation();
                    // Reduced delay - cancellation is immediate, just give process time to respond
                    await Task.Delay(TimeSpan.FromSeconds(1)); // Reduced from config.CancellationTimeout (5s)
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "CDB cancellation failed, proceeding to force restart");
                }

                // Step 3: Check if session is still responsive
                if (await m_healthMonitor.IsSessionResponsive())
                {
                    m_logger.LogInformation("✅ Session recovered successfully after cancellation");
                    ResetRecoveryCounter();

                    await SendRecoveryNotificationAsync(reason, "Recovery Completed", true, "Session recovered successfully after cancellation");
                    return true;
                }

                // Step 4: Force restart the session
                m_logger.LogInformation("🔧 Recovery Step 3: Session unresponsive, attempting force restart");
                var restartSuccess = await ForceRestartSessionInternalAsync(reason);

                if (restartSuccess)
                {
                    m_logger.LogInformation("✅ Session recovered successfully after restart");
                    ResetRecoveryCounter();

                    await SendRecoveryNotificationAsync(reason, "Recovery Completed", true, "Session recovered successfully after restart");
                    return true;
                }
                else
                {
                    m_logger.LogError("❌ Recovery failed - could not restart session");
                    await SendRecoveryNotificationAsync(reason, "Recovery Failed", false, "Could not restart session");
                    return false;
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Recovery attempt failed with exception");
                await SendRecoveryNotificationAsync(reason, "Recovery Failed", false, $"Recovery failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Forces a restart of the CDB session.
        /// This method immediately stops the current session and starts a new one.
        /// </summary>
        /// <param name="reason">The reason for restart (e.g., "manual restart", "critical error").</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if restart was successful; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or empty.</exception>
        public async Task<bool> ForceRestartSessionAsync(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

            m_logger.LogWarning("🔧 Force restarting CDB session: {Reason}", reason);

            // Check if session is already not active
            if (!m_cdbSession.IsActive)
            {
                m_logger.LogInformation("Session is not active, no restart needed");
                return false;
            }

            await SendRecoveryNotificationAsync(reason, "Force Restart Started", false, "Force restarting CDB session");

            return await ForceRestartSessionInternalAsync(reason);
        }

        /// <summary>
        /// Internal method to perform the actual restart operation.
        /// This method handles the low-level details of stopping and starting the CDB session.
        /// </summary>
        /// <param name="reason">The reason for restart.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if restart was successful; otherwise, <c>false</c>.
        /// </returns>
        private async Task<bool> ForceRestartSessionInternalAsync(string reason)
        {
            try
            {
                // Step 1: Cancel all commands
                m_cancelAllCommandsCallback($"Force restart: {reason}");

                // Step 2: Stop current session forcefully
                m_logger.LogDebug("🔧 Force stopping CDB session");
                var stopResult = await m_cdbSession.StopSession();

                if (!stopResult)
                {
                    m_logger.LogWarning("StopSession returned false, session may still be active");
                }

                // Step 3: Wait for cleanup
                var restartDelay = m_config.GetRestartDelay(m_recoveryAttempts);
                await Task.Delay(restartDelay);

                // Step 4: Verify session is stopped
                if (m_cdbSession.IsActive)
                {
                    m_logger.LogError("Session still active after stop attempt - this requires manual intervention");
                    return false;
                }

                m_logger.LogInformation("Session stopped successfully, starting new session");

                // Step 5: Start a new session (empty target for now)
                var startResult = await m_cdbSession.StartSession("", null);

                if (startResult)
                {
                    m_logger.LogInformation("✅ New session started successfully");
                    return true;
                }
                else
                {
                    m_logger.LogError("❌ Failed to start new session");
                    return false;
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Force restart failed");
                return false;
            }
        }

        /// <summary>
        /// This method is called when recovery is successful to reset the retry state.
        /// </summary>
        private void ResetRecoveryCounter()
        {
            m_recoveryLock.EnterWriteLock();
            try
            {
                m_recoveryAttempts = 0;
                m_lastRecoveryAttempt = DateTime.MinValue;
                m_logger.LogDebug("🔧 Recovery counter reset");
            }
            finally
            {
                m_recoveryLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Sends a recovery notification asynchronously.
        /// </summary>
        /// <param name="reason">The reason for the recovery action.</param>
        /// <param name="step">The recovery step being performed.</param>
        /// <param name="success">Whether the recovery step was successful.</param>
        /// <param name="message">Additional message about the recovery.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        private Task SendRecoveryNotificationAsync(string reason, string step, bool success, string message)
        {
            if (m_notificationService == null)
                return Task.CompletedTask;

            _ = Task.Run(async () =>
            {
                try
                {
                    await m_notificationService.NotifySessionRecoveryAsync(reason, step, success, message);
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Failed to send recovery notification");
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the current recovery statistics.
        /// </summary>
        /// <returns>
        /// A <see cref="RecoveryStatistics"/> object containing current recovery information.
        /// </returns>
        public RecoveryStatistics GetRecoveryStatistics()
        {
            m_recoveryLock.EnterReadLock();
            try
            {
                return new RecoveryStatistics
                {
                    RecoveryAttempts = m_recoveryAttempts,
                    LastRecoveryAttempt = m_lastRecoveryAttempt,
                    TimeSinceLastAttempt = m_lastRecoveryAttempt == DateTime.MinValue ?
                        TimeSpan.Zero : DateTime.UtcNow - m_lastRecoveryAttempt,
                    CanAttemptRecovery = m_config.ShouldAttemptRecovery(m_recoveryAttempts, m_lastRecoveryAttempt)
                };
            }
            finally
            {
                m_recoveryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Disposes the recovery orchestrator.
        /// This method cleans up the recovery lock and logs the disposal.
        /// </summary>
        public void Dispose()
        {
            try
            {
                m_recoveryLock?.Dispose();
                m_logger.LogDebug("🔧 Recovery orchestrator disposed");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error disposing recovery orchestrator");
            }
        }
    }

    /// <summary>
    /// Statistics about recovery operations.
    /// Contains information about recovery attempts, timing, and current state.
    /// </summary>
    public class RecoveryStatistics
    {
        /// <summary>
        /// Gets or sets the number of recovery attempts made.
        /// </summary>
        public int RecoveryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last recovery attempt.
        /// </summary>
        public DateTime LastRecoveryAttempt { get; set; }

        /// <summary>
        /// Gets or sets the time elapsed since the last recovery attempt.
        /// </summary>
        public TimeSpan TimeSinceLastAttempt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a recovery attempt can be made.
        /// </summary>
        public bool CanAttemptRecovery { get; set; }
    }
}
