using System;
using System.Collections.Generic;
using Xunit;
using mcp_nexus.Metrics;

namespace mcp_nexus_tests.Metrics
{
    /// <summary>
    /// Tests for MetricsCollector data classes - simple data containers
    /// </summary>
    public class MetricsCollectorDataClassesTests
    {
        [Fact]
        public void MetricsSnapshot_DefaultValues_AreCorrect()
        {
            // Act
            var snapshot = new MetricsSnapshot();

            // Assert
            Assert.True(snapshot.Timestamp > DateTime.MinValue);
            Assert.NotNull(snapshot.Counters);
            Assert.Empty(snapshot.Counters);
            Assert.NotNull(snapshot.Histograms);
            Assert.Empty(snapshot.Histograms);
            Assert.NotNull(snapshot.Gauges);
            Assert.Empty(snapshot.Gauges);
        }

        [Fact]
        public void MetricsSnapshot_WithValues_SetsProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var counters = new List<CounterSnapshot> { new CounterSnapshot("test-counter", 100, null) };
            var histograms = new List<HistogramSnapshot> { new HistogramSnapshot("test-histogram", 0, 0, 0, 0, 0, null) };
            var gauges = new List<GaugeSnapshot> { new GaugeSnapshot("test-gauge", 50.5, null) };

            // Act
            var snapshot = new MetricsSnapshot(timestamp, counters, histograms, gauges);

            // Assert
            Assert.Equal(timestamp, snapshot.Timestamp);
            Assert.Equal(counters, snapshot.Counters);
            Assert.Equal(histograms, snapshot.Histograms);
            Assert.Equal(gauges, snapshot.Gauges);
        }

        [Fact]
        public void Counter_Constructor_SetsProperties()
        {
            // Arrange
            var name = "test-counter";
            var tags = new Dictionary<string, string> { { "env", "test" }, { "service", "api" } };

            // Act
            var counter = new Counter(name, tags);

            // Assert
            Assert.Equal(name, counter.Name);
            Assert.Equal(tags, counter.Tags);
        }

        [Fact]
        public void Counter_Constructor_WithNullTags_HandlesGracefully()
        {
            // Act
            var counter = new Counter("test", null!);

            // Assert
            Assert.Equal("test", counter.Name);
            Assert.Null(counter.Tags);
        }

        [Fact]
        public void Counter_Constructor_WithEmptyTags_HandlesCorrectly()
        {
            // Arrange
            var emptyTags = new Dictionary<string, string>();

            // Act
            var counter = new Counter("test", emptyTags);

            // Assert
            Assert.Equal("test", counter.Name);
            Assert.Equal(emptyTags, counter.Tags);
        }

        [Fact]
        public void CounterSnapshot_DefaultValues_AreCorrect()
        {
            // Act
            var snapshot = new CounterSnapshot();

            // Assert
            Assert.Equal(string.Empty, snapshot.Name);
            Assert.Equal(0, snapshot.Value);
        }

        [Fact]
        public void CounterSnapshot_WithValues_SetsProperties()
        {
            // Act
            var snapshot = new CounterSnapshot("test-counter", 42.5);

            // Assert
            Assert.Equal("test-counter", snapshot.Name);
            Assert.Equal(42.5, snapshot.Value);
        }

        [Fact]
        public void Histogram_Constructor_SetsProperties()
        {
            // Arrange
            var name = "test-histogram";
            var tags = new Dictionary<string, string> { { "env", "test" } };

            // Act
            var histogram = new Histogram(name, tags);

            // Assert
            Assert.Equal(name, histogram.Name);
            Assert.Equal(tags, histogram.Tags);
        }

        [Fact]
        public void Histogram_Constructor_WithNullTags_HandlesGracefully()
        {
            // Act
            var histogram = new Histogram("test", null!);

            // Assert
            Assert.Equal("test", histogram.Name);
            Assert.Null(histogram.Tags);
        }

        [Fact]
        public void HistogramSnapshot_DefaultValues_AreCorrect()
        {
            // Act
            var snapshot = new HistogramSnapshot(string.Empty, 0, 0, 0, 0, 0, null);

            // Assert
            Assert.Equal(string.Empty, snapshot.Name);
            Assert.Equal(0, snapshot.Count);
            Assert.Equal(0, snapshot.Sum);
            Assert.Equal(0, snapshot.Min);
            Assert.Equal(0, snapshot.Max);
            Assert.Equal(0, snapshot.Average);
            Assert.NotNull(snapshot.Tags);
            Assert.Empty(snapshot.Tags);
        }

        [Fact]
        public void HistogramSnapshot_WithValues_SetsProperties()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "env", "test" }, { "service", "api" } };

            // Act
            var snapshot = new HistogramSnapshot("test-histogram", 100, 1000.0, 1.0, 50.0, 10.0, tags);

            // Assert
            Assert.Equal("test-histogram", snapshot.Name);
            Assert.Equal(100, snapshot.Count);
            Assert.Equal(1000.0, snapshot.Sum);
            Assert.Equal(1.0, snapshot.Min);
            Assert.Equal(50.0, snapshot.Max);
            Assert.Equal(10.0, snapshot.Average);
            Assert.Equal(tags, snapshot.Tags);
        }

        [Fact]
        public void Gauge_Constructor_SetsProperties()
        {
            // Arrange
            var name = "test-gauge";
            var tags = new Dictionary<string, string> { { "env", "test" } };

            // Act
            var gauge = new Gauge(name, tags);

            // Assert
            Assert.Equal(name, gauge.Name);
            Assert.Equal(tags, gauge.Tags);
        }

        [Fact]
        public void Gauge_Constructor_WithNullTags_HandlesGracefully()
        {
            // Act
            var gauge = new Gauge("test", null!);

            // Assert
            Assert.Equal("test", gauge.Name);
            Assert.Null(gauge.Tags);
        }

        [Fact]
        public void GaugeSnapshot_DefaultValues_AreCorrect()
        {
            // Act
            var snapshot = new GaugeSnapshot(string.Empty, 0, null);

            // Assert
            Assert.Equal(string.Empty, snapshot.Name);
            Assert.Equal(0, snapshot.Value);
        }

        [Fact]
        public void GaugeSnapshot_WithValues_SetsProperties()
        {
            // Act
            var snapshot = new GaugeSnapshot("test-gauge", 75.5, null);

            // Assert
            Assert.Equal("test-gauge", snapshot.Name);
            Assert.Equal(75.5, snapshot.Value);
        }

        [Fact]
        public void CounterSnapshot_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var snapshot = new CounterSnapshot
            {
                Name = "negative-counter",
                Value = -42.5
            };

            // Assert
            Assert.Equal("negative-counter", snapshot.Name);
            Assert.Equal(-42.5, snapshot.Value);
        }

        [Fact]
        public void HistogramSnapshot_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var snapshot = new HistogramSnapshot("negative-histogram", -100, -1000.0, -50.0, -1.0, -10.0, null);

            // Assert
            Assert.Equal("negative-histogram", snapshot.Name);
            Assert.Equal(-100, snapshot.Count);
            Assert.Equal(-1000.0, snapshot.Sum);
            Assert.Equal(-50.0, snapshot.Min);
            Assert.Equal(-1.0, snapshot.Max);
            Assert.Equal(-10.0, snapshot.Average);
        }

        [Fact]
        public void GaugeSnapshot_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var snapshot = new GaugeSnapshot("negative-gauge", -75.5, null);

            // Assert
            Assert.Equal("negative-gauge", snapshot.Name);
            Assert.Equal(-75.5, snapshot.Value);
        }

        [Fact]
        public void CounterSnapshot_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var snapshot = new CounterSnapshot
            {
                Name = "max-counter",
                Value = double.MaxValue
            };

            // Assert
            Assert.Equal("max-counter", snapshot.Name);
            Assert.Equal(double.MaxValue, snapshot.Value);
        }

        [Fact]
        public void HistogramSnapshot_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var snapshot = new HistogramSnapshot("max-histogram", int.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue, null);

            // Assert
            Assert.Equal("max-histogram", snapshot.Name);
            Assert.Equal(int.MaxValue, snapshot.Count);
            Assert.Equal(double.MaxValue, snapshot.Sum);
            Assert.Equal(double.MaxValue, snapshot.Min);
            Assert.Equal(double.MaxValue, snapshot.Max);
            Assert.Equal(double.MaxValue, snapshot.Average);
        }

        [Fact]
        public void GaugeSnapshot_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var snapshot = new GaugeSnapshot("max-gauge", double.MaxValue, null);

            // Assert
            Assert.Equal("max-gauge", snapshot.Name);
            Assert.Equal(double.MaxValue, snapshot.Value);
        }
    }
}
