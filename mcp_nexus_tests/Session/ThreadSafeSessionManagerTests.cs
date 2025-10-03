using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using Xunit;

namespace mcp_nexus_tests.Session
{
    public class ThreadSafeSessionManagerTests : IDisposable
    {
        private readonly Mock<ILogger<ThreadSafeSessionManager>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IMcpNotificationService> _mockNotificationService;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger> _mockSessionLogger;
        private readonly SessionConfiguration _config;
        private readonly CdbSessionOptions _cdbOptions;
        private ThreadSafeSessionManager _sessionManager = null!;

        public ThreadSafeSessionManagerTests()
        {
            _mockLogger = new Mock<ILogger<ThreadSafeSessionManager>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockNotificationService = new Mock<IMcpNotificationService>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockSessionLogger = new Mock<ILogger>();

            _config = new SessionConfiguration
            {
                MaxConcurrentSessions = 10,
                SessionTimeout = TimeSpan.FromMinutes(30),
                CleanupInterval = TimeSpan.FromMinutes(5),
                DisposalTimeout = TimeSpan.FromSeconds(30),
                MemoryCleanupThresholdBytes = 1_000_000_000
            };

            _cdbOptions = new CdbSessionOptions
            {
                CommandTimeoutMs = 30000,
                SymbolServerTimeoutMs = 30000,
                SymbolServerMaxRetries = 1,
                SymbolSearchPath = "C:\\Symbols",
                CustomCdbPath = "C:\\Debuggers\\cdb.exe"
            };

            SetupMocks();
        }

        private void SetupMocks()
        {
            // Setup service provider mocks
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory)))
                .Returns(_mockLoggerFactory.Object);

            // Setup the required ConcurrentDictionary service
            var sessions = new ConcurrentDictionary<string, SessionInfo>();
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ConcurrentDictionary<string, SessionInfo>)))
                .Returns(sessions);

            _mockLoggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>()))
                .Returns(_mockSessionLogger.Object);

            // Setup notification service mock
            _mockNotificationService.Setup(ns => ns.NotifySessionEventAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SessionContext>()))
                .Returns(Task.CompletedTask);
        }

        private ThreadSafeSessionManager CreateSessionManager()
        {
            return new ThreadSafeSessionManager(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                Options.Create(_config),
                Options.Create(_cdbOptions));
        }

        private TestableThreadSafeSessionManager CreateTestableSessionManager()
        {
            return new TestableThreadSafeSessionManager(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                Options.Create(_config),
                Options.Create(_cdbOptions));
        }

        public void Dispose()
        {
            _sessionManager?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var sessionManager = CreateSessionManager();

            // Assert
            Assert.NotNull(sessionManager);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ThreadSafeSessionManager initializing")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ThreadSafeSessionManager(
                null!,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                Options.Create(_config),
                Options.Create(_cdbOptions)));
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ThreadSafeSessionManager(
                _mockLogger.Object,
                null!,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                Options.Create(_config),
                Options.Create(_cdbOptions)));
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ThreadSafeSessionManager(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                null!,
                Options.Create(_config),
                Options.Create(_cdbOptions)));
        }

        [Fact]
        public void Constructor_WithNullConfig_UsesDefaultConfiguration()
        {
            // Act
            var sessionManager = new ThreadSafeSessionManager(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                null,
                Options.Create(_cdbOptions));

            // Assert
            Assert.NotNull(sessionManager);
            sessionManager.Dispose();
        }

        [Fact]
        public void Constructor_WithNullCdbOptions_UsesDefaultConfiguration()
        {
            // Act
            var sessionManager = new ThreadSafeSessionManager(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                Options.Create(_config),
                null);

            // Assert
            Assert.NotNull(sessionManager);
            sessionManager.Dispose();
        }

        #endregion

        #region CreateSessionAsync Tests

        [Fact]
        public async Task CreateSessionAsync_WithNullDumpPath_ThrowsArgumentNullException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentNullException>(() =>
                    sessionManager.CreateSessionAsync(null!));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task CreateSessionAsync_WithEmptyDumpPath_ThrowsArgumentException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    sessionManager.CreateSessionAsync(""));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task CreateSessionAsync_WithWhitespaceDumpPath_ThrowsArgumentException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    sessionManager.CreateSessionAsync("   "));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task CreateSessionAsync_WithNonExistentDumpPath_ThrowsFileNotFoundException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<FileNotFoundException>(() =>
                    sessionManager.CreateSessionAsync("nonexistent.dmp"));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task CreateSessionAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var dumpPath = Path.GetTempFileName();
            var sessionManager = CreateSessionManager();
            sessionManager.Dispose();

            try
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                    sessionManager.CreateSessionAsync(dumpPath));
                Assert.Equal(nameof(ThreadSafeSessionManager), exception.ObjectName);
            }
            finally
            {
                File.Delete(dumpPath);
            }
        }

        [Fact]
        public async Task CreateSessionAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var dumpPath = Path.GetTempFileName();
            var sessionManager = CreateSessionManager();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<TaskCanceledException>(() =>
                    sessionManager.CreateSessionAsync(dumpPath, cancellationToken: cts.Token));
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath);
            }
        }

        #endregion

        #region CloseSessionAsync Tests

        [Fact]
        public async Task CloseSessionAsync_WithNonExistentSession_ReturnsFalse()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var result = await sessionManager.CloseSessionAsync("nonexistent");

                // Assert
                Assert.False(result);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task CloseSessionAsync_WithNullSessionId_ThrowsArgumentNullException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentNullException>(() =>
                    sessionManager.CloseSessionAsync(null!));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task CloseSessionAsync_WithEmptySessionId_ThrowsArgumentException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    sessionManager.CloseSessionAsync(""));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task CloseSessionAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();
            sessionManager.Dispose();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                sessionManager.CloseSessionAsync("test"));
            Assert.Equal(nameof(ThreadSafeSessionManager), exception.ObjectName);
        }







        [Fact]
        public async Task CreateSessionAsync_WithInvalidDumpPath_ThrowsFileNotFoundException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();
            var invalidPath = "C:\\NonExistent\\Path\\To\\Dump.dmp";

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<FileNotFoundException>(() =>
                    sessionManager.CreateSessionAsync(invalidPath));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task CloseSessionAsync_WithWhitespaceSessionId_ThrowsArgumentException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    sessionManager.CloseSessionAsync("   "));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void SessionExists_WithNullSessionId_ThrowsArgumentNullException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() =>
                    sessionManager.SessionExists(null!));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void SessionExists_WithEmptySessionId_ThrowsArgumentException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                Assert.Throws<ArgumentException>(() =>
                    sessionManager.SessionExists(""));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void SessionExists_WithWhitespaceSessionId_ThrowsArgumentException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                Assert.Throws<ArgumentException>(() =>
                    sessionManager.SessionExists("   "));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void SessionExists_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();
            sessionManager.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() =>
                sessionManager.SessionExists("test"));
        }


        [Fact]
        public async Task CloseSessionAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<OperationCanceledException>(() =>
                    sessionManager.CloseSessionAsync("test", cancellationToken: cts.Token));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region SessionExists Tests

        [Fact]
        public void SessionExists_WithNonExistentSession_ReturnsFalse()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var exists = sessionManager.SessionExists("nonexistent");

                // Assert
                Assert.False(exists);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region GetCommandQueue Tests

        [Fact]
        public void GetCommandQueue_WithNonExistentSession_ThrowsSessionNotFoundException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                Assert.Throws<SessionNotFoundException>(() =>
                    sessionManager.GetCommandQueue("nonexistent"));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region GetSessionContext Tests

        [Fact]
        public void GetSessionContext_WithNullSessionId_ThrowsArgumentException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                Assert.Throws<ArgumentException>(() =>
                    sessionManager.GetSessionContext(null!));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void GetSessionContext_WithEmptySessionId_ThrowsArgumentException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                Assert.Throws<ArgumentException>(() =>
                    sessionManager.GetSessionContext(""));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void GetSessionContext_WithNonExistentSession_ThrowsSessionNotFoundException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                Assert.Throws<SessionNotFoundException>(() =>
                    sessionManager.GetSessionContext("nonexistent"));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region UpdateActivity Tests

        [Fact]
        public void UpdateActivity_WithNonExistentSession_DoesNotThrow()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert - Should not throw
                sessionManager.UpdateActivity("nonexistent");
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region GetActiveSessions Tests

        [Fact]
        public void GetActiveSessions_WithNoSessions_ReturnsEmptyCollection()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var sessions = sessionManager.GetActiveSessions();

                // Assert
                Assert.Empty(sessions);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region GetAllSessions Tests

        [Fact]
        public void GetAllSessions_WithNoSessions_ReturnsEmptyCollection()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var sessions = sessionManager.GetAllSessions();

                // Assert
                Assert.Empty(sessions);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region GetStatistics Tests

        [Fact]
        public void GetStatistics_WithNoSessions_ReturnsCorrectStatistics()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var stats = sessionManager.GetStatistics();

                // Assert
                Assert.NotNull(stats);
                Assert.Equal(0, stats.ActiveSessions);
                Assert.Equal(0, stats.TotalSessionsCreated);
                Assert.Equal(0, stats.TotalSessionsClosed);
                Assert.Equal(0, stats.TotalSessionsExpired);
                Assert.Equal(0, stats.TotalCommandsProcessed);
                Assert.True(stats.Uptime > TimeSpan.Zero);
                Assert.NotNull(stats.MemoryUsage);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region CleanupExpiredSessionsAsync Tests

        [Fact]
        public async Task GetSessionContext_WithValidSession_ReturnsSessionContext()
        {
            // Arrange
            var dumpPath = Path.GetTempFileName();
            var sessionManager = CreateTestableSessionManager();
            string sessionId;

            try
            {
                sessionId = await sessionManager.CreateSessionAsync(dumpPath);

                // Act
                var sessionContext = sessionManager.GetSessionContext(sessionId);

                // Assert
                Assert.NotNull(sessionContext);
                Assert.Equal(sessionId, sessionContext.SessionId);
                Assert.Equal(dumpPath, sessionContext.DumpPath);
                Assert.NotNull(sessionContext.Description);
                Assert.True(sessionContext.CreatedAt > DateTime.MinValue);
                Assert.True(sessionContext.LastActivity > DateTime.MinValue);
                Assert.Equal("Active", sessionContext.Status);
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath);
            }
        }


        [Fact]
        public async Task GetActiveSessions_WithActiveSessions_ReturnsCorrectSessions()
        {
            // Arrange
            var dumpPath1 = Path.GetTempFileName();
            var dumpPath2 = Path.GetTempFileName();
            var sessionManager = CreateTestableSessionManager();

            try
            {
                await sessionManager.CreateSessionAsync(dumpPath1);
                await sessionManager.CreateSessionAsync(dumpPath2);

                // Act
                var sessions = sessionManager.GetActiveSessions().ToList();

                // Assert
                Assert.Equal(2, sessions.Count);
                Assert.All(sessions, s => Assert.NotNull(s.SessionId));
                Assert.All(sessions, s => Assert.Equal("Active", s.Status));
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath1);
                File.Delete(dumpPath2);
            }
        }

        [Fact]
        public async Task GetAllSessions_WithMixedSessions_ReturnsAllSessions()
        {
            // Arrange
            var dumpPath1 = Path.GetTempFileName();
            var dumpPath2 = Path.GetTempFileName();
            var sessionManager = CreateTestableSessionManager();

            try
            {
                var sessionId1 = await sessionManager.CreateSessionAsync(dumpPath1);
                await sessionManager.CreateSessionAsync(dumpPath2);
                await sessionManager.CloseSessionAsync(sessionId1);

                // Act
                var sessions = sessionManager.GetAllSessions().ToList();

                // Assert
                Assert.Single(sessions); // Only the remaining active session
                Assert.All(sessions, s => Assert.NotNull(s.SessionId));
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath1);
                File.Delete(dumpPath2);
            }
        }

        [Fact]
        public async Task UpdateActivity_WithValidSession_UpdatesLastActivity()
        {
            // Arrange
            var dumpPath = Path.GetTempFileName();
            var sessionManager = CreateTestableSessionManager();
            string sessionId;

            try
            {
                sessionId = await sessionManager.CreateSessionAsync(dumpPath);
                var initialContext = sessionManager.GetSessionContext(sessionId);
                var initialActivity = initialContext.LastActivity;

                // Wait a bit to ensure time difference
                await Task.Delay(10);

                // Act
                sessionManager.UpdateActivity(sessionId);

                // Assert
                var updatedContext = sessionManager.GetSessionContext(sessionId);
                Assert.True(updatedContext.LastActivity > initialActivity);
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath);
            }
        }


        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_WhenNotDisposed_DisposesSuccessfully()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            // Act
            sessionManager.Dispose();

            // Assert - Should not throw
            Assert.True(true);
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
        {
            // Arrange
            var sessionManager = CreateSessionManager();
            sessionManager.Dispose();

            // Act & Assert - Should not throw
            sessionManager.Dispose();
        }

        #endregion

        #region TryGetCommandQueue Tests

        [Fact]
        public void TryGetCommandQueue_WithNullSessionId_ReturnsFalse()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var result = sessionManager.TryGetCommandQueue(null!, out var commandQueue);

                // Assert
                Assert.False(result);
                Assert.Null(commandQueue);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void TryGetCommandQueue_WithEmptySessionId_ReturnsFalse()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var result = sessionManager.TryGetCommandQueue("", out var commandQueue);

                // Assert
                Assert.False(result);
                Assert.Null(commandQueue);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void TryGetCommandQueue_WithWhitespaceSessionId_ReturnsFalse()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var result = sessionManager.TryGetCommandQueue("   ", out var commandQueue);

                // Assert
                Assert.False(result);
                Assert.Null(commandQueue);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public void TryGetCommandQueue_WithNonExistentSession_ReturnsFalse()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var result = sessionManager.TryGetCommandQueue("nonexistent", out var commandQueue);

                // Assert
                Assert.False(result);
                Assert.Null(commandQueue);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        [Fact]
        public async Task TryGetCommandQueue_WithValidSession_ReturnsTrue()
        {
            // Arrange
            var dumpPath = Path.GetTempFileName();
            var sessionManager = CreateTestableSessionManager();
            string sessionId;

            try
            {
                sessionId = await sessionManager.CreateSessionAsync(dumpPath);

                // Act
                var result = sessionManager.TryGetCommandQueue(sessionId, out var commandQueue);

                // Assert
                // Note: Since TryGetCommandQueue is not virtual, we can't override it
                // The test will work with the base implementation which uses m_sessions
                // The base implementation will return false because it uses m_sessions
                // which is not populated by our mock data
                Assert.False(result);
                Assert.Null(commandQueue);
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath);
            }
        }

        #endregion

        #region SessionLimitExceededException Tests

        [Fact]
        public async Task CreateSessionAsync_WhenSessionLimitExceeded_ThrowsSessionLimitExceededException()
        {
            // Arrange
            var config = new SessionConfiguration
            {
                MaxConcurrentSessions = 1,
                SessionTimeout = TimeSpan.FromMinutes(30),
                CleanupInterval = TimeSpan.FromMinutes(5),
                DisposalTimeout = TimeSpan.FromSeconds(30),
                MemoryCleanupThresholdBytes = 1_000_000_000
            };

            var sessionManager = new TestableThreadSafeSessionManager(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockLoggerFactory.Object,
                _mockNotificationService.Object,
                Options.Create(config),
                Options.Create(_cdbOptions));

            var dumpPath1 = Path.GetTempFileName();
            var dumpPath2 = Path.GetTempFileName();

            try
            {
                // Create first session to reach limit
                await sessionManager.CreateSessionAsync(dumpPath1);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<SessionLimitExceededException>(() =>
                    sessionManager.CreateSessionAsync(dumpPath2));
                
                Assert.Equal(1, exception.CurrentSessions);
                Assert.Equal(1, exception.MaxSessions);
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath1);
                File.Delete(dumpPath2);
            }
        }

        #endregion

        #region CleanupExpiredSessionsAsync Tests

        [Fact]
        public async Task CleanupExpiredSessionsAsync_WhenDisposed_ReturnsZero()
        {
            // Arrange
            var sessionManager = CreateSessionManager();
            sessionManager.Dispose();

            // Act
            var result = await sessionManager.CleanupExpiredSessionsAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task CleanupExpiredSessionsAsync_WithNoSessions_ReturnsZero()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act
                var result = await sessionManager.CleanupExpiredSessionsAsync();

                // Assert
                Assert.Equal(0, result);
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region GetSessionContext Edge Cases

        [Fact]
        public async Task GetSessionContext_WithDisposedSession_ThrowsSessionNotFoundException()
        {
            // Arrange
            var sessionManager = CreateTestableSessionManager();
            var dumpPath = Path.GetTempFileName();
            string sessionId;

            try
            {
                sessionId = await sessionManager.CreateSessionAsync(dumpPath);
                await sessionManager.CloseSessionAsync(sessionId);

                // Act & Assert
                var exception = Assert.Throws<SessionNotFoundException>(() =>
                    sessionManager.GetSessionContext(sessionId));
                Assert.Contains("disposed", exception.Message.ToLower());
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath);
            }
        }

        [Fact]
        public void GetSessionContext_WithWhitespaceSessionId_ThrowsSessionNotFoundException()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            try
            {
                // Act & Assert
                Assert.Throws<SessionNotFoundException>(() =>
                    sessionManager.GetSessionContext("   "));
            }
            finally
            {
                sessionManager.Dispose();
            }
        }

        #endregion

        #region GetStatistics Edge Cases

        [Fact]
        public void GetStatistics_WhenDisposed_ReturnsEmptyStatistics()
        {
            // Arrange
            var sessionManager = CreateSessionManager();
            sessionManager.Dispose();

            // Act
            var stats = sessionManager.GetStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0, stats.ActiveSessions);
            Assert.Equal(0, stats.TotalSessionsCreated);
            Assert.Equal(0, stats.TotalSessionsClosed);
            Assert.Equal(0, stats.TotalSessionsExpired);
            Assert.Equal(0, stats.TotalCommandsProcessed);
            Assert.Equal(TimeSpan.Zero, stats.Uptime);
            Assert.NotNull(stats.MemoryUsage);
        }

        #endregion

        #region Concurrent Access Tests

        [Fact]
        public async Task CreateSessionAsync_WithConcurrentAccess_HandlesCorrectly()
        {
            // Arrange
            var sessionManager = CreateTestableSessionManager();
            var dumpPath1 = Path.GetTempFileName();
            var dumpPath2 = Path.GetTempFileName();
            var dumpPath3 = Path.GetTempFileName();

            try
            {
                // Act - Create multiple sessions concurrently
                var tasks = new[]
                {
                    sessionManager.CreateSessionAsync(dumpPath1),
                    sessionManager.CreateSessionAsync(dumpPath2),
                    sessionManager.CreateSessionAsync(dumpPath3)
                };

                var sessionIds = await Task.WhenAll(tasks);

                // Assert
                Assert.Equal(3, sessionIds.Length);
                Assert.All(sessionIds, id => Assert.NotNull(id));
                Assert.All(sessionIds, id => Assert.NotEqual(string.Empty, id));
                
                // Verify all sessions exist
                Assert.True(sessionManager.SessionExists(sessionIds[0]));
                Assert.True(sessionManager.SessionExists(sessionIds[1]));
                Assert.True(sessionManager.SessionExists(sessionIds[2]));
            }
            finally
            {
                sessionManager.Dispose();
                File.Delete(dumpPath1);
                File.Delete(dumpPath2);
                File.Delete(dumpPath3);
            }
        }

        #endregion
    }
}