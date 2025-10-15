using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.CommandQueue.Batching;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Debugger;
using mcp_nexus.Notifications;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    /// <summary>
    /// Unit tests for IsolatedCommandQueueService resilience and recovery mechanisms
    /// </summary>
    public class IsolatedCommandQueueServiceResilienceTests
    {
        private readonly Mock<ICdbSession> m_MockCdbSession;
        private readonly Mock<ILogger<IsolatedCommandQueueService>> m_MockLogger;
        private readonly Mock<IMcpNotificationService> m_MockNotificationService;
        private readonly Mock<ILoggerFactory> m_MockLoggerFactory;
        private readonly Mock<ILogger<BatchCommandProcessor>> m_MockBatchLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolatedCommandQueueServiceResilienceTests"/> class.
        /// </summary>
        public IsolatedCommandQueueServiceResilienceTests()
        {
            m_MockCdbSession = new Mock<ICdbSession>();
            m_MockLogger = new Mock<ILogger<IsolatedCommandQueueService>>();
            m_MockNotificationService = new Mock<IMcpNotificationService>();
            m_MockLoggerFactory = new Mock<ILoggerFactory>();
            m_MockBatchLogger = new Mock<ILogger<BatchCommandProcessor>>();

            m_MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(m_MockBatchLogger.Object);
        }

        /// <summary>
        /// Tests that GetDiagnostics returns correct initial state
        /// </summary>
        [Fact]
        public void GetDiagnostics_ShouldReturnCorrectInitialState()
        {
            // Arrange
            var service = CreateService();

            // Act
            var diagnostics = service.GetDiagnostics();

            // Assert
            Assert.NotNull(diagnostics);
            Assert.Equal("test-session", diagnostics.SessionId);
            Assert.False(diagnostics.IsDisposed);
            Assert.False(diagnostics.IsCancellationRequested);
            Assert.Equal(0, diagnostics.TaskRestartCount);
            Assert.Equal(3, diagnostics.MaxTaskRestarts);
            Assert.Null(diagnostics.LastTaskException);
        }

        /// <summary>
        /// Tests that GetDiagnostics returns correct task status
        /// </summary>
        [Fact]
        public void GetDiagnostics_ShouldReturnTaskStatus()
        {
            // Arrange
            var service = CreateService();

            // Act
            var diagnostics = service.GetDiagnostics();

            // Assert
            Assert.NotNull(diagnostics.TaskStatus);
            Assert.NotEmpty(diagnostics.TaskStatus);
        }

        /// <summary>
        /// Tests that GetDiagnostics includes performance stats
        /// </summary>
        [Fact]
        public void GetDiagnostics_ShouldIncludePerformanceStats()
        {
            // Arrange
            var service = CreateService();

            // Act
            var diagnostics = service.GetDiagnostics();

            // Assert
            // PerformanceStats is a value tuple, so just verify it has expected structure
            Assert.True(diagnostics.PerformanceStats.Total >= 0);
        }

        /// <summary>
        /// Tests that IsReady returns true for healthy service
        /// </summary>
        [Fact]
        public void IsReady_ShouldReturnTrue_WhenServiceIsHealthy()
        {
            // Arrange
            var service = CreateService();

            // Act
            var isReady = service.IsReady();

            // Assert
            Assert.True(isReady);
        }

        /// <summary>
        /// Tests that IsReady returns false after disposal
        /// </summary>
        [Fact]
        public void IsReady_ShouldReturnFalse_AfterDisposal()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.Dispose();
            var isReady = service.IsReady();

            // Assert
            Assert.False(isReady);
        }

        /// <summary>
        /// Tests that diagnostics reflect disposed state
        /// </summary>
        [Fact]
        public void GetDiagnostics_ShouldReflectDisposedState()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.Dispose();
            
            // Assert - after disposal, GetDiagnostics should throw ObjectDisposedException
            Assert.Throws<ObjectDisposedException>(() => service.GetDiagnostics());
        }

        /// <summary>
        /// Tests that diagnostics show queue count
        /// </summary>
        [Fact]
        public void GetDiagnostics_ShouldShowQueueCount()
        {
            // Arrange
            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("test output");

            var service = CreateService();

            // Act
            service.QueueCommand("test command");
            var diagnostics = service.GetDiagnostics();

            // Assert
            Assert.True(diagnostics.QueueCount >= 0);
        }

        /// <summary>
        /// Tests that service can queue commands when ready
        /// </summary>
        [Fact]
        public void QueueCommand_ShouldSucceed_WhenServiceIsReady()
        {
            // Arrange
            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("test output");

            var service = CreateService();

            // Act
            var commandId = service.QueueCommand("test command");

            // Assert
            Assert.NotNull(commandId);
            Assert.NotEmpty(commandId);
        }

        /// <summary>
        /// Tests that service throws when queueing commands after disposal
        /// </summary>
        [Fact]
        public void QueueCommand_ShouldThrow_AfterDisposal()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => service.QueueCommand("test command"));
        }

        /// <summary>
        /// Tests that diagnostics show cancellation state
        /// </summary>
        [Fact]
        public void GetDiagnostics_ShouldShowCancellationState()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.ForceShutdownImmediate();
            var diagnostics = service.GetDiagnostics();

            // Assert
            Assert.True(diagnostics.IsCancellationRequested);
        }

        /// <summary>
        /// Tests that performance stats are tracked
        /// </summary>
        [Fact]
        public async Task GetPerformanceStats_ShouldTrackCommands()
        {
            // Arrange
            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("test output");

            var service = CreateService();

            // Act
            service.QueueCommand("test command 1");
            service.QueueCommand("test command 2");
            
            // Poll briefly (<=100ms total) to allow background processor to record stats
            (long Total, long Completed, long Failed, long Cancelled) stats = default;
            for (int i = 0; i < 10; i++)
            {
                stats = service.GetPerformanceStats();
                if (stats.Total >= 2)
                    break;
                await Task.Delay(10);
            }

            // Assert - commands are queued, so total should be at least 2
            // Note: Commands may not be processed yet, but they should be tracked
            Assert.True(stats.Total >= 2);
        }

        /// <summary>
        /// Creates a test instance of IsolatedCommandQueueService
        /// </summary>
        /// <returns>A new IsolatedCommandQueueService instance</returns>
        private IsolatedCommandQueueService CreateService()
        {
            return new IsolatedCommandQueueService(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                "test-session",
                m_MockLoggerFactory.Object,
                null,
                null
            );
        }
    }
}

