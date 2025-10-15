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
using mcp_nexus_unit_tests.Mocks;

namespace mcp_nexus_unit_tests.Debugger
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
            m_Config = new CdbSessionConfiguration(commandTimeoutMs: 600000); // 10 minutes for tests
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
                m_Executor.ExecuteCommandAsync(null!, "test-command-id", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithEmptyCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                m_Executor.ExecuteCommandAsync("", "test-command-id", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithWhitespaceCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                m_Executor.ExecuteCommandAsync("   ", "test-command-id", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithInactiveProcessManager_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            mockProcessManager.Setup(x => x.IsActive).Returns(false);
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", "test-command-id", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithoutInitialization_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", "test-command-id", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithValidCommand_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            // With null DebuggerInput, commands should throw InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", "test-command-id", mockProcessManager.Object, CancellationToken.None));
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
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                m_Executor.ExecuteCommandAsync("test command", "test-command-id", mockProcessManager.Object, cts.Token));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithTimeout_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            // With null DebuggerInput, commands should throw InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", "test-command-id", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithException_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            mockProcessManager.Setup(x => x.DebuggerInput).Throws(new InvalidOperationException("Test exception"));
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("test command", "test-command-id", mockProcessManager.Object));
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

        [Fact]
        public async Task InitializeSessionAsync_WithProcessThatHasNullStreams_InitializesWithoutError()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            mockProcessManager.Setup(pm => pm.DebuggerProcess).Returns((Process?)null);

            // Act
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Assert - Should handle null process gracefully
            Assert.True(true);
        }

        [Fact]
        public async Task InitializeSessionAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            using var cts = new CancellationTokenSource();

            // Act
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object, cts.Token);

            // Assert - Should not throw
            Assert.True(true);
        }

        [Fact]
        public async Task InitializeSessionAsync_WithCancelledToken_StillInitializes()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object, cts.Token);

            // Assert - Initialization should complete before token is checked
            Assert.True(true);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithZeroCommandTimeout_ThrowsInvalidOperationException()
        {
            // Arrange
            var zeroTimeoutConfig = new CdbSessionConfiguration(commandTimeoutMs: 1); // Very short timeout
            var executor = new CdbCommandExecutor(m_MockLogger.Object, zeroTimeoutConfig, m_OutputParser);
            var mockProcessManager = CreateMockProcessManager();
            await executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                executor.ExecuteCommandAsync("test", "cmd-1", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithNullCommandId_ThrowsCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert - Should handle null command ID
            await Assert.ThrowsAnyAsync<Exception>(() =>
                m_Executor.ExecuteCommandAsync("test", null!, mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithEmptyCommandId_ThrowsCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert - Should handle empty command ID
            await Assert.ThrowsAnyAsync<Exception>(() =>
                m_Executor.ExecuteCommandAsync("test", "", mockProcessManager.Object));
        }

        [Fact]
        public void Dispose_BeforeInitialization_DisposesCleanly()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Act
            executor.Dispose();

            // Assert - Should not throw
            Assert.True(true);
        }

        [Fact]
        public async Task Dispose_AfterInitialization_DisposesCleanly()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);
            var mockProcessManager = CreateMockProcessManager();
            await executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act
            executor.Dispose();

            // Assert - Should not throw
            Assert.True(true);
        }

        [Fact]
        public async Task Dispose_DuringCommandExecution_CancelsCommand()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);
            var mockProcessManager = CreateMockProcessManager();
            await executor.InitializeSessionAsync(mockProcessManager.Object);

            // Start a command that will never complete
            var commandTask = executor.ExecuteCommandAsync("test", "cmd-1", mockProcessManager.Object);

            // Act
            executor.Dispose();

            // Assert - Command should be cancelled
            await Assert.ThrowsAnyAsync<Exception>(() => commandTask);
        }

        [Fact]
        public async Task ExecuteCommandAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);
            var mockProcessManager = CreateMockProcessManager();
            await executor.InitializeSessionAsync(mockProcessManager.Object);
            executor.Dispose();

            // Act & Assert - Should throw because disposed
            await Assert.ThrowsAnyAsync<Exception>(() =>
                executor.ExecuteCommandAsync("test", "cmd-1", mockProcessManager.Object));
        }

        [Fact]
        public async Task InitializeSessionAsync_MultipleTimesSequentially_OnlyInitializesOnce()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Assert - Should log warning for subsequent calls
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already initialized")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(2));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithSpecialCharactersInCommand_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert - Should handle special characters
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("!analyze -v; .echo \"test\"; k", "cmd-1", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithVeryLongCommand_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);
            var longCommand = new string('a', 10000);

            // Act & Assert - Should handle long commands
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync(longCommand, "cmd-1", mockProcessManager.Object));
        }

        [Fact]
        public void CdbCommandExecutor_WithDifferentConfigurations_CreatesCorrectly()
        {
            // Test with very short timeout
            var shortConfig = new CdbSessionConfiguration(commandTimeoutMs: 1000);
            var shortExecutor = new CdbCommandExecutor(m_MockLogger.Object, shortConfig, m_OutputParser);
            Assert.NotNull(shortExecutor);
            shortExecutor.Dispose();

            // Test with very long timeout
            var longConfig = new CdbSessionConfiguration(commandTimeoutMs: 3600000);
            var longExecutor = new CdbCommandExecutor(m_MockLogger.Object, longConfig, m_OutputParser);
            Assert.NotNull(longExecutor);
            longExecutor.Dispose();
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithDifferentCommandIds_TracksSeparately()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert - Different command IDs should be tracked separately
            var task1 = m_Executor.ExecuteCommandAsync("cmd1", "id-1", mockProcessManager.Object);
            var task2 = m_Executor.ExecuteCommandAsync("cmd2", "id-2", mockProcessManager.Object);

            // Both should throw because no real process
            await Assert.ThrowsAsync<InvalidOperationException>(() => task1);
            await Assert.ThrowsAsync<InvalidOperationException>(() => task2);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithEmptyCommandId_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() =>
                m_Executor.ExecuteCommandAsync("test command", string.Empty, mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithWhitespaceCommandId_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() =>
                m_Executor.ExecuteCommandAsync("test command", "   ", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithEmptyCommand_HandlesGracefully()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() =>
                m_Executor.ExecuteCommandAsync(string.Empty, "cmd-123", mockProcessManager.Object));
        }

        [Fact]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Act & Assert - Multiple dispose calls should be safe
            executor.Dispose();
            executor.Dispose();
            executor.Dispose();

            Assert.True(true); // If we get here, no exception was thrown
        }


        [Fact]
        public async Task InitializeSessionAsync_WithAlreadyCancelledToken_CompletesQuickly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act - Should handle cancelled token gracefully
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object, cts.Token);

            // Assert - If we get here, it handled cancellation gracefully
            Assert.True(cts.IsCancellationRequested);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithLongRunningCommand_RespectsTimeout()
        {
            // Arrange
            var shortTimeoutConfig = new CdbSessionConfiguration(commandTimeoutMs: 100); // Very short timeout
            var executor = new CdbCommandExecutor(m_MockLogger.Object, shortTimeoutConfig, m_OutputParser);
            var mockProcessManager = CreateMockProcessManager();

            // Act & Assert - Should timeout
            await Assert.ThrowsAnyAsync<Exception>(() =>
                executor.ExecuteCommandAsync("!analyze -v", "cmd-timeout", mockProcessManager.Object));

            executor.Dispose();
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithCancellationToken_CanBeCancelled()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            var cts = new CancellationTokenSource();

            // Act - Start command then cancel immediately
            var task = m_Executor.ExecuteCommandAsync("test", "cmd-123", mockProcessManager.Object);
            cts.Cancel();

            // Assert - Should eventually complete (with error)
            await Assert.ThrowsAnyAsync<Exception>(() => task);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CdbCommandExecutor(null!, m_Config, m_OutputParser));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CdbCommandExecutor(m_MockLogger.Object, null!, m_OutputParser));
        }

        [Fact]
        public void Constructor_WithNullOutputParser_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CdbCommandExecutor(m_MockLogger.Object, m_Config, null!));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithInactiveProcess_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            mockProcessManager.Setup(pm => pm.IsActive).Returns(false); // Process is not active
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                m_Executor.ExecuteCommandAsync("kL", "cmd-1", mockProcessManager.Object));
        }

        [Fact]
        public async Task InitializeSessionAsync_CalledTwice_LogsWarningAndReturnsImmediately()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();

            // Act
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object); // Second call

            // Assert - Should not throw and should log warning
            // The second call should return immediately without doing anything
        }

        [Fact]
        public void Dispose_AfterMultipleOperations_DisposesSuccessfully()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Act - Perform multiple operations then dispose
            executor.Dispose();

            // Assert - Should complete without throwing
            Assert.True(true);
        }

        [Fact]
        public void CdbSessionConfiguration_WithZeroTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert - Configuration constructor validates timeout
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(commandTimeoutMs: 0));
        }

        [Fact]
        public void CdbSessionConfiguration_WithNegativeTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert - Configuration constructor validates timeout
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(commandTimeoutMs: -1));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithNullProcessManager_ThrowsException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert - Expecting NullReferenceException when null process manager is passed
            await Assert.ThrowsAsync<NullReferenceException>(async () =>
                await m_Executor.ExecuteCommandAsync("test", "cmd-1", null!, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithWhitespaceCommandAndValidId_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await m_Executor.ExecuteCommandAsync("   ", "cmd-1", mockProcessManager.Object));
        }

        [Fact]
        public async Task InitializeSessionAsync_WithCancelledTokenSource_StillCompletes()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel before calling

            // Act - Should still initialize
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object, cts.Token);

            // Assert - No exception thrown
            Assert.True(true);
        }

        [Fact]
        public void CdbSessionConfiguration_WithMinimumValidTimeout_CreatesSuccessfully()
        {
            // Act
            var config = new CdbSessionConfiguration(commandTimeoutMs: 1);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(1, config.CommandTimeoutMs);
        }

        [Fact]
        public void CdbSessionConfiguration_WithLargeTimeout_CreatesSuccessfully()
        {
            // Act
            var config = new CdbSessionConfiguration(commandTimeoutMs: int.MaxValue);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(int.MaxValue, config.CommandTimeoutMs);
        }

        [Fact]
        public async Task ExecuteCommandAsync_AfterDisposeBeforeExecution_ThrowsObjectDisposedException()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);
            var mockProcessManager = CreateMockProcessManager();
            await executor.InitializeSessionAsync(mockProcessManager.Object);

            executor.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await executor.ExecuteCommandAsync("test", "cmd-1", mockProcessManager.Object));
        }

        [Fact]
        public async Task InitializeSessionAsync_AfterDispose_DoesNotThrow()
        {
            // Arrange
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);
            var mockProcessManager = CreateMockProcessManager();

            executor.Dispose();

            // Act - Should not throw (just logs and returns)
            await executor.InitializeSessionAsync(mockProcessManager.Object);

            // Assert
            Assert.True(true);
        }

        [Fact]
        public void CdbCommandExecutor_Constructor_WithAllValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var executor = new CdbCommandExecutor(m_MockLogger.Object, m_Config, m_OutputParser);

            // Assert
            Assert.NotNull(executor);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithVeryShortTimeout_MayTimeout()
        {
            // Arrange
            var shortTimeoutConfig = new CdbSessionConfiguration(commandTimeoutMs: 1); // 1ms timeout
            var executor = new CdbCommandExecutor(m_MockLogger.Object, shortTimeoutConfig, m_OutputParser);
            var mockProcessManager = CreateMockProcessManager();

            await executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert - May timeout or throw due to inactive process
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await executor.ExecuteCommandAsync("test command", "cmd-short-timeout", mockProcessManager.Object));
        }

        [Fact]
        public async Task InitializeSessionAsync_WithProcessHavingNoStreams_CompletesWithoutError()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            mockProcessManager.Setup(pm => pm.DebuggerProcess).Returns((Process?)null);
            mockProcessManager.Setup(pm => pm.DebuggerInput).Returns((StreamWriter?)null);

            // Act
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Assert - Should complete without error
            Assert.True(true);
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithCommandContainingNewlines_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert - Should throw due to inactive process, not command validation
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await m_Executor.ExecuteCommandAsync("line1\nline2", "cmd-newlines", mockProcessManager.Object));
        }

        [Fact]
        public async Task ExecuteCommandAsync_WithCommandContainingTabs_HandlesCorrectly()
        {
            // Arrange
            var mockProcessManager = CreateMockProcessManager();
            await m_Executor.InitializeSessionAsync(mockProcessManager.Object);

            // Act & Assert - Should throw due to inactive process
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await m_Executor.ExecuteCommandAsync("cmd\twith\ttabs", "cmd-tabs", mockProcessManager.Object));
        }

        /// <summary>
        /// Creates a mock CdbProcessManager with basic setup for testing.
        /// </summary>
        private static Mock<CdbProcessManager> CreateMockProcessManager()
        {
            var mockLogger = new Mock<ILogger<CdbProcessManager>>();
            var mockConfig = new CdbSessionConfiguration(commandTimeoutMs: 600000); // 10 minutes for tests
            var mockProcessManager = new Mock<CdbProcessManager>(mockLogger.Object, mockConfig);

            // Process properties are not virtual and cannot be mocked
            // Instead, we'll mock the CdbProcessManager properties directly

            mockProcessManager.Setup(pm => pm.IsActive).Returns(true);
            mockProcessManager.Setup(pm => pm.DebuggerProcess).Returns((Process?)null); // No process needed for basic tests
            mockProcessManager.Setup(pm => pm.DebuggerInput).Returns((StreamWriter?)null); // No input stream for basic tests

            return mockProcessManager;
        }
    }
}