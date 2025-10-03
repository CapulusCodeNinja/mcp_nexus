using System.Collections.Concurrent;
using System.Diagnostics;

namespace mcp_nexus.Metrics
{
    /// <summary>
    /// Metrics collector for observability and performance monitoring
    /// </summary>
    public class MetricsCollector : IMetricsCollector, IDisposable
    {
        private readonly ILogger<MetricsCollector> m_logger;
        private readonly ConcurrentDictionary<string, Counter> m_counters = new();
        private readonly ConcurrentDictionary<string, Histogram> m_histograms = new();
        private readonly ConcurrentDictionary<string, Gauge> m_gauges = new();
        private readonly Timer m_reportingTimer;
        private readonly TimeSpan m_reportingInterval = TimeSpan.FromMinutes(1);
        private bool m_disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCollector"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording metrics operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public MetricsCollector(ILogger<MetricsCollector> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Start periodic reporting
            m_reportingTimer = new Timer(ReportMetrics, null, m_reportingInterval, m_reportingInterval);
        }

        /// <summary>
        /// Increments a counter metric by the specified value.
        /// </summary>
        /// <param name="name">The name of the counter metric.</param>
        /// <param name="value">The value to increment the counter by. Default is 1.0.</param>
        /// <param name="tags">Optional tags to associate with the metric.</param>
        public void IncrementCounter(string name, double value = 1.0, Dictionary<string, string>? tags = null)
        {
            var counter = m_counters.GetOrAdd(name, _ => new Counter(name, tags ?? new Dictionary<string, string>()));
            counter.Increment(value);
        }

        /// <summary>
        /// Records a value in a histogram metric.
        /// </summary>
        /// <param name="name">The name of the histogram metric.</param>
        /// <param name="value">The value to record in the histogram.</param>
        /// <param name="tags">Optional tags to associate with the metric.</param>
        public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
        {
            var histogram = m_histograms.GetOrAdd(name, _ => new Histogram(name, tags ?? new Dictionary<string, string>()));
            histogram.Record(value);
        }

        /// <summary>
        /// Sets the value of a gauge metric.
        /// </summary>
        /// <param name="name">The name of the gauge metric.</param>
        /// <param name="value">The value to set the gauge to.</param>
        /// <param name="tags">Optional tags to associate with the metric.</param>
        public void SetGauge(string name, double value, Dictionary<string, string>? tags = null)
        {
            var gauge = m_gauges.GetOrAdd(name, _ => new Gauge(name, tags ?? new Dictionary<string, string>()));
            gauge.Set(value);
        }

        /// <summary>
        /// Records the execution time of an operation as a histogram metric.
        /// </summary>
        /// <param name="operationName">The name of the operation being measured.</param>
        /// <param name="duration">The duration of the operation.</param>
        /// <param name="tags">Optional tags to associate with the metric.</param>
        public void RecordExecutionTime(string operationName, TimeSpan duration, Dictionary<string, string>? tags = null)
        {
            RecordHistogram($"{operationName}_duration_ms", duration.TotalMilliseconds, tags);
        }

        /// <summary>
        /// Records command execution metrics including duration and success status.
        /// </summary>
        /// <param name="commandType">The type of command being executed.</param>
        /// <param name="duration">The duration of the command execution.</param>
        /// <param name="success">Whether the command executed successfully.</param>
        public void RecordCommandExecution(string commandType, TimeSpan duration, bool success)
        {
            var tags = new Dictionary<string, string>
            {
                ["command_type"] = commandType,
                ["success"] = success.ToString()
            };

            RecordExecutionTime("command_execution", duration, tags);
            IncrementCounter("commands_total", 1, tags);

            if (!success)
            {
                IncrementCounter("commands_failed", 1, tags);
            }
        }

        /// <summary>
        /// Records a session event as a counter metric.
        /// </summary>
        /// <param name="eventType">The type of session event.</param>
        /// <param name="additionalTags">Optional additional tags to associate with the metric.</param>
        public void RecordSessionEvent(string eventType, Dictionary<string, string>? additionalTags = null)
        {
            var tags = new Dictionary<string, string>
            {
                ["event_type"] = eventType
            };

            if (additionalTags != null)
            {
                foreach (var tag in additionalTags)
                {
                    tags[tag.Key] = tag.Value;
                }
            }

            IncrementCounter("session_events_total", 1, tags);
        }

        /// <summary>
        /// Gets a snapshot of all current metrics.
        /// </summary>
        /// <returns>A <see cref="MetricsSnapshot"/> containing all current metric values.</returns>
        public MetricsSnapshot GetSnapshot()
        {
            var snapshot = new MetricsSnapshot();
            foreach (var counter in m_counters.Values)
            {
                snapshot.AddCounter(counter.GetSnapshot());
            }
            foreach (var histogram in m_histograms.Values)
            {
                snapshot.AddHistogram(histogram.GetSnapshot());
            }
            foreach (var gauge in m_gauges.Values)
            {
                snapshot.AddGauge(gauge.GetSnapshot());
            }
            return snapshot;
        }

        /// <summary>
        /// Reports current metrics to the logger.
        /// </summary>
        /// <param name="state">The timer state (unused).</param>
        private void ReportMetrics(object? state)
        {
            try
            {
                var snapshot = GetSnapshot();
                var totalCounters = snapshot.Counters.Sum(c => c.Value);
                var totalHistograms = snapshot.Histograms.Count;
                var totalGauges = snapshot.Gauges.Count;

                m_logger.LogInformation("Metrics Report - Counters: {CounterCount}, Histograms: {HistogramCount}, Gauges: {GaugeCount}, Total Counter Value: {TotalCounters}",
                    snapshot.Counters.Count, totalHistograms, totalGauges, totalCounters);

                // Log top counters
                var topCounters = snapshot.Counters
                    .OrderByDescending(c => c.Value)
                    .Take(5)
                    .ToList();

                foreach (var counter in topCounters)
                {
                    m_logger.LogDebug("Top Counter: {Name} = {Value}", counter.Name, counter.Value);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error reporting metrics");
            }
        }

        /// <summary>
        /// Disposes of the metrics collector and stops periodic reporting.
        /// </summary>
        public void Dispose()
        {
            if (!m_disposed)
            {
                m_disposed = true;
                m_reportingTimer?.Dispose();
                m_logger.LogDebug("Metrics collector disposed");
            }
        }
    }


    /// <summary>
    /// Represents a metrics snapshot - properly encapsulated
    /// </summary>
    public class MetricsSnapshot
    {
        #region Private Fields

        private DateTime m_timestamp;
        private readonly List<CounterSnapshot> m_counters = new();
        private readonly List<HistogramSnapshot> m_histograms = new();
        private readonly List<GaugeSnapshot> m_gauges = new();

        #endregion

        #region Public Properties

        /// <summary>Gets the timestamp of the snapshot</summary>
        public DateTime Timestamp => m_timestamp;

        /// <summary>Gets the counter snapshots</summary>
        public IReadOnlyList<CounterSnapshot> Counters => m_counters.AsReadOnly();

        /// <summary>Gets the histogram snapshots</summary>
        public IReadOnlyList<HistogramSnapshot> Histograms => m_histograms.AsReadOnly();

        /// <summary>Gets the gauge snapshots</summary>
        public IReadOnlyList<GaugeSnapshot> Gauges => m_gauges.AsReadOnly();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new metrics snapshot
        /// </summary>
        public MetricsSnapshot()
        {
            m_timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new metrics snapshot with specific values.
        /// </summary>
        /// <param name="timestamp">The timestamp for the snapshot.</param>
        /// <param name="counters">The counter snapshots to include.</param>
        /// <param name="histograms">The histogram snapshots to include.</param>
        /// <param name="gauges">The gauge snapshots to include.</param>
        public MetricsSnapshot(DateTime timestamp, List<CounterSnapshot> counters, List<HistogramSnapshot> histograms, List<GaugeSnapshot> gauges)
        {
            m_timestamp = timestamp;
            m_counters = counters ?? new List<CounterSnapshot>();
            m_histograms = histograms ?? new List<HistogramSnapshot>();
            m_gauges = gauges ?? new List<GaugeSnapshot>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a counter snapshot
        /// </summary>
        /// <param name="counter">Counter snapshot to add</param>
        public void AddCounter(CounterSnapshot counter)
        {
            if (counter != null)
                m_counters.Add(counter);
        }

        /// <summary>
        /// Adds a histogram snapshot
        /// </summary>
        /// <param name="histogram">Histogram snapshot to add</param>
        public void AddHistogram(HistogramSnapshot histogram)
        {
            if (histogram != null)
                m_histograms.Add(histogram);
        }

        /// <summary>
        /// Adds a gauge snapshot
        /// </summary>
        /// <param name="gauge">Gauge snapshot to add</param>
        public void AddGauge(GaugeSnapshot gauge)
        {
            if (gauge != null)
                m_gauges.Add(gauge);
        }

        /// <summary>
        /// Clears all snapshots
        /// </summary>
        public void Clear()
        {
            m_counters.Clear();
            m_histograms.Clear();
            m_gauges.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Thread-safe counter metric for counting events.
    /// </summary>
    public class Counter
    {
        private long m_valueBits = 0; // Store double as long for Interlocked operations

        /// <summary>
        /// Gets the name of the counter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the tags associated with the counter.
        /// </summary>
        public Dictionary<string, string> Tags { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter"/> class.
        /// </summary>
        /// <param name="name">The name of the counter.</param>
        /// <param name="tags">The tags to associate with the counter.</param>
        public Counter(string name, Dictionary<string, string> tags)
        {
            Name = name;
            Tags = tags;
        }

        /// <summary>
        /// Increments the counter by the specified value.
        /// </summary>
        /// <param name="value">The value to increment by. Default is 1.0.</param>
        public void Increment(double value = 1.0)
        {
            // Lock-free increment using Interlocked and double-to-long conversion
            long initialBits, newBits;
            do
            {
                initialBits = Interlocked.Read(ref m_valueBits);
                var currentValue = BitConverter.Int64BitsToDouble(initialBits);
                var newValue = currentValue + value;
                newBits = BitConverter.DoubleToInt64Bits(newValue);
            }
            while (Interlocked.CompareExchange(ref m_valueBits, newBits, initialBits) != initialBits);
        }

        /// <summary>
        /// Gets a snapshot of the current counter value.
        /// </summary>
        /// <returns>A <see cref="CounterSnapshot"/> containing the current counter state.</returns>
        public CounterSnapshot GetSnapshot()
        {
            var valueBits = Interlocked.Read(ref m_valueBits);
            return new CounterSnapshot(
                Name,
                BitConverter.Int64BitsToDouble(valueBits),
                new Dictionary<string, string>(Tags));
        }
    }

    /// <summary>
    /// Thread-safe histogram metric for recording value distributions.
    /// </summary>
    public class Histogram
    {
        private readonly List<double> m_values = new();
        private readonly object m_lock = new();

        /// <summary>
        /// Gets the name of the histogram.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the tags associated with the histogram.
        /// </summary>
        public Dictionary<string, string> Tags { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Histogram"/> class.
        /// </summary>
        /// <param name="name">The name of the histogram.</param>
        /// <param name="tags">The tags to associate with the histogram.</param>
        public Histogram(string name, Dictionary<string, string> tags)
        {
            Name = name;
            Tags = tags;
        }

        /// <summary>
        /// Records a value in the histogram.
        /// </summary>
        /// <param name="value">The value to record.</param>
        public void Record(double value)
        {
            lock (m_lock)
            {
                m_values.Add(value);

                // Keep only last 1000 values to prevent memory growth
                if (m_values.Count > 1000)
                {
                    m_values.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Gets a snapshot of the current histogram statistics.
        /// </summary>
        /// <returns>A <see cref="HistogramSnapshot"/> containing the current histogram statistics.</returns>
        public HistogramSnapshot GetSnapshot()
        {
            lock (m_lock)
            {
                if (m_values.Count == 0)
                {
                    return new HistogramSnapshot(
                        Name, 0, 0, 0, 0, 0,
                        new Dictionary<string, string>(Tags));
                }

                var values = m_values.ToList();
                return new HistogramSnapshot(
                    Name,
                    values.Count,
                    values.Sum(),
                    values.Min(),
                    values.Max(),
                    values.Average(),
                    new Dictionary<string, string>(Tags));
            }
        }
    }

    /// <summary>
    /// Thread-safe gauge metric for recording current values.
    /// </summary>
    public class Gauge
    {
        private double m_value = 0;
        private readonly object m_lock = new();

        /// <summary>
        /// Gets the name of the gauge.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the tags associated with the gauge.
        /// </summary>
        public Dictionary<string, string> Tags { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Gauge"/> class.
        /// </summary>
        /// <param name="name">The name of the gauge.</param>
        /// <param name="tags">The tags to associate with the gauge.</param>
        public Gauge(string name, Dictionary<string, string> tags)
        {
            Name = name;
            Tags = tags;
        }

        /// <summary>
        /// Sets the current value of the gauge.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void Set(double value)
        {
            lock (m_lock)
            {
                m_value = value;
            }
        }

        /// <summary>
        /// Gets a snapshot of the current gauge value.
        /// </summary>
        /// <returns>A <see cref="GaugeSnapshot"/> containing the current gauge state.</returns>
        public GaugeSnapshot GetSnapshot()
        {
            lock (m_lock)
            {
                return new GaugeSnapshot(
                    Name,
                    m_value,
                    new Dictionary<string, string>(Tags));
            }
        }
    }

    /// <summary>
    /// Represents a counter snapshot - properly encapsulated
    /// </summary>
    public class CounterSnapshot
    {
        #region Private Fields

        private string m_name;
        private double m_value;
        private Dictionary<string, string> m_tags;

        #endregion

        #region Public Properties

        /// <summary>Gets or sets the counter name</summary>
        public string Name { get => m_name; set => m_name = value; }

        /// <summary>Gets or sets the counter value</summary>
        public double Value { get => m_value; set => m_value = value; }

        /// <summary>Gets or sets the counter tags</summary>
        public IReadOnlyDictionary<string, string> Tags { get => m_tags.AsReadOnly(); set => m_tags = new Dictionary<string, string>(value); }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new counter snapshot
        /// </summary>
        /// <param name="name">Counter name</param>
        /// <param name="value">Counter value</param>
        /// <param name="tags">Counter tags</param>
        public CounterSnapshot()
        {
            m_name = string.Empty;
            m_value = 0;
            m_tags = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new counter snapshot with specific values.
        /// </summary>
        /// <param name="name">The counter name.</param>
        /// <param name="value">The counter value.</param>
        /// <param name="tags">The counter tags.</param>
        public CounterSnapshot(string name, double value, Dictionary<string, string>? tags = null)
        {
            m_name = name ?? string.Empty;
            m_value = value;
            m_tags = tags ?? new Dictionary<string, string>();
        }

        #endregion
    }

    /// <summary>
    /// Represents a histogram snapshot - properly encapsulated
    /// </summary>
    public class HistogramSnapshot
    {
        #region Private Fields

        private string m_name;
        private int m_count;
        private double m_sum;
        private double m_min;
        private double m_max;
        private double m_average;
        private Dictionary<string, string> m_tags;

        #endregion

        #region Public Properties

        /// <summary>Gets or sets the histogram name</summary>
        public string Name { get => m_name; set => m_name = value; }

        /// <summary>Gets or sets the count of values</summary>
        public int Count { get => m_count; set => m_count = value; }

        /// <summary>Gets or sets the sum of values</summary>
        public double Sum { get => m_sum; set => m_sum = value; }

        /// <summary>Gets or sets the minimum value</summary>
        public double Min { get => m_min; set => m_min = value; }

        /// <summary>Gets or sets the maximum value</summary>
        public double Max { get => m_max; set => m_max = value; }

        /// <summary>Gets or sets the average value</summary>
        public double Average { get => m_average; set => m_average = value; }

        /// <summary>Gets or sets the histogram tags</summary>
        public IReadOnlyDictionary<string, string> Tags { get => m_tags.AsReadOnly(); set => m_tags = new Dictionary<string, string>(value); }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new histogram snapshot
        /// </summary>
        /// <param name="name">Histogram name</param>
        /// <param name="count">Count of values</param>
        /// <param name="sum">Sum of values</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <param name="average">Average value</param>
        /// <param name="tags">Histogram tags</param>
        public HistogramSnapshot(string name, int count, double sum, double min, double max, double average,
            Dictionary<string, string>? tags = null)
        {
            m_name = name ?? string.Empty;
            m_count = count;
            m_sum = sum;
            m_min = min;
            m_max = max;
            m_average = average;
            m_tags = tags ?? new Dictionary<string, string>();
        }

        #endregion
    }

    /// <summary>
    /// Represents a gauge snapshot - properly encapsulated
    /// </summary>
    public class GaugeSnapshot
    {
        #region Private Fields

        private string m_name;
        private double m_value;
        private Dictionary<string, string> m_tags;

        #endregion

        #region Public Properties

        /// <summary>Gets or sets the gauge name</summary>
        public string Name { get => m_name; set => m_name = value; }

        /// <summary>Gets or sets the gauge value</summary>
        public double Value { get => m_value; set => m_value = value; }

        /// <summary>Gets or sets the gauge tags</summary>
        public IReadOnlyDictionary<string, string> Tags { get => m_tags.AsReadOnly(); set => m_tags = new Dictionary<string, string>(value); }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new gauge snapshot with specific values.
        /// </summary>
        /// <param name="name">The gauge name.</param>
        /// <param name="value">The gauge value.</param>
        /// <param name="tags">The gauge tags.</param>
        public GaugeSnapshot(string name, double value, Dictionary<string, string>? tags = null)
        {
            m_name = name ?? string.Empty;
            m_value = value;
            m_tags = tags ?? new Dictionary<string, string>();
        }

        #endregion
    }
}
