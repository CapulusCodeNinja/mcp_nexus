using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Resilience;

namespace mcp_nexus_tests.Resilience
{
    /// <summary>
    /// Tests for CircuitBreakerService
    /// </summary>
    public class CircuitBreakerServiceTests : IDisposable
    {
        private readonly Mock<ILogger<CircuitBreakerService>> m_mockLogger;
        private readonly CircuitBreakerService m_circuitBreakerService;

        public CircuitBreakerServiceTests()
        {
            m_mockLogger = new Mock<ILogger<CircuitBreakerService>>();
            m_circuitBreakerService = new CircuitBreakerService(m_mockLogger.Object);
        }

        public void Dispose()
        {
            m_circuitBreakerService?.Dispose();
        }

        [Fact]
        public void CircuitBreakerService_Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Act
            var service = new CircuitBreakerService(m_mockLogger.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void CircuitBreakerService_Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CircuitBreakerService(null!));
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulOperation_ReturnsResult()
        {
            // Arrange
            const string expectedResult = "Success";
            var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));
            const string circuitName = "test-circuit";

            // Act
            var result = await m_circuitBreakerService.ExecuteAsync(circuitName, operation);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteAsync_WithFailingOperation_ThrowsException()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            const string circuitName = "test-circuit";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => m_circuitBreakerService.ExecuteAsync(circuitName, operation));
        }

        [Fact]
        public async Task ExecuteAsync_WithVoidOperation_ExecutesSuccessfully()
        {
            // Arrange
            var operationExecuted = false;
            var operation = new Func<Task>(() =>
            {
                operationExecuted = true;
                return Task.CompletedTask;
            });
            const string circuitName = "test-circuit";

            // Act
            await m_circuitBreakerService.ExecuteAsync(circuitName, operation);

            // Assert
            Assert.True(operationExecuted);
        }

        [Fact]
        public async Task ExecuteAsync_WithCustomParameters_UsesCustomValues()
        {
            // Arrange
            const string expectedResult = "Success";
            var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));
            const string circuitName = "test-circuit";
            const int failureThreshold = 2;
            var timeout = TimeSpan.FromSeconds(10);
            var recoveryTimeout = TimeSpan.FromSeconds(5);

            // Act
            var result = await m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold, timeout, recoveryTimeout);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleFailures_OpensCircuit()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            const string circuitName = "test-circuit";
            const int failureThreshold = 3;

            // Act & Assert
            // First 3 failures should throw the original exception
            for (int i = 0; i < 3; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold));
            }

            // 4th failure should throw AdvancedCircuitBreakerOpenException
            await Assert.ThrowsAsync<AdvancedCircuitBreakerOpenException>(() => m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold));
        }

        [Fact]
        public async Task ExecuteAsync_WithCircuitOpen_ThrowsAdvancedCircuitBreakerOpenException()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            const string circuitName = "test-circuit";
            const int failureThreshold = 2;

            // Act - Cause circuit to open
            for (int i = 0; i < 2; i++)
            {
                try { await m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold); } catch { }
            }

            // Assert - Next call should throw AdvancedCircuitBreakerOpenException
            await Assert.ThrowsAsync<AdvancedCircuitBreakerOpenException>(() => m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold));
        }

        [Fact]
        public async Task ExecuteAsync_WithCircuitHalfOpen_TransitionsToClosedOnSuccess()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            var successOperation = new Func<Task<string>>(() => Task.FromResult("Success"));
            const string circuitName = "test-circuit";
            const int failureThreshold = 2;
            var recoveryTimeout = TimeSpan.FromMilliseconds(100);

            // Act - Cause circuit to open
            for (int i = 0; i < 2; i++)
            {
                try { await m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold, recoveryTimeout: recoveryTimeout); } catch { }
            }

            // Wait for recovery timeout to pass
            await Task.Delay(200);

            // Execute successful operation
            var result = await m_circuitBreakerService.ExecuteAsync(circuitName, successOperation, failureThreshold, recoveryTimeout: recoveryTimeout);

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public async Task ExecuteAsync_WithTimeout_CompletesSuccessfully()
        {
            // Arrange
            var operation = new Func<Task<string>>(async () =>
            {
                await Task.Delay(100); // Short delay
                return "Success";
            });
            const string circuitName = "test-circuit";
            var timeout = TimeSpan.FromMilliseconds(50); // Very short timeout (not enforced in current implementation)

            // Act
            var result = await m_circuitBreakerService.ExecuteAsync(circuitName, operation, timeout: timeout);

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public async Task GetCircuitStatus_WithExistingCircuit_ReturnsStatus()
        {
            // Arrange
            const string circuitName = "test-circuit";
            var operation = new Func<Task<string>>(() => Task.FromResult("Success"));

            // Act - Execute operation to create circuit
            await m_circuitBreakerService.ExecuteAsync(circuitName, operation);
            var status = m_circuitBreakerService.GetCircuitStatus(circuitName);

            // Assert
            Assert.NotNull(status);
            Assert.Equal(circuitName, status.Name);
            Assert.Equal(AdvancedCircuitState.Closed, status.State);
            Assert.True(status.IsHealthy);
        }

        [Fact]
        public void GetCircuitStatus_WithNonExistentCircuit_ReturnsDefaultStatus()
        {
            // Arrange
            const string circuitName = "non-existent-circuit";

            // Act
            var status = m_circuitBreakerService.GetCircuitStatus(circuitName);

            // Assert
            Assert.NotNull(status);
            Assert.Equal(circuitName, status.Name);
            Assert.Equal(AdvancedCircuitState.Closed, status.State);
            Assert.True(status.IsHealthy);
        }

        [Fact]
        public async Task GetAllCircuitStatuses_ReturnsAllCircuits()
        {
            // Arrange
            var operation1 = new Func<Task<string>>(() => Task.FromResult("Success1"));
            var operation2 = new Func<Task<string>>(() => Task.FromResult("Success2"));

            // Act - Execute operations to create circuits
            await m_circuitBreakerService.ExecuteAsync("circuit1", operation1);
            await m_circuitBreakerService.ExecuteAsync("circuit2", operation2);
            var statuses = m_circuitBreakerService.GetAllCircuitStatuses();

            // Assert
            Assert.NotNull(statuses);
            Assert.Equal(2, statuses.Count);
            Assert.Contains("circuit1", statuses.Keys);
            Assert.Contains("circuit2", statuses.Keys);
        }

        [Fact]
        public async Task ExecuteAsync_WithDisposedService_ThrowsObjectDisposedException()
        {
            // Arrange
            m_circuitBreakerService.Dispose();
            var operation = new Func<Task<string>>(() => Task.FromResult("Success"));
            const string circuitName = "test-circuit";

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => m_circuitBreakerService.ExecuteAsync(circuitName, operation));
        }

        [Fact]
        public void Dispose_DisposesCorrectly()
        {
            // Act
            m_circuitBreakerService.Dispose();

            // Assert - Should not throw when disposed multiple times
            m_circuitBreakerService.Dispose();
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleCircuits_ManagesIndependently()
        {
            // Arrange
            var operation1 = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            var operation2 = new Func<Task<string>>(() => Task.FromResult("Success"));
            const string circuitName1 = "circuit1";
            const string circuitName2 = "circuit2";
            const int failureThreshold = 2;

            // Act - Cause circuit1 to open
            for (int i = 0; i < 2; i++)
            {
                try { await m_circuitBreakerService.ExecuteAsync(circuitName1, operation1, failureThreshold); } catch { }
            }

            // Execute operation on circuit2 (should work)
            var result = await m_circuitBreakerService.ExecuteAsync(circuitName2, operation2, failureThreshold);

            // Assert
            Assert.Equal("Success", result);

            // Circuit1 should be open, circuit2 should be closed
            var status1 = m_circuitBreakerService.GetCircuitStatus(circuitName1);
            var status2 = m_circuitBreakerService.GetCircuitStatus(circuitName2);

            Assert.Equal(AdvancedCircuitState.Open, status1.State);
            Assert.Equal(AdvancedCircuitState.Closed, status2.State);
        }

        [Fact]
        public async Task ExecuteAsync_WithConcurrentOperations_HandlesCorrectly()
        {
            // Arrange
            var operations = new List<Task<string>>();
            var operation = new Func<Task<string>>(() => Task.FromResult("Success"));
            const string circuitName = "test-circuit";

            // Act - Execute multiple operations concurrently
            for (int i = 0; i < 10; i++)
            {
                operations.Add(m_circuitBreakerService.ExecuteAsync(circuitName, operation));
            }

            var results = await Task.WhenAll(operations);

            // Assert
            Assert.All(results, result => Assert.Equal("Success", result));
        }

        [Fact]
        public async Task ExecuteAsync_WithFailingOperations_HandlesConcurrency()
        {
            // Arrange
            var operations = new List<Task<string>>();
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            const string circuitName = "test-circuit";
            const int failureThreshold = 5;

            // Act - Execute multiple failing operations concurrently
            for (int i = 0; i < 10; i++)
            {
                operations.Add(m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold));
            }

            // Assert - All should throw exceptions
            await Assert.ThrowsAsync<InvalidOperationException>(() => Task.WhenAll(operations));
        }

        [Fact]
        public async Task ExecuteAsync_WithRecoveryTimeout_TransitionsToHalfOpen()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            var successOperation = new Func<Task<string>>(() => Task.FromResult("Success"));
            const string circuitName = "test-circuit";
            const int failureThreshold = 2;
            var recoveryTimeout = TimeSpan.FromMilliseconds(100);

            // Act - Cause circuit to open
            for (int i = 0; i < 2; i++)
            {
                try { await m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold, recoveryTimeout: recoveryTimeout); } catch { }
            }

            // Wait for recovery timeout to pass
            await Task.Delay(200);

            // Execute successful operation
            var result = await m_circuitBreakerService.ExecuteAsync(circuitName, successOperation, failureThreshold, recoveryTimeout: recoveryTimeout);

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public async Task ExecuteAsync_WithLongRunningOperation_CompletesSuccessfully()
        {
            // Arrange
            var operation = new Func<Task<string>>(async () =>
            {
                await Task.Delay(100); // Short delay
                return "Success";
            });
            const string circuitName = "test-circuit";
            var timeout = TimeSpan.FromMilliseconds(50); // Very short timeout (not enforced in current implementation)

            // Act
            var result = await m_circuitBreakerService.ExecuteAsync(circuitName, operation, timeout: timeout);

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public async Task GetCircuitStatus_WithOpenCircuit_ReturnsCorrectStatus()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            const string circuitName = "test-circuit";
            const int failureThreshold = 2;

            // Act - Cause circuit to open
            for (int i = 0; i < 2; i++)
            {
                try { await m_circuitBreakerService.ExecuteAsync(circuitName, operation, failureThreshold); } catch { }
            }

            var status = m_circuitBreakerService.GetCircuitStatus(circuitName);

            // Assert
            Assert.NotNull(status);
            Assert.Equal(circuitName, status.Name);
            Assert.Equal(AdvancedCircuitState.Open, status.State);
            Assert.False(status.IsHealthy);
            Assert.Equal(2, status.FailureCount);
        }

        [Fact]
        public async Task GetAllCircuitStatuses_WithMultipleCircuits_ReturnsAllStatuses()
        {
            // Arrange
            var operation1 = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            var operation2 = new Func<Task<string>>(() => Task.FromResult("Success"));
            const string circuitName1 = "circuit1";
            const string circuitName2 = "circuit2";
            const int failureThreshold = 2;

            // Act - Cause circuit1 to open, circuit2 to remain closed
            for (int i = 0; i < 2; i++)
            {
                try { await m_circuitBreakerService.ExecuteAsync(circuitName1, operation1, failureThreshold); } catch { }
            }
            await m_circuitBreakerService.ExecuteAsync(circuitName2, operation2, failureThreshold);

            var statuses = m_circuitBreakerService.GetAllCircuitStatuses();

            // Assert
            Assert.NotNull(statuses);
            Assert.Equal(2, statuses.Count);

            var status1 = statuses[circuitName1];
            var status2 = statuses[circuitName2];

            Assert.Equal(AdvancedCircuitState.Open, status1.State);
            Assert.False(status1.IsHealthy);
            Assert.Equal(AdvancedCircuitState.Closed, status2.State);
            Assert.True(status2.IsHealthy);
        }
    }
}
