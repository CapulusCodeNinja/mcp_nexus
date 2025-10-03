namespace mcp_nexus.Metrics
{
    /// <summary>
    /// Interface for collecting and recording application metrics.
    /// Provides methods for tracking counters, histograms, gauges, and execution times.
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Increments a counter metric by the specified value.
        /// </summary>
        /// <param name="name">The name of the counter metric.</param>
        /// <param name="value">The value to increment by (default is 1.0).</param>
        /// <param name="tags">Optional tags to associate with the metric.</param>
        void IncrementCounter(string name, double value = 1.0, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records a histogram metric with the specified value.
        /// </summary>
        /// <param name="name">The name of the histogram metric.</param>
        /// <param name="value">The value to record.</param>
        /// <param name="tags">Optional tags to associate with the metric.</param>
        void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Sets a gauge metric to the specified value.
        /// </summary>
        /// <param name="name">The name of the gauge metric.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="tags">Optional tags to associate with the metric.</param>
        void SetGauge(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records the execution time of an operation.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="duration">The duration of the operation.</param>
        /// <param name="tags">Optional tags to associate with the metric.</param>
        void RecordExecutionTime(string operationName, TimeSpan duration, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records command execution metrics.
        /// </summary>
        /// <param name="commandType">The type of command executed.</param>
        /// <param name="duration">The duration of command execution.</param>
        /// <param name="success">Whether the command executed successfully.</param>
        void RecordCommandExecution(string commandType, TimeSpan duration, bool success);

        /// <summary>
        /// Records a session event metric.
        /// </summary>
        /// <param name="eventType">The type of session event.</param>
        /// <param name="additionalTags">Optional additional tags to associate with the metric.</param>
        void RecordSessionEvent(string eventType, Dictionary<string, string>? additionalTags = null);

        /// <summary>
        /// Gets a snapshot of all current metrics.
        /// </summary>
        /// <returns>A snapshot containing all current metric values.</returns>
        MetricsSnapshot GetSnapshot();
    }
}
