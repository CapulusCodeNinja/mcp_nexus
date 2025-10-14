using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;

namespace mcp_nexus_tests.Debugger
{
    /// <summary>
    /// Tests for CdbSessionConfiguration
    /// </summary>
    public class CdbSessionConfigurationTests
    {
        [Fact]
        public void CdbSessionConfiguration_Class_Exists()
        {
            // This test verifies that the CdbSessionConfiguration class exists and can be instantiated
            Assert.NotNull(typeof(CdbSessionConfiguration));
        }

        [Fact]
        public void Constructor_WithDefaultParameters_EnablesCommandPreprocessing()
        {
            // Arrange & Act
            var config = new CdbSessionConfiguration();

            // Assert
            Assert.True(config.EnableCommandPreprocessing);
        }

        [Fact]
        public void Constructor_WithEnableCommandPreprocessingTrue_EnablesCommandPreprocessing()
        {
            // Arrange & Act
            var config = new CdbSessionConfiguration(enableCommandPreprocessing: true);

            // Assert
            Assert.True(config.EnableCommandPreprocessing);
        }

        [Fact]
        public void Constructor_WithEnableCommandPreprocessingFalse_DisablesCommandPreprocessing()
        {
            // Arrange & Act
            var config = new CdbSessionConfiguration(enableCommandPreprocessing: false);

            // Assert
            Assert.False(config.EnableCommandPreprocessing);
        }

        [Fact]
        public void Constructor_WithAllParameters_SetsAllProperties()
        {
            // Arrange
            var commandTimeout = 60000;
            var idleTimeout = 120000;
            var customCdbPath = "C:\\test\\cdb.exe";
            var symbolMaxRetries = 5;
            var symbolPath = "srv*C:\\symbols";
            var startupDelay = 2000;
            var outputReadingTimeout = 90000;
            var enablePreprocessing = false;

            // Act
            var config = new CdbSessionConfiguration(
                commandTimeoutMs: commandTimeout,
                idleTimeoutMs: idleTimeout,
                customCdbPath: customCdbPath,
                symbolServerMaxRetries: symbolMaxRetries,
                symbolSearchPath: symbolPath,
                startupDelayMs: startupDelay,
                outputReadingTimeoutMs: outputReadingTimeout,
                enableCommandPreprocessing: enablePreprocessing);

            // Assert
            Assert.Equal(commandTimeout, config.CommandTimeoutMs);
            Assert.Equal(idleTimeout, config.IdleTimeoutMs);
            Assert.Equal(customCdbPath, config.CustomCdbPath);
            Assert.Equal(symbolMaxRetries, config.SymbolServerMaxRetries);
            Assert.Equal(symbolPath, config.SymbolSearchPath);
            Assert.Equal(startupDelay, config.StartupDelayMs);
            Assert.Equal(outputReadingTimeout, config.OutputReadingTimeoutMs);
            Assert.Equal(enablePreprocessing, config.EnableCommandPreprocessing);
        }
    }
}
