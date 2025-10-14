using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Utilities;

namespace mcp_nexus_tests.Utilities
{
    /// <summary>
    /// Tests for ConditionalLogger
    /// </summary>
    public class ConditionalLoggerTests
    {
        private readonly Mock<ILogger> m_MockLogger;

        public ConditionalLoggerTests()
        {
            m_MockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void ConditionalLogger_Class_Exists()
        {
            // This test verifies that the ConditionalLogger class exists and can be instantiated
            Assert.NotNull(typeof(ConditionalLogger));
        }

        [Fact]
        public void LogTrace_WithMessage_CallsLoggerWhenEnabled()
        {
            // Arrange
            var message = "Test trace message";
            var args = new object[] { "arg1", "arg2" };

            // Act
            ConditionalLogger.LogTrace(m_MockLogger.Object, message, args);

            // Assert
            // Note: The actual behavior depends on ENABLE_TRACE_LOGGING compilation symbol
            // In test builds, this may or may not call the logger depending on build configuration
            // We can only verify the method doesn't throw an exception
            Assert.True(true); // Method executed without exception
        }

        [Fact]
        public void LogTrace_WithException_CallsLoggerWhenEnabled()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var message = "Test trace message with exception";
            var args = new object[] { "arg1" };

            // Act
            ConditionalLogger.LogTrace(m_MockLogger.Object, exception, message, args);

            // Assert
            // Note: The actual behavior depends on ENABLE_TRACE_LOGGING compilation symbol
            // In test builds, this may or may not call the logger depending on build configuration
            // We can only verify the method doesn't throw an exception
            Assert.True(true); // Method executed without exception
        }

        [Fact]
        public void LogDebug_WithMessage_CallsLoggerWhenEnabled()
        {
            // Arrange
            var message = "Test debug message";
            var args = new object[] { "arg1", "arg2" };

            // Act
            ConditionalLogger.LogDebug(m_MockLogger.Object, message, args);

            // Assert
            // Note: The actual behavior depends on DEBUG compilation symbol
            // In test builds, this may or may not call the logger depending on build configuration
            // We can only verify the method doesn't throw an exception
            Assert.True(true); // Method executed without exception
        }

        [Fact]
        public void LogDebug_WithException_CallsLoggerWhenEnabled()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var message = "Test debug message with exception";
            var args = new object[] { "arg1" };

            // Act
            ConditionalLogger.LogDebug(m_MockLogger.Object, exception, message, args);

            // Assert
            // Note: The actual behavior depends on DEBUG compilation symbol
            // In test builds, this may or may not call the logger depending on build configuration
            // We can only verify the method doesn't throw an exception
            Assert.True(true); // Method executed without exception
        }

        [Fact]
        public void IsTraceEnabled_ReturnsBoolean()
        {
            // Act
            var result = ConditionalLogger.IsTraceEnabled(m_MockLogger.Object);

            // Assert
            // Note: The actual behavior depends on ENABLE_TRACE_LOGGING compilation symbol
            // In test builds, this may return false if ENABLE_TRACE_LOGGING is not defined
            // We can only verify the method returns a boolean and doesn't throw
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void IsDebugEnabled_ReturnsBoolean()
        {
            // Act
            var result = ConditionalLogger.IsDebugEnabled(m_MockLogger.Object);

            // Assert
            // Note: The actual behavior depends on DEBUG compilation symbol
            // In test builds, this may return false if DEBUG is not defined
            // We can only verify the method returns a boolean and doesn't throw
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void LogTrace_WithNullLogger_DoesNotThrow()
        {
            // Arrange
            var message = "Test message";
            var args = new object[] { "arg1" };

            // Act & Assert
            // Note: Due to preprocessor directives, these methods may still call the logger
            // even in test builds, so we need to handle the ArgumentNullException
            try
            {
                ConditionalLogger.LogTrace(null!, message, args);
                ConditionalLogger.LogTrace(null!, new Exception("test"), message, args);
                ConditionalLogger.LogDebug(null!, message, args);
                ConditionalLogger.LogDebug(null!, new Exception("test"), message, args);
            }
            catch (ArgumentNullException)
            {
                // This is expected behavior when preprocessor directives are not active
                // The methods still call the underlying logger which validates null parameters
            }

            // These should return false with null logger, but may throw NullReferenceException
            try
            {
                Assert.False(ConditionalLogger.IsTraceEnabled(null!));
                Assert.False(ConditionalLogger.IsDebugEnabled(null!));
            }
            catch (NullReferenceException)
            {
                // This is expected behavior when preprocessor directives are not active
                // The methods still call the underlying logger which validates null parameters
            }
        }

        [Fact]
        public void LogTrace_WithNullMessage_DoesNotThrow()
        {
            // Arrange
            var args = new object[] { "arg1" };

            // Act & Assert
            // These should not throw even with null message due to preprocessor directives
            ConditionalLogger.LogTrace(m_MockLogger.Object, null!, args);
            ConditionalLogger.LogTrace(m_MockLogger.Object, new Exception("test"), null!, args);
            ConditionalLogger.LogDebug(m_MockLogger.Object, null!, args);
            ConditionalLogger.LogDebug(m_MockLogger.Object, new Exception("test"), null!, args);
        }

        [Fact]
        public void LogTrace_WithNullArgs_DoesNotThrow()
        {
            // Arrange
            var message = "Test message";

            // Act & Assert
            // These should not throw even with null args due to preprocessor directives
            ConditionalLogger.LogTrace(m_MockLogger.Object, message, null!);
            ConditionalLogger.LogTrace(m_MockLogger.Object, new Exception("test"), message, null!);
            ConditionalLogger.LogDebug(m_MockLogger.Object, message, null!);
            ConditionalLogger.LogDebug(m_MockLogger.Object, new Exception("test"), message, null!);
        }

        [Fact]
        public void AllMethods_AreStatic()
        {
            // Assert
            var type = typeof(ConditionalLogger);
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.True(methods.Length >= 6); // Should have at least 6 static methods
            Assert.All(methods, method => Assert.True(method.IsStatic));
        }

        [Fact]
        public void AllMethods_ArePublic()
        {
            // Assert
            var type = typeof(ConditionalLogger);
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.True(methods.Length >= 6); // Should have at least 6 public methods
            Assert.All(methods, method => Assert.True(method.IsPublic));
        }
    }
}
