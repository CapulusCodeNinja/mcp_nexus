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

        [Fact]
        public void FormatSseResponseForLogging_WithRegularLine_PassesThrough()
        {
            // Arrange - line that's not "event:" or "data:" but not whitespace
            var sse = "event: test\nsome other content\ndata: {}";

            // Act
            var result = LoggingFormatters.FormatSseResponseForLogging(sse);

            // Assert - should include the regular line
            Assert.Contains("some other content", result);
        }

        [Fact]
        public void FormatJsonForLogging_WithVeryLongInvalidJson_Truncates()
        {
            // Arrange - invalid JSON longer than 1000 characters
            var longInvalidJson = "{invalid" + new string('X', 1500);

            // Act
            var result = LoggingFormatters.FormatJsonForLogging(longInvalidJson);

            // Assert - should truncate to 1000 chars + "..."
            Assert.Contains("Invalid JSON", result);
            Assert.Contains("...", result);
            Assert.DoesNotContain(new string('X', 1500), result); // Should be truncated
        }
    }
}

