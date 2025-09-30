using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Metrics;

namespace mcp_nexus_tests.Metrics
{
    /// <summary>
    /// Tests for MetricsCollector
    /// </summary>
    public class MetricsCollectorTests : IDisposable
    {
        private readonly Mock<ILogger<MetricsCollector>> m_mockLogger;
        private readonly MetricsCollector m_metricsCollector;

        public MetricsCollectorTests()
        {
            m_mockLogger = new Mock<ILogger<MetricsCollector>>();
            m_metricsCollector = new MetricsCollector(m_mockLogger.Object);
        }

        public void Dispose()
        {
            m_metricsCollector?.Dispose();
        }

        [Fact]
        public void MetricsCollector_Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Act
            var collector = new MetricsCollector(m_mockLogger.Object);

            // Assert
            Assert.NotNull(collector);
        }

        [Fact]
        public void MetricsCollector_Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MetricsCollector(null!));
        }

        [Fact]
        public void IncrementCounter_WithDefaultValue_IncrementsByOne()
        {
            // Act
            m_metricsCollector.IncrementCounter("test_counter");

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var counter = snapshot.Counters.FirstOrDefault(c => c.Name == "test_counter");
            Assert.NotNull(counter);
            Assert.Equal(1.0, counter.Value);
        }

        [Fact]
        public void IncrementCounter_WithCustomValue_IncrementsBySpecifiedValue()
        {
            // Arrange
            const double incrementValue = 5.5;

            // Act
            m_metricsCollector.IncrementCounter("test_counter", incrementValue);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var counter = snapshot.Counters.FirstOrDefault(c => c.Name == "test_counter");
            Assert.NotNull(counter);
            Assert.Equal(incrementValue, counter.Value);
        }

        [Fact]
        public void IncrementCounter_WithTags_StoresTagsCorrectly()
        {
            // Arrange
            var tags = new Dictionary<string, string>
            {
                ["environment"] = "test",
                ["service"] = "metrics"
            };

            // Act
            m_metricsCollector.IncrementCounter("test_counter", 1.0, tags);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var counter = snapshot.Counters.FirstOrDefault(c => c.Name == "test_counter");
            Assert.NotNull(counter);
            Assert.Equal(tags, counter.Tags);
        }

        [Fact]
        public void IncrementCounter_MultipleTimes_AccumulatesValues()
        {
            // Act
            m_metricsCollector.IncrementCounter("test_counter", 1.0);
            m_metricsCollector.IncrementCounter("test_counter", 2.0);
            m_metricsCollector.IncrementCounter("test_counter", 3.0);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var counter = snapshot.Counters.FirstOrDefault(c => c.Name == "test_counter");
            Assert.NotNull(counter);
            Assert.Equal(6.0, counter.Value);
        }

        [Fact]
        public void RecordHistogram_WithValue_RecordsValue()
        {
            // Arrange
            const double value = 42.5;

            // Act
            m_metricsCollector.RecordHistogram("test_histogram", value);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var histogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "test_histogram");
            Assert.NotNull(histogram);
            Assert.Equal(1, histogram.Count);
            Assert.Equal(value, histogram.Sum);
            Assert.Equal(value, histogram.Min);
            Assert.Equal(value, histogram.Max);
            Assert.Equal(value, histogram.Average);
        }

        [Fact]
        public void RecordHistogram_WithMultipleValues_CalculatesStatistics()
        {
            // Arrange
            var values = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };

            // Act
            foreach (var value in values)
            {
                m_metricsCollector.RecordHistogram("test_histogram", value);
            }

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var histogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "test_histogram");
            Assert.NotNull(histogram);
            Assert.Equal(5, histogram.Count);
            Assert.Equal(150.0, histogram.Sum);
            Assert.Equal(10.0, histogram.Min);
            Assert.Equal(50.0, histogram.Max);
            Assert.Equal(30.0, histogram.Average);
        }

        [Fact]
        public void RecordHistogram_WithTags_StoresTagsCorrectly()
        {
            // Arrange
            var tags = new Dictionary<string, string>
            {
                ["operation"] = "test",
                ["status"] = "success"
            };

            // Act
            m_metricsCollector.RecordHistogram("test_histogram", 42.0, tags);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var histogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "test_histogram");
            Assert.NotNull(histogram);
            Assert.Equal(tags, histogram.Tags);
        }

        [Fact]
        public void SetGauge_WithValue_SetsValue()
        {
            // Arrange
            const double value = 99.9;

            // Act
            m_metricsCollector.SetGauge("test_gauge", value);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var gauge = snapshot.Gauges.FirstOrDefault(g => g.Name == "test_gauge");
            Assert.NotNull(gauge);
            Assert.Equal(value, gauge.Value);
        }

        [Fact]
        public void SetGauge_WithMultipleValues_OverwritesValue()
        {
            // Act
            m_metricsCollector.SetGauge("test_gauge", 10.0);
            m_metricsCollector.SetGauge("test_gauge", 20.0);
            m_metricsCollector.SetGauge("test_gauge", 30.0);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var gauge = snapshot.Gauges.FirstOrDefault(g => g.Name == "test_gauge");
            Assert.NotNull(gauge);
            Assert.Equal(30.0, gauge.Value); // Should be the last value set
        }

        [Fact]
        public void SetGauge_WithTags_StoresTagsCorrectly()
        {
            // Arrange
            var tags = new Dictionary<string, string>
            {
                ["metric_type"] = "gauge",
                ["unit"] = "bytes"
            };

            // Act
            m_metricsCollector.SetGauge("test_gauge", 100.0, tags);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var gauge = snapshot.Gauges.FirstOrDefault(g => g.Name == "test_gauge");
            Assert.NotNull(gauge);
            Assert.Equal(tags, gauge.Tags);
        }

        [Fact]
        public void RecordExecutionTime_RecordsHistogramWithCorrectName()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(150.5);

            // Act
            m_metricsCollector.RecordExecutionTime("test_operation", duration);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var histogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "test_operation_duration_ms");
            Assert.NotNull(histogram);
            Assert.Equal(1, histogram.Count);
            Assert.Equal(150.5, histogram.Sum);
        }

        [Fact]
        public void RecordExecutionTime_WithTags_StoresTagsCorrectly()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(100.0);
            var tags = new Dictionary<string, string>
            {
                ["component"] = "test"
            };

            // Act
            m_metricsCollector.RecordExecutionTime("test_operation", duration, tags);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var histogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "test_operation_duration_ms");
            Assert.NotNull(histogram);
            Assert.Equal(tags, histogram.Tags);
        }

        [Fact]
        public void RecordCommandExecution_WithSuccess_RecordsCorrectMetrics()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(200.0);
            const string commandType = "test_command";

            // Act
            m_metricsCollector.RecordCommandExecution(commandType, duration, true);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();

            // Check execution time histogram
            var executionHistogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "command_execution_duration_ms");
            Assert.NotNull(executionHistogram);
            Assert.Equal(200.0, executionHistogram.Sum);
            Assert.Equal("test_command", executionHistogram.Tags["command_type"]);
            Assert.Equal("True", executionHistogram.Tags["success"]);

            // Check total commands counter
            var totalCounter = snapshot.Counters.FirstOrDefault(c => c.Name == "commands_total");
            Assert.NotNull(totalCounter);
            Assert.Equal(1.0, totalCounter.Value);
            Assert.Equal("test_command", totalCounter.Tags["command_type"]);
            Assert.Equal("True", totalCounter.Tags["success"]);

            // Check that failed commands counter was not incremented
            var failedCounter = snapshot.Counters.FirstOrDefault(c => c.Name == "commands_failed");
            Assert.Null(failedCounter);
        }

        [Fact]
        public void RecordCommandExecution_WithFailure_RecordsCorrectMetrics()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(300.0);
            const string commandType = "failing_command";

            // Act
            m_metricsCollector.RecordCommandExecution(commandType, duration, false);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();

            // Check execution time histogram
            var executionHistogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "command_execution_duration_ms");
            Assert.NotNull(executionHistogram);
            Assert.Equal(300.0, executionHistogram.Sum);
            Assert.Equal("failing_command", executionHistogram.Tags["command_type"]);
            Assert.Equal("False", executionHistogram.Tags["success"]);

            // Check total commands counter
            var totalCounter = snapshot.Counters.FirstOrDefault(c => c.Name == "commands_total");
            Assert.NotNull(totalCounter);
            Assert.Equal(1.0, totalCounter.Value);
            Assert.Equal("failing_command", totalCounter.Tags["command_type"]);
            Assert.Equal("False", totalCounter.Tags["success"]);

            // Check failed commands counter
            var failedCounter = snapshot.Counters.FirstOrDefault(c => c.Name == "commands_failed");
            Assert.NotNull(failedCounter);
            Assert.Equal(1.0, failedCounter.Value);
            Assert.Equal("failing_command", failedCounter.Tags["command_type"]);
            Assert.Equal("False", failedCounter.Tags["success"]);
        }

        [Fact]
        public void RecordSessionEvent_WithEventType_RecordsCorrectMetrics()
        {
            // Arrange
            const string eventType = "session_created";

            // Act
            m_metricsCollector.RecordSessionEvent(eventType);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var counter = snapshot.Counters.FirstOrDefault(c => c.Name == "session_events_total");
            Assert.NotNull(counter);
            Assert.Equal(1.0, counter.Value);
            Assert.Equal(eventType, counter.Tags["event_type"]);
        }

        [Fact]
        public void RecordSessionEvent_WithAdditionalTags_RecordsCorrectMetrics()
        {
            // Arrange
            const string eventType = "session_created";
            var additionalTags = new Dictionary<string, string>
            {
                ["user_id"] = "12345",
                ["session_id"] = "abc-def"
            };

            // Act
            m_metricsCollector.RecordSessionEvent(eventType, additionalTags);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var counter = snapshot.Counters.FirstOrDefault(c => c.Name == "session_events_total");
            Assert.NotNull(counter);
            Assert.Equal(1.0, counter.Value);
            Assert.Equal(eventType, counter.Tags["event_type"]);
            Assert.Equal("12345", counter.Tags["user_id"]);
            Assert.Equal("abc-def", counter.Tags["session_id"]);
        }

        [Fact]
        public void GetSnapshot_ReturnsValidSnapshot()
        {
            // Arrange
            m_metricsCollector.IncrementCounter("test_counter", 5.0);
            m_metricsCollector.RecordHistogram("test_histogram", 10.0);
            m_metricsCollector.SetGauge("test_gauge", 15.0);

            // Act
            var snapshot = m_metricsCollector.GetSnapshot();

            // Assert
            Assert.NotNull(snapshot);
            Assert.True(snapshot.Timestamp > DateTime.MinValue);
            Assert.True(snapshot.Timestamp <= DateTime.UtcNow);
            Assert.NotNull(snapshot.Counters);
            Assert.NotNull(snapshot.Histograms);
            Assert.NotNull(snapshot.Gauges);
        }

        [Fact]
        public void GetSnapshot_WithEmptyMetrics_ReturnsEmptySnapshot()
        {
            // Act
            var snapshot = m_metricsCollector.GetSnapshot();

            // Assert
            Assert.NotNull(snapshot);
            Assert.Empty(snapshot.Counters);
            Assert.Empty(snapshot.Histograms);
            Assert.Empty(snapshot.Gauges);
        }

        [Fact]
        public void GetSnapshot_MultipleCalls_ReturnConsistentResults()
        {
            // Arrange
            m_metricsCollector.IncrementCounter("test_counter", 1.0);

            // Act
            var snapshot1 = m_metricsCollector.GetSnapshot();
            var snapshot2 = m_metricsCollector.GetSnapshot();

            // Assert
            Assert.Equal(snapshot1.Counters.Count, snapshot2.Counters.Count);
            Assert.Equal(snapshot1.Histograms.Count, snapshot2.Histograms.Count);
            Assert.Equal(snapshot1.Gauges.Count, snapshot2.Gauges.Count);
        }

        [Fact]
        public void Dispose_DisposesCorrectly()
        {
            // Arrange
            var collector = new MetricsCollector(m_mockLogger.Object);

            // Act
            collector.Dispose();

            // Assert
            // Should not throw when disposed multiple times
            collector.Dispose();
        }

        [Fact]
        public void Dispose_MultipleTimes_HandlesGracefully()
        {
            // Arrange
            var collector = new MetricsCollector(m_mockLogger.Object);

            // Act & Assert
            collector.Dispose();
            collector.Dispose(); // Should not throw
        }

        [Fact]
        public void Histogram_WithManyValues_KeepsOnlyLast1000()
        {
            // Arrange
            const int valueCount = 1500;

            // Act
            for (int i = 0; i < valueCount; i++)
            {
                m_metricsCollector.RecordHistogram("test_histogram", i);
            }

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var histogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "test_histogram");
            Assert.NotNull(histogram);
            Assert.Equal(1000, histogram.Count); // Should be limited to 1000
        }

        [Fact]
        public void Histogram_WithEmptyValues_ReturnsZeroStatistics()
        {
            // Act
            var snapshot = m_metricsCollector.GetSnapshot();
            var histogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "empty_histogram");

            // Assert
            // No histogram should exist for empty name
            Assert.Null(histogram);
        }

        [Fact]
        public void Counter_WithNegativeValue_HandlesCorrectly()
        {
            // Act
            m_metricsCollector.IncrementCounter("test_counter", -5.0);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var counter = snapshot.Counters.FirstOrDefault(c => c.Name == "test_counter");
            Assert.NotNull(counter);
            Assert.Equal(-5.0, counter.Value);
        }

        [Fact]
        public void Gauge_WithNegativeValue_HandlesCorrectly()
        {
            // Act
            m_metricsCollector.SetGauge("test_gauge", -10.0);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var gauge = snapshot.Gauges.FirstOrDefault(g => g.Name == "test_gauge");
            Assert.NotNull(gauge);
            Assert.Equal(-10.0, gauge.Value);
        }

        [Fact]
        public void Histogram_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            m_metricsCollector.RecordHistogram("test_histogram", -1.0);
            m_metricsCollector.RecordHistogram("test_histogram", -2.0);

            // Assert
            var snapshot = m_metricsCollector.GetSnapshot();
            var histogram = snapshot.Histograms.FirstOrDefault(h => h.Name == "test_histogram");
            Assert.NotNull(histogram);
            Assert.Equal(2, histogram.Count);
            Assert.Equal(-3.0, histogram.Sum);
            Assert.Equal(-2.0, histogram.Min);
            Assert.Equal(-1.0, histogram.Max);
            Assert.Equal(-1.5, histogram.Average);
        }
    }
}
