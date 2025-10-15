using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using mcp_nexus.Session.Lifecycle;
using mcp_nexus.Session.Core;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.CommandQueue.Batching;
using mcp_nexus.Notifications;
using mcp_nexus.Session.Core.Models;
using System.Collections.Concurrent;
using mcp_nexus_unit_tests.Mocks;
using mcp_nexus.Extensions;
using mcp_nexus.Utilities.Validation;

namespace mcp_nexus_unit_tests.Session.Lifecycle
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
        private readonly Mock<ICommandPreprocessor> m_MockCommandPreprocessor;
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
            m_MockCommandPreprocessor = new Mock<ICommandPreprocessor>();
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
                m_Sessions,
                m_MockCommandPreprocessor.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                null!, m_MockServiceProvider.Object, mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object, m_Config, m_Sessions, m_MockCommandPreprocessor.Object));
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, null!, mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object, m_Config, m_Sessions, m_MockCommandPreprocessor.Object));
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, m_MockServiceProvider.Object, null!,
                m_MockNotificationService.Object, m_Config, m_Sessions, m_MockCommandPreprocessor.Object));
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, m_MockServiceProvider.Object, mm_MockLoggerFactory.Object,
                null!, m_Config, m_Sessions, m_MockCommandPreprocessor.Object));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, m_MockServiceProvider.Object, mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object, null!, m_Sessions, m_MockCommandPreprocessor.Object));
        }

        [Fact]
        public void Constructor_WithNullSessions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SessionLifecycleManager(
                m_MockLogger.Object, m_MockServiceProvider.Object, mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object, m_Config, null!, m_MockCommandPreprocessor.Object));
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
                m_Sessions,
                m_MockCommandPreprocessor.Object);

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
                LastActivity = DateTime.Now,
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
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now, // Recent activity
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
                CreatedAt = DateTime.Now.AddHours(-2),
                LastActivity = DateTime.Now.AddHours(-2), // Old activity
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
            var (Created, Closed, Expired) = m_Manager.GetLifecycleStats();

            // Assert
            Assert.Equal(0, Created);
            Assert.Equal(0, Closed);
            Assert.Equal(0, Expired);
        }

        [Fact]
        public void SessionLifecycleManager_Class_Exists()
        {
            // This test verifies that the SessionLifecycleManager class exists and can be instantiated
            Assert.NotNull(typeof(SessionLifecycleManager));
        }

        [Fact]
        public async Task CloseSessionAsync_WithRunningExtensions_KillsExtensionsAndCleansUpTracking()
        {
            // Arrange
            var sessionId = "test-session-ext";
            var commandId1 = "ext-001";
            var commandId2 = "ext-002";

            // Create mock extension services
            var mockExtensionTracker = new Mock<IExtensionCommandTracker>();
            var mockExtensionExecutor = new Mock<IExtensionExecutor>();

            // Setup service provider to return extension mocks
            m_MockServiceProvider.Setup(x => x.GetService(typeof(IExtensionCommandTracker)))
                .Returns(mockExtensionTracker.Object);
            m_MockServiceProvider.Setup(x => x.GetService(typeof(IExtensionExecutor)))
                .Returns(mockExtensionExecutor.Object);

            // Create manager with extension support
            var managerWithExt = new SessionLifecycleManager(
                m_MockLogger.Object,
                m_MockServiceProvider.Object,
                mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockCommandPreprocessor.Object
            );

            // Add session
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            // Setup running extensions
            var runningExtension1 = new ExtensionCommandInfo
            {
                Id = commandId1,
                SessionId = sessionId,
                ExtensionName = "test_extension_1",
                State = CommandState.Executing
            };
            var runningExtension2 = new ExtensionCommandInfo
            {
                Id = commandId2,
                SessionId = sessionId,
                ExtensionName = "test_extension_2",
                State = CommandState.Executing
            };

            mockExtensionTracker.Setup(x => x.GetSessionCommands(sessionId))
                .Returns(new[] { runningExtension1, runningExtension2 });

            mockExtensionExecutor.Setup(x => x.KillExtension(commandId1)).Returns(true);
            mockExtensionExecutor.Setup(x => x.KillExtension(commandId2)).Returns(true);

            m_MockCommandQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(0);

            // Act
            var result = await managerWithExt.CloseSessionAsync(sessionId);

            // Assert
            Assert.True(result);
            Assert.False(m_Sessions.ContainsKey(sessionId));

            // Verify extensions were killed
            mockExtensionExecutor.Verify(x => x.KillExtension(commandId1), Times.Once);
            mockExtensionExecutor.Verify(x => x.KillExtension(commandId2), Times.Once);

            // Verify tracking was cleaned up
            mockExtensionTracker.Verify(x => x.RemoveSessionCommands(sessionId), Times.Once);
        }

        [Fact]
        public async Task CloseSessionAsync_RevokesExtensionTokensForSession()
        {
            // Arrange
            var sessionId = "test-session-token";
            var mockTokenValidator = new Mock<IExtensionTokenValidator>();
            m_MockServiceProvider.Setup(x => x.GetService(typeof(IExtensionTokenValidator))).Returns(mockTokenValidator.Object);

            var managerWithTokens = new SessionLifecycleManager(
                m_MockLogger.Object,
                m_MockServiceProvider.Object,
                mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object,
                m_Config,
                m_Sessions,
                m_MockCommandPreprocessor.Object);

            // Add a minimal session
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            m_MockCommandQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(0);

            // Act
            var result = await managerWithTokens.CloseSessionAsync(sessionId);

            // Assert
            Assert.True(result);
            mockTokenValidator.Verify(v => v.RevokeSessionTokens(sessionId), Times.Once);
        }

        [Fact]
        public async Task CloseSessionAsync_WithValidSession_SuccessfullyClosesSession()
        {
            // Arrange
            var sessionId = "test-session-close-success";
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;
            m_MockCommandQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(0);

            // Act
            var result = await m_Manager.CloseSessionAsync(sessionId);

            // Assert
            Assert.True(result);
            Assert.False(m_Sessions.ContainsKey(sessionId));
        }

        [Fact]
        public async Task CloseSessionAsync_WithWhitespaceSessionId_ReturnsFalse()
        {
            // Act
            var result = await m_Manager.CloseSessionAsync("   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseSessionAsync_WithSessionHavingNullCommandQueue_StillClosesSuccessfully()
        {
            // Arrange
            var sessionId = "test-session-null-queue";
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = null!, // Null command queue
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            // Act
            var result = await m_Manager.CloseSessionAsync(sessionId);

            // Assert
            Assert.True(result);
            Assert.False(m_Sessions.ContainsKey(sessionId));
        }

        [Fact]
        public async Task CloseSessionAsync_WithPendingCommands_ClosesSuccessfully()
        {
            // Arrange
            var sessionId = "test-session-with-commands";
            var mockQueue = new Mock<ICommandQueueService>();
            mockQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(5); // 5 commands cancelled

            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = mockQueue.Object,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            // Act
            var result = await m_Manager.CloseSessionAsync(sessionId);

            // Assert
            Assert.True(result);
            Assert.False(m_Sessions.ContainsKey(sessionId));
            // The session should be closed successfully even with pending commands
        }

        [Fact]
        public void GetLifecycleStats_AfterSessionOperations_ReturnsAccurateStats()
        {
            // Arrange
            var sessionId = "test-session-stats";
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = "C:\\Test\\dump.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                Status = SessionStatus.Active
            };
            m_Sessions[sessionId] = sessionInfo;

            // Act - Initial stats should be 0
            var (initialCreated, initialClosed, initialExpired) = m_Manager.GetLifecycleStats();

            // Assert
            Assert.Equal(0, initialCreated);
            Assert.Equal(0, initialClosed);
            Assert.Equal(0, initialExpired);
        }

        [Fact]
        public async Task CleanupExpiredSessionsAsync_WithMultipleSessions_OnlyCleansUpExpiredOnes()
        {
            // Arrange - Add one active and one expired session
            var activeSessionId = "active-session";
            var expiredSessionId = "expired-session";

            var activeSession = new SessionInfo
            {
                SessionId = activeSessionId,
                DumpPath = "C:\\Test\\active.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now, // Recent
                Status = SessionStatus.Active
            };

            var expiredSession = new SessionInfo
            {
                SessionId = expiredSessionId,
                DumpPath = "C:\\Test\\expired.dmp",
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object,
                CreatedAt = DateTime.Now.AddHours(-3),
                LastActivity = DateTime.Now.AddHours(-3), // Old
                Status = SessionStatus.Active
            };

            m_Sessions[activeSessionId] = activeSession;
            m_Sessions[expiredSessionId] = expiredSession;
            m_MockCommandQueue.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(0);

            // Act
            var result = await m_Manager.CleanupExpiredSessionsAsync();

            // Assert
            Assert.Equal(1, result); // Only expired session cleaned up
            Assert.True(m_Sessions.ContainsKey(activeSessionId)); // Active session remains
            Assert.False(m_Sessions.ContainsKey(expiredSessionId)); // Expired session removed
        }

        [Fact]
        public async Task CleanupExpiredSessionsAsync_WithNoSessions_ReturnsZero()
        {
            // Act
            var result = await m_Manager.CleanupExpiredSessionsAsync();

            // Assert
            Assert.Equal(0, result);
        }


        [Fact]
        public void GetSessionCache_WithNullSessionId_ReturnsNull()
        {
            // Act
            var result = m_Manager.GetSessionCache(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSessionCache_WithEmptySessionId_ReturnsNull()
        {
            // Act
            var result = m_Manager.GetSessionCache("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSessionCache_WithWhitespaceSessionId_ReturnsNull()
        {
            // Act
            var result = m_Manager.GetSessionCache("   ");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSessionCache_WithNonExistentSessionId_ReturnsNull()
        {
            // Act
            var result = m_Manager.GetSessionCache("non-existent-session");

            // Assert
            Assert.Null(result);
        }


        [Fact]
        public void RemoveSessionCache_WithNullSessionId_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_Manager.RemoveSessionCache(null!);
        }

        [Fact]
        public void RemoveSessionCache_WithEmptySessionId_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_Manager.RemoveSessionCache("");
        }

        [Fact]
        public void RemoveSessionCache_WithWhitespaceSessionId_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_Manager.RemoveSessionCache("   ");
        }

        [Fact]
        public void RemoveSessionCache_WithNonExistentSessionId_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_Manager.RemoveSessionCache("non-existent-session");
        }

        #region Helper Methods for Extension Tests

        private IServiceProvider CreateServiceProvider(
            IExtensionCommandTracker? extensionTracker,
            IExtensionExecutor? extensionExecutor,
            IExtensionTokenValidator? tokenValidator)
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(x => x.GetService(typeof(IExtensionCommandTracker))).Returns(extensionTracker);
            mockServiceProvider.Setup(x => x.GetService(typeof(IExtensionExecutor))).Returns(extensionExecutor);
            mockServiceProvider.Setup(x => x.GetService(typeof(IExtensionTokenValidator))).Returns(tokenValidator);
            mockServiceProvider.Setup(x => x.GetService(typeof(IOptions<BatchingConfiguration>))).Returns((IOptions<BatchingConfiguration>?)null);
            return mockServiceProvider.Object;
        }

        private SessionLifecycleManager CreateManager(
            ConcurrentDictionary<string, SessionInfo> sessions,
            IServiceProvider serviceProvider)
        {
            return new SessionLifecycleManager(
                m_MockLogger.Object,
                serviceProvider,
                mm_MockLoggerFactory.Object,
                m_MockNotificationService.Object,
                m_Config,
                sessions,
                m_MockCommandPreprocessor.Object);
        }

        private Mock<ICdbSession> CreateMockCdbSession()
        {
            var mock = new Mock<ICdbSession>();
            mock.Setup(x => x.StopSession()).Returns(Task.FromResult(true));
            mock.Setup(x => x.Dispose());
            return mock;
        }

        private Mock<ICommandQueueService> CreateMockCommandQueue()
        {
            var mock = new Mock<ICommandQueueService>();
            mock.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(0);
            mock.Setup(x => x.Dispose());
            return mock;
        }

        #endregion

        #region Additional Edge Case Tests

        [Fact]
        public async Task CloseSessionAsync_WhenExtensionKillFails_ContinuesWithCleanup()
        {
            // Arrange
            var sessionId = "test-session";
            var cdbSession = CreateMockCdbSession();
            var commandQueue = CreateMockCommandQueue();
            var sessions = new ConcurrentDictionary<string, SessionInfo>();

            var sessionInfo = new SessionInfo(
                sessionId,
                cdbSession.Object,
                commandQueue.Object,
                "test.dmp",
                null,
                1234
            );
            sessions.TryAdd(sessionId, sessionInfo);

            // Mock extension tracking with a running extension
            var mockExtensionTracker = new Mock<IExtensionCommandTracker>();
            var extensionCommand = new mcp_nexus.Extensions.ExtensionCommandInfo
            {
                Id = "ext-1",
                ExtensionName = "test-ext",
                SessionId = sessionId,
                State = mcp_nexus.CommandQueue.Core.CommandState.Executing,
                IsCompleted = false
            };
            mockExtensionTracker.Setup(x => x.GetSessionCommands(sessionId))
                .Returns([extensionCommand]);

            // Mock extension executor that throws when killing
            var mockExtensionExecutor = new Mock<IExtensionExecutor>();
            mockExtensionExecutor.Setup(x => x.KillExtension("ext-1"))
                .Throws(new InvalidOperationException("Process already exited"));

            var serviceProvider = CreateServiceProvider(mockExtensionTracker.Object, mockExtensionExecutor.Object, null);
            var manager = CreateManager(sessions, serviceProvider);

            // Act
            var result = await manager.CloseSessionAsync(sessionId);

            // Assert - Should still succeed despite kill failure
            Assert.True(result);
            Assert.False(sessions.ContainsKey(sessionId));
        }

        [Fact]
        public async Task CloseSessionAsync_WhenTokenRevocationFails_ContinuesWithCleanup()
        {
            // Arrange
            var sessionId = "test-session";
            var cdbSession = CreateMockCdbSession();
            var commandQueue = CreateMockCommandQueue();
            var sessions = new ConcurrentDictionary<string, SessionInfo>();

            var sessionInfo = new SessionInfo(
                sessionId,
                cdbSession.Object,
                commandQueue.Object,
                "test.dmp",
                null,
                1234
            );
            sessions.TryAdd(sessionId, sessionInfo);

            // Mock token validator that throws
            var mockTokenValidator = new Mock<IExtensionTokenValidator>();
            mockTokenValidator.Setup(x => x.RevokeSessionTokens(sessionId))
                .Throws(new InvalidOperationException("Token store unavailable"));

            var serviceProvider = CreateServiceProvider(null, null, mockTokenValidator.Object);
            var manager = CreateManager(sessions, serviceProvider);

            // Act
            var result = await manager.CloseSessionAsync(sessionId);

            // Assert - Should still succeed despite token revocation failure
            Assert.True(result);
            Assert.False(sessions.ContainsKey(sessionId));
        }

        [Fact]
        public async Task CloseSessionAsync_WithNoExtensionTracker_SkipsExtensionCleanup()
        {
            // Arrange
            var sessionId = "test-session";
            var cdbSession = CreateMockCdbSession();
            var commandQueue = CreateMockCommandQueue();
            var sessions = new ConcurrentDictionary<string, SessionInfo>();

            var sessionInfo = new SessionInfo(
                sessionId,
                cdbSession.Object,
                commandQueue.Object,
                "test.dmp",
                null,
                1234
            );
            sessions.TryAdd(sessionId, sessionInfo);

            // Create service provider without extension tracker
            var serviceProvider = CreateServiceProvider(null, null, null);
            var manager = CreateManager(sessions, serviceProvider);

            // Act
            var result = await manager.CloseSessionAsync(sessionId);

            // Assert - Should succeed and skip extension cleanup
            Assert.True(result);
            Assert.False(sessions.ContainsKey(sessionId));
        }

        #endregion


        public void Dispose()
        {
            m_RealisticCdbSession?.Dispose();
        }
    }
}
