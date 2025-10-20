using mcp_nexus.Configuration;

namespace mcp_nexus_unit_tests.Configuration
{
    /// <summary>
    /// Unit tests for the EnhancedTimeoutConfiguration class.
    /// Tests timeout configuration, validation, and adaptive timeout functionality.
    /// </summary>
    public class EnhancedTimeoutConfigurationTests
    {
        /// <summary>
        /// Tests that EnhancedTimeoutConfiguration initializes with default values.
        /// </summary>
        [Fact]
        public void Constructor_WithDefaultValues_InitializesCorrectly()
        {
            // Act
            var config = new EnhancedTimeoutConfiguration();

            // Assert
            Assert.Equal(600000, config.BaseCommandTimeoutMs);
            Assert.Equal(1800000, config.ComplexCommandTimeoutMs);
            Assert.Equal(60000, config.OutputReadingTimeoutMs);
            Assert.Equal(300000, config.IdleTimeoutMs);
            Assert.Equal(2000, config.StartupDelayMs);
            Assert.Equal(3, config.SymbolServerMaxRetries);
            Assert.True(config.EnableAdaptiveTimeouts);
            Assert.Equal(1.0, config.PerformanceMultiplier);
        }

        /// <summary>
        /// Tests that EnhancedTimeoutConfiguration initializes with custom values.
        /// </summary>
        [Fact]
        public void Constructor_WithCustomValues_InitializesCorrectly()
        {
            // Arrange
            var baseTimeout = 300000;
            var complexTimeout = 900000;
            var outputTimeout = 30000;
            var idleTimeout = 180000;
            var startupDelay = 1000;
            var maxRetries = 5;
            var enableAdaptive = false;
            var multiplier = 1.5;

            // Act
            var config = new EnhancedTimeoutConfiguration(
                baseTimeout, complexTimeout, outputTimeout,
                idleTimeout, startupDelay, maxRetries, enableAdaptive, multiplier);

            // Assert
            Assert.Equal(baseTimeout, config.BaseCommandTimeoutMs);
            Assert.Equal(complexTimeout, config.ComplexCommandTimeoutMs);
            Assert.Equal(outputTimeout, config.OutputReadingTimeoutMs);
            Assert.Equal(idleTimeout, config.IdleTimeoutMs);
            Assert.Equal(startupDelay, config.StartupDelayMs);
            Assert.Equal(maxRetries, config.SymbolServerMaxRetries);
            Assert.Equal(enableAdaptive, config.EnableAdaptiveTimeouts);
            Assert.Equal(multiplier, config.PerformanceMultiplier);
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException for invalid base timeout.
        /// </summary>
        [Fact]
        public void ValidateParameters_WithInvalidBaseTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    0, 1800000, 60000, 300000, 2000, 3, 1.0));
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException for invalid complex timeout.
        /// </summary>
        [Fact]
        public void ValidateParameters_WithInvalidComplexTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    600000, 0, 60000, 300000, 2000, 3, 1.0));
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException for invalid output timeout.
        /// </summary>
        [Fact]
        public void ValidateParameters_WithInvalidOutputTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    600000, 1800000, -1, 300000, 2000, 3, 1.0));
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException for invalid output timeout (second test).
        /// </summary>
        [Fact]
        public void ValidateParameters_WithInvalidOutputTimeout2_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    600000, 1800000, 0, 300000, 2000, 3, 1.0));
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException for invalid idle timeout.
        /// </summary>
        [Fact]
        public void ValidateParameters_WithInvalidIdleTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    600000, 1800000, 60000, 0, 2000, 3, 1.0));
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException for invalid startup delay.
        /// </summary>
        [Fact]
        public void ValidateParameters_WithInvalidStartupDelay_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    600000, 1800000, 60000, 300000, -1, 3, 1.0));
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException for invalid max retries.
        /// </summary>
        [Fact]
        public void ValidateParameters_WithInvalidMaxRetries_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    600000, 1800000, 60000, 300000, 2000, -1, 1.0));
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException for invalid performance multiplier.
        /// </summary>
        [Fact]
        public void ValidateParameters_WithInvalidPerformanceMultiplier_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    600000, 1800000, 60000, 300000, 2000, 3, 0.0));
        }

        /// <summary>
        /// Tests that ValidateParameters throws ArgumentOutOfRangeException when complex timeout is less than base timeout.
        /// </summary>
        [Fact]
        public void ValidateParameters_WithComplexTimeoutLessThanBase_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EnhancedTimeoutConfiguration.ValidateParameters(
                    600000, 300000, 60000, 300000, 2000, 3, 1.0));
        }

        /// <summary>
        /// Tests that GetCommandTimeout returns base timeout for simple commands.
        /// </summary>
        [Fact]
        public void GetCommandTimeout_WithSimpleCommand_ReturnsBaseTimeout()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                baseCommandTimeoutMs: 300000,
                complexCommandTimeoutMs: 900000,
                enableAdaptiveTimeouts: false);

            // Act
            var result = config.GetCommandTimeout("k");

            // Assert
            Assert.Equal(300000, result);
        }

        /// <summary>
        /// Tests that GetCommandTimeout returns complex timeout for complex commands.
        /// </summary>
        [Fact]
        public void GetCommandTimeout_WithComplexCommand_ReturnsComplexTimeout()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                baseCommandTimeoutMs: 300000,
                complexCommandTimeoutMs: 900000,
                enableAdaptiveTimeouts: false);

            // Act
            var result = config.GetCommandTimeout("!analyze -v");

            // Assert
            Assert.Equal(900000, result);
        }

        /// <summary>
        /// Tests that GetCommandTimeout returns base timeout for null command.
        /// </summary>
        [Fact]
        public void GetCommandTimeout_WithNullCommand_ReturnsBaseTimeout()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                baseCommandTimeoutMs: 300000,
                complexCommandTimeoutMs: 900000,
                enableAdaptiveTimeouts: false);

            // Act
            var result = config.GetCommandTimeout(null!);

            // Assert
            Assert.Equal(300000, result);
        }

        /// <summary>
        /// Tests that GetCommandTimeout returns base timeout for empty command.
        /// </summary>
        [Fact]
        public void GetCommandTimeout_WithEmptyCommand_ReturnsBaseTimeout()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                baseCommandTimeoutMs: 300000,
                complexCommandTimeoutMs: 900000,
                enableAdaptiveTimeouts: false);

            // Act
            var result = config.GetCommandTimeout("");

            // Assert
            Assert.Equal(300000, result);
        }

        /// <summary>
        /// Tests that GetCommandTimeout applies performance multiplier when adaptive timeouts are enabled.
        /// </summary>
        [Fact]
        public void GetCommandTimeout_WithAdaptiveTimeoutsEnabled_AppliesPerformanceMultiplier()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                baseCommandTimeoutMs: 300000,
                complexCommandTimeoutMs: 900000,
                enableAdaptiveTimeouts: true,
                performanceMultiplier: 1.5);

            // Act
            var result = config.GetCommandTimeout("k");

            // Assert
            Assert.Equal(450000, result); // 300000 * 1.5
        }

        /// <summary>
        /// Tests that GetCommandTimeout does not apply performance multiplier when adaptive timeouts are disabled.
        /// </summary>
        [Fact]
        public void GetCommandTimeout_WithAdaptiveTimeoutsDisabled_DoesNotApplyPerformanceMultiplier()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                baseCommandTimeoutMs: 300000,
                complexCommandTimeoutMs: 900000,
                enableAdaptiveTimeouts: false,
                performanceMultiplier: 1.5);

            // Act
            var result = config.GetCommandTimeout("k");

            // Assert
            Assert.Equal(300000, result);
        }

        /// <summary>
        /// Tests that GetOutputReadingTimeout returns configured timeout when adaptive timeouts are disabled.
        /// </summary>
        [Fact]
        public void GetOutputReadingTimeout_WithAdaptiveTimeoutsDisabled_ReturnsConfiguredTimeout()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                outputReadingTimeoutMs: 30000,
                enableAdaptiveTimeouts: false);

            // Act
            var result = config.GetOutputReadingTimeout();

            // Assert
            Assert.Equal(30000, result);
        }

        /// <summary>
        /// Tests that GetOutputReadingTimeout applies performance multiplier when adaptive timeouts are enabled.
        /// </summary>
        [Fact]
        public void GetOutputReadingTimeout_WithAdaptiveTimeoutsEnabled_AppliesPerformanceMultiplier()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                outputReadingTimeoutMs: 30000,
                enableAdaptiveTimeouts: true,
                performanceMultiplier: 2.0);

            // Act
            var result = config.GetOutputReadingTimeout();

            // Assert
            Assert.Equal(60000, result); // 30000 * 2.0
        }


        /// <summary>
        /// Tests that GetIdleTimeout returns configured timeout when adaptive timeouts are disabled.
        /// </summary>
        [Fact]
        public void GetIdleTimeout_WithAdaptiveTimeoutsDisabled_ReturnsConfiguredTimeout()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                idleTimeoutMs: 180000,
                enableAdaptiveTimeouts: false);

            // Act
            var result = config.GetIdleTimeout();

            // Assert
            Assert.Equal(180000, result);
        }

        /// <summary>
        /// Tests that GetIdleTimeout applies performance multiplier when adaptive timeouts are enabled.
        /// </summary>
        [Fact]
        public void GetIdleTimeout_WithAdaptiveTimeoutsEnabled_AppliesPerformanceMultiplier()
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                idleTimeoutMs: 180000,
                enableAdaptiveTimeouts: true,
                performanceMultiplier: 0.5);

            // Act
            var result = config.GetIdleTimeout();

            // Assert
            Assert.Equal(90000, result); // 180000 * 0.5
        }

        /// <summary>
        /// Tests that GetCommandTimeout correctly identifies various complex commands.
        /// </summary>
        [Theory]
        [InlineData("!analyze -v")]
        [InlineData("!analyze")]
        [InlineData("!heap")]
        [InlineData("!address")]
        [InlineData("!process")]
        [InlineData("!thread")]
        [InlineData("!locks")]
        [InlineData("!handle")]
        [InlineData("!gflags")]
        [InlineData("!ext")]
        [InlineData("!sym")]
        [InlineData("!peb")]
        [InlineData("!teb")]
        public void GetCommandTimeout_WithComplexCommands_ReturnsComplexTimeout(string command)
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                baseCommandTimeoutMs: 300000,
                complexCommandTimeoutMs: 900000,
                enableAdaptiveTimeouts: false);

            // Act
            var result = config.GetCommandTimeout(command);

            // Assert
            Assert.Equal(900000, result);
        }

        /// <summary>
        /// Tests that GetCommandTimeout correctly identifies simple commands.
        /// </summary>
        [Theory]
        [InlineData("k")]
        [InlineData("g")]
        [InlineData("p")]
        [InlineData("t")]
        [InlineData("r")]
        [InlineData("x")]
        [InlineData("u")]
        [InlineData("d")]
        public void GetCommandTimeout_WithSimpleCommands_ReturnsBaseTimeout(string command)
        {
            // Arrange
            var config = new EnhancedTimeoutConfiguration(
                baseCommandTimeoutMs: 300000,
                complexCommandTimeoutMs: 900000,
                enableAdaptiveTimeouts: false);

            // Act
            var result = config.GetCommandTimeout(command);

            // Assert
            Assert.Equal(300000, result);
        }
    }
}
