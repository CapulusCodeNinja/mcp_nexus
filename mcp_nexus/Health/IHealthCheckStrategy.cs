namespace mcp_nexus.Health
{
    /// <summary>
    /// Strategy interface for health check implementations using Strategy Pattern
    /// </summary>
    public interface IHealthCheckStrategy
    {
        /// <summary>
        /// Gets the name of the health check strategy
        /// </summary>
        string StrategyName { get; }

        /// <summary>
        /// Performs the health check
        /// </summary>
        /// <returns>Health check result</returns>
        Task<IHealthCheckResult> CheckHealthAsync();

        /// <summary>
        /// Checks if this strategy is applicable for the current context
        /// </summary>
        /// <returns>True if applicable, false otherwise</returns>
        bool IsApplicable();
    }
}
