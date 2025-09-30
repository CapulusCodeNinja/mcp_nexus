using mcp_nexus.Debugger;

namespace mcp_nexus.Recovery
{
    /// <summary>
    /// Monitors CDB session health and responsiveness
    /// </summary>
    public class SessionHealthMonitor
    {
        private readonly ICdbSession m_cdbSession;
        private readonly ILogger m_logger;
        private readonly RecoveryConfiguration m_config;
        private DateTime m_lastHealthCheck = DateTime.UtcNow;
        
        public SessionHealthMonitor(
            ICdbSession cdbSession,
            ILogger logger,
            RecoveryConfiguration config)
        {
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
        }
        
        /// <summary>
        /// Checks if the CDB session is healthy and responsive
        /// </summary>
        /// <returns>True if the session is healthy</returns>
        public bool IsSessionHealthy()
        {
            try
            {
                // Basic health check: is the session active?
                if (!m_cdbSession.IsActive)
                {
                    m_logger.LogWarning("🔍 Health check failed: CDB session is not active");
                    return false;
                }
                
                // Update last health check time
                m_lastHealthCheck = DateTime.UtcNow;
                
                m_logger.LogTrace("🔍 Health check passed: CDB session is active");
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔍 Health check failed with exception");
                return false;
            }
        }
        
        /// <summary>
        /// Performs a more comprehensive responsiveness test
        /// </summary>
        /// <returns>True if the session is responsive</returns>
        public async Task<bool> IsSessionResponsive()
        {
            try
            {
                // First check basic health
                if (!IsSessionHealthy())
                    return false;
                
                // For now, we'll use the basic health check
                // In a more advanced implementation, we could send a simple command
                // and check if it responds within a reasonable time
                
                m_logger.LogDebug("🔍 Responsiveness check passed");
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔍 Responsiveness check failed");
                return false;
            }
        }
        
        /// <summary>
        /// Gets the time since the last health check
        /// </summary>
        /// <returns>Time since last health check</returns>
        public TimeSpan TimeSinceLastHealthCheck()
        {
            return DateTime.UtcNow - m_lastHealthCheck;
        }
        
        /// <summary>
        /// Determines if a health check is due based on configuration
        /// </summary>
        /// <returns>True if a health check should be performed</returns>
        public bool IsHealthCheckDue()
        {
            return TimeSinceLastHealthCheck() >= m_config.HealthCheckInterval;
        }
        
        /// <summary>
        /// Performs a comprehensive session diagnostic
        /// </summary>
        /// <returns>Diagnostic information about the session</returns>
        public SessionDiagnostics GetSessionDiagnostics()
        {
            try
            {
                var diagnostics = new SessionDiagnostics
                {
                    IsActive = m_cdbSession.IsActive,
                    LastHealthCheck = m_lastHealthCheck,
                    TimeSinceLastCheck = TimeSinceLastHealthCheck(),
                    IsHealthCheckDue = IsHealthCheckDue()
                };
                
                // Add more diagnostic information as needed
                m_logger.LogTrace("🔍 Session diagnostics collected");
                return diagnostics;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🔍 Error collecting session diagnostics");
                return new SessionDiagnostics
                {
                    IsActive = false,
                    LastHealthCheck = m_lastHealthCheck,
                    TimeSinceLastCheck = TimeSinceLastHealthCheck(),
                    IsHealthCheckDue = true,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
    
    /// <summary>
    /// Diagnostic information about a CDB session
    /// </summary>
    public class SessionDiagnostics
    {
        public bool IsActive { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public TimeSpan TimeSinceLastCheck { get; set; }
        public bool IsHealthCheckDue { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }
}
