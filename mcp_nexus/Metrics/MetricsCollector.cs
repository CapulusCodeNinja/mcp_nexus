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

        public MetricsCollector(ILogger<MetricsCollector> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Start periodic reporting
            m_reportingTimer = new Timer(ReportMetrics, null, m_reportingInterval, m_reportingInterval);
        }

        public void IncrementCounter(string name, double value = 1.0, Dictionary<string, string>? tags = null)
        {
            var counter = m_counters.GetOrAdd(name, _ => new Counter(name, tags ?? new Dictionary<string, string>()));
            counter.Increment(value);
        }

        public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
        {
            var histogram = m_histograms.GetOrAdd(name, _ => new Histogram(name, tags ?? new Dictionary<string, string>()));
            histogram.Record(value);
        }

        public void SetGauge(string name, double value, Dictionary<string, string>? tags = null)
        {
            var gauge = m_gauges.GetOrAdd(name, _ => new Gauge(name, tags ?? new Dictionary<string, string>()));
            gauge.Set(value);
        }

        public void RecordExecutionTime(string operationName, TimeSpan duration, Dictionary<string, string>? tags = null)
        {
            RecordHistogram($"{operationName}_duration_ms", duration.TotalMilliseconds, tags);
        }

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

    public interface IMetricsCollector
    {
        void IncrementCounter(string name, double value = 1.0, Dictionary<string, string>? tags = null);
        void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null);
        void SetGauge(string name, double value, Dictionary<string, string>? tags = null);
        void RecordExecutionTime(string operationName, TimeSpan duration, Dictionary<string, string>? tags = null);
        void RecordCommandExecution(string commandType, TimeSpan duration, bool success);
        void RecordSessionEvent(string eventType, Dictionary<string, string>? additionalTags = null);
        MetricsSnapshot GetSnapshot();
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
            m_timestamp = DateTime.MinValue;
        }

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

    public class Counter
    {
        private long m_valueBits = 0; // Store double as long for Interlocked operations

        public string Name { get; }
        public Dictionary<string, string> Tags { get; }

        public Counter(string name, Dictionary<string, string> tags)
        {
            Name = name;
            Tags = tags;
        }

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

        public CounterSnapshot GetSnapshot()
        {
            var valueBits = Interlocked.Read(ref m_valueBits);
            return new CounterSnapshot(
                Name,
                BitConverter.Int64BitsToDouble(valueBits),
                new Dictionary<string, string>(Tags));
        }
    }

    public class Histogram
    {
        private readonly List<double> m_values = new();
        private readonly object m_lock = new();

        public string Name { get; }
        public Dictionary<string, string> Tags { get; }

        public Histogram(string name, Dictionary<string, string> tags)
        {
            Name = name;
            Tags = tags;
        }

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

    public class Gauge
    {
        private double m_value = 0;
        private readonly object m_lock = new();

        public string Name { get; }
        public Dictionary<string, string> Tags { get; }

        public Gauge(string name, Dictionary<string, string> tags)
        {
            Name = name;
            Tags = tags;
        }

        public void Set(double value)
        {
            lock (m_lock)
            {
                m_value = value;
            }
        }

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

        private readonly string m_name;
        private readonly double m_value;
        private readonly Dictionary<string, string> m_tags;

        #endregion

        #region Public Properties

        /// <summary>Gets the counter name</summary>
        public string Name => m_name;

        /// <summary>Gets the counter value</summary>
        public double Value => m_value;

        /// <summary>Gets the counter tags</summary>
        public IReadOnlyDictionary<string, string> Tags => m_tags.AsReadOnly();

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

        private readonly string m_name;
        private readonly int m_count;
        private readonly double m_sum;
        private readonly double m_min;
        private readonly double m_max;
        private readonly double m_average;
        private readonly Dictionary<string, string> m_tags;

        #endregion

        #region Public Properties

        /// <summary>Gets the histogram name</summary>
        public string Name => m_name;

        /// <summary>Gets the count of values</summary>
        public int Count => m_count;

        /// <summary>Gets the sum of values</summary>
        public double Sum => m_sum;

        /// <summary>Gets the minimum value</summary>
        public double Min => m_min;

        /// <summary>Gets the maximum value</summary>
        public double Max => m_max;

        /// <summary>Gets the average value</summary>
        public double Average => m_average;

        /// <summary>Gets the histogram tags</summary>
        public IReadOnlyDictionary<string, string> Tags => m_tags.AsReadOnly();

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

        private readonly string m_name;
        private readonly double m_value;
        private readonly Dictionary<string, string> m_tags;

        #endregion

        #region Public Properties

        /// <summary>Gets the gauge name</summary>
        public string Name => m_name;

        /// <summary>Gets the gauge value</summary>
        public double Value => m_value;

        /// <summary>Gets the gauge tags</summary>
        public IReadOnlyDictionary<string, string> Tags => m_tags.AsReadOnly();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new gauge snapshot
        /// </summary>
        /// <param name="name">Gauge name</param>
        /// <param name="value">Gauge value</param>
        /// <param name="tags">Gauge tags</param>
        public GaugeSnapshot(string name, double value, Dictionary<string, string>? tags = null)
        {
            m_name = name ?? string.Empty;
            m_value = value;
            m_tags = tags ?? new Dictionary<string, string>();
        }

        #endregion
    }
}
