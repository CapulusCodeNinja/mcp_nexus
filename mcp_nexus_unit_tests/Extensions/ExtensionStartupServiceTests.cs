using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using mcp_nexus.Extensions;

namespace mcp_nexus_unit_tests.Extensions
{
    /// <summary>
    /// Tests for ExtensionStartupService - Hosted service that loads extensions at startup.
    /// </summary>
    public class ExtensionStartupServiceTests
    {
        private readonly Mock<ILogger<ExtensionStartupService>> m_MockLogger;
        private readonly Mock<IExtensionManager> m_MockExtensionManager;

        public ExtensionStartupServiceTests()
        {
            m_MockLogger = new Mock<ILogger<ExtensionStartupService>>();
            m_MockExtensionManager = new Mock<IExtensionManager>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExtensionStartupService(null!, m_MockExtensionManager.Object));
        }

        [Fact]
        public void Constructor_WithNullExtensionManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExtensionStartupService(m_MockLogger.Object, null!));
        }

        #endregion

        #region StartAsync Tests

        [Fact]
        public async Task StartAsync_LoadsExtensionsSuccessfully()
        {
            // Arrange
            var extensions = new List<ExtensionMetadata>
            {
                new ExtensionMetadata { Name = "test1", Version = "1.0.0" },
                new ExtensionMetadata { Name = "test2", Version = "1.0.0" }
            };

            m_MockExtensionManager.Setup(x => x.LoadExtensionsAsync())
                .Returns(Task.CompletedTask);
            m_MockExtensionManager.Setup(x => x.GetAllExtensions())
                .Returns(extensions);

            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            m_MockExtensionManager.Verify(x => x.LoadExtensionsAsync(), Times.Once);
            m_MockExtensionManager.Verify(x => x.GetAllExtensions(), Times.Once);
        }

        [Fact]
        public async Task StartAsync_LogsInformationMessages()
        {
            // Arrange
            var extensions = new List<ExtensionMetadata>
            {
                new ExtensionMetadata { Name = "test", Version = "1.0.0" }
            };

            m_MockExtensionManager.Setup(x => x.LoadExtensionsAsync())
                .Returns(Task.CompletedTask);
            m_MockExtensionManager.Setup(x => x.GetAllExtensions())
                .Returns(extensions);

            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading extensions")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Extensions loaded")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task StartAsync_WithNoExtensions_CompletesSuccessfully()
        {
            // Arrange
            var extensions = new List<ExtensionMetadata>();

            m_MockExtensionManager.Setup(x => x.LoadExtensionsAsync())
                .Returns(Task.CompletedTask);
            m_MockExtensionManager.Setup(x => x.GetAllExtensions())
                .Returns(extensions);

            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            m_MockExtensionManager.Verify(x => x.LoadExtensionsAsync(), Times.Once);
        }

        [Fact]
        public async Task StartAsync_WithLoadException_LogsError()
        {
            // Arrange
            var exception = new Exception("Load failed");
            m_MockExtensionManager.Setup(x => x.LoadExtensionsAsync())
                .ThrowsAsync(exception);

            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load extensions")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task StartAsync_WithCancellation_HandlesGracefully()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancel

            m_MockExtensionManager.Setup(x => x.LoadExtensionsAsync())
                .ThrowsAsync(new OperationCanceledException());

            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Act
            await service.StartAsync(cts.Token);

            // Assert
            // Should complete without logging error for OperationCanceledException
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task StartAsync_WithManyExtensions_LogsCorrectCount()
        {
            // Arrange
            var extensions = Enumerable.Range(1, 10)
                .Select(i => new ExtensionMetadata { Name = $"ext{i}", Version = "1.0.0" })
                .ToList();

            m_MockExtensionManager.Setup(x => x.LoadExtensionsAsync())
                .Returns(Task.CompletedTask);
            m_MockExtensionManager.Setup(x => x.GetAllExtensions())
                .Returns(extensions);

            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("10")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region StopAsync Tests

        [Fact]
        public async Task StopAsync_CompletesSuccessfully()
        {
            // Arrange
            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Act
            var task = service.StopAsync(CancellationToken.None);
            await task;

            // Assert
            Assert.True(task.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task StopAsync_DoesNotThrow()
        {
            // Arrange
            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);

            // Act & Assert
            // Should complete without throwing
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StopAsync_WithCancelledToken_CompletesAnyway()
        {
            // Arrange
            var service = new ExtensionStartupService(m_MockLogger.Object, m_MockExtensionManager.Object);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await service.StopAsync(cts.Token);

            // Assert
            // Should complete without throwing
            Assert.True(true);
        }

        #endregion
    }
}

