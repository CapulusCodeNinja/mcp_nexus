using System;
using Xunit;
using mcp_nexus.Exceptions;

namespace mcp_nexus_unit_tests.Exceptions
{
    /// <summary>
    /// Tests for McpToolException - simple exception class with error codes
    /// </summary>
    public class McpToolExceptionTests
    {
        [Fact]
        public void McpToolException_WithErrorCodeAndMessage_SetsProperties()
        {
            // Arrange
            const int errorCode = 12345;
            const string message = "Test error message";
            var errorData = new { detail = "test detail" };

            // Act
            var exception = new McpToolException(errorCode, message, errorData);

            // Assert
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorData, exception.ErrorData);
        }

        [Fact]
        public void McpToolException_WithErrorCodeAndMessageOnly_SetsProperties()
        {
            // Arrange
            const int errorCode = 54321;
            const string message = "Simple error message";

            // Act
            var exception = new McpToolException(errorCode, message);

            // Assert
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.ErrorData);
        }

        [Fact]
        public void McpToolException_WithInnerException_SetsProperties()
        {
            // Arrange
            const int errorCode = 99999;
            const string message = "Error with inner exception";
            var innerException = new InvalidOperationException("Inner error");
            var errorData = new { stackTrace = "trace" };

            // Act
            var exception = new McpToolException(errorCode, message, innerException, errorData);

            // Assert
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(errorData, exception.ErrorData);
        }

        [Fact]
        public void McpToolException_WithInnerExceptionOnly_SetsProperties()
        {
            // Arrange
            const int errorCode = 11111;
            const string message = "Error with inner exception only";
            var innerException = new ArgumentException("Inner argument error");

            // Act
            var exception = new McpToolException(errorCode, message, innerException);

            // Assert
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Null(exception.ErrorData);
        }

        [Fact]
        public void McpToolException_InheritsFromException()
        {
            // Arrange & Act
            var exception = new McpToolException(1, "test");

            // Assert
            Assert.IsAssignableFrom<Exception>(exception);
        }

        [Theory]
        [InlineData(0, "Zero error code")]
        [InlineData(-1, "Negative error code")]
        [InlineData(int.MaxValue, "Max error code")]
        [InlineData(int.MinValue, "Min error code")]
        public void McpToolException_WithVariousErrorCodes_SetsCorrectly(int errorCode, string message)
        {
            // Act
            var exception = new McpToolException(errorCode, message);

            // Assert
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void McpToolException_WithNullMessage_HandlesGracefully()
        {
            // Act
            var exception = new McpToolException(1, null!);

            // Assert
            Assert.Equal(1, exception.ErrorCode);
            // Exception base class will set a default message when null is passed
            Assert.NotNull(exception.Message);
        }

        [Fact]
        public void McpToolException_WithEmptyMessage_HandlesGracefully()
        {
            // Act
            var exception = new McpToolException(1, string.Empty);

            // Assert
            Assert.Equal(1, exception.ErrorCode);
            Assert.Equal(string.Empty, exception.Message);
        }
    }
}
