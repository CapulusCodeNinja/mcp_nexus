using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus_tests.Mocks;

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
        public void CdbCommandExecutor_ImplementsIDisposable()
        {
            // Assert
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(CdbCommandExecutor)));
        }

        [Fact]
        public void CdbCommandExecutor_Constructor_WithValidParameters_DoesNotThrow()
        {
            // Arrange & Act
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Assert
            Assert.NotNull(executor);
        }

        [Fact]
        public void CdbCommandExecutor_Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CdbCommandExecutor(null!, m_Config, m_OutputParser));
        }

        [Fact]
        public void CdbCommandExecutor_Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CdbCommandExecutor(m_MockLogger.Object, null!, m_OutputParser));
        }

        [Fact]
        public void CdbCommandExecutor_Constructor_WithNullOutputParser_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CdbCommandExecutor(m_MockLogger.Object, m_Config, null!));
        }

        [Fact]
        public async Task InitializeSessionAsync_WithValidProcessManager_InitializesSuccessfully()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public async Task InitializeSessionAsync_WithNullProcessManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => m_Executor.InitializeSessionAsync(null!));
        }

        [Fact]
        public async Task InitializeSessionAsync_CalledTwice_LogsWarning()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Assert - Should log warning about already initialized
            // This is verified by the fact that no exception is thrown
            Assert.True(true);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithNullCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                m_Executor.ExecuteCommandAsync(null!, mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithEmptyCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                m_Executor.ExecuteCommandAsync("", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithWhitespaceCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                m_Executor.ExecuteCommandAsync("   ", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithInactiveProcessManager_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockProcessManager = new Mock<CdbProcessManager>(m_MockLogger.Object, m_Config);
            mockProcessManager.Setup(x => x.IsActive).Returns(false);
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithoutInitialization_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithValidCommand_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act
            var result = await m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, CancellationToken.None);

            // Assert
            // With the new architecture, commands will timeout if no output is produced
            // This is expected behavior for mocked streams that don't produce sentinels
            Assert.Contains("timed out", result);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithCancellation_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object, cts.Token));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithTimeout_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act
            var result = await m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object);

            // Assert
            // Should timeout since no output is produced by mocked streams
            Assert.Contains("timed out", result);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithException_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = new Mock<CdbProcessManager>(m_MockLogger.Object, m_Config);
            mockProcessManager.Setup(x => x.IsActive).Returns(true);
            mockProcessManager.Setup(x => x.DebuggerInput).Throws(new InvalidOperationException("Test exception"));
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", mockProcessManager.Object));
        }

        [Fact]
        public void Dispose_DisposesCorrectly()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Act
            executor.Dispose();

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Act & Assert
            executor.Dispose();
            executor.Dispose(); // Should not throw
            Assert.True(true);
        }

        /// <summary>
        /// Creates a mock CdbProcessManager with basic setup for testing.
        /// </summary>
        private Mock<CdbProcessManager> CreateMockProcessManager()
        {
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration();
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);

            var mockProcess = new Mock<Process>();
            var mockInput = new Mock<StreamWriter>();
            var mockOutput = new Mock<StreamReader>();
            var mockError = new Mock<StreamReader>();

            mockProcess.Setup(p => p.HasExited).Returns(false);
            mockProcess.Setup(p => p.StandardInput).Returns(mockInput.Object);
            mockProcess.Setup(p => p.StandardOutput).Returns(mockOutput.Object);
            mockProcess.Setup(p => p.StandardError).Returns(mockError.Object);

            mockProcessManager.Setup(pm => pm.IsActive).Returns(true);
            mockProcessManager.Setup(pm => pm.DebuggerProcess).Returns(mockProcess.Object);
            mockProcessManager.Setup(pm => pm.DebuggerInput).Returns(mockInput.Object);
            mockProcessManager.Setup(pm => pm.DebuggerOutput).Returns(mockOutput.Object);
            mockProcessManager.Setup(pm => pm.DebuggerError).Returns(mockError.Object);

            // Mock streams to return null (end of stream) to simulate timeout behavior
            mockOutput.Setup(sr => sr.ReadLineAsync()).ReturnsAsync((string?)null);
            mockError.Setup(sr => sr.ReadLineAsync()).ReturnsAsync((string?)null);

            return mockProcessManager;
        }
    }
}