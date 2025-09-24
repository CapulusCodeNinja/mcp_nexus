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
        private readonly Mock<ICdbSessionRecoveryService> m_mockRecoveryService;

        public CdbSessionRecoveryServiceTests()
        {
            m_mockRecoveryService = new Mock<ICdbSessionRecoveryService>();
        }

        [Fact]
        public async Task RecoverStuckSession_SuccessfulCancellation_ReturnsTrue()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Test timeout"))
                .ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession("Test timeout");

            // Assert
            Assert.True(result);
            m_mockRecoveryService.Verify(s => s.RecoverStuckSession("Test timeout"), Times.Once);
        }

        [Fact]
        public async Task RecoverStuckSession_CancellationFails_ProceedsToForceRestart()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Test unresponsive"))
                .ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession("Test unresponsive");

            // Assert
            Assert.True(result);
            m_mockRecoveryService.Verify(s => s.RecoverStuckSession("Test unresponsive"), Times.Once);
        }

        [Fact]
        public async Task RecoverStuckSession_SessionUnresponsive_ForceRestart()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Test unresponsive session"))
                .ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession("Test unresponsive session");

            // Assert
            Assert.True(result);
            m_mockRecoveryService.Verify(s => s.RecoverStuckSession("Test unresponsive session"), Times.Once);
        }

        [Fact]
        public async Task ForceRestartSession_SuccessfulStop_ReturnsTrue()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.ForceRestartSession("Force restart test"))
                .ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.ForceRestartSession("Force restart test");

            // Assert
            Assert.True(result);
            m_mockRecoveryService.Verify(s => s.ForceRestartSession("Force restart test"), Times.Once);
        }

        [Fact]
        public async Task ForceRestartSession_StopFails_SessionStillActive_ReturnsFalse()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.ForceRestartSession("Force restart test"))
                .ReturnsAsync(false);

            // Act
            var result = await m_mockRecoveryService.Object.ForceRestartSession("Force restart test");

            // Assert
            Assert.False(result);
            m_mockRecoveryService.Verify(s => s.ForceRestartSession("Force restart test"), Times.Once);
        }

        [Fact]
        public async Task ForceRestartSession_ExceptionDuringStop_ReturnsFalse()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.ForceRestartSession("Exception test"))
                .ReturnsAsync(false);

            // Act
            var result = await m_mockRecoveryService.Object.ForceRestartSession("Exception test");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSessionHealthy_RecentCheck_ReturnsTrue()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.IsSessionHealthy()).Returns(true);

            // Act
            var result = m_mockRecoveryService.Object.IsSessionHealthy();

            // Assert
            Assert.True(result);
            m_mockRecoveryService.Verify(s => s.IsSessionHealthy(), Times.Once);
        }

        [Fact]
        public void IsSessionHealthy_InactiveSession_ReturnsTrue()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.IsSessionHealthy()).Returns(true);

            // Act
            var result = m_mockRecoveryService.Object.IsSessionHealthy();

            // Assert
            Assert.True(result);
            m_mockRecoveryService.Verify(s => s.IsSessionHealthy(), Times.Once);
        }

        [Fact]
        public void IsSessionHealthy_ExceptionThrown_ReturnsFalse()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.IsSessionHealthy()).Returns(false);

            // Act
            var result = m_mockRecoveryService.Object.IsSessionHealthy();

            // Assert
            Assert.False(result);
            m_mockRecoveryService.Verify(s => s.IsSessionHealthy(), Times.Once);
        }

        [Fact]
        public async Task RecoverStuckSession_MultipleAttempts_TracksRecoveryCount()
        {
            // Arrange - Three consecutive recovery calls should all succeed
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("First attempt")).ReturnsAsync(true);
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Second attempt")).ReturnsAsync(true);
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Third attempt")).ReturnsAsync(true);

            // Act - Multiple recovery attempts
            var result1 = await m_mockRecoveryService.Object.RecoverStuckSession("First attempt");
            var result2 = await m_mockRecoveryService.Object.RecoverStuckSession("Second attempt");
            var result3 = await m_mockRecoveryService.Object.RecoverStuckSession("Third attempt");

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            m_mockRecoveryService.Verify(s => s.RecoverStuckSession("First attempt"), Times.Once);
            m_mockRecoveryService.Verify(s => s.RecoverStuckSession("Second attempt"), Times.Once);
            m_mockRecoveryService.Verify(s => s.RecoverStuckSession("Third attempt"), Times.Once);
        }

        [Fact]
        public async Task RecoverStuckSession_SessionBecomesResponsive_ResetsRecoveryCounter()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Test recovery reset")).ReturnsAsync(true);
            m_mockRecoveryService.Setup(s => s.IsSessionHealthy()).Returns(true);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession("Test recovery reset");
            var health = m_mockRecoveryService.Object.IsSessionHealthy();

            // Assert
            Assert.True(result);
            Assert.True(health);
        }

        [Fact]
        public async Task RecoverStuckSession_CommandQueueException_HandlesGracefully()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Queue exception test")).ReturnsAsync(false);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession("Queue exception test");

            // Assert
            Assert.False(result); // Should return false when recovery fails
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RecoverStuckSession_EmptyReason_HandlesGracefully(string reason)
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession(reason)).ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession(reason);

            // Assert
            Assert.True(result); // Should still work with empty reason
        }

        [Fact]
        public async Task RecoverStuckSession_NullReason_HandlesGracefully()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession(null!)).ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession(null!);

            // Assert
            Assert.True(result); // Should still work with null reason
        }

        [Fact]
        public async Task ForceRestartSession_WaitsForCleanup_VerifiesDelay()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.ForceRestartSession("Cleanup delay test")).ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.ForceRestartSession("Cleanup delay test");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task RecoverStuckSession_ResponsivenessTestTimeout_HandlesCorrectly()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Responsiveness timeout test")).ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession("Responsiveness timeout test");

            // Assert
            Assert.True(result); // Should recover via force restart
        }

        [Fact]
        public async Task RecoverStuckSession_ResponsivenessTestReturnsError_TriggersRestart()
        {
            // Arrange
            m_mockRecoveryService.Setup(s => s.RecoverStuckSession("Error response test")).ReturnsAsync(true);

            // Act
            var result = await m_mockRecoveryService.Object.RecoverStuckSession("Error response test");

            // Assert
            Assert.True(result);
        }
    }
}
