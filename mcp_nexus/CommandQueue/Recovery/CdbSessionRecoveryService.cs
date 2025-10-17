using mcp_nexus.Debugger;
using mcp_nexus.Notifications;

namespace mcp_nexus.CommandQueue.Recovery
{
    /// <summary>
    /// Interface for CDB session recovery operations.
    /// Provides methods for recovering stuck sessions and restarting sessions.
    /// </summary>
    public interface ICdbSessionRecoveryService
    {
        /// <summary>
        /// Recovers a stuck session asynchronously.
        /// </summary>
        /// <param name="reason">The reason for the recovery operation.</param>
        /// <returns>A task that represents the asynchronous operation and contains the recovery result.</returns>
        Task<bool> RecoverStuckSession(string reason);

        /// <summary>
        /// Forces a session restart asynchronously.
        /// </summary>
        /// <param name="reason">The reason for the restart operation.</param>
        /// <returns>A task that represents the asynchronous operation and contains the restart result.</returns>
        Task<bool> ForceRestartSession(string reason);

        /// <summary>
        /// Checks if the session is healthy.
        /// </summary>
        /// <returns><c>true</c> if the session is healthy; otherwise, <c>false</c>.</returns>
        bool IsSessionHealthy();
    }

    /// <summary>
    /// Refactored CDB session recovery service that orchestrates focused recovery components
    /// Provides automated recovery for unattended server operation
    /// </summary>
    public class CdbSessionRecoveryService : ICdbSessionRecoveryService, IDisposable
    {
        private readonly ILogger<CdbSessionRecoveryService> m_Logger;
        private bool m_Disposed = false;

        // Focused components
        private readonly RecoveryConfiguration m_Config;
        private readonly SessionHealthMonitor m_HealthMonitor;
        private readonly RecoveryOrchestrator m_Orchestrator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbSessionRecoveryService"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session to monitor and recover.</param>
        /// <param name="logger">The logger instance for recording recovery operations and errors.</param>
        /// <param name="cancelAllCommandsCallback">Callback function to cancel all commands.</param>
        /// <param name="notificationService">Optional notification service for publishing recovery events.</param>
        public CdbSessionRecoveryService(
            ICdbSession cdbSession,
            ILogger<CdbSessionRecoveryService> logger,
            Func<string, int> cancelAllCommandsCallback,
            IMcpNotificationService? notificationService = null)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create focused components
            m_Config = new RecoveryConfiguration();
            m_HealthMonitor = new SessionHealthMonitor(cdbSession, logger, m_Config);
            m_Orchestrator = new RecoveryOrchestrator(
                cdbSession, logger, cancelAllCommandsCallback, m_Config, m_HealthMonitor, notificationService);

            m_Logger.LogInformation("🔧 CdbSessionRecoveryService initialized with focused components");
        }

        /// <summary>
        /// Attempts to recover a stuck CDB session
        /// </summary>
        /// <param name="reason">The reason for recovery</param>
        /// <returns>True if recovery was successful</returns>
        public async Task<bool> RecoverStuckSession(string reason)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));

            ArgumentNullException.ThrowIfNull(reason);

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be empty or whitespace", nameof(reason));

            m_Logger.LogInformation("🔧 Recovery requested: {Reason}", reason);

            try
            {
                return await m_Orchestrator.RecoverStuckSessionAsync(reason);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "🔧 Recovery operation failed");
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
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));

            ArgumentNullException.ThrowIfNull(reason);

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be empty or whitespace", nameof(reason));

            m_Logger.LogWarning("🔧 Force restart requested: {Reason}", reason);

            try
            {
                return await m_Orchestrator.ForceRestartSessionAsync(reason);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "🔧 Force restart operation failed");
                return false;
            }
        }

        /// <summary>
        /// Checks if the CDB session is healthy
        /// </summary>
        /// <returns>True if the session is healthy</returns>
        public bool IsSessionHealthy()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CdbSessionRecoveryService));

            try
            {
                return m_HealthMonitor.IsSessionHealthy();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "🔧 Health check failed");
                return false;
            }
        }

        /// <summary>
        /// Gets comprehensive session diagnostics
        /// </summary>
        /// <returns>Session diagnostic information</returns>
        public SessionDiagnostics GetSessionDiagnostics()
        {
            if (m_Disposed)
                return new SessionDiagnostics { ErrorMessage = "Service is disposed" };

            try
            {
                return m_HealthMonitor.GetSessionDiagnostics();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "🔧 Error getting session diagnostics");
                return new SessionDiagnostics { ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Gets recovery statistics
        /// </summary>
        /// <returns>Recovery operation statistics</returns>
        public RecoveryStatistics GetRecoveryStatistics()
        {
            if (m_Disposed)
                return new RecoveryStatistics();

            try
            {
                return m_Orchestrator.GetRecoveryStatistics();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "🔧 Error getting recovery statistics");
                return new RecoveryStatistics();
            }
        }

        /// <summary>
        /// Performs a comprehensive health check if due
        /// </summary>
        /// <returns>True if health check passed or wasn't due</returns>
        public async Task<bool> PerformScheduledHealthCheckAsync()
        {
            if (m_Disposed)
                return false;

            try
            {
                if (m_HealthMonitor.IsHealthCheckDue())
                {
                    m_Logger.LogTrace("🔧 Performing scheduled health check");
                    return await m_HealthMonitor.IsSessionResponsive();
                }

                return true; // Not due, assume healthy
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "🔧 Scheduled health check failed");
                return false;
            }
        }

        /// <summary>
        /// Disposes the recovery service and all resources.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            try
            {
                m_Logger.LogInformation("🔧 Shutting down CdbSessionRecoveryService");

                // Dispose components
                m_Orchestrator?.Dispose();

                m_Logger.LogInformation("✅ CdbSessionRecoveryService shutdown complete");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "💥 Error during CdbSessionRecoveryService disposal");
            }
        }
    }
}
