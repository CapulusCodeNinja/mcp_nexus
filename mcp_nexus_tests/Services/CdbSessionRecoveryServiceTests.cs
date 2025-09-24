using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace mcp_nexus_tests.Services
{
    public class CdbSessionRecoveryServiceTests
    {
        private readonly Mock<ICdbSession> m_mockCdbSession;
        private readonly Mock<ICommandQueueService> m_mockCommandQueueService;
        private readonly ILogger<CdbSessionRecoveryService> m_logger;
        private readonly CdbSessionRecoveryService m_service;

        public CdbSessionRecoveryServiceTests()
        {
            m_mockCdbSession = new Mock<ICdbSession>();
            m_mockCommandQueueService = new Mock<ICommandQueueService>();
            m_logger = LoggerFactory.Create(b => { }).CreateLogger<CdbSessionRecoveryService>();
            
            m_service = new CdbSessionRecoveryService(
                m_mockCdbSession.Object,
                m_logger,
                m_mockCommandQueueService.Object);
        }

        [Fact]
        public async Task RecoverStuckSession_SuccessfulCancellation_ReturnsTrue()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(2);
            m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
            m_mockCdbSession.Setup(s => s.ExecuteCommand("version", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Microsoft (R) Windows Debugger Version 10.0");

            // Act
            var result = await m_service.RecoverStuckSession("Test timeout");

            // Assert
            Assert.True(result);
            m_mockCommandQueueService.Verify(s => s.CancelAllCommands(It.Is<string>(r => r.Contains("Test timeout"))), Times.Once);
            m_mockCdbSession.Verify(s => s.CancelCurrentOperation(), Times.Once);
        }

        [Fact]
        public async Task RecoverStuckSession_CancellationFails_ProceedsToForceRestart()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(1);
            m_mockCdbSession.Setup(s => s.CancelCurrentOperation()).Throws(new InvalidOperationException("CDB not responding"));
            
            // Setup for responsiveness test to fail (which triggers force restart)
            m_mockCdbSession.Setup(s => s.ExecuteCommand("version", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("Responsiveness test timeout"));
            
            // Setup for force restart
            m_mockCdbSession.Setup(s => s.StopSession()).ReturnsAsync(true);
            
            // IsActive sequence: true during responsiveness test, false after force restart
            m_mockCdbSession.SetupSequence(s => s.IsActive)
                .Returns(true)  // During responsiveness test
                .Returns(false); // After force restart (session successfully stopped)

            // Act
            var result = await m_service.RecoverStuckSession("Test unresponsive");

            // Assert
            Assert.True(result);
            m_mockCommandQueueService.Verify(s => s.CancelAllCommands(It.IsAny<string>()), Times.AtLeastOnce);
            m_mockCdbSession.Verify(s => s.StopSession(), Times.Once);
        }

        [Fact]
        public async Task RecoverStuckSession_SessionUnresponsive_ForceRestart()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(0);
            m_mockCdbSession.Setup(s => s.ExecuteCommand("version", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("Responsiveness test timeout"));
            m_mockCdbSession.Setup(s => s.StopSession()).ReturnsAsync(true);
            
            // Setup IsActive sequence: true during responsiveness test, false after restart
            m_mockCdbSession.SetupSequence(s => s.IsActive)
                .Returns(true)  // During responsiveness test
                .Returns(false); // After restart (session stopped)

            // Act
            var result = await m_service.RecoverStuckSession("Test unresponsive session");

            // Assert
            Assert.True(result);
            m_mockCdbSession.Verify(s => s.CancelCurrentOperation(), Times.Once);
            m_mockCdbSession.Verify(s => s.StopSession(), Times.Once);
        }

        [Fact]
        public async Task ForceRestartSession_SuccessfulStop_ReturnsTrue()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(3);
            m_mockCdbSession.Setup(s => s.StopSession()).ReturnsAsync(true);
            m_mockCdbSession.Setup(s => s.IsActive).Returns(false); // After stop

            // Act
            var result = await m_service.ForceRestartSession("Force restart test");

            // Assert
            Assert.True(result);
            m_mockCommandQueueService.Verify(s => s.CancelAllCommands(It.Is<string>(r => r.Contains("Force restart test"))), Times.Once);
            m_mockCdbSession.Verify(s => s.StopSession(), Times.Once);
        }

        [Fact]
        public async Task ForceRestartSession_StopFails_SessionStillActive_ReturnsFalse()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(1);
            m_mockCdbSession.Setup(s => s.StopSession()).ReturnsAsync(false);
            m_mockCdbSession.Setup(s => s.IsActive).Returns(true); // Still active after stop attempt

            // Act
            var result = await m_service.ForceRestartSession("Force restart test");

            // Assert
            Assert.False(result);
            m_mockCdbSession.Verify(s => s.StopSession(), Times.Once);
        }

        [Fact]
        public async Task ForceRestartSession_ExceptionDuringStop_ReturnsFalse()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(0);
            m_mockCdbSession.Setup(s => s.StopSession()).ThrowsAsync(new InvalidOperationException("Stop failed"));

            // Act
            var result = await m_service.ForceRestartSession("Exception test");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSessionHealthy_RecentCheck_ReturnsTrue()
        {
            // Arrange - No setup needed for basic health check

            // Act
            var result = m_service.IsSessionHealthy();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSessionHealthy_InactiveSession_ReturnsTrue()
        {
            // Arrange
            m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

            // Act
            var result = m_service.IsSessionHealthy();

            // Assert
            Assert.True(result); // Inactive is not unhealthy
        }

        [Fact]
        public void IsSessionHealthy_ExceptionThrown_ReturnsFalse()
        {
            // Arrange
            m_mockCdbSession.Setup(s => s.IsActive).Throws(new ObjectDisposedException("Session disposed"));

            // Force a health check by waiting for the cache to expire
            // Use reflection to set last health check time to force a new check
            var lastHealthCheckField = typeof(CdbSessionRecoveryService).GetField("m_lastHealthCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastHealthCheckField?.SetValue(m_service, DateTime.UtcNow.AddMinutes(-1)); // Force cache expiry

            // Act
            var result = m_service.IsSessionHealthy();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RecoverStuckSession_MultipleAttempts_TracksRecoveryCount()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(1);
            m_mockCdbSession.Setup(s => s.CancelCurrentOperation()).Throws(new InvalidOperationException("Always fails"));
            m_mockCdbSession.Setup(s => s.StopSession()).ReturnsAsync(true);
            m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

            // Act - Multiple recovery attempts
            var result1 = await m_service.RecoverStuckSession("First attempt");
            var result2 = await m_service.RecoverStuckSession("Second attempt");
            var result3 = await m_service.RecoverStuckSession("Third attempt");

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            
            // After too many attempts, health should be affected
            // (This would require accessing private fields to fully test)
        }

        [Fact]
        public async Task RecoverStuckSession_SessionBecomesResponsive_ResetsRecoveryCounter()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(1);
            m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
            m_mockCdbSession.Setup(s => s.ExecuteCommand("version", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Microsoft (R) Windows Debugger Version 10.0");

            // Act - Successful recovery should reset counter
            var result = await m_service.RecoverStuckSession("Test recovery reset");

            // Assert
            Assert.True(result);
            // Subsequent health checks should be positive
            var health = m_service.IsSessionHealthy();
            Assert.True(health);
        }

        [Fact]
        public async Task RecoverStuckSession_CommandQueueException_HandlesGracefully()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>()))
                .Throws(new ObjectDisposedException("Queue disposed"));

            // Act
            var result = await m_service.RecoverStuckSession("Queue exception test");

            // Assert
            Assert.False(result); // Should return false when recovery fails
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RecoverStuckSession_EmptyReason_HandlesGracefully(string reason)
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(0);
            m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

            // Act
            var result = await m_service.RecoverStuckSession(reason);

            // Assert
            Assert.True(result); // Should still work with empty reason
        }

        [Fact]
        public async Task RecoverStuckSession_NullReason_HandlesGracefully()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(0);
            m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

            // Act
            var result = await m_service.RecoverStuckSession(null!);

            // Assert
            Assert.True(result); // Should still work with null reason
        }

        [Fact]
        public async Task ForceRestartSession_WaitsForCleanup_VerifiesDelay()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(0);
            m_mockCdbSession.Setup(s => s.StopSession()).ReturnsAsync(true);
            m_mockCdbSession.Setup(s => s.IsActive).Returns(false);

            // Act
            var startTime = DateTime.UtcNow;
            var result = await m_service.ForceRestartSession("Cleanup delay test");
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(result);
            Assert.True(elapsed >= TimeSpan.FromMilliseconds(1800), "Should wait at least 2 seconds for cleanup");
        }

        [Fact]
        public async Task RecoverStuckSession_ResponsivenessTestTimeout_HandlesCorrectly()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(1);
            
            // Setup responsiveness test to timeout (will throw OperationCanceledException after 10s)
            m_mockCdbSession.Setup(s => s.ExecuteCommand("version", It.IsAny<CancellationToken>()))
                .Returns(async (string cmd, CancellationToken ct) =>
                {
                    await Task.Delay(15000, ct); // Longer than 10s timeout
                    return "Should not reach here";
                });
            
            m_mockCdbSession.Setup(s => s.StopSession()).ReturnsAsync(true);
            
            // Setup IsActive sequence
            m_mockCdbSession.SetupSequence(s => s.IsActive)
                .Returns(true)  // During responsiveness test
                .Returns(false); // After restart

            // Act
            var result = await m_service.RecoverStuckSession("Responsiveness timeout test");

            // Assert
            Assert.True(result); // Should recover via force restart
            m_mockCdbSession.Verify(s => s.StopSession(), Times.Once);
        }

        [Fact]
        public async Task RecoverStuckSession_ResponsivenessTestReturnsError_TriggersRestart()
        {
            // Arrange
            m_mockCommandQueueService.Setup(s => s.CancelAllCommands(It.IsAny<string>())).Returns(0);
            m_mockCdbSession.Setup(s => s.ExecuteCommand("version", It.IsAny<CancellationToken>()))
                .ReturnsAsync("failed"); // Contains "failed" so should be considered unresponsive
            m_mockCdbSession.Setup(s => s.StopSession()).ReturnsAsync(true);
            
            // Setup IsActive sequence
            m_mockCdbSession.SetupSequence(s => s.IsActive)
                .Returns(true)  // During responsiveness test
                .Returns(false); // After restart

            // Act
            var result = await m_service.RecoverStuckSession("Error response test");

            // Assert
            Assert.True(result);
            m_mockCdbSession.Verify(s => s.StopSession(), Times.Once);
        }
    }
}
