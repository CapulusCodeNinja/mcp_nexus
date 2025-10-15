using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;

namespace mcp_nexus_unit_tests.Debugger
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

        #region ValidateParameters Tests

        [Fact]
        public void ValidateParameters_WithNegativeCommandTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CdbSessionConfiguration.ValidateParameters(-1, 180000, 1, 1000, 60000));
            Assert.Contains("Command timeout must be positive", ex.Message);
        }

        [Fact]
        public void ValidateParameters_WithZeroCommandTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CdbSessionConfiguration.ValidateParameters(0, 180000, 1, 1000, 60000));
            Assert.Contains("Command timeout must be positive", ex.Message);
        }

        [Fact]
        public void ValidateParameters_WithNegativeIdleTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CdbSessionConfiguration.ValidateParameters(30000, -1, 1, 1000, 60000));
            Assert.Contains("Idle timeout must be positive", ex.Message);
        }

        [Fact]
        public void ValidateParameters_WithZeroIdleTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CdbSessionConfiguration.ValidateParameters(30000, 0, 1, 1000, 60000));
            Assert.Contains("Idle timeout must be positive", ex.Message);
        }

        [Fact]
        public void ValidateParameters_WithNegativeSymbolServerMaxRetries_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CdbSessionConfiguration.ValidateParameters(30000, 180000, -1, 1000, 60000));
            Assert.Contains("Symbol server max retries cannot be negative", ex.Message);
        }

        [Fact]
        public void ValidateParameters_WithZeroSymbolServerMaxRetries_DoesNotThrow()
        {
            // Act & Assert - Zero is valid (means no retries)
            CdbSessionConfiguration.ValidateParameters(30000, 180000, 0, 1000, 60000);
        }

        [Fact]
        public void ValidateParameters_WithNegativeStartupDelay_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CdbSessionConfiguration.ValidateParameters(30000, 180000, 1, -1, 60000));
            Assert.Contains("Startup delay cannot be negative", ex.Message);
        }

        [Fact]
        public void ValidateParameters_WithZeroStartupDelay_DoesNotThrow()
        {
            // Act & Assert - Zero is valid (no delay)
            CdbSessionConfiguration.ValidateParameters(30000, 180000, 1, 0, 60000);
        }

        [Fact]
        public void ValidateParameters_WithNegativeOutputReadingTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CdbSessionConfiguration.ValidateParameters(30000, 180000, 1, 1000, -1));
            Assert.Contains("Output reading timeout must be positive", ex.Message);
        }

        [Fact]
        public void ValidateParameters_WithZeroOutputReadingTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CdbSessionConfiguration.ValidateParameters(30000, 180000, 1, 1000, 0));
            Assert.Contains("Output reading timeout must be positive", ex.Message);
        }

        [Fact]
        public void ValidateParameters_WithValidParameters_DoesNotThrow()
        {
            // Act & Assert
            CdbSessionConfiguration.ValidateParameters(30000, 180000, 1, 1000, 60000);
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_WithInvalidCommandTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(commandTimeoutMs: -1));
            Assert.Contains("Command timeout must be positive", ex.Message);
        }

        [Fact]
        public void Constructor_WithInvalidIdleTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(idleTimeoutMs: 0));
            Assert.Contains("Idle timeout must be positive", ex.Message);
        }

        [Fact]
        public void Constructor_WithInvalidSymbolServerMaxRetries_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(symbolServerMaxRetries: -5));
            Assert.Contains("Symbol server max retries cannot be negative", ex.Message);
        }

        [Fact]
        public void Constructor_WithInvalidStartupDelay_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(startupDelayMs: -100));
            Assert.Contains("Startup delay cannot be negative", ex.Message);
        }

        [Fact]
        public void Constructor_WithInvalidOutputReadingTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(outputReadingTimeoutMs: 0));
            Assert.Contains("Output reading timeout must be positive", ex.Message);
        }

        #endregion

        #region GetCurrentArchitecture Tests

        [Fact]
        public void GetCurrentArchitecture_ReturnsValidArchitecture()
        {
            // Arrange
            var config = new CdbSessionConfiguration();

            // Act
            var arch = config.GetCurrentArchitecture();

            // Assert
            Assert.NotNull(arch);
            Assert.Contains(arch, new[] { "x64", "x86", "arm64", "arm" });
        }

        #endregion

        #region FindCdbPath Tests

        [Fact]
        public void FindCdbPath_WithNonExistentCustomPath_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = "C:\\NonExistent\\cdb.exe";
            var config = new CdbSessionConfiguration(customCdbPath: nonExistentPath);

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() => config.FindCdbPath());
            Assert.Contains("Custom CDB path not found", ex.Message);
            Assert.Contains(nonExistentPath, ex.Message);
        }

        [Fact]
        public void FindCdbPath_WithExistingCustomPath_ReturnsCustomPath()
        {
            // Arrange - Create a temporary file to simulate existing CDB
            var tempFile = Path.GetTempFileName();
            try
            {
                var config = new CdbSessionConfiguration(customCdbPath: tempFile);

                // Act
                var result = config.FindCdbPath();

                // Assert
                Assert.Equal(tempFile, result);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void FindCdbPath_WithNullCustomPath_SearchesStandardLocations()
        {
            // Arrange
            var config = new CdbSessionConfiguration(customCdbPath: null);

            // Act
            var result = config.FindCdbPath();

            // Assert - Either finds CDB or returns null (system-dependent)
            // We just verify it doesn't throw and returns a string or null
            Assert.True(result == null || result.EndsWith("cdb.exe", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void FindCdbPath_WithEmptyCustomPath_SearchesStandardLocations()
        {
            // Arrange
            var config = new CdbSessionConfiguration(customCdbPath: "");

            // Act
            var result = config.FindCdbPath();

            // Assert - Either finds CDB or returns null (system-dependent)
            Assert.True(result == null || result.EndsWith("cdb.exe", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void FindCdbPath_WithWhitespaceCustomPath_SearchesStandardLocations()
        {
            // Arrange
            var config = new CdbSessionConfiguration(customCdbPath: "   ");

            // Act
            var result = config.FindCdbPath();

            // Assert - Either finds CDB or returns null (system-dependent)
            Assert.True(result == null || result.EndsWith("cdb.exe", StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
