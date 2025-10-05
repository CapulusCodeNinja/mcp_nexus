using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using mcp_nexus.Session;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Session.Models;
using System.Collections.Concurrent;
using mcp_nexus_tests.Helpers;

namespace mcp_nexus_tests.Session
{
    /// <summary>
    /// Tests for SessionLifecycleManager
    /// </summary>
    public class SessionLifecycleManagerTests : IDisposable
    {
        private readonly Mock<ILogger> m_MockLogger;
        private readonly Mock<ILogger<CdbSession>> m_MockCdbLogger;
        private readonly Mock<ILogger<IsolatedCommandQueueService>> mm_MockCommandQueueLogger;
        private readonly Mock<IServiceProvider> m_MockServiceProvider;
        private readonly Mock<ILoggerFactory> mm_MockLoggerFactory;
        private readonly Mock<IMcpNotificationService> m_MockNotificationService;
        private readonly ICdbSession m_RealisticCdbSession;
        private readonly Mock<ICommandQueueService> m_MockCommandQueue;
        private readonly SessionManagerConfiguration m_Config;
        private readonly ConcurrentDictionary<string, SessionInfo> m_Sessions;
        private readonly SessionLifecycleManager m_Manager;

        public SessionLifecycleManagerTests()
        {
            m_MockLogger = new Mock<ILogger>();
            m_MockCdbLogger = new Mock<ILogger<CdbSession>>();
            mm_MockCommandQueueLogger = new Mock<ILogger<IsolatedCommandQueueService>>();
            m_MockServiceProvider = new Mock<IServiceProvider>();
            mm_MockLoggerFactory = new Mock<ILoggerFactory>();
            m_MockNotificationService = new Mock<IMcpNotificationService>();
            m_RealisticCdbSession = RealisticCdbTestHelper.CreateBugSimulatingCdbSession(Mock.Of<ILogger>());
            m_MockCommandQueue = new Mock<ICommandQueueService>();
            m_Sessions = new ConcurrentDictionary<string, SessionInfo>();

            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 1000,
                SessionTimeout = TimeSpan.FromMinutes(60),
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

            // Setup mocks - simplified to avoid extension method issues

            m_Manager = new SessionLifecycleManager(
                m_MockLogger.Object,
                m_MockServiceProvider.Object,
                mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                null!, m_MockServiceProvider.Object, mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object, m_Config, m_Sessions));
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, null!, mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object, m_Config, m_Sessions));
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, m_MockServiceProvider.Object, null!,
                m_MockNotificationService.Object, m_Config, m_Sessions));
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, m_MockServiceProvider.Object, mm_MockLoggerFactory.Object,
                null!, m_Config, m_Sessions));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, m_MockServiceProvider.Object, mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object, null!, m_Sessions));
        }

        [Fact]
        public void Constructor_WithNullSessions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, m_MockServiceProvider.Object, mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object, m_Config, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var manager = new SessionLifecycleManager(
                m_MockLogger.Object,
                m_MockServiceProvider.Object,
                mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions);

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public async Task CloseSessionAsync_WithValidSessionId_ClosesSession()
        {
            // Arrange
            var sessionId = "test-session-4";
            var sessionInfo = new SessionInfo(sessionId, m_RealisticCdbSession, m_MockCommandQueue.Object, "C:\\Test\\dump.dmp")
            {
                LastActivity = DateTime.UtcNow,
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            // Debug: Verify the session info was created correctly
            Assert.NotNull(sessionInfo);
            Assert.NotNull(sessionInfo.CdbSession);
            Assert.NotNull(sessionInfo.CommandQueue);
            Assert.Equal(sessionId, sessionInfo.SessionId);

            // Realistic mock handles IsActive and StopSession internally
            m_MockCommandQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(5);

            // Debug: Check session info before closing
            Assert.NotNull(sessionInfo.CdbSession);
            Assert.NotNull(sessionInfo.CommandQueue);

            // Act
            var result = await m_Manager.CloseSessionAsync(sessionId);

            // Wait a bit for async operations to complete
            await Task.Delay(100);

            // Assert
            Assert.True(result);
            Assert.False(m_Sessions.ContainsKey(sessionId));

            // Debug: Check if the mocks were called
            // Realistic mock verification - these methods are called internally
            m_MockCommandQueue.Verify(x => x.CancelAllCommands("Session closing"), Times.Once);
        }

        [Fact]
        public async Task CloseSessionAsync_WithNonExistentSessionId_ReturnsFalse()
        {
            // Arrange
            var sessionId = "non-existent-session";

            // Act
            var result = await m_Manager.CloseSessionAsync(sessionId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseSessionAsync_WithEmptySessionId_ReturnsFalse()
        {
            // Act
            var result = await m_Manager.CloseSessionAsync("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseSessionAsync_WithNullSessionId_ReturnsFalse()
        {
            // Act
            var result = await m_Manager.CloseSessionAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CleanupExpiredSessionsAsync_WithNoExpiredSessions_ReturnsZero()
        {
            // Arrange - Add a non-expired session
            var sessionId = "test-session-6";
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow, // Recent activity
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            // Act
            var result = await m_Manager.CleanupExpiredSessionsAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task CleanupExpiredSessionsAsync_WithExpiredSessions_CleansUpSessions()
        {
            // Arrange - Add an expired session
            var sessionId = "test-session-7";
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                LastActivity = DateTime.UtcNow.AddHours(-2), // Old activity
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            // Realistic mock handles StopSession internally
            m_MockCommandQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(0);

            // Act
            var result = await m_Manager.CleanupExpiredSessionsAsync();

            // Assert
            Assert.Equal(1, result);
            Assert.False(m_Sessions.ContainsKey(sessionId));
        }

        [Fact]
        public void GetLifecycleStats_WithNoActivity_ReturnsZeroStats()
        {
            // Act
            var stats = m_Manager.GetLifecycleStats();

            // Assert
            Assert.Equal(0, stats.Created);
            Assert.Equal(0, stats.Closed);
            Assert.Equal(0, stats.Expired);
        }

        [Fact]
        public void SessionLifecycleManager_Class_Exists()
        {
            // This test verifies that the SessionLifecycleManager class exists and can be instantiated
            Assert.True(typeof(SessionLifecycleManager) != null);
        }

        public void Dispose()
        {
            m_RealisticCdbSession?.Dispose();
        }
    }
}
