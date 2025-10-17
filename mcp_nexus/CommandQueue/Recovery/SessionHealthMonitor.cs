using mcp_nexus.Debugger;

namespace mcp_nexus.CommandQueue.Recovery
{
    /// <summary>
    /// Monitors CDB session health and responsiveness
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SessionHealthMonitor"/> class.
    /// </remarks>
    /// <param name="cdbSession">The CDB session to monitor.</param>
    /// <param name="logger">The logger instance for recording health monitoring operations.</param>
    /// <param name="config">The recovery configuration settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
    public class SessionHealthMonitor(
        ICdbSession cdbSession,
        ILogger logger,
        RecoveryConfiguration config)
    {
        private readonly ICdbSession m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
        private readonly ILogger m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly RecoveryConfiguration m_Config = config ?? throw new ArgumentNullException(nameof(config));
        private DateTime m_LastHealthCheck = DateTime.Now;
        private bool m_LastHealthResult = true;

        /// <summary>
        /// Checks if the session is currently healthy.
        /// </summary>
        /// <returns>True if the session is healthy</returns>
        public bool IsSessionHealthy()
        {
            try
            {
                // Use cached result if within cooldown period (30 seconds)
                var now = DateTime.Now;
                if (now - m_LastHealthCheck < TimeSpan.FromSeconds(30))
                {
                    return m_LastHealthResult;
                }

                // Perform actual health check
                m_LastHealthCheck = now;

                // Basic health check: is the session active?
                if (!m_CdbSession.IsActive)
                {
                    m_LastHealthResult = false;
                    return false;
                }

                m_LastHealthResult = true;
                return true;
            }
            catch (Exception)
            {
                m_LastHealthResult = false;
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
            return DateTime.Now - m_LastHealthCheck;
        }

        /// <summary>
        /// Determines if a health check should be performed based on timing.
        /// </summary>
        /// <returns>True if a health check should be performed</returns>
        public bool IsHealthCheckDue()
        {
            return TimeSinceLastHealthCheck() >= m_Config.HealthCheckInterval;
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
                    IsActive = m_CdbSession.IsActive,
                    LastHealthCheck = m_LastHealthCheck,
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
                    LastHealthCheck = m_LastHealthCheck,
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
        public Dictionary<string, object> AdditionalInfo { get; set; } = [];
    }
}
