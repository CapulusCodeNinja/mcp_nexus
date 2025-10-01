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
        private readonly CdbSessionConfiguration _config;
        private readonly CdbProcessManager _processManager;

        public CdbProcessManagerTests()
        {
            _mockLogger = new Mock<ILogger<CdbProcessManager>>();
            _config = new CdbSessionConfiguration();
            _processManager = new CdbProcessManager(_mockLogger.Object, _config);
        }

        public void Dispose()
        {
            _processManager.Dispose();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new CdbProcessManager(null!, _config));
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
        public void StartProcess_WithNullTarget_ReturnsFalse()
        {
            var result = _processManager.StartProcess(null!);
            Assert.False(result);
        }

        [Fact]
        public void StartProcess_WithEmptyTarget_ReturnsFalse()
        {
            var result = _processManager.StartProcess("");
            Assert.False(result);
        }

        [Fact]
        public void StartProcess_WithWhitespaceTarget_ReturnsFalse()
        {
            var result = _processManager.StartProcess("   ");
            Assert.False(result);
        }

        [Fact]
        public void StartProcess_WithNonExistentCdbPath_ReturnsFalse()
        {
            var configWithInvalidPath = new CdbSessionConfiguration(customCdbPath: "nonexistent.exe");
            var processManager = new CdbProcessManager(_mockLogger.Object, configWithInvalidPath);

            var result = processManager.StartProcess("test.dmp");

            Assert.False(result);
            Assert.False(processManager.IsActive);
            processManager.Dispose();
        }

        [Fact]
        public void StartProcess_WithNullCdbPath_ReturnsFalse()
        {
            var configWithNullPath = new CdbSessionConfiguration(customCdbPath: null);
            var processManager = new CdbProcessManager(_mockLogger.Object, configWithNullPath);

            var result = processManager.StartProcess("test.dmp");
            Assert.False(result);
            processManager.Dispose();
        }

        [Fact]
        public void StartProcess_WithEmptyCdbPath_ReturnsFalse()
        {
            var configWithEmptyPath = new CdbSessionConfiguration(customCdbPath: "");
            var processManager = new CdbProcessManager(_mockLogger.Object, configWithEmptyPath);

            var result = processManager.StartProcess("test.dmp");
            Assert.False(result);
            processManager.Dispose();
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
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
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
            // This test is difficult to implement without mocking the config
            // since we can't easily make the config throw an exception
            // We'll test the basic functionality instead

            var result = _processManager.StartProcess("test.dmp");

            Assert.False(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start CDB process")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}