namespace mcp_nexus.Health
{
    /// <summary>
    /// Implementation of health check result
    /// </summary>
    public class HealthCheckResult : IHealthCheckResult
    {
        #region Private Fields

        private readonly bool m_isHealthy;
        private readonly string m_message;
        private readonly DateTime m_timestamp;
        private readonly Dictionary<string, object> m_data;

        #endregion

        #region Public Properties

        /// <summary>Gets whether the health check passed</summary>
        public bool IsHealthy => m_isHealthy;

        /// <summary>Gets the health check message</summary>
        public string Message => m_message;

        /// <summary>Gets the health check timestamp</summary>
        public DateTime Timestamp => m_timestamp;

        /// <summary>Gets additional health check data</summary>
        public IReadOnlyDictionary<string, object> Data => m_data.AsReadOnly();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new health check result
        /// </summary>
        /// <param name="isHealthy">Whether the health check passed</param>
        /// <param name="message">Health check message</param>
        /// <param name="data">Additional health check data</param>
        public HealthCheckResult(bool isHealthy, string message, Dictionary<string, object>? data = null)
        {
            m_isHealthy = isHealthy;
            m_message = message ?? string.Empty;
            m_timestamp = DateTime.UtcNow;
            m_data = data ?? new Dictionary<string, object>();
        }

        #endregion
    }
}
