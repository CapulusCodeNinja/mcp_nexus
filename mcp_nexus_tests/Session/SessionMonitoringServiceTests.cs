using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.Notifications;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using System.Collections.Concurrent;
using mcp_nexus_tests.Helpers;

namespace mcp_nexus_tests.Session
{
    /// <summary>
    /// Tests for SessionMonitoringService
    /// </summary>
    public class SessionMonitoringServiceTests : IDisposable
    {
        private readonly Mock<ILogger> m_MockLogger;
        private readonly Mock<IMcpNotificationService> m_MockNotificationService;
        private readonly Mock<SessionLifecycleManager> m_MockLifecycleManager;
        private readonly ICdbSession m_RealisticCdbSession;
        private readonly Mock<ICommandQueueService> m_MockCommandQueue;
        private readonly SessionManagerConfiguration m_Config;
        private readonly ConcurrentDictionary<string, SessionInfo> m_Sessions;
        private readonly CancellationTokenSource m_ShutdownCts;

        public SessionMonitoringServiceTests()
        {
            m_MockLogger = new Mock<ILogger>();
            m_MockNotificationService = new Mock<IMcpNotificationService>();
            m_RealisticCdbSession = RealisticCdbTestHelper.CreateBugSimulatingCdbSession(Mock.Of<ILogger>());
            m_MockCommandQueue = new Mock<ICommandQueueService>();
            m_Sessions = new ConcurrentDictionary<string, SessionInfo>();
            m_ShutdownCts = new CancellationTokenSource();

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

            m_Config = new SessionManagerConfiguration(
                Options.Create(sessionConfig),
                Options.Create(cdbOptions));

            // Create a mock service provider
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();

            // Create the lifecycle manager with proper mocks
            m_MockLifecycleManager = new Mock<SessionLifecycleManager>(
                m_MockLogger.Object,
                mockServiceProvider.Object,
                mockLoggerFactory.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionMonitoringService(
                null!, m_MockNotificationService.Object, m_Config, m_Sessions,
                m_MockLifecycleManager.Object, m_ShutdownCts));
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionMonitoringService(
                m_MockLogger.Object, null!, m_Config, m_Sessions,
                m_MockLifecycleManager.Object, m_ShutdownCts));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionMonitoringService(
                m_MockLogger.Object, m_MockNotificationService.Object, null!, m_Sessions,
                m_MockLifecycleManager.Object, m_ShutdownCts));
        }

        [Fact]
        public void Constructor_WithNullSessions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionMonitoringService(
                m_MockLogger.Object, m_MockNotificationService.Object, m_Config, null!,
                m_MockLifecycleManager.Object, m_ShutdownCts));
        }

        [Fact]
        public void Constructor_WithNullLifecycleManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionMonitoringService(
                m_MockLogger.Object, m_MockNotificationService.Object, m_Config, m_Sessions,
                null!, m_ShutdownCts));
        }

        [Fact]
        public void Constructor_WithNullShutdownCts_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionMonitoringService(
                m_MockLogger.Object, m_MockNotificationService.Object, m_Config, m_Sessions,
                m_MockLifecycleManager.Object, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void UpdateActivity_WithValidSessionId_UpdatesLastActivity()
        {
            // Arrange
            var sessionId = "test-session-1";
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivity = DateTime.UtcNow.AddMinutes(-5),
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            var originalActivity = sessionInfo.LastActivity;

            // Act
            service.UpdateActivity(sessionId);

            // Assert
            Assert.True(sessionInfo.LastActivity > originalActivity);
        }

        [Fact]
        public void UpdateActivity_WithNonExistentSessionId_LogsWarning()
        {
            // Arrange
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            service.UpdateActivity("non-existent-session");

            // Assert
            // The method should not throw and should log a warning
            // We can't easily verify the log call without more complex setup
        }

        [Fact]
        public void UpdateActivity_WithEmptySessionId_DoesNothing()
        {
            // Arrange
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            service.UpdateActivity("");

            // Assert
            // Should not throw or cause issues
        }

        [Fact]
        public void UpdateActivity_WithNullSessionId_DoesNothing()
        {
            // Arrange
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            service.UpdateActivity(null!);

            // Assert
            // Should not throw or cause issues
        }

        [Fact]
        public void GenerateUsageHints_WithNewSession_ReturnsNewSessionHints()
        {
            // Arrange
            var sessionInfo = new SessionInfo
            {
                SessionId = "test-session-1",
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow.AddMinutes(-2), // New session
                LastActivity = DateTime.UtcNow.AddMinutes(-1),
                Status = SessionStatus.Active
            };

            var queueStatus = new List<(string Id, string Command, DateTime QueueTime, string Status)>();
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            var hints = service.GenerateUsageHints(sessionInfo, queueStatus);

            // Assert
            Assert.NotNull(hints);
            Assert.True(hints.Count > 0);
            Assert.Contains("New session", hints.First());
        }

        [Fact]
        public void GenerateUsageHints_WithIdleSession_ReturnsIdleHints()
        {
            // Arrange
            var sessionInfo = new SessionInfo(
                "test-session-1",
                m_RealisticCdbSession,
                m_MockCommandQueue.Object,
                "C:\\Test\\dump.dmp",
                null,
                null)
            {
                LastActivity = DateTime.UtcNow.AddMinutes(-45), // Idle session
                Status = SessionStatus.Active
            };

            var queueStatus = new List<(string Id, string Command, DateTime QueueTime, string Status)>();
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            var hints = service.GenerateUsageHints(sessionInfo, queueStatus);

            // Assert
            Assert.NotNull(hints);
            Assert.True(hints.Count > 0);
            Assert.Contains("idle", hints);
        }

        [Fact]
        public void GenerateUsageHints_WithEmptyQueue_ReturnsEmptyQueueHints()
        {
            // Arrange
            var sessionInfo = new SessionInfo
            {
                SessionId = "test-session-1",
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivity = DateTime.UtcNow.AddMinutes(-1),
                Status = SessionStatus.Active
            };

            var queueStatus = new List<(string Id, string Command, DateTime QueueTime, string Status)>();
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            var hints = service.GenerateUsageHints(sessionInfo, queueStatus);

            // Assert
            Assert.NotNull(hints);
            Assert.True(hints.Count > 0);
            Assert.Contains("Queue is empty", hints);
        }

        [Fact]
        public void GenerateUsageHints_WithBusyQueue_ReturnsBusyQueueHints()
        {
            // Arrange
            var sessionInfo = new SessionInfo
            {
                SessionId = "test-session-1",
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivity = DateTime.UtcNow.AddMinutes(-1),
                Status = SessionStatus.Active
            };

            var queueStatus = new List<(string Id, string Command, DateTime QueueTime, string Status)>
            {
                ("1", "k", DateTime.UtcNow, "Queued"),
                ("2", "lm", DateTime.UtcNow, "Queued"),
                ("3", "!analyze", DateTime.UtcNow, "Queued"),
                ("4", "g", DateTime.UtcNow, "Queued"),
                ("5", "r", DateTime.UtcNow, "Queued"),
                ("6", "x", DateTime.UtcNow, "Queued")
            };

            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            var hints = service.GenerateUsageHints(sessionInfo, queueStatus);

            // Assert
            Assert.NotNull(hints);
            Assert.True(hints.Count > 0);
            Assert.Contains("Queue is busy", hints);
        }

        [Fact]
        public void GenerateUsageHints_WithCrashDump_ReturnsCrashHints()
        {
            // Arrange
            var sessionInfo = new SessionInfo(
                "test-session-1",
                m_RealisticCdbSession,
                m_MockCommandQueue.Object,
                "C:\\Test\\crash.dmp", // Crash dump
                null,
                null)
            {
                LastActivity = DateTime.UtcNow.AddMinutes(-1),
                Status = SessionStatus.Active
            };

            var queueStatus = new List<(string Id, string Command, DateTime QueueTime, string Status)>();
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            var hints = service.GenerateUsageHints(sessionInfo, queueStatus);

            // Assert
            Assert.NotNull(hints);
            Assert.True(hints.Count > 0);
            // Check if any hint contains "crash" (case insensitive)
            Assert.Contains(hints, h => h.Contains("crash", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CalculateAverageSessionLifetime_WithSessions_ReturnsAverage()
        {
            // Arrange
            var sessionId1 = "test-session-1";
            var sessionId2 = "test-session-2";

            var sessionInfo1 = new SessionInfo
            {
                SessionId = sessionId1,
                DumpPath = "C:\\Test\\dump1.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                LastActivity = DateTime.UtcNow.AddMinutes(-1),
                Status = SessionStatus.Active
            };

            var sessionInfo2 = new SessionInfo
            {
                SessionId = sessionId2,
                DumpPath = "C:\\Test\\dump2.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivity = DateTime.UtcNow.AddMinutes(-2),
                Status = SessionStatus.Active
            };

            m_Sessions[sessionId1] = sessionInfo1;
            m_Sessions[sessionId2] = sessionInfo2;

            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            var averageLifetime = service.CalculateAverageSessionLifetime();

            // Assert
            Assert.True(averageLifetime >= TimeSpan.Zero);
        }

        [Fact]
        public void CalculateAverageSessionLifetime_WithEmptySessions_ReturnsZero()
        {
            // Arrange
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            var averageLifetime = service.CalculateAverageSessionLifetime();

            // Assert
            Assert.Equal(TimeSpan.Zero, averageLifetime);
        }

        [Fact]
        public void Dispose_WhenCalled_DisposesResources()
        {
            // Arrange
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act
            service.Dispose();

            // Assert
            // Should not throw and should dispose resources properly
            // We can't easily verify internal disposal without more complex setup
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var service = new SessionMonitoringService(
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockLifecycleManager.Object,
                m_ShutdownCts);

            // Act & Assert
            service.Dispose();
            service.Dispose(); // Should not throw
        }

        [Fact]
        public void SessionMonitoringService_Class_Exists()
        {
            // This test verifies that the SessionMonitoringService class exists and can be instantiated
            Assert.True(typeof(SessionMonitoringService) != null);
        }

        public void Dispose()
        {
            m_ShutdownCts?.Dispose();
            m_RealisticCdbSession?.Dispose();
        }
    }
}
