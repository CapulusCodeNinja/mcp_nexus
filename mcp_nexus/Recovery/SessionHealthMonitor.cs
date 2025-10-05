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
        private bool m_lastHealthResult = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionHealthMonitor"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session to monitor.</param>
        /// <param name="logger">The logger instance for recording health monitoring operations.</param>
        /// <param name="config">The recovery configuration settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
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
        /// Checks if the session is currently healthy.
        /// </summary>
        /// <returns>True if the session is healthy</returns>
        public bool IsSessionHealthy()
        {
            try
            {
                // Use cached result if within cooldown period (30 seconds)
                var now = DateTime.UtcNow;
                if (now - m_lastHealthCheck < TimeSpan.FromSeconds(30))
                {
                    return m_lastHealthResult;
                }

                // Perform actual health check
                m_lastHealthCheck = now;

                // Basic health check: is the session active?
                if (!m_cdbSession.IsActive)
                {
                    m_lastHealthResult = false;
                    return false;
                }

                m_lastHealthResult = true;
                return true;
            }
            catch (Exception)
            {
                m_lastHealthResult = false;
                return false;
            }
        }

        /// <summary>
        /// Performs a more comprehensive responsiveness test
        /// </summary>
        /// <returns>True if the session is responsive</returns>
        public Task<bool> IsSessionResponsive()
        {
            try
            {
                // First check basic health
                if (!IsSessionHealthy())
                    return Task.FromResult(false);

                // For now, we'll use the basic health check
                // In a more advanced implementation, we could send a simple command
                // and check if it responds within a reasonable time

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Gets the time elapsed since the last health check.
        /// </summary>
        /// <returns>Time since last health check</returns>
        public TimeSpan TimeSinceLastHealthCheck()
        {
            return DateTime.UtcNow - m_lastHealthCheck;
        }

        /// <summary>
        /// Determines if a health check should be performed based on timing.
        /// </summary>
        /// <returns>True if a health check should be performed</returns>
        public bool IsHealthCheckDue()
        {
            return TimeSinceLastHealthCheck() >= m_config.HealthCheckInterval;
        }

        /// <summary>
        /// Gets diagnostic information about the session.
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
                return diagnostics;
            }
            catch (Exception ex)
            {
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
