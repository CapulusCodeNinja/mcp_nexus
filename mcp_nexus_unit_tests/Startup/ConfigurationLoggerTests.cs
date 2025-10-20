using Microsoft.Extensions.Configuration;
using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for ConfigurationLogger class.
    /// </summary>
    public class ConfigurationLoggerTests
    {
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

        [Fact]
        public void GetProviderInfo_WithNullProvider_ReturnsUnknown()
        {
            var result = ConfigurationLogger.GetProviderInfo(null!);
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void LogConfigurationSettings_WithValidConfig_DoesNotThrow()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["McpNexus:Server:Host"] = "localhost",
                    ["McpNexus:Server:Port"] = "5000"
                })
                .Build();

            var args = new CommandLineArguments
            {
                CustomCdbPath = "C:\\CDB\\cdb.exe",
                UseHttp = true,
                ServiceMode = false,
                Host = "localhost",
                Port = 5000
            };

            // Act & Assert - Should not throw
            ConfigurationLogger.LogConfigurationSettings(configuration, args);
        }

        [Fact]
        public void LogConfigurationSettings_WithMinimalConfig_DoesNotThrow()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var args = new CommandLineArguments();

            // Act & Assert - Should not throw
            ConfigurationLogger.LogConfigurationSettings(configuration, args);
        }

        [Fact]
        public void LogConfigurationSettings_WithExtensiveConfig_DoesNotThrow()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["McpNexus:Server:Host"] = "0.0.0.0",
                    ["McpNexus:Server:Port"] = "8080",
                    ["McpNexus:Transport:Mode"] = "HTTP",
                    ["McpNexus:Debugging:CdbPath"] = "C:\\Debuggers\\cdb.exe",
                    ["McpNexus:Debugging:CommandTimeoutMs"] = "600000",
                    ["McpNexus:Debugging:SymbolServerTimeoutMs"] = "300000"
                })
                .Build();

            var args = new CommandLineArguments
            {
                CustomCdbPath = "C:\\Custom\\cdb.exe",
                UseHttp = true,
                ServiceMode = true,
                Host = "0.0.0.0",
                Port = 8080,
                HostFromCommandLine = true,
                PortFromCommandLine = true
            };

            // Act & Assert - Should not throw
            ConfigurationLogger.LogConfigurationSettings(configuration, args);
        }
    }
}

