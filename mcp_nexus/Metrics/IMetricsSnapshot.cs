namespace mcp_nexus.Metrics
{
    /// <summary>
    /// Interface for metrics snapshot with proper encapsulation
    /// </summary>
    public interface IMetricsSnapshot
    {
        /// <summary>Gets the timestamp of the snapshot</summary>
        DateTime Timestamp { get; }

        /// <summary>Gets the counter snapshots</summary>
        IReadOnlyList<ICounterSnapshot> Counters { get; }

        /// <summary>Gets the histogram snapshots</summary>
        IReadOnlyList<IHistogramSnapshot> Histograms { get; }

        /// <summary>Gets the gauge snapshots</summary>
        IReadOnlyList<IGaugeSnapshot> Gauges { get; }

        /// <summary>
        /// Adds a counter snapshot
        /// </summary>
        /// <param name="counter">Counter snapshot to add</param>
        void AddCounter(ICounterSnapshot counter);

        /// <summary>
        /// Adds a histogram snapshot
        /// </summary>
        /// <param name="histogram">Histogram snapshot to add</param>
        void AddHistogram(IHistogramSnapshot histogram);

        /// <summary>
        /// Adds a gauge snapshot
        /// </summary>
        /// <param name="gauge">Gauge snapshot to add</param>
        void AddGauge(IGaugeSnapshot gauge);

        /// <summary>
        /// Clears all snapshots
        /// </summary>
        void Clear();
    }
}
