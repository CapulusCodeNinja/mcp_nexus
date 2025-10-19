using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for all Startup classes.
    /// Consolidates tests for CommandLineParser, StartupHelper, HelpDisplay, ExceptionLogger,
    /// StartupBanner, ConfigurationLogger, and LoggingFormatters.
    /// </summary>
    public class StartupTests
    {
        #region CommandLineParser Tests

        [Fact]
        public void Parse_WithEmptyArgs_ReturnsDefaultValues()
        {
            var result = CommandLineParser.Parse(Array.Empty<string>());
            Assert.NotNull(result);
            Assert.False(result.UseHttp);
            Assert.False(result.ServiceMode);
            Assert.False(result.Install);
        }

        [Fact]
        public void Parse_WithHttpFlag_SetsUseHttpTrue()
        {
            var result = CommandLineParser.Parse(new[] { "--http" });
            Assert.True(result.UseHttp);
        }

        [Fact]
        public void Parse_WithServiceFlag_SetsServiceModeTrue()
        {
            var result = CommandLineParser.Parse(new[] { "--service" });
            Assert.True(result.ServiceMode);
        }

        [Fact]
        public void Parse_WithPortOption_SetsPort()
        {
            var result = CommandLineParser.Parse(new[] { "--port", "8080" });
            Assert.Equal(8080, result.Port);
            Assert.True(result.PortFromCommandLine);
        }

        [Fact]
        public void IsHelpRequest_WithHelpFlag_ReturnsTrue()
        {
            Assert.True(CommandLineParser.IsHelpRequest(new[] { "--help" }));
            Assert.True(CommandLineParser.IsHelpRequest(new[] { "-h" }));
            Assert.True(CommandLineParser.IsHelpRequest(new[] { "help" }));
        }

        [Fact]
        public void IsHelpRequest_WithEmptyArgs_ReturnsFalse()
        {
            Assert.False(CommandLineParser.IsHelpRequest(Array.Empty<string>()));
        }

        #endregion

        #region StartupHelper Tests

        [Fact]
        public void SetupConsoleEncoding_ExecutesWithoutException()
        {
            // Act & Assert - Should not throw
            StartupHelper.SetupConsoleEncoding();
        }

        [Fact]
        public void ValidateServiceModeOnWindows_WithServiceModeFalse_ReturnsTrue()
        {
            var result = StartupHelper.ValidateServiceModeOnWindows(false);
            Assert.True(result);
        }

        [Fact]
        public void SetEnvironmentForServiceMode_WithServiceFlag_SetsEnvironment()
        {
            var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            try
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
                StartupHelper.SetEnvironmentForServiceMode(new[] { "--service" });
                var newEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                Assert.Equal("Service", newEnv);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            }
        }

        #endregion

        #region HelpDisplay Tests

        [Fact]
        public async Task ShowHelpAsync_ExecutesWithoutException()
        {
            // Act & Assert - Should complete without throwing
            await HelpDisplay.ShowHelpAsync();
        }

        #endregion

        #region ExceptionLogger Tests

        [Fact]
        public void SetupGlobalExceptionHandlers_ExecutesWithoutException()
        {
            // Act & Assert - Should not throw
            ExceptionLogger.SetupGlobalExceptionHandlers();
        }

        [Fact]
        public void LogFatalException_WithNullException_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            ExceptionLogger.LogFatalException(null, "TestSource", false);
        }

        [Fact]
        public void LogFatalException_WithSimpleException_LogsDetails()
        {
            var exception = new InvalidOperationException("Test exception");
            // Act & Assert - Should not throw
            ExceptionLogger.LogFatalException(exception, "TestSource", false);
        }

        [Fact]
        public void LogFatalException_WithAggregateException_LogsAllInnerExceptions()
        {
            var innerExceptions = new Exception[]
            {
                new InvalidOperationException("First exception"),
                new ArgumentException("Second exception")
            };
            var aggregateException = new AggregateException("Multiple exceptions", innerExceptions);

            // Act & Assert - Should not throw
            ExceptionLogger.LogFatalException(aggregateException, "TestSource", false);
        }

        #endregion

        #region StartupBanner Tests

        [Fact]
        public void FormatBannerLine_WithNormalInput_FormatsCorrectly()
        {
            var result = StartupBanner.FormatBannerLine("Label:", "Value", 65);
            Assert.Contains("Label:", result);
            Assert.Contains("Value", result);
            Assert.StartsWith("*", result);
            Assert.EndsWith("*", result);
        }

        [Fact]
        public void FormatBannerLine_WithLongContent_Truncates()
        {
            var longValue = new string('X', 100);
            var result = StartupBanner.FormatBannerLine("Label:", longValue, 65);
            Assert.True(result.Length <= 69); // 65 + 4 for "* " and " *"
        }

        [Fact]
        public void FormatCenteredBannerLine_WithNormalText_CentersCorrectly()
        {
            var result = StartupBanner.FormatCenteredBannerLine("TEST", 65);
            Assert.Contains("TEST", result);
            Assert.StartsWith("*", result);
            Assert.EndsWith("*", result);
        }

        #endregion

        #region ConfigurationLogger Tests

        [Fact]
        public void MaskSecret_WithNull_ReturnsNotSet()
        {
            Assert.Equal("Not set", ConfigurationLogger.MaskSecret(null));
        }

        [Fact]
        public void MaskSecret_WithEmptyString_ReturnsNotSet()
        {
            Assert.Equal("Not set", ConfigurationLogger.MaskSecret(""));
        }

        [Fact]
        public void MaskSecret_WithShortSecret_ReturnsOriginal()
        {
            Assert.Equal("abc", ConfigurationLogger.MaskSecret("abc"));
        }

        [Fact]
        public void MaskSecret_WithLongSecret_MasksCorrectly()
        {
            var result = ConfigurationLogger.MaskSecret("secret123456");
            Assert.StartsWith("secre", result);
            Assert.Contains("*", result);
        }

        [Fact]
        public void GetCdbPathInfo_ReturnsResult()
        {
            var result = ConfigurationLogger.GetCdbPathInfo();
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion

        #region LoggingFormatters Tests

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

        #endregion
    }
}

