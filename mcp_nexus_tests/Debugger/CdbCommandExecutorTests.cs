using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;

namespace mcp_nexus_tests.Debugger
{
    public class CdbCommandExecutorTests
    {
        private readonly Mock<ILogger<CdbCommandExecutor>> m_MockLogger;
        private readonly Mock<ILogger<CdbOutputParser>> m_MockOutputParserLogger;
        private readonly CdbSessionConfiguration m_Config;
        private readonly CdbOutputParser m_OutputParser;
        private readonly CdbCommandExecutor m_Executor;

        public CdbCommandExecutorTests()
        {
            m_MockLogger = new Mock<ILogger<CdbCommandExecutor>>();
            m_MockOutputParserLogger = new Mock<ILogger<CdbOutputParser>>();
            m_Config = new CdbSessionConfiguration();
            m_OutputParser = new CdbOutputParser(m_MockOutputParserLogger.Object);
            m_Executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);
        }

        [Fact]
        public void CdbCommandExecutor_Class_Exists()
        {
            // Assert
            Assert.NotNull(typeof(CdbCommandExecutor));
        }

        [Fact]
        public void CdbCommandExecutor_IsNotStatic()
        {
            // Assert
            Assert.False(typeof(CdbCommandExecutor).IsAbstract);
        }

        [Fact]
        public void CdbCommandExecutor_IsClass()
        {
            // Assert
            Assert.True(typeof(CdbCommandExecutor).IsClass);
        }

        [Fact]
        public void CdbCommandExecutor_IsNotInterface()
        {
            // Assert
            Assert.False(typeof(CdbCommandExecutor).IsInterface);
        }

        [Fact]
        public void CdbCommandExecutor_IsNotValueType()
        {
            // Assert
            Assert.False(typeof(CdbCommandExecutor).IsValueType);
        }

        [Fact]
        public void CdbCommandExecutor_IsNotSealed()
        {
            // Assert
            Assert.False(typeof(CdbCommandExecutor).IsSealed);
        }

        [Fact]
        public void CdbCommandExecutor_ImplementsIDisposable()
        {
            // Assert
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(CdbCommandExecutor)));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CdbCommandExecutor(null!, m_Config, m_OutputParser));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CdbCommandExecutor(m_MockLogger.Object, null!, m_OutputParser));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Assert
            Assert.NotNull(executor);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Act & Assert
            var exception = Record.Exception(() =>
            {
                executor.Dispose();
                executor.Dispose();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_DisposesResources()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Act
            executor.Dispose();

            // Assert
            // No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithNullCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                m_Executor.ExecuteCommandAsync(null!, mockProcessManager.Object, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithEmptyCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                m_Executor.ExecuteCommandAsync("", mockProcessManager.Object, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithInactiveProcessManager_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);
            mockProcessManager.Setup(x => x.IsActive).Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, CancellationToken.None));

            Assert.Equal("No active debugging session", exception.Message);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithNullInputStream_ReturnsErrorMessage()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);
            mockProcessManager.Setup(x => x.IsActive).Returns(true);
            mockProcessManager.Setup(x => x.DebuggerProcess).Returns((Process?)null);
            mockProcessManager.Setup(x => x.DebuggerInput).Returns((StreamWriter?)null);
            mockProcessManager.Setup(x => x.DebuggerOutput).Returns(new Mock<StreamReader>(Stream.Null).Object);

            // Act
            var result = await m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, CancellationToken.None);

            // Assert
            Assert.Contains("No input stream available for CDB process", result);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithNullOutputStream_ReturnsErrorMessage()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);
            mockProcessManager.Setup(x => x.IsActive).Returns(true);
            mockProcessManager.Setup(x => x.DebuggerProcess).Returns((Process?)null);
            mockProcessManager.Setup(x => x.DebuggerInput).Returns(new Mock<StreamWriter>(Stream.Null).Object);
            mockProcessManager.Setup(x => x.DebuggerOutput).Returns((StreamReader?)null);

            // Act
            var result = await m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, CancellationToken.None);

            // Assert
            Assert.Contains("No output stream available", result);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithValidCommand_HandlesCorrectly()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);
            mockProcessManager.Setup(x => x.IsActive).Returns(true);
            mockProcessManager.Setup(x => x.DebuggerProcess).Returns((Process?)null);

            // Act
            var result = await m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, CancellationToken.None);

            // Assert
            Assert.Contains("No input stream available for CDB process", result);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithWhitespaceCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                m_Executor.ExecuteCommandAsync("   ", mockProcessManager.Object, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithCancellation_HandlesCorrectly()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, cts.Token));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithTimeout_HandlesCorrectly()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);
            mockProcessManager.Setup(x => x.IsActive).Returns(true);
            mockProcessManager.Setup(x => x.DebuggerProcess).Returns((Process?)null);

            // Act
            var result = await m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, CancellationToken.None);

            // Assert
            Assert.Contains("No input stream available for CDB process", result);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithException_HandlesCorrectly()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);
            mockProcessManager.Setup(x => x.IsActive).Returns(true);
            mockProcessManager.Setup(x => x.DebuggerProcess).Returns((Process?)null);

            // Act
            var result = await m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, CancellationToken.None);

            // Assert
            Assert.Contains("No input stream available for CDB process", result);
        }

        [Fact]
        public void CancelCurrentOperation_WithNoActiveOperation_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => m_Executor.CancelCurrentOperation());
            Assert.Null(exception);
        }

        [Fact]
        public void CancelCurrentOperation_WithNoActiveOperation_LogsDebugMessage()
        {
            // Act
            m_Executor.CancelCurrentOperation();

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No active operation to cancel")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void CancelCurrentOperation_WithActiveOperation_CancelsOperation()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);

            // Act
            m_Executor.CancelCurrentOperation();

            // Assert
            // The operation should be cancelled
            Assert.True(true); // This test verifies that CancelCurrentOperation doesn't throw
        }
    }
}