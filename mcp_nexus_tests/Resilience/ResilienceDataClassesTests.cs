using System;
using Xunit;
using mcp_nexus.Resilience;

namespace mcp_nexus_tests.Resilience
{
    /// <summary>
    /// Tests for Resilience data classes - simple data containers
    /// </summary>
    public class ResilienceDataClassesTests
    {
        [Fact]
        public void AdvancedCircuitState_EnumValues_AreCorrect()
        {
            // Assert
            Assert.Equal(0, (int)AdvancedCircuitState.Closed);
            Assert.Equal(1, (int)AdvancedCircuitState.Open);
            Assert.Equal(2, (int)AdvancedCircuitState.HalfOpen);
        }

        [Fact]
        public void CircuitBreakerState_DefaultValues_AreCorrect()
        {
            // Act
            var state = new CircuitBreakerState();

            // Assert
            Assert.Equal(string.Empty, state.Name);
            Assert.Equal(AdvancedCircuitState.Closed, state.State);
            Assert.Equal(0, state.FailureThreshold);
            Assert.Equal(TimeSpan.Zero, state.RecoveryTimeout);
            Assert.Equal(0, state.FailureCount);
            Assert.Null(state.LastFailureTime);
            Assert.Null(state.NextAttemptTime);
        }

        [Fact]
        public void CircuitBreakerState_WithValues_SetsProperties()
        {
            // Arrange
            const string name = "test-circuit";
            const int failureThreshold = 5;
            var recoveryTimeout = TimeSpan.FromMinutes(2);
            const int failureCount = 3;
            var lastFailureTime = DateTime.UtcNow.AddMinutes(-1);
            var nextAttemptTime = DateTime.UtcNow.AddMinutes(1);

            // Act
            var state = new CircuitBreakerState
            {
                Name = name,
                State = AdvancedCircuitState.HalfOpen,
                FailureThreshold = failureThreshold,
                RecoveryTimeout = recoveryTimeout,
                FailureCount = failureCount,
                LastFailureTime = lastFailureTime,
                NextAttemptTime = nextAttemptTime
            };

            // Assert
            Assert.Equal(name, state.Name);
            Assert.Equal(AdvancedCircuitState.HalfOpen, state.State);
            Assert.Equal(failureThreshold, state.FailureThreshold);
            Assert.Equal(recoveryTimeout, state.RecoveryTimeout);
            Assert.Equal(failureCount, state.FailureCount);
            Assert.Equal(lastFailureTime, state.LastFailureTime);
            Assert.Equal(nextAttemptTime, state.NextAttemptTime);
        }

        [Theory]
        [InlineData(AdvancedCircuitState.Closed)]
        [InlineData(AdvancedCircuitState.Open)]
        [InlineData(AdvancedCircuitState.HalfOpen)]
        public void CircuitBreakerState_WithVariousStates_SetsCorrectly(AdvancedCircuitState circuitState)
        {
            // Act
            var state = new CircuitBreakerState
            {
                State = circuitState
            };

            // Assert
            Assert.Equal(circuitState, state.State);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void CircuitBreakerState_WithVariousFailureThresholds_SetsCorrectly(int failureThreshold)
        {
            // Act
            var state = new CircuitBreakerState
            {
                FailureThreshold = failureThreshold
            };

            // Assert
            Assert.Equal(failureThreshold, state.FailureThreshold);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void CircuitBreakerState_WithVariousFailureCounts_SetsCorrectly(int failureCount)
        {
            // Act
            var state = new CircuitBreakerState
            {
                FailureCount = failureCount
            };

            // Assert
            Assert.Equal(failureCount, state.FailureCount);
        }

        [Fact]
        public void CircuitBreakerState_WithNullTimes_HandlesCorrectly()
        {
            // Act
            var state = new CircuitBreakerState
            {
                LastFailureTime = null,
                NextAttemptTime = null
            };

            // Assert
            Assert.Null(state.LastFailureTime);
            Assert.Null(state.NextAttemptTime);
        }

        [Fact]
        public void CircuitBreakerState_WithFutureTimes_HandlesCorrectly()
        {
            // Arrange
            var futureTime = DateTime.UtcNow.AddHours(1);

            // Act
            var state = new CircuitBreakerState
            {
                LastFailureTime = futureTime,
                NextAttemptTime = futureTime
            };

            // Assert
            Assert.Equal(futureTime, state.LastFailureTime);
            Assert.Equal(futureTime, state.NextAttemptTime);
        }

        [Fact]
        public void CircuitBreakerState_WithPastTimes_HandlesCorrectly()
        {
            // Arrange
            var pastTime = DateTime.UtcNow.AddHours(-1);

            // Act
            var state = new CircuitBreakerState
            {
                LastFailureTime = pastTime,
                NextAttemptTime = pastTime
            };

            // Assert
            Assert.Equal(pastTime, state.LastFailureTime);
            Assert.Equal(pastTime, state.NextAttemptTime);
        }

        [Fact]
        public void CircuitBreakerStatus_DefaultValues_AreCorrect()
        {
            // Act
            var status = new CircuitBreakerStatus();

            // Assert
            Assert.Equal(string.Empty, status.Name);
            Assert.Equal(AdvancedCircuitState.Closed, status.State);
            Assert.Equal(0, status.FailureCount);
            Assert.Null(status.LastFailureTime);
            Assert.Null(status.NextAttemptTime);
            Assert.False(status.IsHealthy);
        }

        [Fact]
        public void CircuitBreakerStatus_WithValues_SetsProperties()
        {
            // Arrange
            const string name = "status-circuit";
            const int failureCount = 2;
            var lastFailureTime = DateTime.UtcNow.AddMinutes(-5);
            var nextAttemptTime = DateTime.UtcNow.AddMinutes(5);

            // Act
            var status = new CircuitBreakerStatus
            {
                Name = name,
                State = AdvancedCircuitState.Open,
                FailureCount = failureCount,
                LastFailureTime = lastFailureTime,
                NextAttemptTime = nextAttemptTime,
                IsHealthy = false
            };

            // Assert
            Assert.Equal(name, status.Name);
            Assert.Equal(AdvancedCircuitState.Open, status.State);
            Assert.Equal(failureCount, status.FailureCount);
            Assert.Equal(lastFailureTime, status.LastFailureTime);
            Assert.Equal(nextAttemptTime, status.NextAttemptTime);
            Assert.False(status.IsHealthy);
        }

        [Fact]
        public void CircuitBreakerStatus_WithHealthyState_SetsCorrectly()
        {
            // Act
            var status = new CircuitBreakerStatus
            {
                State = AdvancedCircuitState.Closed,
                IsHealthy = true
            };

            // Assert
            Assert.Equal(AdvancedCircuitState.Closed, status.State);
            Assert.True(status.IsHealthy);
        }

        [Fact]
        public void CircuitBreakerStatus_WithUnhealthyState_SetsCorrectly()
        {
            // Act
            var status = new CircuitBreakerStatus
            {
                State = AdvancedCircuitState.Open,
                IsHealthy = false
            };

            // Assert
            Assert.Equal(AdvancedCircuitState.Open, status.State);
            Assert.False(status.IsHealthy);
        }

        [Theory]
        [InlineData(AdvancedCircuitState.Closed, true)]
        [InlineData(AdvancedCircuitState.Open, false)]
        [InlineData(AdvancedCircuitState.HalfOpen, false)]
        public void CircuitBreakerStatus_WithVariousStatesAndHealth_SetsCorrectly(AdvancedCircuitState state, bool expectedHealthy)
        {
            // Act
            var status = new CircuitBreakerStatus
            {
                State = state,
                IsHealthy = expectedHealthy
            };

            // Assert
            Assert.Equal(state, status.State);
            Assert.Equal(expectedHealthy, status.IsHealthy);
        }

        [Fact]
        public void CircuitBreakerStatus_WithNullTimes_HandlesCorrectly()
        {
            // Act
            var status = new CircuitBreakerStatus
            {
                LastFailureTime = null,
                NextAttemptTime = null
            };

            // Assert
            Assert.Null(status.LastFailureTime);
            Assert.Null(status.NextAttemptTime);
        }

        [Fact]
        public void AllResilienceClasses_CanBeInstantiated()
        {
            // Act & Assert - Just verify they can be created without throwing
            Assert.NotNull(new CircuitBreakerState());
            Assert.NotNull(new CircuitBreakerStatus());
        }

        [Fact]
        public void CircuitBreakerState_CanBeModifiedAfterCreation()
        {
            // Arrange
            var state = new CircuitBreakerState();

            // Act
            state.Name = "modified-circuit";
            state.State = AdvancedCircuitState.Open;
            state.FailureCount = 10;

            // Assert
            Assert.Equal("modified-circuit", state.Name);
            Assert.Equal(AdvancedCircuitState.Open, state.State);
            Assert.Equal(10, state.FailureCount);
        }

        [Fact]
        public void CircuitBreakerStatus_CanBeModifiedAfterCreation()
        {
            // Arrange
            var status = new CircuitBreakerStatus();

            // Act
            status.Name = "modified-status";
            status.State = AdvancedCircuitState.HalfOpen;
            status.IsHealthy = true;

            // Assert
            Assert.Equal("modified-status", status.Name);
            Assert.Equal(AdvancedCircuitState.HalfOpen, status.State);
            Assert.True(status.IsHealthy);
        }

        [Fact]
        public void CircuitBreakerState_WithMinMaxValues_HandlesCorrectly()
        {
            // Act
            var state = new CircuitBreakerState
            {
                FailureThreshold = int.MaxValue,
                FailureCount = int.MaxValue,
                RecoveryTimeout = TimeSpan.MaxValue
            };

            // Assert
            Assert.Equal(int.MaxValue, state.FailureThreshold);
            Assert.Equal(int.MaxValue, state.FailureCount);
            Assert.Equal(TimeSpan.MaxValue, state.RecoveryTimeout);
        }

        [Fact]
        public void CircuitBreakerStatus_WithMinMaxValues_HandlesCorrectly()
        {
            // Act
            var status = new CircuitBreakerStatus
            {
                FailureCount = int.MaxValue
            };

            // Assert
            Assert.Equal(int.MaxValue, status.FailureCount);
        }
    }
}
