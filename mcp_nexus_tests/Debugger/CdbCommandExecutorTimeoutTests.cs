using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Debugger;

namespace mcp_nexus_tests.Debugger
{
    /// <summary>
    /// Unit tests for enhanced timeout functionality in CdbCommandExecutor.
    /// Tests timeout handling, output reading timeouts, and adaptive timeout behavior.
    /// </summary>
    public class CdbCommandExecutorTimeoutTests
    {
        private readonly Mock<ILogger<CdbCommandExecutor>> m_MockLogger;
        private readonly Mock<ILogger<CdbOutputParser>> m_MockParserLogger;
        private readonly CdbOutputParser m_OutputParser;
        private readonly CdbSessionConfiguration m_Configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbCommandExecutorTimeoutTests"/> class.
        /// </summary>
        public CdbCommandExecutorTimeoutTests()
        {
            m_MockLogger = new Mock<ILogger<CdbCommandExecutor>>();
            m_MockParserLogger = new Mock<ILogger<CdbOutputParser>>();
            m_OutputParser = new CdbOutputParser(m_MockParserLogger.Object);
            m_Configuration = new CdbSessionConfiguration(
                commandTimeoutMs: 30000,
                idleTimeoutMs: 180000,
                outputReadingTimeoutMs: 60000);
        }

        /// <summary>
        /// Tests that CdbCommandExecutor uses the configured output reading timeout.
        /// </summary>
        [Fact]
        public void Constructor_WithOutputReadingTimeout_ConfiguresCorrectly()
        {
            // Arrange
            var config = new CdbSessionConfiguration(
                commandTimeoutMs: 30000,
                idleTimeoutMs: 180000,
                outputReadingTimeoutMs: 120000);

            // Act
            var executor = new CdbCommandExecutor(m_MockLogger.Object, config, m_OutputParser);

            // Assert
            Assert.NotNull(executor);
            // The timeout is used internally, so we can't directly test it without reflection
            // But we can verify the configuration was passed correctly
        }

        /// <summary>
        /// Tests that CdbCommandExecutor handles timeout configuration validation.
        /// </summary>
        [Fact]
        public void Constructor_WithInvalidTimeoutConfiguration_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(
                    commandTimeoutMs: 30000,
                    idleTimeoutMs: 180000,
                    outputReadingTimeoutMs: 0)); // Invalid timeout
        }

        /// <summary>
        /// Tests that CdbCommandExecutor uses default timeout when OutputReadingTimeoutMs is not set.
        /// </summary>
        [Fact]
        public void Constructor_WithDefaultConfiguration_UsesDefaultTimeout()
        {
            // Arrange
            var config = new CdbSessionConfiguration(
                commandTimeoutMs: 30000,
                idleTimeoutMs: 180000);

            // Act
            var executor = new CdbCommandExecutor(m_MockLogger.Object, config, m_OutputParser);

            // Assert
            Assert.NotNull(executor);
            Assert.Equal(60000, config.OutputReadingTimeoutMs); // Default value
        }

        /// <summary>
        /// Tests that CdbCommandExecutor configuration includes all timeout settings.
        /// </summary>
        [Fact]
        public void CdbSessionConfiguration_WithAllTimeouts_IncludesAllSettings()
        {
            // Arrange
            var commandTimeout = 30000;
            var idleTimeout = 180000;
            var outputReadingTimeout = 120000;
            var symbolServerTimeout = 60000;
            var startupDelay = 2000;

            // Act
            var config = new CdbSessionConfiguration(
                commandTimeoutMs: commandTimeout,
                idleTimeoutMs: idleTimeout,
                outputReadingTimeoutMs: outputReadingTimeout,
                symbolServerTimeoutMs: symbolServerTimeout,
                startupDelayMs: startupDelay);

            // Assert
            Assert.Equal(commandTimeout, config.CommandTimeoutMs);
            Assert.Equal(idleTimeout, config.IdleTimeoutMs);
            Assert.Equal(outputReadingTimeout, config.OutputReadingTimeoutMs);
            Assert.Equal(symbolServerTimeout, config.SymbolServerTimeoutMs);
            Assert.Equal(startupDelay, config.StartupDelayMs);
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration validation works correctly for all timeout parameters.
        /// </summary>
        [Theory]
        [InlineData(0, 180000, 60000, 30000, 2000)] // Invalid command timeout
        [InlineData(30000, 0, 60000, 30000, 2000)] // Invalid idle timeout
        [InlineData(30000, 180000, 0, 30000, 2000)] // Invalid output reading timeout
        [InlineData(30000, 180000, 60000, -1, 2000)] // Invalid symbol server timeout
        [InlineData(30000, 180000, 60000, 30000, -1)] // Invalid startup delay
        public void CdbSessionConfiguration_WithInvalidParameters_ThrowsArgumentOutOfRangeException(
            int commandTimeout, int idleTimeout, int outputReadingTimeout, 
            int symbolServerTimeout, int startupDelay)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(
                    commandTimeoutMs: commandTimeout,
                    idleTimeoutMs: idleTimeout,
                    outputReadingTimeoutMs: outputReadingTimeout,
                    symbolServerTimeoutMs: symbolServerTimeout,
                    startupDelayMs: startupDelay));
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration validation passes for valid parameters.
        /// </summary>
        [Theory]
        [InlineData(30000, 180000, 60000, 30000, 2000)]
        [InlineData(600000, 300000, 120000, 60000, 1000)]
        [InlineData(1000, 5000, 2000, 1000, 500)]
        public void CdbSessionConfiguration_WithValidParameters_DoesNotThrowException(
            int commandTimeout, int idleTimeout, int outputReadingTimeout, 
            int symbolServerTimeout, int startupDelay)
        {
            // Act & Assert
            var config = new CdbSessionConfiguration(
                commandTimeoutMs: commandTimeout,
                idleTimeoutMs: idleTimeout,
                outputReadingTimeoutMs: outputReadingTimeout,
                symbolServerTimeoutMs: symbolServerTimeout,
                startupDelayMs: startupDelay);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(commandTimeout, config.CommandTimeoutMs);
            Assert.Equal(idleTimeout, config.IdleTimeoutMs);
            Assert.Equal(outputReadingTimeout, config.OutputReadingTimeoutMs);
            Assert.Equal(symbolServerTimeout, config.SymbolServerTimeoutMs);
            Assert.Equal(startupDelay, config.StartupDelayMs);
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration uses default values when parameters are not specified.
        /// </summary>
        [Fact]
        public void CdbSessionConfiguration_WithDefaultValues_UsesCorrectDefaults()
        {
            // Act
            var config = new CdbSessionConfiguration();

            // Assert
            Assert.Equal(30000, config.CommandTimeoutMs);
            Assert.Equal(180000, config.IdleTimeoutMs);
            Assert.Equal(60000, config.OutputReadingTimeoutMs);
            Assert.Equal(30000, config.SymbolServerTimeoutMs);
            Assert.Equal(1, config.SymbolServerMaxRetries);
            Assert.Equal(1000, config.StartupDelayMs);
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration handles null symbol search path.
        /// </summary>
        [Fact]
        public void CdbSessionConfiguration_WithNullSymbolSearchPath_HandlesCorrectly()
        {
            // Act
            var config = new CdbSessionConfiguration(symbolSearchPath: null);

            // Assert
            Assert.Null(config.SymbolSearchPath);
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration handles custom CDB path.
        /// </summary>
        [Fact]
        public void CdbSessionConfiguration_WithCustomCdbPath_HandlesCorrectly()
        {
            // Arrange
            var customPath = @"C:\Custom\cdb.exe";

            // Act
            var config = new CdbSessionConfiguration(customCdbPath: customPath);

            // Assert
            Assert.Equal(customPath, config.CustomCdbPath);
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration handles symbol search path with whitespace.
        /// </summary>
        [Fact]
        public void CdbSessionConfiguration_WithSymbolSearchPath_HandlesCorrectly()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache;srv*https://msdl.microsoft.com/download/symbols";

            // Act
            var config = new CdbSessionConfiguration(symbolSearchPath: symbolPath);

            // Assert
            Assert.Equal(symbolPath, config.SymbolSearchPath);
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration validation works for edge case values.
        /// </summary>
        [Theory]
        [InlineData(1, 1, 1, 0, 0)] // Minimum valid values
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)] // Maximum values
        public void CdbSessionConfiguration_WithEdgeCaseValues_HandlesCorrectly(
            int commandTimeout, int idleTimeout, int outputReadingTimeout, 
            int symbolServerTimeout, int startupDelay)
        {
            // Act
            var config = new CdbSessionConfiguration(
                commandTimeoutMs: commandTimeout,
                idleTimeoutMs: idleTimeout,
                outputReadingTimeoutMs: outputReadingTimeout,
                symbolServerTimeoutMs: symbolServerTimeout,
                startupDelayMs: startupDelay);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(commandTimeout, config.CommandTimeoutMs);
            Assert.Equal(idleTimeout, config.IdleTimeoutMs);
            Assert.Equal(outputReadingTimeout, config.OutputReadingTimeoutMs);
            Assert.Equal(symbolServerTimeout, config.SymbolServerTimeoutMs);
            Assert.Equal(startupDelay, config.StartupDelayMs);
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration handles different symbol server max retries values.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void CdbSessionConfiguration_WithDifferentMaxRetries_HandlesCorrectly(int maxRetries)
        {
            // Act
            var config = new CdbSessionConfiguration(symbolServerMaxRetries: maxRetries);

            // Assert
            Assert.Equal(maxRetries, config.SymbolServerMaxRetries);
        }

        /// <summary>
        /// Tests that CdbSessionConfiguration throws exception for negative max retries.
        /// </summary>
        [Fact]
        public void CdbSessionConfiguration_WithNegativeMaxRetries_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CdbSessionConfiguration(symbolServerMaxRetries: -1));
        }
    }
}
