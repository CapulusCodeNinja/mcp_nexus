namespace mcp_nexus.Metrics
{
    /// <summary>
    /// Interface for histogram snapshot with proper encapsulation
    /// </summary>
    public interface IHistogramSnapshot
    {
        /// <summary>Gets the histogram name</summary>
        string Name { get; }

        /// <summary>Gets the count of values</summary>
        int Count { get; }

        /// <summary>Gets the sum of values</summary>
        double Sum { get; }

        /// <summary>Gets the minimum value</summary>
        double Min { get; }

        /// <summary>Gets the maximum value</summary>
        double Max { get; }

        /// <summary>Gets the average value</summary>
        double Average { get; }

        /// <summary>Gets the histogram tags</summary>
        IReadOnlyDictionary<string, string> Tags { get; }
    }
}
