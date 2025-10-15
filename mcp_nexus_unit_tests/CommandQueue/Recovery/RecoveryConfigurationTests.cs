using Xunit;
using mcp_nexus.CommandQueue.Recovery;

namespace mcp_nexus_unit_tests.CommandQueue.Recovery
{
    /// <summary>
    /// Tests for RecoveryConfiguration
    /// </summary>
    public class RecoveryConfigurationTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultParameters_UsesDefaults()
        {
            // Act
            var config = new RecoveryConfiguration();

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), config.CancellationTimeout);
            Assert.Equal(TimeSpan.FromSeconds(2), config.RestartDelay);
            Assert.Equal(TimeSpan.FromMinutes(1), config.HealthCheckInterval);
            Assert.Equal(3, config.MaxRecoveryAttempts);
            Assert.Equal(TimeSpan.FromMinutes(5), config.RecoveryAttemptCooldown);
        }

        [Fact]
        public void Constructor_WithCustomCancellationTimeout_UsesProvidedValue()
        {
            // Arrange
            var customTimeout = TimeSpan.FromSeconds(10);

            // Act
            var config = new RecoveryConfiguration(cancellationTimeout: customTimeout);

            // Assert
            Assert.Equal(customTimeout, config.CancellationTimeout);
        }

        [Fact]
        public void Constructor_WithNullCancellationTimeout_UsesDefault()
        {
            // Act
            var config = new RecoveryConfiguration(cancellationTimeout: null);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), config.CancellationTimeout);
        }

        [Fact]
        public void Constructor_WithCustomRestartDelay_UsesProvidedValue()
        {
            // Arrange
            var customDelay = TimeSpan.FromSeconds(5);

            // Act
            var config = new RecoveryConfiguration(restartDelay: customDelay);

            // Assert
            Assert.Equal(customDelay, config.RestartDelay);
        }

        [Fact]
        public void Constructor_WithNullRestartDelay_UsesDefault()
        {
            // Act
            var config = new RecoveryConfiguration(restartDelay: null);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(2), config.RestartDelay);
        }

        [Fact]
        public void Constructor_WithCustomHealthCheckInterval_UsesProvidedValue()
        {
            // Arrange
            var customInterval = TimeSpan.FromMinutes(2);

            // Act
            var config = new RecoveryConfiguration(healthCheckInterval: customInterval);

            // Assert
            Assert.Equal(customInterval, config.HealthCheckInterval);
        }

        [Fact]
        public void Constructor_WithNullHealthCheckInterval_UsesDefault()
        {
            // Act
            var config = new RecoveryConfiguration(healthCheckInterval: null);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(1), config.HealthCheckInterval);
        }

        [Fact]
        public void Constructor_WithCustomMaxRecoveryAttempts_UsesProvidedValue()
        {
            // Act
            var config = new RecoveryConfiguration(maxRecoveryAttempts: 5);

            // Assert
            Assert.Equal(5, config.MaxRecoveryAttempts);
        }

        [Fact]
        public void Constructor_WithCustomRecoveryAttemptCooldown_UsesProvidedValue()
        {
            // Arrange
            var customCooldown = TimeSpan.FromMinutes(10);

            // Act
            var config = new RecoveryConfiguration(recoveryAttemptCooldown: customCooldown);

            // Assert
            Assert.Equal(customCooldown, config.RecoveryAttemptCooldown);
        }

        [Fact]
        public void Constructor_WithNullRecoveryAttemptCooldown_UsesDefault()
        {
            // Act
            var config = new RecoveryConfiguration(recoveryAttemptCooldown: null);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), config.RecoveryAttemptCooldown);
        }

        #endregion

        #region ShouldAttemptRecovery Tests

        [Fact]
        public void ShouldAttemptRecovery_WhenAttemptCountBelowMaxAndCooldownElapsed_ReturnsTrue()
        {
            // Arrange
            var config = new RecoveryConfiguration(maxRecoveryAttempts: 3, recoveryAttemptCooldown: TimeSpan.FromSeconds(1));
            var lastAttempt = DateTime.Now.AddSeconds(-2); // 2 seconds ago (cooldown elapsed)

            // Act
            var result = config.ShouldAttemptRecovery(attemptCount: 2, lastAttemptTime: lastAttempt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldAttemptRecovery_WhenAttemptCountEqualsMax_ReturnsFalse()
        {
            // Arrange
            var config = new RecoveryConfiguration(maxRecoveryAttempts: 3);
            var lastAttempt = DateTime.Now.AddMinutes(-10); // Long time ago

            // Act
            var result = config.ShouldAttemptRecovery(attemptCount: 3, lastAttemptTime: lastAttempt);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldAttemptRecovery_WhenAttemptCountExceedsMax_ReturnsFalse()
        {
            // Arrange
            var config = new RecoveryConfiguration(maxRecoveryAttempts: 3);
            var lastAttempt = DateTime.Now.AddMinutes(-10); // Long time ago

            // Act
            var result = config.ShouldAttemptRecovery(attemptCount: 5, lastAttemptTime: lastAttempt);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldAttemptRecovery_WhenCooldownNotElapsed_ReturnsFalse()
        {
            // Arrange
            var config = new RecoveryConfiguration(maxRecoveryAttempts: 3, recoveryAttemptCooldown: TimeSpan.FromMinutes(5));
            var lastAttempt = DateTime.Now.AddSeconds(-10); // Only 10 seconds ago (cooldown not elapsed)

            // Act
            var result = config.ShouldAttemptRecovery(attemptCount: 1, lastAttemptTime: lastAttempt);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldAttemptRecovery_WhenAttemptCountZeroAndCooldownElapsed_ReturnsTrue()
        {
            // Arrange
            var config = new RecoveryConfiguration(recoveryAttemptCooldown: TimeSpan.FromSeconds(1));
            var lastAttempt = DateTime.Now.AddSeconds(-2); // 2 seconds ago

            // Act
            var result = config.ShouldAttemptRecovery(attemptCount: 0, lastAttemptTime: lastAttempt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldAttemptRecovery_WithMinValueLastAttempt_ReturnsTrue()
        {
            // Arrange
            var config = new RecoveryConfiguration(maxRecoveryAttempts: 3);
            var lastAttempt = DateTime.MinValue; // Never attempted before

            // Act
            var result = config.ShouldAttemptRecovery(attemptCount: 0, lastAttemptTime: lastAttempt);

            // Assert
            Assert.True(result); // Cooldown has definitely elapsed
        }

        #endregion

        #region GetRestartDelay Tests

        [Fact]
        public void GetRestartDelay_WithAttemptNumber1_ReturnsBaseDelay()
        {
            // Arrange
            var baseDelay = TimeSpan.FromSeconds(2);
            var config = new RecoveryConfiguration(restartDelay: baseDelay);

            // Act
            var delay = config.GetRestartDelay(attemptNumber: 1);

            // Assert
            Assert.Equal(baseDelay, delay); // 2^0 = 1
        }

        [Fact]
        public void GetRestartDelay_WithAttemptNumber2_ReturnsDoubleDelay()
        {
            // Arrange
            var baseDelay = TimeSpan.FromSeconds(2);
            var config = new RecoveryConfiguration(restartDelay: baseDelay);

            // Act
            var delay = config.GetRestartDelay(attemptNumber: 2);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(4), delay); // 2^1 = 2, so 2s * 2 = 4s
        }

        [Fact]
        public void GetRestartDelay_WithAttemptNumber3_ReturnsQuadrupleDelay()
        {
            // Arrange
            var baseDelay = TimeSpan.FromSeconds(2);
            var config = new RecoveryConfiguration(restartDelay: baseDelay);

            // Act
            var delay = config.GetRestartDelay(attemptNumber: 3);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(8), delay); // 2^2 = 4, so 2s * 4 = 8s
        }

        [Fact]
        public void GetRestartDelay_WithAttemptNumber4_ReturnsExponentialDelay()
        {
            // Arrange
            var baseDelay = TimeSpan.FromSeconds(1);
            var config = new RecoveryConfiguration(restartDelay: baseDelay);

            // Act
            var delay = config.GetRestartDelay(attemptNumber: 4);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(8), delay); // 2^3 = 8, so 1s * 8 = 8s
        }

        [Fact]
        public void GetRestartDelay_WithZeroAttemptNumber_ReturnsHalfDelay()
        {
            // Arrange
            var baseDelay = TimeSpan.FromSeconds(2);
            var config = new RecoveryConfiguration(restartDelay: baseDelay);

            // Act
            var delay = config.GetRestartDelay(attemptNumber: 0);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(1), delay); // 2^-1 = 0.5, so 2s * 0.5 = 1s
        }

        #endregion

        [Fact]
        public void RecoveryConfiguration_Class_Exists()
        {
            // This test verifies that the RecoveryConfiguration class exists and can be instantiated
            Assert.NotNull(typeof(RecoveryConfiguration));
        }
    }
}
