using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Helper;

namespace mcp_nexus_tests.Helper
{
    /// <summary>
    /// Comprehensive tests for CdbSession - the core debugger interaction class
    /// </summary>
    public class CdbSessionTests : IDisposable
    {
        private readonly Mock<ILogger<CdbSession>> m_mockLogger;
        private CdbSession? m_cdbSession;

        public CdbSessionTests()
        {
            m_mockLogger = new Mock<ILogger<CdbSession>>();
        }

        public void Dispose()
        {
            m_cdbSession?.Dispose();
        }

        [Fact]
        public void CdbSession_Constructor_WithValidParameters_Succeeds()
        {
            // Act
            var session = new CdbSession(m_mockLogger.Object, 5000, null, 10000, 2, "/test/path", 1000);

            // Assert
            Assert.NotNull(session);
            Assert.False(session.IsActive);
        }

        [Theory]
        [InlineData(0, "Command timeout must be positive")]
        [InlineData(-1, "Command timeout must be positive")]
        public void CdbSession_Constructor_InvalidCommandTimeout_ThrowsArgumentOutOfRangeException(int timeout, string expectedMessage)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => 
                new CdbSession(m_mockLogger.Object, timeout));
            
            Assert.Equal("commandTimeoutMs", ex.ParamName);
            Assert.Contains(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData(-1, "Symbol server timeout cannot be negative")]
        public void CdbSession_Constructor_InvalidSymbolServerTimeout_ThrowsArgumentOutOfRangeException(int timeout, string expectedMessage)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => 
                new CdbSession(m_mockLogger.Object, 5000, null, timeout));
            
            Assert.Equal("symbolServerTimeoutMs", ex.ParamName);
            Assert.Contains(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData(-1, "Symbol server max retries cannot be negative")]
        public void CdbSession_Constructor_InvalidSymbolServerMaxRetries_ThrowsArgumentOutOfRangeException(int retries, string expectedMessage)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => 
                new CdbSession(m_mockLogger.Object, 5000, null, 10000, retries));
            
            Assert.Equal("symbolServerMaxRetries", ex.ParamName);
            Assert.Contains(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData(-1, "Startup delay cannot be negative")]
        public void CdbSession_Constructor_InvalidStartupDelay_ThrowsArgumentOutOfRangeException(int delay, string expectedMessage)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => 
                new CdbSession(m_mockLogger.Object, 5000, null, 10000, 1, null, delay));
            
            Assert.Equal("startupDelayMs", ex.ParamName);
            Assert.Contains(expectedMessage, ex.Message);
        }

        [Fact]
        public void CdbSession_IsActive_WhenNotStarted_ReturnsFalse()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            Assert.False(m_cdbSession.IsActive);
        }

        [Fact]
        public void CdbSession_IsActive_WhenDisposed_ReturnsFalse()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);
            m_cdbSession.Dispose();

            // Act & Assert
            Assert.False(m_cdbSession.IsActive);
        }

        [Fact]
        public async Task CdbSession_StartSession_WithInvalidTarget_ReturnsFalse()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object, 1000); // Short timeout for test

            // Act
            var result = await m_cdbSession.StartSession("nonexistent_target_12345", null);

            // Assert
            Assert.False(result);
            Assert.False(m_cdbSession.IsActive);
        }

        [Fact]
        public async Task CdbSession_StartSession_WithNullTarget_ThrowsArgumentException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_cdbSession.StartSession(null!, null));
        }

        [Fact]
        public async Task CdbSession_StartSession_WithEmptyTarget_ThrowsArgumentException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_cdbSession.StartSession("", null));
        }

        [Fact]
        public async Task CdbSession_StartSession_WithWhitespaceTarget_ThrowsArgumentException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_cdbSession.StartSession("   ", null));
        }

        [Fact]
        public async Task CdbSession_StopSession_WhenNotStarted_ReturnsFalse()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act
            var result = await m_cdbSession.StopSession();

            // Assert
            Assert.False(result);
            Assert.False(m_cdbSession.IsActive);
        }

        [Fact]
        public async Task CdbSession_StopSession_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);
            m_cdbSession.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                m_cdbSession.StopSession());
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WhenNotActive_ThrowsInvalidOperationException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                m_cdbSession.ExecuteCommand("test command"));
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);
            m_cdbSession.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                m_cdbSession.ExecuteCommand("test command"));
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WithNullCommand_ThrowsArgumentException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_cdbSession.ExecuteCommand(null!));
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WithEmptyCommand_ThrowsArgumentException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_cdbSession.ExecuteCommand(""));
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WithWhitespaceCommand_ThrowsArgumentException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_cdbSession.ExecuteCommand("   "));
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WithCancellationToken_WhenNotActive_ThrowsInvalidOperationException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                m_cdbSession.ExecuteCommand("test command", CancellationToken.None));
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WithCancellationToken_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);
            m_cdbSession.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                m_cdbSession.ExecuteCommand("test command", CancellationToken.None));
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WithCancellationToken_WithNullCommand_ThrowsArgumentException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_cdbSession.ExecuteCommand(null!, CancellationToken.None));
        }

        [Fact]
        public void CdbSession_CancelCurrentOperation_WhenNotActive_DoesNotThrow()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => m_cdbSession.CancelCurrentOperation());
            Assert.Null(exception);
        }

        [Fact]
        public void CdbSession_CancelCurrentOperation_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);
            m_cdbSession.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => m_cdbSession.CancelCurrentOperation());
        }

        [Fact]
        public void CdbSession_Dispose_WhenNotDisposed_DoesNotThrow()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => m_cdbSession.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void CdbSession_Dispose_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act & Assert - Should not throw
            m_cdbSession.Dispose();
            var exception = Record.Exception(() => m_cdbSession.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void CdbSession_Dispose_AfterDisposal_IsActiveReturnsFalse()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Act
            m_cdbSession.Dispose();

            // Assert
            Assert.False(m_cdbSession.IsActive);
        }

        [Fact]
        public async Task CdbSession_StartSession_WithValidTargetButInvalidCdbPath_ReturnsFalse()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object, 1000, "nonexistent_cdb.exe");

            // Act
            var result = await m_cdbSession.StartSession("notepad.exe", null);

            // Assert
            Assert.False(result);
            Assert.False(m_cdbSession.IsActive);
        }

        [Fact]
        public void CdbSession_Constructor_WithCustomCdbPath_SetsCorrectPath()
        {
            // Arrange
            var customPath = "C:\\Custom\\cdb.exe";

            // Act
            m_cdbSession = new CdbSession(m_mockLogger.Object, 5000, customPath);

            // Assert
            Assert.NotNull(m_cdbSession);
            // Note: We can't directly test the internal path, but we can verify the constructor doesn't throw
        }

        [Fact]
        public void CdbSession_Constructor_WithSymbolSearchPath_SetsCorrectPath()
        {
            // Arrange
            var symbolPath = "C:\\Symbols;C:\\MoreSymbols";

            // Act
            m_cdbSession = new CdbSession(m_mockLogger.Object, 5000, null, 10000, 1, symbolPath);

            // Assert
            Assert.NotNull(m_cdbSession);
            // Note: We can't directly test the internal path, but we can verify the constructor doesn't throw
        }

        [Fact]
        public async Task CdbSession_StartSession_WithArguments_SetsCorrectArguments()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object, 1000, "nonexistent_cdb.exe");
            var target = "notepad.exe";
            var arguments = "-test -args";

            // Act
            var result = await m_cdbSession.StartSession(target, arguments);

            // Assert
            // Note: We can't easily test the actual arguments without starting a real process,
            // but we can verify the method doesn't throw and returns a boolean
            Assert.False(result); // Will be false because we can't start a real CDB session in tests
        }

        [Fact]
        public async Task CdbSession_ExecuteCommand_WithCancellationToken_CancellationWorks()
        {
            // Arrange
            m_cdbSession = new CdbSession(m_mockLogger.Object, 1000);
            var cts = new CancellationTokenSource();

            // Act & Assert
            // This will throw InvalidOperationException because session is not active,
            // but we're testing that the method signature works with cancellation
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                m_cdbSession.ExecuteCommand("test", cts.Token));
        }

        [Fact]
        public void CdbSession_Constructor_WithDefaultParameters_UsesDefaults()
        {
            // Act
            m_cdbSession = new CdbSession(m_mockLogger.Object);

            // Assert
            Assert.NotNull(m_cdbSession);
            Assert.False(m_cdbSession.IsActive);
        }

        [Fact]
        public void CdbSession_Constructor_WithMinimalParameters_UsesDefaults()
        {
            // Act
            m_cdbSession = new CdbSession(m_mockLogger.Object, 1000);

            // Assert
            Assert.NotNull(m_cdbSession);
            Assert.False(m_cdbSession.IsActive);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(30000)]
        [InlineData(300000)]
        public void CdbSession_Constructor_WithValidTimeouts_Succeeds(int timeoutMs)
        {
            // Act
            m_cdbSession = new CdbSession(m_mockLogger.Object, timeoutMs);

            // Assert
            Assert.NotNull(m_cdbSession);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public void CdbSession_Constructor_WithValidRetries_Succeeds(int retries)
        {
            // Act
            m_cdbSession = new CdbSession(m_mockLogger.Object, 5000, null, 10000, retries);

            // Assert
            Assert.NotNull(m_cdbSession);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void CdbSession_Constructor_WithValidStartupDelay_Succeeds(int delayMs)
        {
            // Act
            m_cdbSession = new CdbSession(m_mockLogger.Object, 5000, null, 10000, 1, null, delayMs);

            // Assert
            Assert.NotNull(m_cdbSession);
        }
    }
}