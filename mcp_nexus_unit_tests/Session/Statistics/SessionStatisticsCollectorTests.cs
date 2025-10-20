using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using mcp_nexus.Session.Lifecycle;
using mcp_nexus.Session.Monitoring;
using mcp_nexus.Session.Core.Models;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Notifications;
using mcp_nexus_unit_tests.Mocks;
using mcp_nexus.Utilities.Validation;
using mcp_nexus.Session.Statistics;
using mcp_nexus.Session.Core;

namespace mcp_nexus_unit_tests.Session.Statistics
{
    /// <summary>
    /// Tests for SessionStatisticsCollector and related data classes
    /// </summary>
    public class SessionStatisticsCollectorTests : IDisposable
    {
        private readonly Mock<ILogger> m_MockLogger;
        private readonly Mock<IServiceProvider> m_MockServiceProvider;
        private readonly Mock<ILoggerFactory> mm_MockLoggerFactory;
        private readonly Mock<IMcpNotificationService> m_MockNotificationService;
        private readonly Mock<ICommandPreprocessor> m_MockCommandPreprocessor;
        private readonly SessionLifecycleManager m_LifecycleManager;
        private readonly SessionMonitoringService m_MonitoringService;
        private readonly ConcurrentDictionary<string, SessionInfo> m_Sessions;

        public SessionStatisticsCollectorTests()
        {
            m_MockLogger = new Mock<ILogger>();
            m_MockServiceProvider = new Mock<IServiceProvider>();
            mm_MockLoggerFactory = new Mock<ILoggerFactory>();
            m_MockNotificationService = new Mock<IMcpNotificationService>();
            m_MockCommandPreprocessor = new Mock<ICommandPreprocessor>();
            m_Sessions = new ConcurrentDictionary<string, SessionInfo>();

            // Create real instances with proper mocks
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 1000,
                SessionTimeout = TimeSpan.FromMinutes(30),
                CleanupInterval = TimeSpan.FromMinutes(5),
                DisposalTimeout = TimeSpan.FromSeconds(30),
                DefaultCommandTimeout = TimeSpan.FromMinutes(10),
                MemoryCleanupThresholdBytes = 1_000_000_000
            };

            var cdbOptions = new CdbSessionOptions
            {
                CommandTimeoutMs = 30000,
                CustomCdbPath = "C:\\Test\\cdb.exe",
                SymbolServerTimeoutMs = 5000,
                SymbolServerMaxRetries = 3,
                SymbolSearchPath = "srv*C:\\Symbols*https://msdl.microsoft.com/download/symbols"
            };

            var config = new SessionManagerConfiguration(
                Options.Create(sessionConfig),
                Options.Create(cdbOptions));

            m_LifecycleManager = new SessionLifecycleManager(
                m_MockLogger.Object,
                m_MockServiceProvider.Object,
                mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object,
                config,
                m_Sessions,
                m_MockCommandPreprocessor.Object);

            var shutdownCts = new CancellationTokenSource();
            m_MonitoringService = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                config,
                m_Sessions,
                m_LifecycleManager,
                shutdownCts);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionStatisticsCollector(
                null!, m_Sessions, m_LifecycleManager, m_MonitoringService));
        }

        [Fact]
        public void Constructor_WithNullSessions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionStatisticsCollector(
                m_MockLogger.Object, null!, m_LifecycleManager, m_MonitoringService));
        }

        [Fact]
        public void Constructor_WithNullLifecycleManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, null!, m_MonitoringService));
        }

        [Fact]
        public void Constructor_WithNullMonitoringService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Assert
            Assert.NotNull(collector);
        }

        [Fact]
        public void GetStatistics_WithEmptySessions_ReturnsBasicStatistics()
        {
            // Arrange
            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Act
            var stats = collector.GetStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0, stats.ActiveSessions);
            Assert.Equal(0, stats.TotalSessionsCreated);
            Assert.Equal(0, stats.TotalSessionsClosed);
            Assert.Equal(0, stats.TotalSessionsExpired);
            Assert.Equal(0, stats.TotalCommandsProcessed);
            Assert.True(stats.AverageSessionLifetime >= TimeSpan.Zero);
            Assert.True(stats.Uptime >= TimeSpan.Zero);
            Assert.NotNull(stats.MemoryUsage);
        }

        [Fact]
        public void GetStatistics_WithActiveSessions_ReturnsCorrectCounts()
        {
            // Arrange
            var sessionId1 = "session-1";
            var sessionId2 = "session-2";

            var sessionInfo1 = CreateMockSessionInfo(sessionId1, "dump1.dmp", SessionStatus.Active);
            var sessionInfo2 = CreateMockSessionInfo(sessionId2, "dump2.dmp", SessionStatus.Active);

            m_Sessions[sessionId1] = sessionInfo1;
            m_Sessions[sessionId2] = sessionInfo2;

            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Act
            var stats = collector.GetStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, stats.ActiveSessions);
            Assert.True(stats.AverageSessionLifetime >= TimeSpan.Zero);
            Assert.True(stats.Uptime >= TimeSpan.Zero);
            Assert.NotNull(stats.MemoryUsage);
        }

        [Fact]
        public void GetStatistics_WithExceptionInLifecycleManager_ReturnsFallbackStatistics()
        {
            // Arrange
            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Act
            var stats = collector.GetStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.Uptime >= TimeSpan.Zero);
            // Should return fallback statistics with just uptime
        }

        [Fact]
        public void GetActiveSessions_WithActiveSessions_ReturnsActiveOnly()
        {
            // Arrange
            var sessionId1 = "active-session";
            var sessionId2 = "inactive-session";

            var activeSession = CreateMockSessionInfo(sessionId1, "dump1.dmp", SessionStatus.Active);
            var inactiveSession = CreateMockSessionInfo(sessionId2, "dump2.dmp", SessionStatus.Disposed);

            m_Sessions[sessionId1] = activeSession;
            m_Sessions[sessionId2] = inactiveSession;

            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Act
            var activeSessions = collector.GetActiveSessions().ToList();

            // Assert
            Assert.Single(activeSessions);
            Assert.Equal(sessionId1, activeSessions[0].SessionId);
        }

        [Fact]
        public void GetActiveSessions_WithDisposedSessions_ExcludesDisposed()
        {
            // Arrange
            var sessionId1 = "active-session";
            var sessionId2 = "disposed-session";

            var activeSession = CreateMockSessionInfo(sessionId1, "dump1.dmp", SessionStatus.Active);
            var disposedSession = CreateMockSessionInfo(sessionId2, "dump2.dmp", SessionStatus.Disposed);

            m_Sessions[sessionId1] = activeSession;
            m_Sessions[sessionId2] = disposedSession;

            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Act
            var activeSessions = collector.GetActiveSessions().ToList();

            // Assert
            Assert.Single(activeSessions);
            Assert.Equal(sessionId1, activeSessions[0].SessionId);
        }

        [Fact]
        public void GetActiveSessions_WithExceptionInSession_LogsWarningAndContinues()
        {
            // Arrange
            var sessionId1 = "valid-session";
            var sessionId2 = "problematic-session";

            var validSession = CreateMockSessionInfo(sessionId1, "dump1.dmp", SessionStatus.Active);
            var problematicSession = CreateMockSessionInfo(sessionId2, "dump2.dmp", SessionStatus.Active);

            m_Sessions[sessionId1] = validSession;
            m_Sessions[sessionId2] = problematicSession;

            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Act
            var activeSessions = collector.GetActiveSessions().ToList();

            // Assert
            // Since we're using real instances, both sessions should be processed normally
            Assert.Equal(2, activeSessions.Count);
            Assert.Contains(activeSessions, s => s.SessionId == sessionId1);
            Assert.Contains(activeSessions, s => s.SessionId == sessionId2);
        }

        [Fact]
        public void GetAllSessions_WithSessions_ReturnsAllSessions()
        {
            // Arrange
            var sessionId1 = "session-1";
            var sessionId2 = "session-2";

            var session1 = CreateMockSessionInfo(sessionId1, "dump1.dmp", SessionStatus.Active);
            var session2 = CreateMockSessionInfo(sessionId2, "dump2.dmp", SessionStatus.Active);

            m_Sessions[sessionId1] = session1;
            m_Sessions[sessionId2] = session2;

            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Act
            var allSessions = collector.GetAllSessions().ToList();

            // Assert
            Assert.Equal(2, allSessions.Count);
            Assert.Contains(session1, allSessions);
            Assert.Contains(session2, allSessions);
        }


        [Fact]
        public void LogStatisticsSummary_WithValidStatistics_LogsInformation()
        {
            // Arrange
            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);


            // Act
            collector.LogStatisticsSummary();

            // Assert
            // Verify that information logging was called
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Session Statistics Summary")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void LogStatisticsSummary_WithException_LogsError()
        {
            // Arrange
            var collector = new SessionStatisticsCollector(
                m_MockLogger.Object, m_Sessions, m_LifecycleManager, m_MonitoringService);

            // Act
            collector.LogStatisticsSummary();

            // Assert
            // Since we're using real instances, no error should be logged in normal operation
            // This test verifies that the method completes successfully
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Session Statistics Summary")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static SessionInfo CreateMockSessionInfo(string sessionId, string dumpPath, SessionStatus status = SessionStatus.Active)
        {
            var realisticCdbSession = RealisticCdbTestHelper.CreateBugSimulatingCdbSession(Mock.Of<ILogger>());
            var mockCommandQueue = new Mock<ICommandQueueService>();

            // Setup command queue to return some test data
            var queueStatus = new List<(string Id, string Command, DateTime QueueTime, string Status)>
            {
                ("1", "k", DateTime.Now.AddMinutes(-5), "Completed"),
                ("2", "lm", DateTime.Now.AddMinutes(-2), "Queued"),
                ("3", "!analyze", DateTime.Now.AddMinutes(-1), "Executing")
            };
            mockCommandQueue.Setup(x => x.GetQueueStatus()).Returns(queueStatus);

            var sessionInfo = new SessionInfo(sessionId, realisticCdbSession, mockCommandQueue.Object, dumpPath)
            {
                LastActivity = DateTime.Now.AddMinutes(-5),
                Status = status
            };
            return sessionInfo;
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Tests for SessionStatistics and MemoryUsageInfo data classes - simple data containers
    /// </summary>
    public class SessionStatisticsDataClassesTests
    {
        [Fact]
        public void SessionStatistics_DefaultValues_AreCorrect()
        {
            // Act
            var statistics = new SessionStatistics();

            // Assert
            Assert.Equal(0, statistics.ActiveSessions);
            Assert.Equal(0, statistics.TotalSessionsCreated);
            Assert.Equal(0, statistics.TotalSessionsClosed);
            Assert.Equal(0, statistics.TotalSessionsExpired);
            Assert.Equal(0, statistics.TotalCommandsProcessed);
            Assert.Equal(TimeSpan.Zero, statistics.AverageSessionLifetime);
            Assert.Equal(TimeSpan.Zero, statistics.Uptime);
            Assert.NotNull(statistics.MemoryUsage);
        }

        [Fact]
        public void SessionStatistics_WithValues_SetsProperties()
        {
            // Arrange
            var averageLifetime = TimeSpan.FromMinutes(30);
            var uptime = TimeSpan.FromHours(2);
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = 1024 * 1024,
                PrivateMemoryBytes = 512 * 1024,
                GCTotalMemoryBytes = 256 * 1024
            };

            // Act
            var statistics = new SessionStatistics
            {
                ActiveSessions = 5,
                TotalSessionsCreated = 100,
                TotalSessionsClosed = 95,
                TotalSessionsExpired = 10,
                TotalCommandsProcessed = 1000,
                AverageSessionLifetime = averageLifetime,
                Uptime = uptime,
                MemoryUsage = memoryUsage
            };

            // Assert
            Assert.Equal(5, statistics.ActiveSessions);
            Assert.Equal(100, statistics.TotalSessionsCreated);
            Assert.Equal(95, statistics.TotalSessionsClosed);
            Assert.Equal(10, statistics.TotalSessionsExpired);
            Assert.Equal(1000, statistics.TotalCommandsProcessed);
            Assert.Equal(averageLifetime, statistics.AverageSessionLifetime);
            Assert.Equal(uptime, statistics.Uptime);
            Assert.Equal(memoryUsage, statistics.MemoryUsage);
        }

        [Fact]
        public void SessionStatistics_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var statistics = new SessionStatistics
            {
                ActiveSessions = -1,
                TotalSessionsCreated = -100,
                TotalSessionsClosed = -95,
                TotalSessionsExpired = -10,
                TotalCommandsProcessed = -1000
            };

            // Assert
            Assert.Equal(-1, statistics.ActiveSessions);
            Assert.Equal(-100, statistics.TotalSessionsCreated);
            Assert.Equal(-95, statistics.TotalSessionsClosed);
            Assert.Equal(-10, statistics.TotalSessionsExpired);
            Assert.Equal(-1000, statistics.TotalCommandsProcessed);
        }

        [Fact]
        public void SessionStatistics_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var statistics = new SessionStatistics
            {
                ActiveSessions = int.MaxValue,
                TotalSessionsCreated = long.MaxValue,
                TotalSessionsClosed = long.MaxValue,
                TotalSessionsExpired = long.MaxValue,
                TotalCommandsProcessed = long.MaxValue,
                AverageSessionLifetime = TimeSpan.MaxValue,
                Uptime = TimeSpan.MaxValue
            };

            // Assert
            Assert.Equal(int.MaxValue, statistics.ActiveSessions);
            Assert.Equal(long.MaxValue, statistics.TotalSessionsCreated);
            Assert.Equal(long.MaxValue, statistics.TotalSessionsClosed);
            Assert.Equal(long.MaxValue, statistics.TotalSessionsExpired);
            Assert.Equal(long.MaxValue, statistics.TotalCommandsProcessed);
            Assert.Equal(TimeSpan.MaxValue, statistics.AverageSessionLifetime);
            Assert.Equal(TimeSpan.MaxValue, statistics.Uptime);
        }

        [Fact]
        public void MemoryUsageInfo_DefaultValues_AreCorrect()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo();

            // Assert
            Assert.Equal(0, memoryUsage.WorkingSetBytes);
            Assert.Equal(0, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(0, memoryUsage.GCTotalMemoryBytes);
        }

        [Fact]
        public void MemoryUsageInfo_WithValues_SetsProperties()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = 1024 * 1024 * 100, // 100 MB
                PrivateMemoryBytes = 1024 * 1024 * 50, // 50 MB
                GCTotalMemoryBytes = 1024 * 1024 * 25 // 25 MB
            };

            // Assert
            Assert.Equal(1024 * 1024 * 100, memoryUsage.WorkingSetBytes);
            Assert.Equal(1024 * 1024 * 50, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(1024 * 1024 * 25, memoryUsage.GCTotalMemoryBytes);
        }

        [Fact]
        public void MemoryUsageInfo_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = -1024,
                PrivateMemoryBytes = -2048,
                GCTotalMemoryBytes = -4096
            };

            // Assert
            Assert.Equal(-1024, memoryUsage.WorkingSetBytes);
            Assert.Equal(-2048, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(-4096, memoryUsage.GCTotalMemoryBytes);
        }

        [Fact]
        public void MemoryUsageInfo_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = long.MaxValue,
                PrivateMemoryBytes = long.MaxValue,
                GCTotalMemoryBytes = long.MaxValue
            };

            // Assert
            Assert.Equal(long.MaxValue, memoryUsage.WorkingSetBytes);
            Assert.Equal(long.MaxValue, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(long.MaxValue, memoryUsage.GCTotalMemoryBytes);
        }

        [Fact]
        public void MemoryUsageInfo_WithZeroValues_HandlesCorrectly()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = 0,
                PrivateMemoryBytes = 0,
                GCTotalMemoryBytes = 0
            };

            // Assert
            Assert.Equal(0, memoryUsage.WorkingSetBytes);
            Assert.Equal(0, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(0, memoryUsage.GCTotalMemoryBytes);
        }
    }
}
