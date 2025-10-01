namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for health check results
    /// </summary>
    public interface IHealthCheckResult
    {
        /// <summary>Gets whether the health check passed</summary>
        bool IsHealthy { get; }

        /// <summary>Gets the health check message</summary>
        string Message { get; }

        /// <summary>Gets the health check timestamp</summary>
        DateTime Timestamp { get; }

        /// <summary>Gets additional health check data</summary>
        IReadOnlyDictionary<string, object> Data { get; }
    }
}
