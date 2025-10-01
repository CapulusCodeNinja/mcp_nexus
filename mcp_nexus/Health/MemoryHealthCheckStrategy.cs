using System.Diagnostics;

namespace mcp_nexus.Health
{
    /// <summary>
    /// Memory health check strategy using Strategy Pattern
    /// </summary>
    public class MemoryHealthCheckStrategy : IHealthCheckStrategy
    {
        private readonly Process m_currentProcess;
        private readonly long m_memoryThresholdBytes;

        /// <summary>
        /// Gets the name of the health check strategy
        /// </summary>
        public string StrategyName => "Memory Health Check";

        /// <summary>
        /// Initializes a new memory health check strategy
        /// </summary>
        /// <param name="memoryThresholdBytes">Memory threshold in bytes (default: 1GB)</param>
        public MemoryHealthCheckStrategy(long memoryThresholdBytes = 1024 * 1024 * 1024)
        {
            m_currentProcess = Process.GetCurrentProcess();
            m_memoryThresholdBytes = memoryThresholdBytes;
        }

        /// <summary>
        /// Performs the memory health check
        /// </summary>
        /// <returns>Health check result</returns>
        public async Task<IHealthCheckResult> CheckHealthAsync()
        {
            try
            {
                await Task.Yield(); // Allow async operation
                
                m_currentProcess.Refresh();
                var workingSet = m_currentProcess.WorkingSet64;
                var isHealthy = workingSet < m_memoryThresholdBytes;
                
                var data = new Dictionary<string, object>
                {
                    ["WorkingSetBytes"] = workingSet,
                    ["WorkingSetMB"] = workingSet / (1024.0 * 1024.0),
                    ["ThresholdBytes"] = m_memoryThresholdBytes,
                    ["ThresholdMB"] = m_memoryThresholdBytes / (1024.0 * 1024.0)
                };

                var message = isHealthy 
                    ? $"Memory usage is healthy ({workingSet / (1024.0 * 1024.0):F1} MB)"
                    : $"High memory usage detected ({workingSet / (1024.0 * 1024.0):F1} MB)";

                return new HealthCheckResult(isHealthy, message, data);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(false, $"Memory health check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if this strategy is applicable for the current context
        /// </summary>
        /// <returns>True if applicable, false otherwise</returns>
        public bool IsApplicable()
        {
            return true; // Memory check is always applicable
        }
    }
}
