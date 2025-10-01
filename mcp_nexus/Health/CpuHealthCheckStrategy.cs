using System.Diagnostics;

namespace mcp_nexus.Health
{
    /// <summary>
    /// CPU health check strategy using Strategy Pattern
    /// </summary>
    public class CpuHealthCheckStrategy : IHealthCheckStrategy
    {
        private readonly Process m_currentProcess;
        private readonly double m_cpuThresholdPercent;

        /// <summary>
        /// Gets the name of the health check strategy
        /// </summary>
        public string StrategyName => "CPU Health Check";

        /// <summary>
        /// Initializes a new CPU health check strategy
        /// </summary>
        /// <param name="cpuThresholdPercent">CPU threshold percentage (default: 80%)</param>
        public CpuHealthCheckStrategy(double cpuThresholdPercent = 80.0)
        {
            m_currentProcess = Process.GetCurrentProcess();
            m_cpuThresholdPercent = cpuThresholdPercent;
        }

        /// <summary>
        /// Performs the CPU health check
        /// </summary>
        /// <returns>Health check result</returns>
        public async Task<IHealthCheckResult> CheckHealthAsync()
        {
            try
            {
                await Task.Yield(); // Allow async operation

                var totalProcessorTime = m_currentProcess.TotalProcessorTime;
                var cpuUsage = totalProcessorTime.TotalMilliseconds / Environment.TickCount;
                var cpuUsagePercent = Math.Min(100, cpuUsage * 100);
                var isHealthy = cpuUsagePercent < m_cpuThresholdPercent;

                var data = new Dictionary<string, object>
                {
                    ["CpuUsagePercent"] = cpuUsagePercent,
                    ["TotalProcessorTime"] = totalProcessorTime,
                    ["ThresholdPercent"] = m_cpuThresholdPercent
                };

                var message = isHealthy
                    ? $"CPU usage is healthy ({cpuUsagePercent:F1}%)"
                    : $"High CPU usage detected ({cpuUsagePercent:F1}%)";

                return new HealthCheckResult(isHealthy, message, data);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(false, $"CPU health check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if this strategy is applicable for the current context
        /// </summary>
        /// <returns>True if applicable, false otherwise</returns>
        public bool IsApplicable()
        {
            return true; // CPU check is always applicable
        }
    }
}
