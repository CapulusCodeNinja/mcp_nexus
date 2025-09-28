using System;
using System.Collections.Generic;
using Xunit;
using mcp_nexus.Metrics;

namespace mcp_nexus_tests.Metrics
{
    /// <summary>
    /// Tests for Metrics data classes - simple data containers
    /// </summary>
    public class MetricsDataClassesTests
    {
        [Fact]
        public void PerformanceCounter_DefaultValues_AreCorrect()
        {
            // Act
            var counter = new PerformanceCounter();

            // Assert
            Assert.Equal(0L, counter.Total);
            Assert.Equal(0L, counter.Successful);
            Assert.Equal(0L, counter.Failed);
        }

        [Fact]
        public void PerformanceCounter_WithValues_SetsProperties()
        {
            // Arrange
            const long total = 1000L;
            const long successful = 950L;
            const long failed = 50L;

            // Act
            var counter = new PerformanceCounter
            {
                Total = total,
                Successful = successful,
                Failed = failed
            };

            // Assert
            Assert.Equal(total, counter.Total);
            Assert.Equal(successful, counter.Successful);
            Assert.Equal(failed, counter.Failed);
        }

        [Theory]
        [InlineData(0L, 0L, 0L)]
        [InlineData(1L, 1L, 0L)]
        [InlineData(100L, 95L, 5L)]
        [InlineData(1000L, 900L, 100L)]
        [InlineData(long.MaxValue, long.MaxValue, 0L)]
        [InlineData(long.MaxValue, 0L, long.MaxValue)]
        public void PerformanceCounter_WithVariousValues_SetsCorrectly(long total, long successful, long failed)
        {
            // Act
            var counter = new PerformanceCounter
            {
                Total = total,
                Successful = successful,
                Failed = failed
            };

            // Assert
            Assert.Equal(total, counter.Total);
            Assert.Equal(successful, counter.Successful);
            Assert.Equal(failed, counter.Failed);
        }

        [Fact]
        public void AdvancedHistogram_DefaultValues_AreCorrect()
        {
            // Act
            var histogram = new AdvancedHistogram();

            // Assert
            Assert.NotNull(histogram.Values);
            Assert.Empty(histogram.Values);
        }

        [Fact]
        public void AdvancedHistogram_WithValues_SetsProperties()
        {
            // Arrange
            var values = new List<double> { 1.0, 2.5, 3.7, 4.2, 5.9 };

            // Act
            var histogram = new AdvancedHistogram
            {
                Values = values
            };

            // Assert
            Assert.Equal(values, histogram.Values);
            Assert.Equal(5, histogram.Values.Count);
        }

        [Fact]
        public void AdvancedHistogram_WithEmptyValues_HandlesCorrectly()
        {
            // Act
            var histogram = new AdvancedHistogram
            {
                Values = new List<double>()
            };

            // Assert
            Assert.NotNull(histogram.Values);
            Assert.Empty(histogram.Values);
        }

        [Fact]
        public void AdvancedHistogram_WithNullValues_HandlesCorrectly()
        {
            // Act
            var histogram = new AdvancedHistogram
            {
                Values = null!
            };

            // Assert
            Assert.Null(histogram.Values);
        }

        [Theory]
        [InlineData(new double[] { })]
        [InlineData(new double[] { 1.0 })]
        [InlineData(new double[] { 1.0, 2.0, 3.0 })]
        [InlineData(new double[] { 0.0, 0.1, 0.5, 1.0, 2.0, 5.0, 10.0 })]
        [InlineData(new double[] { -1.0, 0.0, 1.0 })]
        [InlineData(new double[] { double.MinValue, double.MaxValue })]
        public void AdvancedHistogram_WithVariousValues_SetsCorrectly(double[] values)
        {
            // Arrange
            var valuesList = new List<double>(values);

            // Act
            var histogram = new AdvancedHistogram
            {
                Values = valuesList
            };

            // Assert
            Assert.Equal(valuesList, histogram.Values);
            Assert.Equal(values.Length, histogram.Values.Count);
        }

        [Fact]
        public void AdvancedMetricsSnapshot_DefaultValues_AreCorrect()
        {
            // Act
            var snapshot = new AdvancedMetricsSnapshot();

            // Assert
            Assert.Equal(DateTime.MinValue, snapshot.Timestamp);
            Assert.NotNull(snapshot.Counters);
            Assert.Empty(snapshot.Counters);
            Assert.NotNull(snapshot.Histograms);
            Assert.Empty(snapshot.Histograms);
        }

        [Fact]
        public void AdvancedMetricsSnapshot_WithValues_SetsProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var counters = new Dictionary<string, PerformanceCounter>
            {
                { "command.executions", new PerformanceCounter { Total = 100, Successful = 95, Failed = 5 } },
                { "session.creates", new PerformanceCounter { Total = 10, Successful = 10, Failed = 0 } }
            };
            var histograms = new Dictionary<string, AdvancedHistogram>
            {
                { "command.duration", new AdvancedHistogram { Values = new List<double> { 1.0, 2.0, 3.0 } } },
                { "session.duration", new AdvancedHistogram { Values = new List<double> { 10.0, 20.0 } } }
            };

            // Act
            var snapshot = new AdvancedMetricsSnapshot
            {
                Timestamp = timestamp,
                Counters = counters,
                Histograms = histograms
            };

            // Assert
            Assert.Equal(timestamp, snapshot.Timestamp);
            Assert.Equal(counters, snapshot.Counters);
            Assert.Equal(histograms, snapshot.Histograms);
            Assert.Equal(2, snapshot.Counters.Count);
            Assert.Equal(2, snapshot.Histograms.Count);
        }

        [Fact]
        public void AdvancedMetricsSnapshot_WithEmptyCollections_HandlesCorrectly()
        {
            // Act
            var snapshot = new AdvancedMetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Counters = new Dictionary<string, PerformanceCounter>(),
                Histograms = new Dictionary<string, AdvancedHistogram>()
            };

            // Assert
            Assert.True(snapshot.Timestamp > DateTime.MinValue);
            Assert.NotNull(snapshot.Counters);
            Assert.Empty(snapshot.Counters);
            Assert.NotNull(snapshot.Histograms);
            Assert.Empty(snapshot.Histograms);
        }

        [Fact]
        public void AdvancedMetricsSnapshot_WithNullCollections_HandlesCorrectly()
        {
            // Act
            var snapshot = new AdvancedMetricsSnapshot
            {
                Counters = null!,
                Histograms = null!
            };

            // Assert
            Assert.Null(snapshot.Counters);
            Assert.Null(snapshot.Histograms);
        }

        [Fact]
        public void AllMetricsClasses_CanBeInstantiated()
        {
            // Act & Assert - Just verify they can be created without throwing
            Assert.NotNull(new PerformanceCounter());
            Assert.NotNull(new AdvancedHistogram());
            Assert.NotNull(new AdvancedMetricsSnapshot());
        }

        [Fact]
        public void PerformanceCounter_Calculations_WorkCorrectly()
        {
            // Arrange
            var counter = new PerformanceCounter
            {
                Total = 1000,
                Successful = 950,
                Failed = 50
            };

            // Act & Assert
            Assert.Equal(1000, counter.Total);
            Assert.Equal(950, counter.Successful);
            Assert.Equal(50, counter.Failed);
            Assert.True(counter.Successful + counter.Failed <= counter.Total);
        }

        [Fact]
        public void AdvancedHistogram_CanAddValues()
        {
            // Arrange
            var histogram = new AdvancedHistogram();

            // Act
            histogram.Values.Add(1.0);
            histogram.Values.Add(2.0);
            histogram.Values.Add(3.0);

            // Assert
            Assert.Equal(3, histogram.Values.Count);
            Assert.Contains(1.0, histogram.Values);
            Assert.Contains(2.0, histogram.Values);
            Assert.Contains(3.0, histogram.Values);
        }

        [Fact]
        public void AdvancedMetricsSnapshot_CanAddCounters()
        {
            // Arrange
            var snapshot = new AdvancedMetricsSnapshot();

            // Act
            snapshot.Counters["test"] = new PerformanceCounter { Total = 1, Successful = 1, Failed = 0 };

            // Assert
            Assert.Single(snapshot.Counters);
            Assert.True(snapshot.Counters.ContainsKey("test"));
            Assert.Equal(1, snapshot.Counters["test"].Total);
        }

        [Fact]
        public void AdvancedMetricsSnapshot_CanAddHistograms()
        {
            // Arrange
            var snapshot = new AdvancedMetricsSnapshot();

            // Act
            snapshot.Histograms["test"] = new AdvancedHistogram { Values = new List<double> { 1.0, 2.0 } };

            // Assert
            Assert.Single(snapshot.Histograms);
            Assert.True(snapshot.Histograms.ContainsKey("test"));
            Assert.Equal(2, snapshot.Histograms["test"].Values.Count);
        }
    }
}
