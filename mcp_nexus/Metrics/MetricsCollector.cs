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
            return new MetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Counters = m_counters.Values.Select(c => c.GetSnapshot()).ToList(),
                Histograms = m_histograms.Values.Select(h => h.GetSnapshot()).ToList(),
                Gauges = m_gauges.Values.Select(g => g.GetSnapshot()).ToList()
            };
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

    public class MetricsSnapshot
    {
        public DateTime Timestamp { get; set; }
        public List<CounterSnapshot> Counters { get; set; } = new();
        public List<HistogramSnapshot> Histograms { get; set; } = new();
        public List<GaugeSnapshot> Gauges { get; set; } = new();
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
            return new CounterSnapshot
            {
                Name = Name,
                Value = BitConverter.Int64BitsToDouble(valueBits),
                Tags = new Dictionary<string, string>(Tags)
            };
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
                    return new HistogramSnapshot
                    {
                        Name = Name,
                        Count = 0,
                        Sum = 0,
                        Min = 0,
                        Max = 0,
                        Average = 0,
                        Tags = new Dictionary<string, string>(Tags)
                    };
                }

                var values = m_values.ToList();
                return new HistogramSnapshot
                {
                    Name = Name,
                    Count = values.Count,
                    Sum = values.Sum(),
                    Min = values.Min(),
                    Max = values.Max(),
                    Average = values.Average(),
                    Tags = new Dictionary<string, string>(Tags)
                };
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
                return new GaugeSnapshot
                {
                    Name = Name,
                    Value = m_value,
                    Tags = new Dictionary<string, string>(Tags)
                };
            }
        }
    }

    public class CounterSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class HistogramSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Sum { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class GaugeSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}
