using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;

namespace mcp_nexus_tests.Debugger
{
    /// <summary>
    /// Tests for CdbProcessManager
    /// </summary>
    public class CdbProcessManagerTests : IDisposable
    {
        private readonly Mock<ILogger<CdbProcessManager>> _mockLogger;
        private readonly Mock<CdbSessionConfiguration> _mockConfig;
        private readonly CdbProcessManager _processManager;

        public CdbProcessManagerTests()
        {
            _mockLogger = new Mock<ILogger<CdbProcessManager>>();
            _mockConfig = new Mock<CdbSessionConfiguration>();
            _processManager = new CdbProcessManager(_mockLogger.Object, _mockConfig.Object);
        }

        public void Dispose()
        {
            _processManager.Dispose();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new CdbProcessManager(null!, _mockConfig.Object));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new CdbProcessManager(_mockLogger.Object, null!));
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            Assert.False(_processManager.IsActive);
            Assert.Null(_processManager.DebuggerProcess);
            Assert.Null(_processManager.DebuggerInput);
            Assert.Null(_processManager.DebuggerOutput);
            Assert.Null(_processManager.DebuggerError);
        }

        [Fact]
        public void StartProcess_WithNullTarget_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                _processManager.StartProcess(null!));
        }

        [Fact]
        public void StartProcess_WithEmptyTarget_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                _processManager.StartProcess(""));
        }

        [Fact]
        public void StartProcess_WithWhitespaceTarget_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                _processManager.StartProcess("   "));
        }

        [Fact]
        public void StartProcess_WithNonExistentCdbPath_ReturnsFalse()
        {
            _mockConfig.SetupGet(c => c.CustomCdbPath).Returns("nonexistent.exe");

            var result = _processManager.StartProcess("test.dmp");

            Assert.False(result);
            Assert.False(_processManager.IsActive);
        }

        [Fact]
        public void StartProcess_WithNullCdbPath_ThrowsFileNotFoundException()
        {
            _mockConfig.SetupGet(c => c.CustomCdbPath).Returns((string?)null);

            Assert.Throws<FileNotFoundException>(() => 
                _processManager.StartProcess("test.dmp"));
        }

        [Fact]
        public void StartProcess_WithEmptyCdbPath_ThrowsFileNotFoundException()
        {
            _mockConfig.SetupGet(c => c.CustomCdbPath).Returns("");

            Assert.Throws<FileNotFoundException>(() => 
                _processManager.StartProcess("test.dmp"));
        }

        [Fact]
        public void StopProcess_WhenNotActive_ReturnsFalse()
        {
            var result = _processManager.StopProcess();

            Assert.False(result);
        }

        [Fact]
        public void LogProcessDiagnostics_WithNullProcess_LogsNoProcess()
        {
            _processManager.LogProcessDiagnostics("test");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No debugger process")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void Dispose_WhenNotDisposed_SetsDisposedFlag()
        {
            _processManager.Dispose();

            Assert.False(_processManager.IsActive);
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
        {
            _processManager.Dispose();
            _processManager.Dispose();

            Assert.True(true);
        }

        [Fact]
        public void IsActive_WhenDisposed_ReturnsFalse()
        {
            _processManager.Dispose();

            Assert.False(_processManager.IsActive);
        }

        [Fact]
        public void IsActive_WhenNotDisposed_ReturnsFalse()
        {
            Assert.False(_processManager.IsActive);
        }

        [Fact]
        public void DebuggerProcess_WhenNotStarted_ReturnsNull()
        {
            Assert.Null(_processManager.DebuggerProcess);
        }

        [Fact]
        public void DebuggerInput_WhenNotStarted_ReturnsNull()
        {
            Assert.Null(_processManager.DebuggerInput);
        }

        [Fact]
        public void DebuggerOutput_WhenNotStarted_ReturnsNull()
        {
            Assert.Null(_processManager.DebuggerOutput);
        }

        [Fact]
        public void DebuggerError_WhenNotStarted_ReturnsNull()
        {
            Assert.Null(_processManager.DebuggerError);
        }

        [Fact]
        public void StartProcess_WithException_LogsErrorAndReturnsFalse()
        {
            _mockConfig.SetupGet(c => c.CustomCdbPath).Throws(new Exception("Test exception"));

            var result = _processManager.StartProcess("test.dmp");

            Assert.False(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start CDB process")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}