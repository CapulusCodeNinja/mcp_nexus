using mcp_nexus.Debugger;
using mcp_nexus.Notifications;

namespace mcp_nexus.Recovery
{
    public interface ICdbSessionRecoveryService
    {
        Task<bool> RecoverStuckSession(string reason);
        Task<bool> ForceRestartSession(string reason);
        bool IsSessionHealthy();
    }

    public class CdbSessionRecoveryService : ICdbSessionRecoveryService, IDisposable
    {
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger<CdbSessionRecoveryService> m_logger;
        private readonly Func<string, int> m_cancelAllCommandsCallback;
        private readonly IMcpNotificationService? m_notificationService;
        private DateTime m_lastHealthCheck = DateTime.UtcNow;
        private volatile int m_recoveryAttempts = 0;
        private bool m_disposed = false;
        // FIXED: Use ReaderWriterLockSlim for better concurrency
        private readonly ReaderWriterLockSlim m_recoveryLock = new();

        public CdbSessionRecoveryService(
            ICdbSession cdbSession,
            ILogger<CdbSessionRecoveryService> logger,
            Func<string, int> cancelAllCommandsCallback,
            IMcpNotificationService? notificationService = null)
        {
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_cancelAllCommandsCallback = cancelAllCommandsCallback ?? throw new ArgumentNullException(nameof(cancelAllCommandsCallback));
            m_notificationService = notificationService;
        }

        public async Task<bool> RecoverStuckSession(string reason)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));
            if (reason == null)
                throw new ArgumentNullException(nameof(reason));
            if (reason.Length == 0)
                throw new ArgumentException("Reason cannot be empty", nameof(reason));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be whitespace only", nameof(reason));

            // Check if session is already not active
            if (!m_cdbSession.IsActive)
            {
                m_logger.LogInformation("Session is not active, no recovery needed");
                return false;
            }

            // FIXED: Use write lock for recovery attempt counter
            m_recoveryLock.EnterWriteLock();
            try
            {
                m_recoveryAttempts++;
                m_logger.LogWarning("Starting recovery attempt #{Attempt}: {Reason}", m_recoveryAttempts, reason);
            }
            finally
            {
                m_recoveryLock.ExitWriteLock();
            }

            // Send recovery start notification
            _ = Task.Run(async () =>
            {
                try
                {
                    if (m_notificationService != null)
                    {
                        await m_notificationService.NotifySessionRecoveryAsync(
                            reason, "Recovery Started", false, $"Starting recovery attempt #{m_recoveryAttempts}");
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Failed to send recovery start notification");
                }
            });

            try
            {
                // Step 1: Cancel all pending commands
                m_logger.LogInformation("Recovery Step 1: Cancelling all pending commands");
                var cancelledCount = m_cancelAllCommandsCallback($"Recovery: {reason}");
                m_logger.LogDebug("Cancelled {Count} pending commands", cancelledCount);

                // Step 2: Try gentle CDB cancellation first
                m_logger.LogInformation("Recovery Step 2: Attempting CDB cancellation");
                try
                {
                    m_cdbSession.CancelCurrentOperation();
                    await Task.Delay(5000); // Give it 5 seconds to respond
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "CDB cancellation failed, proceeding to force restart");
                }

                // Step 3: Check if session is still responsive
                if (await IsSessionResponsive())
                {
                    m_logger.LogInformation("Session recovered successfully after cancellation");
                    ResetRecoveryCounter();

                    // Send successful recovery notification
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (m_notificationService != null)
                            {
                                await m_notificationService.NotifySessionRecoveryAsync(
                                    reason, "Recovery Completed", true, "Session recovered successfully after cancellation");
                            }
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogWarning(ex, "Failed to send recovery success notification");
                        }
                    });

                    return true;
                }

                // Step 4: Stop current session and start a new one
                m_logger.LogInformation("Session is unresponsive, stopping and restarting");
                var stopResult = await m_cdbSession.StopSession();
                if (!stopResult)
                {
                    m_logger.LogWarning("StopSession returned false, cannot restart");
                    return false;
                }

                // Step 5: Start new session
                var startResult = await m_cdbSession.StartSession("", null);
                if (startResult)
                {
                    m_logger.LogInformation("New session started successfully");
                    ResetRecoveryCounter();
                    return true;
                }
                else
                {
                    m_logger.LogError("Failed to start new session");
                    return false;
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Recovery attempt failed");
                return false;
            }
        }

        public async Task<bool> ForceRestartSession(string reason)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));
            if (reason == null)
                throw new ArgumentNullException(nameof(reason));
            if (reason.Length == 0)
                throw new ArgumentException("Reason cannot be empty", nameof(reason));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be whitespace only", nameof(reason));

            m_logger.LogWarning("Force restarting CDB session: {Reason}", reason);

            // Check if session is already not active
            if (!m_cdbSession.IsActive)
            {
                m_logger.LogInformation("Session is not active, no restart needed");
                return false;
            }

            try
            {
                // Send notification about force restart
                if (m_notificationService != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await m_notificationService.NotifySessionRecoveryAsync(
                                reason, "Force Restart Started", false, "Force restarting CDB session");
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogWarning(ex, "Failed to send force restart notification");
                        }
                    });
                }

                // Step 1: Cancel all commands
                m_cancelAllCommandsCallback($"Force restart: {reason}");

                // Step 2: Stop current session forcefully
                m_logger.LogDebug("Force stopping CDB session");
                var stopResult = await m_cdbSession.StopSession();

                if (!stopResult)
                {
                    m_logger.LogWarning("StopSession returned false, session may still be active");
                }

                // Step 3: Wait a moment for cleanup
                await Task.Delay(2000);

                // Step 4: Verify session is stopped
                if (m_cdbSession.IsActive)
                {
                    m_logger.LogError("Session still active after stop attempt - this requires manual intervention");
                    return false;
                }

                m_logger.LogInformation("Session stopped successfully, starting new session");

                // Step 5: Start a new session
                var startResult = await m_cdbSession.StartSession("", null);
                if (!startResult)
                {
                    m_logger.LogError("Failed to start new session after force restart");
                    return false;
                }

                m_logger.LogInformation("New session started successfully");
                ResetRecoveryCounter();

                // Send success notification
                if (m_notificationService != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await m_notificationService.NotifySessionRecoveryAsync(
                                reason, "Force Restart Completed", true, "CDB session force restarted successfully");
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogWarning(ex, "Failed to send force restart success notification");
                        }
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Force restart failed");
                return false;
            }
        }

        public bool IsSessionHealthy()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));

            try
            {
                var now = DateTime.UtcNow;

                // Don't check too frequently
                if ((now - m_lastHealthCheck).TotalSeconds < 30)
                {
                    return true; // Assume healthy if checked recently
                }

                m_lastHealthCheck = now;

                // Basic health checks
                if (!m_cdbSession.IsActive)
                {
                    m_logger.LogDebug("🔍 Health check: Session not active");
                    return false; // Not active is unhealthy
                }

                // FIXED: Use read lock for recovery attempt check
                m_recoveryLock.EnterReadLock();
                try
                {
                    if (m_recoveryAttempts > 3)
                    {
                        m_logger.LogError("🚨 Health check FAILED: Too many recovery attempts ({Count})", m_recoveryAttempts);
                        return false;
                    }
                }
                finally
                {
                    m_recoveryLock.ExitReadLock();
                }

                // Could add more sophisticated health checks here:
                // - Memory usage monitoring
                // - Response time monitoring
                // - Error rate monitoring

                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Health check failed");
                return false;
            }
        }

        private async Task<bool> IsSessionResponsive()
        {
            if (!m_cdbSession.IsActive)
            {
                return true; // If not active, it's not unresponsive
            }

            try
            {
                // Try a simple command with short timeout to test responsiveness
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var result = await m_cdbSession.ExecuteCommand("version", cts.Token);

                return !string.IsNullOrEmpty(result) &&
                       !result.Contains("timeout", StringComparison.OrdinalIgnoreCase) &&
                       !result.Contains("failed", StringComparison.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException)
            {
                m_logger.LogWarning("⚠️ Session responsiveness test timed out");
                return false;
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "⚠️ Session responsiveness test failed");
                return false;
            }
        }

        private void ResetRecoveryCounter()
        {
            // FIXED: Use write lock for counter reset
            m_recoveryLock.EnterWriteLock();
            try
            {
                if (m_recoveryAttempts > 0)
                {
                    m_logger.LogInformation("✅ Recovery successful, resetting attempt counter (was {Count})", m_recoveryAttempts);
                    m_recoveryAttempts = 0;
                }
            }
            finally
            {
                m_recoveryLock.ExitWriteLock();
            }
        }

        // FIXED: Add proper disposal for ReaderWriterLockSlim
        public void Dispose()
        {
            if (!m_disposed)
            {
                m_disposed = true;
                m_recoveryLock?.Dispose();
            }
        }
    }
}

