namespace mcp_nexus.Metrics
{
    /// <summary>
    /// Interface for gauge snapshot with proper encapsulation
    /// </summary>
    public interface IGaugeSnapshot
    {
        /// <summary>Gets the gauge name</summary>
        string Name { get; }

        /// <summary>Gets the gauge value</summary>
        double Value { get; }

        /// <summary>Gets the gauge tags</summary>
        IReadOnlyDictionary<string, string> Tags { get; }
    }
}
