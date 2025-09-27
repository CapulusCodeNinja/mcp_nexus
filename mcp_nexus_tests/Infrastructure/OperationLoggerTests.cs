using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;

namespace mcp_nexus_tests.Services
{
    public class OperationLoggerTests
    {
        private readonly Mock<ILogger> m_mockLogger;

        public OperationLoggerTests()
        {
            m_mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void LogInfo_WithValidParameters_LogsInformation()
        {
            // Arrange
            var operation = "TestOperation";
            var message = "Test message";

            // Act
            OperationLogger.LogInfo(m_mockLogger.Object, operation, message);

            // Assert
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"[{operation}]") && v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogInfo_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => OperationLogger.LogInfo(null, "Test", "Message"));
            Assert.Null(exception);
        }

        [Fact]
        public void LogWarning_WithValidParameters_LogsWarning()
        {
            // Arrange
            var operation = "TestOperation";
            var message = "Warning message";

            // Act
            OperationLogger.LogWarning(m_mockLogger.Object, operation, message);

            // Assert
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"[{operation}]") && v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogError_WithMessage_LogsError()
        {
            // Arrange
            var operation = "TestOperation";
            var message = "Error message";

            // Act
            OperationLogger.LogError(m_mockLogger.Object, operation, message);

            // Assert
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"[{operation}]") && v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogError_WithException_LogsErrorWithException()
        {
            // Arrange
            var operation = "TestOperation";
            var exception = new InvalidOperationException("Test exception");
            var message = "Error occurred";

            // Act
            OperationLogger.LogError(m_mockLogger.Object, operation, exception, message);

            // Assert
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"[{operation}]") && v.ToString()!.Contains(message)),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogDebug_WithValidParameters_LogsDebug()
        {
            // Arrange
            var operation = "TestOperation";
            var message = "Debug message";

            // Act
            OperationLogger.LogDebug(m_mockLogger.Object, operation, message);

            // Assert
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"[{operation}]") && v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogTrace_WithValidParameters_LogsTrace()
        {
            // Arrange
            var operation = "TestOperation";
            var message = "Trace message";

            // Act
            OperationLogger.LogTrace(m_mockLogger.Object, operation, message);

            // Assert
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"[{operation}]") && v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Operations_Constants_AreCorrectValues()
        {
            // Assert
            Assert.Equal("Install", OperationLogger.Operations.Install);
            Assert.Equal("Uninstall", OperationLogger.Operations.Uninstall);
            Assert.Equal("ForceUninstall", OperationLogger.Operations.ForceUninstall);
            Assert.Equal("Update", OperationLogger.Operations.Update);
            Assert.Equal("Build", OperationLogger.Operations.Build);
            Assert.Equal("Copy", OperationLogger.Operations.Copy);
            Assert.Equal("Registry", OperationLogger.Operations.Registry);
            Assert.Equal("Service", OperationLogger.Operations.Service);
            Assert.Equal("Cleanup", OperationLogger.Operations.Cleanup);
            Assert.Equal("HTTP", OperationLogger.Operations.Http);
            Assert.Equal("Stdio", OperationLogger.Operations.Stdio);
            Assert.Equal("MCP", OperationLogger.Operations.Mcp);
            Assert.Equal("Tool", OperationLogger.Operations.Tool);
            Assert.Equal("Protocol", OperationLogger.Operations.Protocol);
            Assert.Equal("Debug", OperationLogger.Operations.Debug);
            Assert.Equal("Startup", OperationLogger.Operations.Startup);
            Assert.Equal("Shutdown", OperationLogger.Operations.Shutdown);
        }

        [Fact]
        public void LogInfo_WithParameterizedMessage_FormatsCorrectly()
        {
            // Arrange
            var operation = "TestOperation";
            var messageTemplate = "Processing {count} items";
            var count = 42;

            // Act
            OperationLogger.LogInfo(m_mockLogger.Object, operation, messageTemplate, count);

            // Assert
            // The logged message will have the parameter substituted, so check for the formatted result
            var expectedFormattedMessage = "Processing 42 items";
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"[{operation}]") && v.ToString()!.Contains(expectedFormattedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}

