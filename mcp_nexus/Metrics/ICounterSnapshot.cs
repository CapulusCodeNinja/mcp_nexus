namespace mcp_nexus.Metrics
{
    /// <summary>
    /// Interface for counter snapshot with proper encapsulation
    /// </summary>
    public interface ICounterSnapshot
    {
        /// <summary>Gets the counter name</summary>
        string Name { get; }

        /// <summary>Gets the counter value</summary>
        double Value { get; }

        /// <summary>Gets the counter tags</summary>
        IReadOnlyDictionary<string, string> Tags { get; }
    }
}
