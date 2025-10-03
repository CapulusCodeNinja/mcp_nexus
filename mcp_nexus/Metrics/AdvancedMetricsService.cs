using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Metrics
{
    /// <summary>
    /// Advanced metrics service for comprehensive performance monitoring
    /// </summary>
    public class AdvancedMetricsService : IDisposable
    {
        #region Private Fields

        private readonly ILogger<AdvancedMetricsService> m_logger;
        private readonly ConcurrentDictionary<string, PerformanceCounter> m_counters = new();
        private readonly ConcurrentDictionary<string, AdvancedHistogram> m_histograms = new();
        private readonly Timer m_metricsTimer;
        private volatile bool m_disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AdvancedMetricsService class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public AdvancedMetricsService(ILogger<AdvancedMetricsService> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Start metrics collection timer (every 30 seconds)
            m_metricsTimer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            m_logger.LogInformation("🚀 AdvancedMetricsService initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Records command execution metrics
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="command">The command executed</param>
        /// <param name="duration">The execution duration</param>
        /// <param name="success">Whether the command succeeded</param>
        public void RecordCommandExecution(string sessionId, string command, TimeSpan duration, bool success)
        {
            if (m_disposed) return;

            var counterKey = $"commands.{sessionId}";
            var histogramKey = $"command_duration.{sessionId}";

            // Record counter
            m_counters.AddOrUpdate(counterKey,
                new PerformanceCounter { Total = 1, Successful = success ? 1 : 0, Failed = success ? 0 : 1 },
                (key, existing) => new PerformanceCounter
                {
                    Total = existing.Total + 1,
                    Successful = existing.Successful + (success ? 1 : 0),
                    Failed = existing.Failed + (success ? 0 : 1)
                });

            // Record histogram
            m_histograms.AddOrUpdate(histogramKey,
                new AdvancedHistogram { Values = new List<double> { duration.TotalMilliseconds } },
                (key, existing) =>
                {
                    lock (existing.Values)
                    {
                        existing.Values.Add(duration.TotalMilliseconds);
                        // Keep only last 1000 values to prevent memory leaks
                        if (existing.Values.Count > 1000)
                        {
                            existing.Values.RemoveAt(0);
                        }
                    }
                    return existing;
                });

            m_logger.LogDebug("📊 Recorded command execution: {SessionId}, {Command}, {Duration}ms, {Success}",
                sessionId, command, duration.TotalMilliseconds, success);
        }

        /// <summary>
        /// Records session event metrics
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="eventType">The type of event</param>
        /// <param name="duration">Optional duration of the event</param>
        public void RecordSessionEvent(string sessionId, string eventType, TimeSpan? duration = null)
        {
            if (m_disposed) return;

            var counterKey = $"sessions.{eventType}";
            m_counters.AddOrUpdate(counterKey,
                new PerformanceCounter { Total = 1, Successful = 1, Failed = 0 },
                (key, existing) => new PerformanceCounter
                {
                    Total = existing.Total + 1,
                    Successful = existing.Successful + 1,
                    Failed = existing.Failed
                });

            if (duration.HasValue)
            {
                var histogramKey = $"session_{eventType}_duration";
                m_histograms.AddOrUpdate(histogramKey,
                    new AdvancedHistogram { Values = new List<double> { duration.Value.TotalMilliseconds } },
                    (key, existing) =>
                    {
                        lock (existing.Values)
                        {
                            existing.Values.Add(duration.Value.TotalMilliseconds);
                            if (existing.Values.Count > 1000)
                            {
                                existing.Values.RemoveAt(0);
                            }
                        }
                        return existing;
                    });
            }

            m_logger.LogDebug("📊 Recorded session event: {SessionId}, {EventType}, {Duration}ms",
                sessionId, eventType, duration?.TotalMilliseconds);
        }

        /// <summary>
        /// Gets a snapshot of current metrics
        /// </summary>
        /// <returns>A snapshot of current metrics</returns>
        public AdvancedMetricsSnapshot GetMetricsSnapshot()
        {
            if (m_disposed) return new AdvancedMetricsSnapshot();

            var snapshot = new AdvancedMetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Counters = new Dictionary<string, PerformanceCounter>(m_counters),
                Histograms = new Dictionary<string, AdvancedHistogram>()
            };

            // Create thread-safe copies of histograms
            foreach (var kvp in m_histograms)
            {
                lock (kvp.Value.Values)
                {
                    snapshot.Histograms[kvp.Key] = new AdvancedHistogram
                    {
                        Values = new List<double>(kvp.Value.Values)
                    };
                }
            }

            return snapshot;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Collects and logs current metrics.
        /// </summary>
        /// <param name="state">The timer state (unused).</param>
        private void CollectMetrics(object? state)
        {
            if (m_disposed) return;

            try
            {
                var snapshot = GetMetricsSnapshot();
                LogMetricsSummary(snapshot);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error collecting metrics");
            }
        }

        /// <summary>
        /// Logs a summary of the current metrics snapshot.
        /// </summary>
        /// <param name="snapshot">The metrics snapshot to log.</param>
        private void LogMetricsSummary(AdvancedMetricsSnapshot snapshot)
        {
            m_logger.LogInformation("📊 METRICS SUMMARY - {Timestamp}", snapshot.Timestamp);

            foreach (var counter in snapshot.Counters)
            {
                var successRate = counter.Value.Total > 0 ? (double)counter.Value.Successful / counter.Value.Total * 100 : 0;
                m_logger.LogInformation("  {Key}: Total={Total}, Success={Success}, Failed={Failed}, SuccessRate={SuccessRate:F1}%",
                    counter.Key, counter.Value.Total, counter.Value.Successful, counter.Value.Failed, successRate);
            }

            foreach (var histogram in snapshot.Histograms)
            {
                if (histogram.Value.Values.Count > 0)
                {
                    var values = histogram.Value.Values;
                    var avg = values.Average();
                    var min = values.Min();
                    var max = values.Max();
                    var p95 = CalculatePercentile(values, 95);
                    var p99 = CalculatePercentile(values, 99);

                    m_logger.LogInformation("  {Key}: Count={Count}, Avg={Avg:F1}ms, Min={Min:F1}ms, Max={Max:F1}ms, P95={P95:F1}ms, P99={P99:F1}ms",
                        histogram.Key, values.Count, avg, min, max, p95, p99);
                }
            }
        }

        private static double CalculatePercentile(List<double> values, int percentile)
        {
            if (values.Count == 0) return 0;

            var sorted = values.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
            return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the metrics service
        /// </summary>
        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            m_metricsTimer?.Dispose();
            m_logger.LogInformation("📊 AdvancedMetricsService disposed");
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Represents a performance counter
    /// </summary>
    public class PerformanceCounter
    {
        /// <summary>
        /// Total count
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// Successful count
        /// </summary>
        public long Successful { get; set; }

        /// <summary>
        /// Failed count
        /// </summary>
        public long Failed { get; set; }
    }

    /// <summary>
    /// Represents a histogram for tracking value distributions
    /// </summary>
    public class AdvancedHistogram
    {
        /// <summary>
        /// The values in the histogram
        /// </summary>
        public List<double> Values { get; set; } = new();
    }

    /// <summary>
    /// Represents a snapshot of metrics at a point in time
    /// </summary>
    public class AdvancedMetricsSnapshot
    {
        /// <summary>
        /// The timestamp when the snapshot was taken
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The performance counters
        /// </summary>
        public Dictionary<string, PerformanceCounter> Counters { get; set; } = new();

        /// <summary>
        /// The histograms
        /// </summary>
        public Dictionary<string, AdvancedHistogram> Histograms { get; set; } = new();
    }

    #endregion
}
