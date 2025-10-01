using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using mcp_nexus.Configuration;
using System;
using System.IO;
using System.Text;
using NLog;
using NLog.Config;

namespace mcp_nexus_tests.Configuration
{
    /// <summary>
    /// Tests for LoggingSetup
    /// </summary>
    public class LoggingSetupTests
    {
        [Fact]
        public void LoggingSetup_Class_Exists()
        {
            // This test verifies that the LoggingSetup class exists and can be instantiated
            Assert.True(typeof(LoggingSetup) != null);
        }

        [Fact]
        public void LoggingSetup_IsStaticClass()
        {
            // Verify that LoggingSetup is a static class
            var type = typeof(LoggingSetup);
            Assert.True(type.IsAbstract && type.IsSealed);
        }

        [Fact]
        public void ConfigureLogging_WithServiceMode_LogsToConsole()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:LogLevel"] = "Information"
                })
                .Build();
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act - Note: We can't easily test the full ConfigureLogging method due to ILoggingBuilder complexity
                // Instead, we'll test the console output behavior by calling the private methods via reflection
                var logStartMethod = typeof(LoggingSetup).GetMethod("LogConfigurationStart",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var logCompleteMethod = typeof(LoggingSetup).GetMethod("LogConfigurationComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                logStartMethod!.Invoke(null, new object[] { true });
                logCompleteMethod!.Invoke(null, new object[] { true, Microsoft.Extensions.Logging.LogLevel.Information });

                // Assert
                var output = consoleOutput.ToString();
                Assert.Contains("Configuring logging...", output);
                Assert.Contains("Logging configured with NLog (Level: Information)", output);
            }
            finally
            {
                // Restore console output
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Fact]
        public void ConfigureLogging_WithNonServiceMode_LogsToConsoleError()
        {
            // Arrange
            var consoleError = new StringWriter();
            Console.SetError(consoleError);

            try
            {
                // Act - Test the console error output behavior
                var logStartMethod = typeof(LoggingSetup).GetMethod("LogConfigurationStart",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var logCompleteMethod = typeof(LoggingSetup).GetMethod("LogConfigurationComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                logStartMethod!.Invoke(null, new object[] { false });
                logCompleteMethod!.Invoke(null, new object[] { false, Microsoft.Extensions.Logging.LogLevel.Information });

                // Assert
                var output = consoleError.ToString();
                Assert.Contains("Configuring logging...", output);
                Assert.Contains("Logging configured with NLog (Level: Information)", output);
            }
            finally
            {
                // Restore console error
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
        }

        [Theory]
        [InlineData("Trace", Microsoft.Extensions.Logging.LogLevel.Trace)]
        [InlineData("Debug", Microsoft.Extensions.Logging.LogLevel.Debug)]
        [InlineData("Information", Microsoft.Extensions.Logging.LogLevel.Information)]
        [InlineData("Info", Microsoft.Extensions.Logging.LogLevel.Information)]
        [InlineData("Warning", Microsoft.Extensions.Logging.LogLevel.Warning)]
        [InlineData("Warn", Microsoft.Extensions.Logging.LogLevel.Warning)]
        [InlineData("Error", Microsoft.Extensions.Logging.LogLevel.Error)]
        [InlineData("Critical", Microsoft.Extensions.Logging.LogLevel.Critical)]
        [InlineData("None", Microsoft.Extensions.Logging.LogLevel.None)]
        [InlineData("Invalid", Microsoft.Extensions.Logging.LogLevel.Information)] // Default case
        public void ParseLogLevel_WithDifferentInputs_ReturnsCorrectLogLevel(string logLevelString, Microsoft.Extensions.Logging.LogLevel expectedLogLevel)
        {
            // Arrange
            var parseMethod = typeof(LoggingSetup).GetMethod("ParseLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { logLevelString });

            // Assert
            Assert.Equal(expectedLogLevel, result);
        }

        [Fact]
        public void GetLogLevelFromConfiguration_WithValidConfiguration_ReturnsCorrectLevel()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:LogLevel"] = "Debug"
                })
                .Build();

            var getLogLevelMethod = typeof(LoggingSetup).GetMethod("GetLogLevelFromConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getLogLevelMethod!.Invoke(null, new object[] { configuration });

            // Assert
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Debug, result);
        }

        [Fact]
        public void GetLogLevelFromConfiguration_WithMissingConfiguration_ReturnsInformation()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();

            var getLogLevelMethod = typeof(LoggingSetup).GetMethod("GetLogLevelFromConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getLogLevelMethod!.Invoke(null, new object[] { configuration });

            // Assert
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, result);
        }

        [Fact]
        public void GetNLogLevel_WithTraceLevel_ReturnsTrace()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Trace;
            var expectedNLogLevel = NLog.LogLevel.Trace;

            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, new object[] { microsoftLevel });

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithInformationLevel_ReturnsInfo()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Information;
            var expectedNLogLevel = NLog.LogLevel.Info;

            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, new object[] { microsoftLevel });

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithCriticalLevel_ReturnsFatal()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Critical;
            var expectedNLogLevel = NLog.LogLevel.Fatal;

            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, new object[] { microsoftLevel });

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void ConfigureNLogDynamically_WithValidConfiguration_DoesNotThrow()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:LogLevel"] = "Debug"
                })
                .Build();

            // Set up a basic NLog configuration without targets (just rules)
            var nlogConfig = new LoggingConfiguration();
            LogManager.Configuration = nlogConfig;

            try
            {
                var configureNLogMethod = typeof(LoggingSetup).GetMethod("ConfigureNLogDynamically",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                // Act & Assert
                var exception = Record.Exception(() =>
                    configureNLogMethod!.Invoke(null, new object[] { configuration, Microsoft.Extensions.Logging.LogLevel.Debug }));
                Assert.Null(exception);
            }
            finally
            {
                // Clean up
                LogManager.Configuration = null;
            }
        }

        [Fact]
        public void ConfigureNLogDynamically_WithNullConfiguration_DoesNotThrow()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            LogManager.Configuration = null;

            try
            {
                var configureNLogMethod = typeof(LoggingSetup).GetMethod("ConfigureNLogDynamically",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                // Act & Assert
                var exception = Record.Exception(() =>
                    configureNLogMethod!.Invoke(null, new object[] { configuration, Microsoft.Extensions.Logging.LogLevel.Information }));
                Assert.Null(exception);
            }
            finally
            {
                // Clean up
                LogManager.Configuration = null;
            }
        }
    }
}
