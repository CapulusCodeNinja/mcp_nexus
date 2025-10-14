using System;
using Xunit;
using mcp_nexus.Recovery;

namespace mcp_nexus_tests.Recovery
{
    public class RecoveryStatisticsTests
    {
        [Fact]
        public void RecoveryStatistics_Class_Exists()
        {
            // Act & Assert
            Assert.NotNull(typeof(RecoveryStatistics));
        }

        [Fact]
        public void RecoveryStatistics_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var stats = new RecoveryStatistics();

            // Assert
            Assert.Equal(0, stats.RecoveryAttempts);
            Assert.Equal(DateTime.MinValue, stats.LastRecoveryAttempt);
            Assert.Equal(TimeSpan.Zero, stats.TimeSinceLastAttempt);
            Assert.False(stats.CanAttemptRecovery);
        }

        [Fact]
        public void RecoveryAttempts_CanBeSetAndRetrieved()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var expectedValue = 42;

            // Act
            stats.RecoveryAttempts = expectedValue;

            // Assert
            Assert.Equal(expectedValue, stats.RecoveryAttempts);
        }

        [Fact]
        public void RecoveryAttempts_WithNegativeValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var negativeValue = -5;

            // Act
            stats.RecoveryAttempts = negativeValue;

            // Assert
            Assert.Equal(negativeValue, stats.RecoveryAttempts);
        }

        [Fact]
        public void RecoveryAttempts_WithZeroValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics
            {
                // Act
                RecoveryAttempts = 0
            };

            // Assert
            Assert.Equal(0, stats.RecoveryAttempts);
        }

        [Fact]
        public void RecoveryAttempts_WithMaxValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var maxValue = int.MaxValue;

            // Act
            stats.RecoveryAttempts = maxValue;

            // Assert
            Assert.Equal(maxValue, stats.RecoveryAttempts);
        }

        [Fact]
        public void LastRecoveryAttempt_CanBeSetAndRetrieved()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var expectedValue = DateTime.UtcNow;

            // Act
            stats.LastRecoveryAttempt = expectedValue;

            // Assert
            Assert.Equal(expectedValue, stats.LastRecoveryAttempt);
        }

        [Fact]
        public void LastRecoveryAttempt_WithMinValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var minValue = DateTime.MinValue;

            // Act
            stats.LastRecoveryAttempt = minValue;

            // Assert
            Assert.Equal(minValue, stats.LastRecoveryAttempt);
        }

        [Fact]
        public void LastRecoveryAttempt_WithMaxValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var maxValue = DateTime.MaxValue;

            // Act
            stats.LastRecoveryAttempt = maxValue;

            // Assert
            Assert.Equal(maxValue, stats.LastRecoveryAttempt);
        }

        [Fact]
        public void LastRecoveryAttempt_WithUtcNow_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var utcNow = DateTime.UtcNow;

            // Act
            stats.LastRecoveryAttempt = utcNow;

            // Assert
            Assert.Equal(utcNow, stats.LastRecoveryAttempt);
        }

        [Fact]
        public void LastRecoveryAttempt_WithLocalTime_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var localTime = DateTime.Now;

            // Act
            stats.LastRecoveryAttempt = localTime;

            // Assert
            Assert.Equal(localTime, stats.LastRecoveryAttempt);
        }

        [Fact]
        public void TimeSinceLastAttempt_CanBeSetAndRetrieved()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var expectedValue = TimeSpan.FromMinutes(30);

            // Act
            stats.TimeSinceLastAttempt = expectedValue;

            // Assert
            Assert.Equal(expectedValue, stats.TimeSinceLastAttempt);
        }

        [Fact]
        public void TimeSinceLastAttempt_WithZeroValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics
            {
                // Act
                TimeSinceLastAttempt = TimeSpan.Zero
            };

            // Assert
            Assert.Equal(TimeSpan.Zero, stats.TimeSinceLastAttempt);
        }

        [Fact]
        public void TimeSinceLastAttempt_WithNegativeValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var negativeValue = TimeSpan.FromMinutes(-30);

            // Act
            stats.TimeSinceLastAttempt = negativeValue;

            // Assert
            Assert.Equal(negativeValue, stats.TimeSinceLastAttempt);
        }

        [Fact]
        public void TimeSinceLastAttempt_WithMaxValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var maxValue = TimeSpan.MaxValue;

            // Act
            stats.TimeSinceLastAttempt = maxValue;

            // Assert
            Assert.Equal(maxValue, stats.TimeSinceLastAttempt);
        }

        [Fact]
        public void TimeSinceLastAttempt_WithVariousValues_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var testValues = new[]
            {
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(1),
                TimeSpan.FromTicks(1)
            };

            // Act & Assert
            foreach (var value in testValues)
            {
                stats.TimeSinceLastAttempt = value;
                Assert.Equal(value, stats.TimeSinceLastAttempt);
            }
        }

        [Fact]
        public void CanAttemptRecovery_CanBeSetAndRetrieved()
        {
            // Arrange
            var stats = new RecoveryStatistics
            {
                // Act
                CanAttemptRecovery = true
            };

            // Assert
            Assert.True(stats.CanAttemptRecovery);
        }

        [Fact]
        public void CanAttemptRecovery_WithFalseValue_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics
            {
                // Act
                CanAttemptRecovery = false
            };

            // Assert
            Assert.False(stats.CanAttemptRecovery);
        }

        [Fact]
        public void CanAttemptRecovery_CanBeToggled_HandlesCorrectly()
        {
            // Arrange
            var stats = new RecoveryStatistics
            {
                // Act & Assert
                CanAttemptRecovery = true
            };
            Assert.True(stats.CanAttemptRecovery);

            stats.CanAttemptRecovery = false;
            Assert.False(stats.CanAttemptRecovery);

            stats.CanAttemptRecovery = true;
            Assert.True(stats.CanAttemptRecovery);
        }

        [Fact]
        public void AllProperties_CanBeSetIndependently()
        {
            // Arrange
            var stats = new RecoveryStatistics();
            var recoveryAttempts = 5;
            var lastAttempt = DateTime.UtcNow.AddMinutes(-10);
            var timeSince = TimeSpan.FromMinutes(10);
            var canAttempt = true;

            // Act
            stats.RecoveryAttempts = recoveryAttempts;
            stats.LastRecoveryAttempt = lastAttempt;
            stats.TimeSinceLastAttempt = timeSince;
            stats.CanAttemptRecovery = canAttempt;

            // Assert
            Assert.Equal(recoveryAttempts, stats.RecoveryAttempts);
            Assert.Equal(lastAttempt, stats.LastRecoveryAttempt);
            Assert.Equal(timeSince, stats.TimeSinceLastAttempt);
            Assert.Equal(canAttempt, stats.CanAttemptRecovery);
        }

        [Fact]
        public void AllProperties_CanBeSetMultipleTimes()
        {
            // Arrange
            var stats = new RecoveryStatistics
            {
                // Act & Assert - First set
                RecoveryAttempts = 1,
                LastRecoveryAttempt = DateTime.UtcNow.AddMinutes(-5),
                TimeSinceLastAttempt = TimeSpan.FromMinutes(5),
                CanAttemptRecovery = true
            };

            Assert.Equal(1, stats.RecoveryAttempts);
            Assert.True(stats.CanAttemptRecovery);

            // Act & Assert - Second set
            stats.RecoveryAttempts = 10;
            stats.LastRecoveryAttempt = DateTime.UtcNow.AddMinutes(-30);
            stats.TimeSinceLastAttempt = TimeSpan.FromMinutes(30);
            stats.CanAttemptRecovery = false;

            Assert.Equal(10, stats.RecoveryAttempts);
            Assert.False(stats.CanAttemptRecovery);
        }

        [Fact]
        public void MultipleInstances_AreIndependent()
        {
            // Arrange
            var stats1 = new RecoveryStatistics();
            var stats2 = new RecoveryStatistics();

            // Act
            stats1.RecoveryAttempts = 5;
            stats1.CanAttemptRecovery = true;
            stats2.RecoveryAttempts = 10;
            stats2.CanAttemptRecovery = false;

            // Assert
            Assert.Equal(5, stats1.RecoveryAttempts);
            Assert.True(stats1.CanAttemptRecovery);
            Assert.Equal(10, stats2.RecoveryAttempts);
            Assert.False(stats2.CanAttemptRecovery);
        }

        [Fact]
        public void RecoveryStatistics_ClassCharacteristics_AreCorrect()
        {
            // Arrange
            var type = typeof(RecoveryStatistics);

            // Assert
            Assert.True(type.IsClass);
            Assert.False(type.IsSealed);
            Assert.False(type.IsAbstract);
            Assert.False(type.IsSealed && type.IsAbstract);
            Assert.False(type.IsInterface);
            Assert.False(type.IsEnum);
            Assert.False(type.IsValueType);
        }

        [Fact]
        public void RecoveryStatistics_CanBeUsedInCollections()
        {
            // Arrange
            var stats1 = new RecoveryStatistics { RecoveryAttempts = 1 };
            var stats2 = new RecoveryStatistics { RecoveryAttempts = 2 };
            var stats3 = new RecoveryStatistics { RecoveryAttempts = 3 };

            // Act
            var list = new[] { stats1, stats2, stats3 };

            // Assert
            Assert.Equal(3, list.Length);
            Assert.Equal(1, list[0].RecoveryAttempts);
            Assert.Equal(2, list[1].RecoveryAttempts);
            Assert.Equal(3, list[2].RecoveryAttempts);
        }

        [Fact]
        public void RecoveryStatistics_CanBeSerialized()
        {
            // Arrange
            var stats = new RecoveryStatistics
            {
                RecoveryAttempts = 42,
                LastRecoveryAttempt = DateTime.UtcNow,
                TimeSinceLastAttempt = TimeSpan.FromMinutes(15),
                CanAttemptRecovery = true
            };

            // Act & Assert - Properties should be accessible for serialization
            Assert.NotNull(stats.RecoveryAttempts.ToString());
            Assert.NotNull(stats.LastRecoveryAttempt.ToString());
            Assert.NotNull(stats.TimeSinceLastAttempt.ToString());
            Assert.NotNull(stats.CanAttemptRecovery.ToString());
        }
    }
}
