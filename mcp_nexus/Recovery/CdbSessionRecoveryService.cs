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

    /// <summary>
    /// Refactored CDB session recovery service that orchestrates focused recovery components
    /// Provides automated recovery for unattended server operation
    /// </summary>
    public class CdbSessionRecoveryService : ICdbSessionRecoveryService, IDisposable
    {
        private readonly ILogger<CdbSessionRecoveryService> m_logger;
        private bool m_disposed = false;

        // Focused components
        private readonly RecoveryConfiguration m_config;
        private readonly SessionHealthMonitor m_healthMonitor;
        private readonly RecoveryOrchestrator m_orchestrator;

        public CdbSessionRecoveryService(
            ICdbSession cdbSession,
            ILogger<CdbSessionRecoveryService> logger,
            Func<string, int> cancelAllCommandsCallback,
            IMcpNotificationService? notificationService = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create focused components
            m_config = new RecoveryConfiguration();
            m_healthMonitor = new SessionHealthMonitor(cdbSession, logger, m_config);
            m_orchestrator = new RecoveryOrchestrator(
                cdbSession, logger, cancelAllCommandsCallback, m_config, m_healthMonitor, notificationService);

            m_logger.LogInformation("🔧 CdbSessionRecoveryService initialized with focused components");
        }

        /// <summary>
        /// Attempts to recover a stuck CDB session
        /// </summary>
        /// <param name="reason">The reason for recovery</param>
        /// <returns>True if recovery was successful</returns>
        public async Task<bool> RecoverStuckSession(string reason)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));

            if (reason == null)
                throw new ArgumentNullException(nameof(reason));

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be empty or whitespace", nameof(reason));

            m_logger.LogInformation("🔧 Recovery requested: {Reason}", reason);

            try
            {
                return await m_orchestrator.RecoverStuckSessionAsync(reason);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔧 Recovery operation failed");
                return false;
            }
        }

        /// <summary>
        /// Forces a restart of the CDB session
        /// </summary>
        /// <param name="reason">The reason for restart</param>
        /// <returns>True if restart was successful</returns>
        public async Task<bool> ForceRestartSession(string reason)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));

            if (reason == null)
                throw new ArgumentNullException(nameof(reason));

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be empty or whitespace", nameof(reason));

            m_logger.LogWarning("🔧 Force restart requested: {Reason}", reason);

            try
            {
                return await m_orchestrator.ForceRestartSessionAsync(reason);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔧 Force restart operation failed");
                return false;
            }
        }

        /// <summary>
        /// Checks if the CDB session is healthy
        /// </summary>
        /// <returns>True if the session is healthy</returns>
        public bool IsSessionHealthy()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));

            try
            {
                return m_healthMonitor.IsSessionHealthy();
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔧 Health check failed");
                return false;
            }
        }

        /// <summary>
        /// Gets comprehensive session diagnostics
        /// </summary>
        /// <returns>Session diagnostic information</returns>
        public SessionDiagnostics GetSessionDiagnostics()
        {
            if (m_disposed)
                return new SessionDiagnostics { ErrorMessage = "Service is disposed" };

            try
            {
                return m_healthMonitor.GetSessionDiagnostics();
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔧 Error getting session diagnostics");
                return new SessionDiagnostics { ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Gets recovery statistics
        /// </summary>
        /// <returns>Recovery operation statistics</returns>
        public RecoveryStatistics GetRecoveryStatistics()
        {
            if (m_disposed)
                return new RecoveryStatistics();

            try
            {
                return m_orchestrator.GetRecoveryStatistics();
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔧 Error getting recovery statistics");
                return new RecoveryStatistics();
            }
        }

        /// <summary>
        /// Performs a comprehensive health check if due
        /// </summary>
        /// <returns>True if health check passed or wasn't due</returns>
        public async Task<bool> PerformScheduledHealthCheckAsync()
        {
            if (m_disposed)
                return false;

            try
            {
                if (m_healthMonitor.IsHealthCheckDue())
                {
                    m_logger.LogTrace("🔧 Performing scheduled health check");
                    return await m_healthMonitor.IsSessionResponsive();
                }

                return true; // Not due, assume healthy
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔧 Scheduled health check failed");
                return false;
            }
        }

        /// <summary>
        /// Disposes the recovery service and all resources
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            try
            {
                m_logger.LogInformation("🔧 Shutting down CdbSessionRecoveryService");

                // Dispose components
                m_orchestrator?.Dispose();

                m_logger.LogInformation("✅ CdbSessionRecoveryService shutdown complete");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Error during CdbSessionRecoveryService disposal");
            }
        }
    }
}
