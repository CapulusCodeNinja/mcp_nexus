using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for LoggingFormatters class.
    /// </summary>
    public class LoggingFormattersTests
    {
        [Fact]
        public void ShouldEnableJsonRpcLogging_WithDebugEnabled_ReturnsTrue()
        {
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

            var result = LoggingFormatters.ShouldEnableJsonRpcLogging(mockLoggerFactory.Object);
            Assert.True(result);
        }

        [Fact]
        public void ShouldEnableJsonRpcLogging_WithDebugDisabled_ReturnsFalse()
        {
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(false);
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

            var result = LoggingFormatters.ShouldEnableJsonRpcLogging(mockLoggerFactory.Object);
            Assert.False(result);
        }

        [Fact]
        public void FormatJsonForLogging_WithValidJson_FormatsCorrectly()
        {
            var json = "{\"key\":\"value\"}";
            var result = LoggingFormatters.FormatJsonForLogging(json);
            Assert.Contains("key", result);
            Assert.Contains("value", result);
        }

        [Fact]
        public void FormatJsonForLogging_WithInvalidJson_ReturnsErrorMessage()
        {
            var invalidJson = "{invalid";
            var result = LoggingFormatters.FormatJsonForLogging(invalidJson);
            Assert.Contains("Invalid JSON", result);
        }

        [Fact]
        public void FormatSseResponseForLogging_WithEventAndData_FormatsCorrectly()
        {
            var sse = "event: test\ndata: {\"key\":\"value\"}";
            var result = LoggingFormatters.FormatSseResponseForLogging(sse);
            Assert.Contains("event:", result);
            Assert.Contains("data:", result);
        }

        [Fact]
        public void FormatSseResponseForLogging_WithEmptyInput_ReturnsEmpty()
        {
            var result = LoggingFormatters.FormatSseResponseForLogging("");
            Assert.Equal("", result);
        }
    }
}

