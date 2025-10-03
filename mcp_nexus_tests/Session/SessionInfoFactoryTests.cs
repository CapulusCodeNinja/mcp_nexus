using System;
using Moq;
using Xunit;
using mcp_nexus.Session;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Session.Models;

namespace mcp_nexus_tests.Session
{
    public class SessionInfoFactoryTests
    {
        private readonly SessionInfoFactory _factory;
        private readonly Mock<ICdbSession> _mockCdbSession;
        private readonly Mock<ICommandQueueService> _mockCommandQueue;

        public SessionInfoFactoryTests()
        {
            _factory = new SessionInfoFactory();
            _mockCdbSession = new Mock<ICdbSession>();
            _mockCommandQueue = new Mock<ICommandQueueService>();
        }

        [Fact]
        public void SessionInfoFactory_Class_Exists()
        {
            // Act
            var type = typeof(SessionInfoFactory);

            // Assert
            Assert.NotNull(type);
            Assert.True(type.IsClass);
        }

        [Fact]
        public void SessionInfoFactory_DefaultConstructor_CreatesInstance()
        {
            // Act
            var factory = new SessionInfoFactory();

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void CreateSessionInfo_WithValidParameters_ReturnsSessionInfo()
        {
            // Arrange
            var sessionId = "test-session-123";
            var dumpPath = @"C:\temp\test.dmp";
            var symbolsPath = @"C:\symbols";
            var processId = 1234;

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, symbolsPath, processId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
            Assert.Equal(_mockCdbSession.Object, result.CdbSession);
            Assert.Equal(_mockCommandQueue.Object, result.CommandQueue);
            Assert.Equal(dumpPath, result.DumpPath);
            Assert.Equal(symbolsPath, result.SymbolsPath);
            Assert.Equal(processId, result.ProcessId);
        }

        [Fact]
        public void CreateSessionInfo_WithMinimalParameters_ReturnsSessionInfo()
        {
            // Arrange
            var sessionId = "minimal-session";
            var dumpPath = @"C:\temp\minimal.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
            Assert.Equal(_mockCdbSession.Object, result.CdbSession);
            Assert.Equal(_mockCommandQueue.Object, result.CommandQueue);
            Assert.Equal(dumpPath, result.DumpPath);
            Assert.Null(result.SymbolsPath);
            Assert.Null(result.ProcessId);
        }

        [Fact]
        public void CreateSessionInfo_WithNullSessionId_ThrowsArgumentNullException()
        {
            // Arrange
            var dumpPath = @"C:\temp\test.dmp";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _factory.CreateSessionInfo(null!, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath));
        }

        [Fact]
        public void CreateSessionInfo_WithEmptySessionId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "";
            var dumpPath = @"C:\temp\test.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
        }

        [Fact]
        public void CreateSessionInfo_WithWhitespaceSessionId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "   ";
            var dumpPath = @"C:\temp\test.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
        }

        [Fact]
        public void CreateSessionInfo_WithNullCdbSession_ThrowsArgumentNullException()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _factory.CreateSessionInfo(sessionId, null!, _mockCommandQueue.Object, dumpPath));
        }

        [Fact]
        public void CreateSessionInfo_WithNullCommandQueue_ThrowsArgumentNullException()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, null!, dumpPath));
        }

        [Fact]
        public void CreateSessionInfo_WithNullDumpPath_ThrowsArgumentNullException()
        {
            // Arrange
            var sessionId = "test-session";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, null!));
        }

        [Fact]
        public void CreateSessionInfo_WithEmptyDumpPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = "";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dumpPath, result.DumpPath);
        }

        [Fact]
        public void CreateSessionInfo_WithWhitespaceDumpPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = "   ";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dumpPath, result.DumpPath);
        }

        [Fact]
        public void CreateSessionInfo_WithUnicodeSessionId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "测试会话-123";
            var dumpPath = @"C:\temp\test.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
        }

        [Fact]
        public void CreateSessionInfo_WithUnicodeDumpPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\测试\转储文件.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dumpPath, result.DumpPath);
        }

        [Fact]
        public void CreateSessionInfo_WithUnicodeSymbolsPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var symbolsPath = @"C:\符号文件\调试符号";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, symbolsPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbolsPath, result.SymbolsPath);
        }

        [Fact]
        public void CreateSessionInfo_WithVeryLongSessionId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = new string('A', 10000);
            var dumpPath = @"C:\temp\test.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
        }

        [Fact]
        public void CreateSessionInfo_WithVeryLongDumpPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\" + new string('A', 10000) + ".dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dumpPath, result.DumpPath);
        }

        [Fact]
        public void CreateSessionInfo_WithVeryLongSymbolsPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var symbolsPath = @"C:\" + new string('B', 10000);

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, symbolsPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbolsPath, result.SymbolsPath);
        }

        [Fact]
        public void CreateSessionInfo_WithSpecialCharactersInSessionId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "session-123!@#$%^&*()_+-=[]{}|;':\",./<>?";
            var dumpPath = @"C:\temp\test.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
        }

        [Fact]
        public void CreateSessionInfo_WithSpecialCharactersInDumpPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test!@#$%^&*()_+-=[]{}|;':\,./<>?.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dumpPath, result.DumpPath);
        }

        [Fact]
        public void CreateSessionInfo_WithSpecialCharactersInSymbolsPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var symbolsPath = @"C:\symbols!@#$%^&*()_+-=[]{}|;':\,./<>?";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, symbolsPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbolsPath, result.SymbolsPath);
        }

        [Fact]
        public void CreateSessionInfo_WithZeroProcessId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var processId = 0;

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, null, processId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processId, result.ProcessId);
        }

        [Fact]
        public void CreateSessionInfo_WithNegativeProcessId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var processId = -1;

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, null, processId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processId, result.ProcessId);
        }

        [Fact]
        public void CreateSessionInfo_WithMaxValueProcessId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var processId = int.MaxValue;

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, null, processId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processId, result.ProcessId);
        }

        [Fact]
        public void CreateSessionInfo_WithMinValueProcessId_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var processId = int.MinValue;

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, null, processId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processId, result.ProcessId);
        }

        [Fact]
        public void CreateSessionInfo_WithEmptySymbolsPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var symbolsPath = "";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, symbolsPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbolsPath, result.SymbolsPath);
        }

        [Fact]
        public void CreateSessionInfo_WithWhitespaceSymbolsPath_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var symbolsPath = "   ";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, symbolsPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(symbolsPath, result.SymbolsPath);
        }

        [Fact]
        public void CreateSessionInfo_MultipleCalls_ReturnDifferentInstances()
        {
            // Arrange
            var sessionId1 = "session-1";
            var sessionId2 = "session-2";
            var dumpPath = @"C:\temp\test.dmp";

            // Act
            var result1 = _factory.CreateSessionInfo(sessionId1, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);
            var result2 = _factory.CreateSessionInfo(sessionId2, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotSame(result1, result2);
            Assert.NotEqual(result1.SessionId, result2.SessionId);
        }

        [Fact]
        public void CreateSessionInfo_SameParameters_ReturnDifferentInstances()
        {
            // Arrange
            var sessionId = "same-session";
            var dumpPath = @"C:\temp\test.dmp";

            // Act
            var result1 = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);
            var result2 = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath);

            // Assert
            Assert.NotSame(result1, result2);
            Assert.Equal(result1.SessionId, result2.SessionId);
            Assert.Equal(result1.DumpPath, result2.DumpPath);
        }

        [Fact]
        public void CreateDefaultSessionInfo_ReturnsSessionInfo()
        {
            // Act
            var result = _factory.CreateDefaultSessionInfo();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<SessionInfo>(result);
        }

        [Fact]
        public void CreateDefaultSessionInfo_MultipleCalls_ReturnDifferentInstances()
        {
            // Act
            var result1 = _factory.CreateDefaultSessionInfo();
            var result2 = _factory.CreateDefaultSessionInfo();

            // Assert
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void CreateDefaultSessionInfo_ReturnsDefaultValues()
        {
            // Act
            var result = _factory.CreateDefaultSessionInfo();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SessionId);
            Assert.Null(result.CdbSession);
            Assert.Null(result.CommandQueue);
            Assert.Equal(string.Empty, result.DumpPath);
            Assert.Null(result.SymbolsPath);
            Assert.Null(result.ProcessId);
        }

        [Fact]
        public void SessionInfoFactory_ClassCharacteristics_AreCorrect()
        {
            // Arrange
            var type = typeof(SessionInfoFactory);

            // Assert
            Assert.True(type.IsClass);
            Assert.False(type.IsSealed);
            Assert.False(type.IsAbstract);
            Assert.False(type.IsInterface);
            Assert.False(type.IsEnum);
            Assert.False(type.IsValueType);
        }

        [Fact]
        public void SessionInfoFactory_CanBeUsedInCollections()
        {
            // Arrange
            var factory1 = new SessionInfoFactory();
            var factory2 = new SessionInfoFactory();

            // Act
            var list = new List<SessionInfoFactory> { factory1, factory2 };

            // Assert
            Assert.Equal(2, list.Count);
            Assert.Contains(factory1, list);
            Assert.Contains(factory2, list);
        }

        [Fact]
        public void SessionInfoFactory_CanBeSerialized()
        {
            // Arrange
            var factory = new SessionInfoFactory();

            // Act & Assert
            var exception = Record.Exception(() =>
            {
                var json = System.Text.Json.JsonSerializer.Serialize(factory);
                var deserialized = System.Text.Json.JsonSerializer.Deserialize<SessionInfoFactory>(json);
                Assert.NotNull(deserialized);
            });

            Assert.Null(exception);
        }

        [Fact]
        public void CreateSessionInfo_WithAllNullOptionalParameters_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";

            // Act
            var result = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
            Assert.Equal(dumpPath, result.DumpPath);
            Assert.Null(result.SymbolsPath);
            Assert.Null(result.ProcessId);
        }

        [Fact]
        public void CreateSessionInfo_WithMixedValidAndNullParameters_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var dumpPath = @"C:\temp\test.dmp";
            var symbolsPath = @"C:\symbols";
            var processId = 1234;

            // Act
            var result1 = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, symbolsPath, null);
            var result2 = _factory.CreateSessionInfo(sessionId, _mockCdbSession.Object, _mockCommandQueue.Object, dumpPath, null, processId);

            // Assert
            Assert.NotNull(result1);
            Assert.Equal(symbolsPath, result1.SymbolsPath);
            Assert.Null(result1.ProcessId);

            Assert.NotNull(result2);
            Assert.Null(result2.SymbolsPath);
            Assert.Equal(processId, result2.ProcessId);
        }
    }
}
