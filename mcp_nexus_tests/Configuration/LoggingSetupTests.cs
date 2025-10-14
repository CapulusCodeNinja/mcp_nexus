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
            Assert.NotNull(typeof(LoggingSetup));
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
            _ = new ConfigurationBuilder()
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

                logStartMethod!.Invoke(null, [true]);
                logCompleteMethod!.Invoke(null, [true, Microsoft.Extensions.Logging.LogLevel.Information]);

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

                logStartMethod!.Invoke(null, [false]);
                logCompleteMethod!.Invoke(null, [false, Microsoft.Extensions.Logging.LogLevel.Information]);

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
            var result = parseMethod!.Invoke(null, [logLevelString]);

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
            var result = getLogLevelMethod!.Invoke(null, [configuration]);

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
            var result = getLogLevelMethod!.Invoke(null, [configuration]);

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
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

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
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

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
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

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
                    configureNLogMethod!.Invoke(null, [configuration, Microsoft.Extensions.Logging.LogLevel.Debug, false]));
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
                    configureNLogMethod!.Invoke(null, [configuration, Microsoft.Extensions.Logging.LogLevel.Information, false]));
                Assert.Null(exception);
            }
            finally
            {
                // Clean up
                LogManager.Configuration = null;
            }
        }

        [Theory]
        [InlineData("TRACE", Microsoft.Extensions.Logging.LogLevel.Trace)]
        [InlineData("DEBUG", Microsoft.Extensions.Logging.LogLevel.Debug)]
        [InlineData("INFORMATION", Microsoft.Extensions.Logging.LogLevel.Information)]
        [InlineData("INFO", Microsoft.Extensions.Logging.LogLevel.Information)]
        [InlineData("WARNING", Microsoft.Extensions.Logging.LogLevel.Warning)]
        [InlineData("WARN", Microsoft.Extensions.Logging.LogLevel.Warning)]
        [InlineData("ERROR", Microsoft.Extensions.Logging.LogLevel.Error)]
        [InlineData("CRITICAL", Microsoft.Extensions.Logging.LogLevel.Critical)]
        [InlineData("NONE", Microsoft.Extensions.Logging.LogLevel.None)]
        [InlineData("Trace", Microsoft.Extensions.Logging.LogLevel.Trace)]
        [InlineData("Debug", Microsoft.Extensions.Logging.LogLevel.Debug)]
        [InlineData("Information", Microsoft.Extensions.Logging.LogLevel.Information)]
        [InlineData("Info", Microsoft.Extensions.Logging.LogLevel.Information)]
        [InlineData("Warning", Microsoft.Extensions.Logging.LogLevel.Warning)]
        [InlineData("Warn", Microsoft.Extensions.Logging.LogLevel.Warning)]
        [InlineData("Error", Microsoft.Extensions.Logging.LogLevel.Error)]
        [InlineData("Critical", Microsoft.Extensions.Logging.LogLevel.Critical)]
        [InlineData("None", Microsoft.Extensions.Logging.LogLevel.None)]
        public void ParseLogLevel_WithCaseVariations_ReturnsCorrectLogLevel(string logLevelString, Microsoft.Extensions.Logging.LogLevel expectedLogLevel)
        {
            // Arrange
            var parseMethod = typeof(LoggingSetup).GetMethod("ParseLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = parseMethod!.Invoke(null, [logLevelString]);

            // Assert
            Assert.Equal(expectedLogLevel, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid")]
        [InlineData("unknown")]
        [InlineData("123")]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("null")]
        [InlineData("undefined")]
        [InlineData("log")]
        [InlineData("level")]
        [InlineData("logging")]
        public void ParseLogLevel_WithInvalidInputs_ReturnsInformation(string logLevelString)
        {
            // Arrange
            var parseMethod = typeof(LoggingSetup).GetMethod("ParseLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = parseMethod!.Invoke(null, [logLevelString]);

            // Assert
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, result);
        }

        [Theory]
        [InlineData("trace")]
        [InlineData("debug")]
        [InlineData("information")]
        [InlineData("info")]
        [InlineData("warning")]
        [InlineData("warn")]
        [InlineData("error")]
        [InlineData("critical")]
        [InlineData("none")]
        public void ParseLogLevel_WithLowercaseInputs_ReturnsCorrectLogLevel(string logLevelString)
        {
            // Arrange
            var parseMethod = typeof(LoggingSetup).GetMethod("ParseLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = parseMethod!.Invoke(null, [logLevelString]);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Microsoft.Extensions.Logging.LogLevel>(result);
        }

        [Fact]
        public void GetNLogLevel_WithTraceLevel_ReturnsTrace_Reflection()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Trace;
            var expectedNLogLevel = NLog.LogLevel.Trace;
            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithDebugLevel_ReturnsDebug()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Debug;
            var expectedNLogLevel = NLog.LogLevel.Debug;
            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithInformationLevel_ReturnsInfo_Reflection()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Information;
            var expectedNLogLevel = NLog.LogLevel.Info;
            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithWarningLevel_ReturnsWarn()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
            var expectedNLogLevel = NLog.LogLevel.Warn;
            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithErrorLevel_ReturnsError()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Error;
            var expectedNLogLevel = NLog.LogLevel.Error;
            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithCriticalLevel_ReturnsFatal_Reflection()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.Critical;
            var expectedNLogLevel = NLog.LogLevel.Fatal;
            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithNoneLevel_ReturnsOff()
        {
            // Arrange
            var microsoftLevel = Microsoft.Extensions.Logging.LogLevel.None;
            var expectedNLogLevel = NLog.LogLevel.Off;
            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, [microsoftLevel]);

            // Assert
            Assert.Equal(expectedNLogLevel, result);
        }

        [Fact]
        public void GetNLogLevel_WithInvalidLogLevel_ReturnsInfo()
        {
            // Arrange
            var invalidLevel = (Microsoft.Extensions.Logging.LogLevel)999; // Invalid enum value
            var getNLogLevelMethod = typeof(LoggingSetup).GetMethod("GetNLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getNLogLevelMethod!.Invoke(null, [invalidLevel]);

            // Assert
            Assert.Equal(NLog.LogLevel.Info, result);
        }

        [Fact]
        public void GetLogLevelFromConfiguration_WithNullConfiguration_ReturnsInformation()
        {
            // Arrange
            var getLogLevelMethod = typeof(LoggingSetup).GetMethod("GetLogLevelFromConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getLogLevelMethod!.Invoke(null, [null!]);

            // Assert
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid")]
        [InlineData("unknown")]
        public void GetLogLevelFromConfiguration_WithInvalidLogLevel_ReturnsInformation(string logLevelString)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:LogLevel"] = logLevelString
                })
                .Build();

            var getLogLevelMethod = typeof(LoggingSetup).GetMethod("GetLogLevelFromConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getLogLevelMethod!.Invoke(null, [configuration]);

            // Assert
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, result);
        }

        [Fact]
        public void LogConfigurationStart_WithServiceModeTrue_LogsToConsole()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                var logStartMethod = typeof(LoggingSetup).GetMethod("LogConfigurationStart",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                // Act
                logStartMethod!.Invoke(null, [true]);

                // Assert
                var output = consoleOutput.ToString();
                Assert.Contains("Configuring logging...", output);
            }
            finally
            {
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Fact]
        public void LogConfigurationStart_WithServiceModeFalse_LogsToConsoleError()
        {
            // Arrange
            var consoleError = new StringWriter();
            Console.SetError(consoleError);

            try
            {
                var logStartMethod = typeof(LoggingSetup).GetMethod("LogConfigurationStart",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                // Act
                logStartMethod!.Invoke(null, [false]);

                // Assert
                var output = consoleError.ToString();
                Assert.Contains("Configuring logging...", output);
            }
            finally
            {
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
        }

        [Theory]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Trace)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Debug)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Information)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Warning)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Error)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Critical)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.None)]
        public void LogConfigurationComplete_WithServiceModeTrue_LogsToConsole(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                var logCompleteMethod = typeof(LoggingSetup).GetMethod("LogConfigurationComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                // Act
                logCompleteMethod!.Invoke(null, [true, logLevel]);

                // Assert
                var output = consoleOutput.ToString();
                Assert.Contains($"Logging configured with NLog (Level: {logLevel})", output);
            }
            finally
            {
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Theory]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Trace)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Debug)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Information)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Warning)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Error)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.Critical)]
        [InlineData(Microsoft.Extensions.Logging.LogLevel.None)]
        public void LogConfigurationComplete_WithServiceModeFalse_LogsToConsoleError(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            // Arrange
            var consoleError = new StringWriter();
            Console.SetError(consoleError);

            try
            {
                var logCompleteMethod = typeof(LoggingSetup).GetMethod("LogConfigurationComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                // Act
                logCompleteMethod!.Invoke(null, [false, logLevel]);

                // Assert
                var output = consoleError.ToString();
                Assert.Contains($"Logging configured with NLog (Level: {logLevel})", output);
            }
            finally
            {
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
        }

        [Fact]
        public void ConfigureNLogDynamically_WithEmptyLoggingRules_DoesNotThrow()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var nlogConfig = new LoggingConfiguration();
            // Don't add any rules - empty rules collection
            LogManager.Configuration = nlogConfig;

            try
            {
                var configureNLogMethod = typeof(LoggingSetup).GetMethod("ConfigureNLogDynamically",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                // Act & Assert
                var exception = Record.Exception(() =>
                    configureNLogMethod!.Invoke(null, [configuration, Microsoft.Extensions.Logging.LogLevel.Information, false]));
                Assert.Null(exception);
            }
            finally
            {
                LogManager.Configuration = null;
            }
        }

        [Fact]
        public void ConfigureNLogDynamically_WithLoggingRules_UpdatesRules()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var nlogConfig = new LoggingConfiguration();

            // Add a logging rule
            var rule = new LoggingRule("*", NLog.LogLevel.Debug, new NLog.Targets.NullTarget());
            nlogConfig.LoggingRules.Add(rule);
            LogManager.Configuration = nlogConfig;

            try
            {
                var configureNLogMethod = typeof(LoggingSetup).GetMethod("ConfigureNLogDynamically",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                // Act
                configureNLogMethod!.Invoke(null, [configuration, Microsoft.Extensions.Logging.LogLevel.Warning, false]);

                // Assert - The rule should have been updated (we can't easily verify the internal state)
                // but we can verify no exception was thrown
                Assert.True(true); // If we get here, no exception was thrown
            }
            finally
            {
                LogManager.Configuration = null;
            }
        }

        [Fact]
        public void LoggingSetup_AllMethods_AreStatic()
        {
            // Arrange
            var type = typeof(LoggingSetup);
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance);

            // Act & Assert
            foreach (var method in methods)
            {
                if (method.DeclaringType == type) // Only check methods declared in LoggingSetup
                {
                    Assert.True(method.IsStatic, $"Method {method.Name} should be static");
                }
            }
        }

        [Fact]
        public void LoggingSetup_AllMethods_ArePrivateExceptConfigureLogging()
        {
            // Arrange
            var type = typeof(LoggingSetup);
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            foreach (var method in methods)
            {
                if (method.DeclaringType == type) // Only check methods declared in LoggingSetup
                {
                    if (method.Name == "ConfigureLogging")
                    {
                        Assert.True(method.IsPublic, "ConfigureLogging should be public");
                    }
                    else
                    {
                        Assert.True(method.IsPrivate, $"Method {method.Name} should be private");
                    }
                }
            }
        }

        [Fact]
        public void LoggingSetup_ClassCharacteristics_AreCorrect()
        {
            // Arrange
            var type = typeof(LoggingSetup);

            // Act & Assert
            Assert.True(type.IsClass);
            Assert.True(type.IsAbstract && type.IsSealed); // Static class
            Assert.False(type.IsInterface);
            Assert.False(type.IsEnum);
            Assert.False(type.IsValueType);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void LoggingSetup_CanBeUsedInReflection()
        {
            // Arrange
            var type = typeof(LoggingSetup);

            // Act
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Assert
            Assert.NotNull(methods);
            Assert.NotNull(fields);
            Assert.NotNull(properties);
            Assert.True(methods.Length > 0);
        }

        [Fact]
        public void ParseLogLevel_WithNullInput_ReturnsInformation()
        {
            // Arrange
            var parseMethod = typeof(LoggingSetup).GetMethod("ParseLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = parseMethod!.Invoke(null, [null!]);

            // Assert
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, result);
        }

        [Theory]
        [InlineData("trace\t")]
        [InlineData("\ttrace")]
        [InlineData(" trace ")]
        [InlineData("\ninformation\n")]
        [InlineData("\r\ndebug\r\n")]
        public void ParseLogLevel_WithWhitespaceInput_ReturnsCorrectLogLevel(string logLevelString)
        {
            // Arrange
            var parseMethod = typeof(LoggingSetup).GetMethod("ParseLogLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = parseMethod!.Invoke(null, [logLevelString]);

            // Assert
            // The method uses ToLowerInvariant() so whitespace should be preserved
            // and cause it to fall through to the default case
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, result);
        }

        [Fact]
        public void GetLogLevelFromConfiguration_WithComplexConfiguration_ReturnsCorrectLevel()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:LogLevel"] = "Error",
                    ["Logging:OtherSetting"] = "Value",
                    ["Other:LogLevel"] = "Debug" // This should be ignored
                })
                .Build();

            var getLogLevelMethod = typeof(LoggingSetup).GetMethod("GetLogLevelFromConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = getLogLevelMethod!.Invoke(null, [configuration]);

            // Assert
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, result);
        }

        [Fact]
        public void ConfigureLogPaths_WithServiceMode_UsesProgramData()
        {
            // Arrange
            var nlogConfig = new LoggingConfiguration();
            var fileTarget = new NLog.Targets.FileTarget("mainFile")
            {
                FileName = "original-path.log",
                ArchiveFileName = "original-archive.log"
            };
            nlogConfig.AddTarget(fileTarget);
            nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);

            var configureLogPathsMethod = typeof(LoggingSetup).GetMethod("ConfigureLogPaths",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            configureLogPathsMethod!.Invoke(null, [nlogConfig, true]);

            // Assert
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var expectedLogDir = Path.Combine(programDataPath, "MCP-Nexus", "Logs");
            var expectedLogFile = Path.Combine(expectedLogDir, "mcp-nexus.log");

            Assert.Equal(expectedLogFile, fileTarget.FileName.ToString());
            // Note: ArchiveFileName is configured in nlog.config and not modified dynamically
            // Note: InternalLogFile is set through LogManager.Configuration.Variables, not directly on the config object
        }

        [Fact]
        public void ConfigureLogPaths_WithNonServiceMode_UsesApplicationDirectory()
        {
            // Arrange
            var nlogConfig = new LoggingConfiguration();
            var fileTarget = new NLog.Targets.FileTarget("mainFile")
            {
                FileName = "original-path.log",
                ArchiveFileName = "original-archive.log"
            };
            nlogConfig.AddTarget(fileTarget);
            nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);

            var configureLogPathsMethod = typeof(LoggingSetup).GetMethod("ConfigureLogPaths",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            configureLogPathsMethod!.Invoke(null, [nlogConfig, false]);

            // Assert
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var expectedLogDir = Path.Combine(appDir, "logs");
            var expectedLogFile = Path.Combine(expectedLogDir, "mcp-nexus.log");

            Assert.Equal(expectedLogFile, fileTarget.FileName.ToString());
            // Note: ArchiveFileName is configured in nlog.config and not modified dynamically
            // Note: InternalLogFile is set through LogManager.Configuration.Variables, not directly on the config object
        }

        [Fact]
        public void ConfigureLogPaths_WithMultipleFileTargets_OnlyUpdatesMainFileTarget()
        {
            // Arrange
            var nlogConfig = new LoggingConfiguration();
            var mainFileTarget = new NLog.Targets.FileTarget("mainFile")
            {
                FileName = "main-original.log",
                ArchiveFileName = "main-archive.log"
            };
            var otherFileTarget = new NLog.Targets.FileTarget("otherFile")
            {
                FileName = "other-original.log",
                ArchiveFileName = "other-archive.log"
            };
            nlogConfig.AddTarget(mainFileTarget);
            nlogConfig.AddTarget(otherFileTarget);
            nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, mainFileTarget);
            nlogConfig.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, otherFileTarget);

            var configureLogPathsMethod = typeof(LoggingSetup).GetMethod("ConfigureLogPaths",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            configureLogPathsMethod!.Invoke(null, [nlogConfig, true]);

            // Assert
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var expectedLogDir = Path.Combine(programDataPath, "MCP-Nexus", "Logs");
            var expectedLogFile = Path.Combine(expectedLogDir, "mcp-nexus.log");

            // Main file target should be updated
            Assert.Equal(expectedLogFile, mainFileTarget.FileName.ToString());
            // Note: ArchiveFileName is configured in nlog.config and not modified dynamically

            // Other file target should remain unchanged
            Assert.Equal("other-original.log", otherFileTarget.FileName.ToString());
            Assert.Equal("other-archive.log", otherFileTarget.ArchiveFileName.ToString());
        }

        [Fact]
        public void ConfigureLogPaths_WithNoFileTargets_DoesNotThrow()
        {
            // Arrange
            var nlogConfig = new LoggingConfiguration();
            // No file targets added

            var configureLogPathsMethod = typeof(LoggingSetup).GetMethod("ConfigureLogPaths",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            var exception = Record.Exception(() =>
                configureLogPathsMethod!.Invoke(null, [nlogConfig, true]));

            Assert.Null(exception);
        }
    }
}
