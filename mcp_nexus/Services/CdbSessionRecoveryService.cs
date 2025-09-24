using mcp_nexus.Helper;

namespace mcp_nexus.Services
{
    public interface ICdbSessionRecoveryService
    {
        Task<bool> RecoverStuckSession(string reason);
        Task<bool> ForceRestartSession(string reason);
        bool IsSessionHealthy();
    }

    public class CdbSessionRecoveryService : ICdbSessionRecoveryService
    {
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger<CdbSessionRecoveryService> m_logger;
        private readonly ICommandQueueService m_commandQueueService;
        private DateTime m_lastHealthCheck = DateTime.UtcNow;
        private int m_recoveryAttempts = 0;
        private readonly object m_recoveryLock = new();

        public CdbSessionRecoveryService(
            ICdbSession cdbSession, 
            ILogger<CdbSessionRecoveryService> logger,
            ICommandQueueService commandQueueService)
        {
            m_cdbSession = cdbSession;
            m_logger = logger;
            m_commandQueueService = commandQueueService;
        }

        public async Task<bool> RecoverStuckSession(string reason)
        {
            lock (m_recoveryLock)
            {
                m_recoveryAttempts++;
                m_logger.LogError("🚨 RECOVERY ATTEMPT #{Attempt}: {Reason}", m_recoveryAttempts, reason);
            }

            try
            {
                // Step 1: Cancel all pending commands
                m_logger.LogWarning("🔄 Recovery Step 1: Cancelling all pending commands");
                var cancelledCount = m_commandQueueService.CancelAllCommands($"Recovery: {reason}");
                m_logger.LogInformation("✅ Cancelled {Count} pending commands", cancelledCount);

                // Step 2: Try gentle CDB cancellation first
                m_logger.LogWarning("🔄 Recovery Step 2: Attempting CDB cancellation");
                try
                {
                    m_cdbSession.CancelCurrentOperation();
                    await Task.Delay(5000); // Give it 5 seconds to respond
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "⚠️ CDB cancellation failed, proceeding to force restart");
                }

                // Step 3: Check if session is still responsive
                if (await IsSessionResponsive())
                {
                    m_logger.LogInformation("✅ Session recovered successfully after cancellation");
                    ResetRecoveryCounter();
                    return true;
                }

                // Step 4: Force restart if still unresponsive
                m_logger.LogError("❌ Session still unresponsive, forcing restart");
                return await ForceRestartSession($"Recovery escalation: {reason}");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Recovery attempt failed");
                return false;
            }
        }

        public async Task<bool> ForceRestartSession(string reason)
        {
            m_logger.LogError("🚨 FORCE RESTART: {Reason}", reason);

            try
            {
                // Step 1: Cancel all commands
                m_commandQueueService.CancelAllCommands($"Force restart: {reason}");

                // Step 2: Stop current session forcefully
                m_logger.LogWarning("🔄 Force stopping CDB session");
                var stopResult = await m_cdbSession.StopSession();
                
                if (!stopResult)
                {
                    m_logger.LogWarning("⚠️ StopSession returned false, session may still be active");
                }

                // Step 3: Wait a moment for cleanup
                await Task.Delay(2000);

                // Step 4: Verify session is stopped
                if (m_cdbSession.IsActive)
                {
                    m_logger.LogError("❌ Session still active after stop attempt - this requires manual intervention");
                    return false;
                }

                m_logger.LogInformation("✅ Session stopped successfully, ready for new connections");
                ResetRecoveryCounter();
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
                    return true; // Not active is not unhealthy
                }

                // Check for excessive recovery attempts
                if (m_recoveryAttempts > 3)
                {
                    m_logger.LogError("🚨 Health check FAILED: Too many recovery attempts ({Count})", m_recoveryAttempts);
                    return false;
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
            lock (m_recoveryLock)
            {
                if (m_recoveryAttempts > 0)
                {
                    m_logger.LogInformation("✅ Recovery successful, resetting attempt counter (was {Count})", m_recoveryAttempts);
                    m_recoveryAttempts = 0;
                }
            }
        }
    }
}
