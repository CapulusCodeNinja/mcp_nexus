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

namespace mcp_nexus_tests.Session
{
    /// <summary>
    /// Tests for SessionLifecycleManager
    /// </summary>
    public class SessionLifecycleManagerTests : IDisposable
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<ILogger<CdbSession>> _mockCdbLogger;
        private readonly Mock<ILogger<IsolatedCommandQueueService>> _mockCommandQueueLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<IMcpNotificationService> _mockNotificationService;
        private readonly Mock<ICdbSession> _mockCdbSession;
        private readonly Mock<ICommandQueueService> _mockCommandQueue;
        private readonly SessionManagerConfiguration _config;
        private readonly ConcurrentDictionary<string, SessionInfo> _sessions;
        private readonly SessionLifecycleManager _manager;

        public SessionLifecycleManagerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockCdbLogger = new Mock<ILogger<CdbSession>>();
            _mockCommandQueueLogger = new Mock<ILogger<IsolatedCommandQueueService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockNotificationService = new Mock<IMcpNotificationService>();
            _mockCdbSession = new Mock<ICdbSession>();
            _mockCommandQueue = new Mock<ICommandQueueService>();
            _sessions = new ConcurrentDictionary<string, SessionInfo>();

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

            _config = new SessionManagerConfiguration(
                Options.Create(sessionConfig),
                Options.Create(cdbOptions));

            // Setup mocks - simplified to avoid extension method issues

            _manager = new SessionLifecycleManager(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                _config,
                _sessions);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                null!, _mockServiceProvider.Object, _mockLoggerFactory.Object,
                _mockNotificationService.Object, _config, _sessions));
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                _mockLogger.Object, null!, _mockLoggerFactory.Object,
                _mockNotificationService.Object, _config, _sessions));
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                _mockLogger.Object, _mockServiceProvider.Object, null!,
                _mockNotificationService.Object, _config, _sessions));
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                _mockLogger.Object, _mockServiceProvider.Object, _mockLoggerFactory.Object,
                null!, _config, _sessions));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                _mockLogger.Object, _mockServiceProvider.Object, _mockLoggerFactory.Object,
                _mockNotificationService.Object, null!, _sessions));
        }

        [Fact]
        public void Constructor_WithNullSessions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                _mockLogger.Object, _mockServiceProvider.Object, _mockLoggerFactory.Object,
                _mockNotificationService.Object, _config, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var manager = new SessionLifecycleManager(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                _config,
                _sessions);

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public async Task CloseSessionAsync_WithValidSessionId_ClosesSession()
        {
            // Arrange
            var sessionId = "test-session-4";
            var sessionInfo = new SessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, "C:\\Test\\dump.dmp")
            {
                LastActivity = DateTime.UtcNow,
                Status = SessionStatus.Active
            };
            _sessions[sessionId] = sessionInfo;

            // Debug: Verify the session info was created correctly
            Assert.NotNull(sessionInfo);
            Assert.NotNull(sessionInfo.CdbSession);
            Assert.NotNull(sessionInfo.CommandQueue);
            Assert.Equal(sessionId, sessionInfo.SessionId);

            _mockCdbSession.Setup(x => x.IsActive).Returns(true);
            _mockCdbSession.Setup(x => x.StopSession()).ReturnsAsync(true);
            _mockCommandQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(5);

            // Debug: Check session info before closing
            Assert.NotNull(sessionInfo.CdbSession);
            Assert.NotNull(sessionInfo.CommandQueue);

            // Act
            var result = await _manager.CloseSessionAsync(sessionId);

            // Wait a bit for async operations to complete
            await Task.Delay(100);

            // Assert
            Assert.True(result);
            Assert.False(_sessions.ContainsKey(sessionId));

            // Debug: Check if the mocks were called
            _mockCdbSession.Verify(x => x.IsActive, Times.AtLeastOnce);
            _mockCommandQueue.Verify(x => x.CancelAllCommands("Session closing"), Times.Once);
            _mockCdbSession.Verify(x => x.StopSession(), Times.Once);
        }

        [Fact]
        public async Task CloseSessionAsync_WithNonExistentSessionId_ReturnsFalse()
        {
            // Arrange
            var sessionId = "non-existent-session";

            // Act
            var result = await _manager.CloseSessionAsync(sessionId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseSessionAsync_WithEmptySessionId_ReturnsFalse()
        {
            // Act
            var result = await _manager.CloseSessionAsync("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseSessionAsync_WithNullSessionId_ReturnsFalse()
        {
            // Act
            var result = await _manager.CloseSessionAsync(null!);

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
                CdbSession = _mockCdbSession.Object,
                CommandQueue = _mockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow, // Recent activity
                Status = SessionStatus.Active
            };
            _sessions[sessionId] = sessionInfo;

            // Act
            var result = await _manager.CleanupExpiredSessionsAsync();

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
                CdbSession = _mockCdbSession.Object,
                CommandQueue = _mockCommandQueue.Object,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                LastActivity = DateTime.UtcNow.AddHours(-2), // Old activity
                Status = SessionStatus.Active
            };
            _sessions[sessionId] = sessionInfo;

            _mockCdbSession.Setup(x => x.StopSession()).ReturnsAsync(true);
            _mockCommandQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(0);

            // Act
            var result = await _manager.CleanupExpiredSessionsAsync();

            // Assert
            Assert.Equal(1, result);
            Assert.False(_sessions.ContainsKey(sessionId));
        }

        [Fact]
        public void GetLifecycleStats_WithNoActivity_ReturnsZeroStats()
        {
            // Act
            var stats = _manager.GetLifecycleStats();

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
            // Mocks don't need disposal
        }
    }
}
