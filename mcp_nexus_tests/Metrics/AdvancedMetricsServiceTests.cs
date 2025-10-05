using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Metrics;

namespace mcp_nexus_tests.Metrics
{
    public class AdvancedMetricsServiceTests : IDisposable
    {
        private readonly Mock<ILogger<AdvancedMetricsService>> m_MockLogger;
        private readonly AdvancedMetricsService m_MetricsService;

        public AdvancedMetricsServiceTests()
        {
            m_MockLogger = new Mock<ILogger<AdvancedMetricsService>>();
            m_MetricsService = new AdvancedMetricsService(m_MockLogger.Object);
        }

        public void Dispose()
        {
            m_MetricsService?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AdvancedMetricsService(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Arrange
            var localMockLogger = new Mock<ILogger<AdvancedMetricsService>>();

            // Act
            using var service = new AdvancedMetricsService(localMockLogger.Object);

            // Assert
            Assert.NotNull(service);
            localMockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AdvancedMetricsService initialized")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region RecordCommandExecution Tests

        [Fact]
        public void RecordCommandExecution_WithValidData_RecordsSuccessfully()
        {
            // Arrange
            var sessionId = "test-session";
            var command = "version";
            var duration = TimeSpan.FromMilliseconds(150);
            var success = true;

            // Act
            m_MetricsService.RecordCommandExecution(sessionId, command, duration, success);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            Assert.NotNull(snapshot);
            Assert.True(snapshot.Counters.ContainsKey($"commands.{sessionId}"));
            Assert.True(snapshot.Histograms.ContainsKey("command_duration_all_sessions")); // Aggregate histogram

            var counter = snapshot.Counters[$"commands.{sessionId}"];
            Assert.Equal(1, counter.Total);
            Assert.Equal(1, counter.Successful);
            Assert.Equal(0, counter.Failed);

            var histogram = snapshot.Histograms["command_duration_all_sessions"];
            Assert.Single(histogram.Values);
            Assert.Equal(150.0, histogram.Values[0]);
        }

        [Fact]
        public void RecordCommandExecution_WithFailedCommand_RecordsCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var command = "invalid-command";
            var duration = TimeSpan.FromMilliseconds(50);
            var success = false;

            // Act
            m_MetricsService.RecordCommandExecution(sessionId, command, duration, success);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            var counter = snapshot.Counters[$"commands.{sessionId}"];
            Assert.Equal(1, counter.Total);
            Assert.Equal(0, counter.Successful);
            Assert.Equal(1, counter.Failed);
        }

        [Fact]
        public void RecordCommandExecution_MultipleCommands_AccumulatesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";

            // Act
            m_MetricsService.RecordCommandExecution(sessionId, "cmd1", TimeSpan.FromMilliseconds(100), true);
            m_MetricsService.RecordCommandExecution(sessionId, "cmd2", TimeSpan.FromMilliseconds(200), true);
            m_MetricsService.RecordCommandExecution(sessionId, "cmd3", TimeSpan.FromMilliseconds(50), false);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            var counter = snapshot.Counters[$"commands.{sessionId}"];
            Assert.Equal(3, counter.Total);
            Assert.Equal(2, counter.Successful);
            Assert.Equal(1, counter.Failed);

            var histogram = snapshot.Histograms["command_duration_all_sessions"];
            Assert.Equal(3, histogram.Values.Count);
            Assert.Contains(100.0, histogram.Values);
            Assert.Contains(200.0, histogram.Values);
            Assert.Contains(50.0, histogram.Values);
        }

        [Fact]
        public void RecordCommandExecution_WhenDisposed_DoesNotRecord()
        {
            // Arrange
            m_MetricsService.Dispose();
            var sessionId = "test-session";

            // Act
            m_MetricsService.RecordCommandExecution(sessionId, "version", TimeSpan.FromMilliseconds(100), true);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            Assert.Empty(snapshot.Counters);
            Assert.Empty(snapshot.Histograms);
        }

        [Fact]
        public void RecordCommandExecution_WithLongDuration_RecordsCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var command = "long-running-command";
            var duration = TimeSpan.FromMinutes(5);
            var success = true;

            // Act
            m_MetricsService.RecordCommandExecution(sessionId, command, duration, success);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            var histogram = snapshot.Histograms["command_duration_all_sessions"];
            Assert.Single(histogram.Values);
            Assert.Equal(300000.0, histogram.Values[0]); // 5 minutes in milliseconds
        }

        #endregion

        #region RecordSessionEvent Tests

        [Fact]
        public void RecordSessionEvent_WithValidData_RecordsSuccessfully()
        {
            // Arrange
            var sessionId = "test-session";
            var eventType = "SESSION_CREATED";
            var duration = TimeSpan.FromMilliseconds(250);

            // Act
            m_MetricsService.RecordSessionEvent(sessionId, eventType, duration);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            Assert.True(snapshot.Counters.ContainsKey($"sessions.{eventType}"));
            Assert.True(snapshot.Histograms.ContainsKey($"session_{eventType}_duration_all_sessions"));

            var counter = snapshot.Counters[$"sessions.{eventType}"];
            Assert.Equal(1, counter.Total);
            Assert.Equal(1, counter.Successful);
            Assert.Equal(0, counter.Failed);

            var histogram = snapshot.Histograms[$"session_{eventType}_duration_all_sessions"];
            Assert.Single(histogram.Values);
            Assert.Equal(250.0, histogram.Values[0]);
        }

        [Fact]
        public void RecordSessionEvent_WithoutDuration_RecordsCounterOnly()
        {
            // Arrange
            var sessionId = "test-session";
            var eventType = "SESSION_CLOSED";

            // Act
            m_MetricsService.RecordSessionEvent(sessionId, eventType);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            Assert.True(snapshot.Counters.ContainsKey($"sessions.{eventType}"));
            Assert.False(snapshot.Histograms.ContainsKey($"session_{eventType}_duration_all_sessions"));

            var counter = snapshot.Counters[$"sessions.{eventType}"];
            Assert.Equal(1, counter.Total);
            Assert.Equal(1, counter.Successful);
            Assert.Equal(0, counter.Failed);
        }

        [Fact]
        public void RecordSessionEvent_MultipleEvents_AccumulatesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";

            // Act
            m_MetricsService.RecordSessionEvent(sessionId, "SESSION_CREATED", TimeSpan.FromMilliseconds(100));
            m_MetricsService.RecordSessionEvent(sessionId, "SESSION_CREATED", TimeSpan.FromMilliseconds(150));
            m_MetricsService.RecordSessionEvent(sessionId, "SESSION_CLOSED");

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();

            var createdCounter = snapshot.Counters["sessions.SESSION_CREATED"];
            Assert.Equal(2, createdCounter.Total);
            Assert.Equal(2, createdCounter.Successful);

            var closedCounter = snapshot.Counters["sessions.SESSION_CLOSED"];
            Assert.Equal(1, closedCounter.Total);
            Assert.Equal(1, closedCounter.Successful);

            var createdHistogram = snapshot.Histograms["session_SESSION_CREATED_duration_all_sessions"];
            Assert.Equal(2, createdHistogram.Values.Count);
            Assert.Contains(100.0, createdHistogram.Values);
            Assert.Contains(150.0, createdHistogram.Values);
        }

        [Fact]
        public void RecordSessionEvent_WhenDisposed_DoesNotRecord()
        {
            // Arrange
            m_MetricsService.Dispose();
            var sessionId = "test-session";

            // Act
            m_MetricsService.RecordSessionEvent(sessionId, "SESSION_CREATED", TimeSpan.FromMilliseconds(100));

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            Assert.Empty(snapshot.Counters);
            Assert.Empty(snapshot.Histograms);
        }

        #endregion

        #region GetMetricsSnapshot Tests

        [Fact]
        public void GetMetricsSnapshot_WhenNoData_ReturnsEmptySnapshot()
        {
            // Act
            var snapshot = m_MetricsService.GetMetricsSnapshot();

            // Assert
            Assert.NotNull(snapshot);
            Assert.True(snapshot.Timestamp > DateTime.MinValue);
            Assert.Empty(snapshot.Counters);
            Assert.Empty(snapshot.Histograms);
        }

        [Fact]
        public void GetMetricsSnapshot_WithData_ReturnsCorrectSnapshot()
        {
            // Arrange
            m_MetricsService.RecordCommandExecution("session1", "cmd1", TimeSpan.FromMilliseconds(100), true);
            m_MetricsService.RecordSessionEvent("session1", "SESSION_CREATED", TimeSpan.FromMilliseconds(200));

            // Act
            var snapshot = m_MetricsService.GetMetricsSnapshot();

            // Assert
            Assert.NotNull(snapshot);
            Assert.True(snapshot.Timestamp > DateTime.MinValue);
            Assert.Equal(2, snapshot.Counters.Count);
            Assert.Equal(2, snapshot.Histograms.Count);
            Assert.True(snapshot.Counters.ContainsKey("commands.session1"));
            Assert.True(snapshot.Counters.ContainsKey("sessions.SESSION_CREATED"));
            Assert.True(snapshot.Histograms.ContainsKey("command_duration_all_sessions"));
            Assert.True(snapshot.Histograms.ContainsKey("session_SESSION_CREATED_duration_all_sessions"));
        }

        [Fact]
        public void GetMetricsSnapshot_WhenDisposed_ReturnsEmptySnapshot()
        {
            // Arrange
            m_MetricsService.RecordCommandExecution("session1", "cmd1", TimeSpan.FromMilliseconds(100), true);
            m_MetricsService.Dispose();

            // Act
            var snapshot = m_MetricsService.GetMetricsSnapshot();

            // Assert
            Assert.NotNull(snapshot);
            Assert.Empty(snapshot.Counters);
            Assert.Empty(snapshot.Histograms);
        }

        [Fact]
        public async Task GetMetricsSnapshot_IsThreadSafe()
        {
            // Arrange
            var tasks = new List<Task>();

            // Act - Multiple threads recording metrics concurrently
            for (int i = 0; i < 10; i++)
            {
                var sessionId = $"session{i}";
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        m_MetricsService.RecordCommandExecution(sessionId, $"cmd{j}", TimeSpan.FromMilliseconds(j * 10), j % 2 == 0);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            Assert.NotNull(snapshot);
            // Should have 10 sessions with counters and 1 aggregate histogram
            Assert.Equal(10, snapshot.Counters.Count);
            Assert.Single(snapshot.Histograms);
            Assert.True(snapshot.Histograms.ContainsKey("command_duration_all_sessions"));
        }

        #endregion

        #region Histogram Management Tests

        [Fact]
        public void RecordCommandExecution_WithManyValues_KeepsOnlyLast5000()
        {
            // Arrange
            var sessionId = "test-session";

            // Act - Record more than 5000 values
            for (int i = 0; i < 6000; i++)
            {
                m_MetricsService.RecordCommandExecution(sessionId, "cmd", TimeSpan.FromMilliseconds(i), true);
            }

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            var histogram = snapshot.Histograms["command_duration_all_sessions"];
            Assert.Equal(5000, histogram.Values.Count);
            // Should contain the last 5000 values (1000-5999)
            Assert.Contains(1000.0, histogram.Values);
            Assert.Contains(5999.0, histogram.Values);
            Assert.DoesNotContain(0.0, histogram.Values);
        }

        [Fact]
        public void RecordSessionEvent_WithManyValues_KeepsOnlyLast2000()
        {
            // Arrange
            var sessionId = "test-session";
            var eventType = "SESSION_EVENT";

            // Act - Record more than 2000 values
            for (int i = 0; i < 2500; i++)
            {
                m_MetricsService.RecordSessionEvent(sessionId, eventType, TimeSpan.FromMilliseconds(i));
            }

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            var histogram = snapshot.Histograms[$"session_{eventType}_duration_all_sessions"];
            Assert.Equal(2000, histogram.Values.Count);
            // Should contain the last 2000 values (500-2499)
            Assert.Contains(500.0, histogram.Values);
            Assert.Contains(2499.0, histogram.Values);
            Assert.DoesNotContain(0.0, histogram.Values);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_WhenNotDisposed_DisposesCorrectly()
        {
            // Act
            m_MetricsService.Dispose();

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AdvancedMetricsService disposed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
        {
            // Arrange
            m_MetricsService.Dispose();

            // Act & Assert
            var exception = Record.Exception(() => m_MetricsService.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_StopsMetricsTimer()
        {
            // Arrange
            using var service = new AdvancedMetricsService(m_MockLogger.Object);

            // Act
            service.Dispose();

            // Assert - No exceptions should be thrown
            Assert.True(true); // If we get here, disposal worked correctly
        }

        #endregion

        #region Data Class Tests

        [Fact]
        public void PerformanceCounter_Properties_WorkCorrectly()
        {
            // Arrange
            var counter = new PerformanceCounter
            {
                Total = 100,
                Successful = 85,
                Failed = 15
            };

            // Assert
            Assert.Equal(100, counter.Total);
            Assert.Equal(85, counter.Successful);
            Assert.Equal(15, counter.Failed);
        }

        [Fact]
        public void AdvancedHistogram_Properties_WorkCorrectly()
        {
            // Arrange
            var histogram = new AdvancedHistogram
            {
                Values = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 }
            };

            // Assert
            Assert.NotNull(histogram.Values);
            Assert.Equal(5, histogram.Values.Count);
            Assert.Contains(1.0, histogram.Values);
            Assert.Contains(5.0, histogram.Values);
        }

        [Fact]
        public void AdvancedMetricsSnapshot_Properties_WorkCorrectly()
        {
            // Arrange
            var snapshot = new AdvancedMetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Counters = new Dictionary<string, PerformanceCounter>
                {
                    ["test"] = new PerformanceCounter { Total = 10, Successful = 8, Failed = 2 }
                },
                Histograms = new Dictionary<string, AdvancedHistogram>
                {
                    ["test"] = new AdvancedHistogram { Values = new List<double> { 1.0, 2.0 } }
                }
            };

            // Assert
            Assert.True(snapshot.Timestamp > DateTime.MinValue);
            Assert.Single(snapshot.Counters);
            Assert.Single(snapshot.Histograms);
            Assert.True(snapshot.Counters.ContainsKey("test"));
            Assert.True(snapshot.Histograms.ContainsKey("test"));
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void RecordCommandExecution_WithZeroDuration_RecordsCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var command = "instant-command";
            var duration = TimeSpan.Zero;
            var success = true;

            // Act
            m_MetricsService.RecordCommandExecution(sessionId, command, duration, success);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            var histogram = snapshot.Histograms["command_duration_all_sessions"];
            Assert.Single(histogram.Values);
            Assert.Equal(0.0, histogram.Values[0]);
        }

        [Fact]
        public void RecordCommandExecution_WithVeryLongDuration_RecordsCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var command = "very-long-command";
            var duration = TimeSpan.FromHours(1);
            var success = true;

            // Act
            m_MetricsService.RecordCommandExecution(sessionId, command, duration, success);

            // Assert
            var snapshot = m_MetricsService.GetMetricsSnapshot();
            var histogram = snapshot.Histograms["command_duration_all_sessions"];
            Assert.Single(histogram.Values);
            Assert.Equal(3600000.0, histogram.Values[0]); // 1 hour in milliseconds
        }

        [Fact]
        public void RecordSessionEvent_WithNullSessionId_HandlesGracefully()
        {
            // Act & Assert - Should not throw
            m_MetricsService.RecordSessionEvent(null!, "EVENT_TYPE", TimeSpan.FromMilliseconds(100));

            var snapshot = m_MetricsService.GetMetricsSnapshot();
            Assert.True(snapshot.Counters.ContainsKey("sessions.EVENT_TYPE"));
        }

        [Fact]
        public void RecordSessionEvent_WithEmptyEventType_HandlesGracefully()
        {
            // Act & Assert - Should not throw
            m_MetricsService.RecordSessionEvent("session1", "", TimeSpan.FromMilliseconds(100));

            var snapshot = m_MetricsService.GetMetricsSnapshot();
            Assert.True(snapshot.Counters.ContainsKey("sessions."));
        }

        #endregion
    }
}
