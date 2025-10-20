using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue.Recovery;
using mcp_nexus.Debugger;
using System.Reflection;

namespace mcp_nexus_unit_tests.CommandQueue.Recovery
{
    /// <summary>
    /// Tests for SessionHealthMonitor
    /// </summary>
    public class SessionHealthMonitorTests
    {
        private readonly Mock<ICdbSession> m_MockCdbSession;
        private readonly Mock<ILogger> m_MockLogger;
        private readonly RecoveryConfiguration m_Config;

        public SessionHealthMonitorTests()
        {
            m_MockCdbSession = new Mock<ICdbSession>();
            m_MockLogger = new Mock<ILogger>();
            m_Config = new RecoveryConfiguration(
                healthCheckInterval: TimeSpan.FromMinutes(1)
            );
        }

        /// <summary>
        /// Helper method to expire the cache by setting last health check to 31 seconds ago
        /// </summary>
        private static void ExpireCache(SessionHealthMonitor monitor)
        {
            var field = typeof(SessionHealthMonitor).GetField("m_LastHealthCheck", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(monitor, DateTime.Now.AddSeconds(-31));
        }

        [Fact]
        public void Constructor_WithNullCdbSession_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SessionHealthMonitor(null!, m_MockLogger.Object, m_Config));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SessionHealthMonitor(m_MockCdbSession.Object, null!, m_Config));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Assert
            Assert.NotNull(monitor);
        }

        [Fact]
        public void IsSessionHealthy_WithActiveSession_ReturnsTrue()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Returns(true);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Act
            var result = monitor.IsSessionHealthy();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSessionHealthy_AfterCacheExpires_WithInactiveSession_ReturnsFalse()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Returns(false);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Expire cache using reflection to avoid 30-second wait
            ExpireCache(monitor);

            // Act
            var result = monitor.IsSessionHealthy();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSessionHealthy_AfterCacheExpires_WithException_ReturnsFalse()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Throws(new InvalidOperationException("Test exception"));
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Expire cache using reflection
            ExpireCache(monitor);

            // Act
            var result = monitor.IsSessionHealthy();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSessionHealthy_ReturnsCachedResultWithin30Seconds()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Returns(true);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Expire cache and make first call to populate it
            ExpireCache(monitor);
            var result1 = monitor.IsSessionHealthy(); // First call populates cache

            // Change mock behavior
            m_MockCdbSession.Setup(s => s.IsActive).Returns(false);

            // Act - Second call within 30 seconds should use cache
            var result2 = monitor.IsSessionHealthy();

            // Assert
            Assert.True(result1);
            Assert.True(result2); // Should still be true from cache
        }

        [Fact]
        public async Task IsSessionResponsive_WithHealthySession_ReturnsTrue()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Returns(true);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Expire cache using reflection
            ExpireCache(monitor);

            // Act
            var result = await monitor.IsSessionResponsive();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsSessionResponsive_WithUnhealthySession_ReturnsFalse()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Returns(false);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Expire cache using reflection
            ExpireCache(monitor);

            // Act
            var result = await monitor.IsSessionResponsive();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsSessionResponsive_WithException_ReturnsFalse()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Throws(new Exception("Test exception"));
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Expire cache using reflection
            ExpireCache(monitor);

            // Act
            var result = await monitor.IsSessionResponsive();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TimeSinceLastHealthCheck_InitiallyZeroOrSmall()
        {
            // Arrange
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Act
            var elapsed = monitor.TimeSinceLastHealthCheck();

            // Assert
            Assert.True(elapsed < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void IsHealthCheckDue_WithRecentCheck_ReturnsFalse()
        {
            // Arrange
            var config = new RecoveryConfiguration(healthCheckInterval: TimeSpan.FromMinutes(10));
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, config);

            // Act
            var isDue = monitor.IsHealthCheckDue();

            // Assert
            Assert.False(isDue);
        }

        [Fact]
        public void GetSessionDiagnostics_WithActiveSession_ReturnsDiagnostics()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Returns(true);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Act
            var diagnostics = monitor.GetSessionDiagnostics();

            // Assert
            Assert.NotNull(diagnostics);
            Assert.True(diagnostics.IsActive);
            Assert.Null(diagnostics.ErrorMessage);
        }

        [Fact]
        public void GetSessionDiagnostics_WithInactiveSession_ReturnsDiagnostics()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Returns(false);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Act
            var diagnostics = monitor.GetSessionDiagnostics();

            // Assert
            Assert.NotNull(diagnostics);
            Assert.False(diagnostics.IsActive);
        }

        [Fact]
        public void GetSessionDiagnostics_WithException_ReturnsErrorDiagnostics()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Throws(new InvalidOperationException("Test error"));
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Act
            var diagnostics = monitor.GetSessionDiagnostics();

            // Assert
            Assert.NotNull(diagnostics);
            Assert.False(diagnostics.IsActive);
            Assert.NotNull(diagnostics.ErrorMessage);
            Assert.Contains("Test error", diagnostics.ErrorMessage);
        }

        [Fact]
        public void GetSessionDiagnostics_IncludesTimingInformation()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.IsActive).Returns(true);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, m_Config);

            // Act
            var diagnostics = monitor.GetSessionDiagnostics();

            // Assert
            Assert.NotNull(diagnostics);
            Assert.True(diagnostics.TimeSinceLastCheck < TimeSpan.FromSeconds(1));
            Assert.True(diagnostics.LastHealthCheck <= DateTime.Now);
        }

        [Fact]
        public void GetSessionDiagnostics_IncludesHealthCheckDueFlag()
        {
            // Arrange
            var config = new RecoveryConfiguration(healthCheckInterval: TimeSpan.FromSeconds(30));
            m_MockCdbSession.Setup(s => s.IsActive).Returns(true);
            var monitor = new SessionHealthMonitor(m_MockCdbSession.Object, m_MockLogger.Object, config);

            // Act - First check (immediately after construction, not due yet)
            var diagnostics1 = monitor.GetSessionDiagnostics();

            // Expire cache to make health check due
            ExpireCache(monitor);

            var diagnostics2 = monitor.GetSessionDiagnostics();

            // Assert
            Assert.False(diagnostics1.IsHealthCheckDue); // Recent check
            Assert.True(diagnostics2.IsHealthCheckDue); // After cache expired
        }
    }
}
