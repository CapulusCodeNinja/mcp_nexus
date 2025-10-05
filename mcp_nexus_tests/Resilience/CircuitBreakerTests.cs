using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Resilience;

namespace mcp_nexus_tests.Resilience
{
    /// <summary>
    /// Tests for CircuitBreaker
    /// </summary>
    public class CircuitBreakerTests : IDisposable
    {
        private readonly Mock<ILogger<CircuitBreaker>> m_MockLogger;
        private readonly CircuitBreaker m_CircuitBreaker;

        public CircuitBreakerTests()
        {
            m_MockLogger = new Mock<ILogger<CircuitBreaker>>();
            m_CircuitBreaker = new CircuitBreaker(m_MockLogger.Object, failureThreshold: 3, timeout: TimeSpan.FromSeconds(5), retryTimeout: TimeSpan.FromSeconds(10));
        }

        public void Dispose()
        {
            m_CircuitBreaker?.Dispose();
        }

        [Fact]
        public void CircuitBreaker_Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var circuitBreaker = new CircuitBreaker(m_MockLogger.Object);

            // Assert
            Assert.NotNull(circuitBreaker);
        }

        [Fact]
        public void CircuitBreaker_Constructor_WithCustomParameters_InitializesCorrectly()
        {
            // Arrange
            const int failureThreshold = 5;
            var timeout = TimeSpan.FromMinutes(2);
            var retryTimeout = TimeSpan.FromMinutes(10);

            // Act
            var circuitBreaker = new CircuitBreaker(m_MockLogger.Object, failureThreshold, timeout, retryTimeout);

            // Assert
            Assert.NotNull(circuitBreaker);
        }

        [Fact]
        public void CircuitBreaker_Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CircuitBreaker(null!));
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulOperation_ReturnsResult()
        {
            // Arrange
            const string expectedResult = "Success";
            var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));

            // Act
            var result = await m_CircuitBreaker.ExecuteAsync(operation, "test-operation");

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteAsync_WithFailingOperation_ThrowsException()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));
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

            // Act
            await m_CircuitBreaker.ExecuteAsync(operation, "test-operation");

            // Assert
            Assert.True(operationExecuted);
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleFailures_OpensCircuit()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));

            // Act & Assert
            // First 3 failures should throw the original exception
            for (int i = 0; i < 3; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));
            }

            // 4th failure should throw CircuitBreakerOpenException
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));
        }

        [Fact]
        public async Task ExecuteAsync_WithCircuitOpen_ThrowsCircuitBreakerOpenException()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));

            // Act - Cause circuit to open
            for (int i = 0; i < 3; i++)
            {
                try { await m_CircuitBreaker.ExecuteAsync(operation, "test-operation"); } catch { }
            }

            // Assert - Next call should throw CircuitBreakerOpenException
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));
        }

        [Fact]
        public async Task ExecuteAsync_WithCircuitHalfOpen_TransitionsToClosedOnSuccess()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            var successOperation = new Func<Task<string>>(() => Task.FromResult("Success"));

            // Act - Cause circuit to open
            for (int i = 0; i < 3; i++)
            {
                try { await m_CircuitBreaker.ExecuteAsync(operation, "test-operation"); } catch { }
            }

            // Wait for retry timeout to pass
            await Task.Delay(11000); // Wait longer than retry timeout (10 seconds)

            // Execute successful operation
            var result = await m_CircuitBreaker.ExecuteAsync(successOperation, "test-operation");

            // Assert
            Assert.Equal("Success", result);
            Assert.Equal(CircuitState.Closed, m_CircuitBreaker.GetState());
        }

        [Fact]
        public async Task ExecuteAsync_WithTimeout_ThrowsTaskCanceledException()
        {
            // Arrange
            var operation = new Func<Task<string>>(async () =>
            {
                await Task.Delay(10000); // Longer than timeout (5 seconds)
                return "Success";
            });

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));
        }

        [Fact]
        public void GetState_Initially_ReturnsClosed()
        {
            // Act
            var state = m_CircuitBreaker.GetState();

            // Assert
            Assert.Equal(CircuitState.Closed, state);
        }

        [Fact]
        public async Task GetState_AfterFailures_ReturnsOpen()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));

            // Act - Cause circuit to open
            for (int i = 0; i < 3; i++)
            {
                try { await m_CircuitBreaker.ExecuteAsync(operation, "test-operation"); } catch { }
            }

            // Assert
            Assert.Equal(CircuitState.Open, m_CircuitBreaker.GetState());
        }

        [Fact]
        public async Task Reset_ResetsCircuitToClosed()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));

            // Act - Cause circuit to open
            for (int i = 0; i < 3; i++)
            {
                try { await m_CircuitBreaker.ExecuteAsync(operation, "test-operation"); } catch { }
            }

            Assert.Equal(CircuitState.Open, m_CircuitBreaker.GetState());

            // Reset circuit
            m_CircuitBreaker.Reset();

            // Assert
            Assert.Equal(CircuitState.Closed, m_CircuitBreaker.GetState());
        }

        [Fact]
        public async Task ExecuteAsync_AfterReset_WorksNormally()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));
            var successOperation = new Func<Task<string>>(() => Task.FromResult("Success"));

            // Act - Cause circuit to open
            for (int i = 0; i < 3; i++)
            {
                try { await m_CircuitBreaker.ExecuteAsync(operation, "test-operation"); } catch { }
            }

            // Reset circuit
            m_CircuitBreaker.Reset();

            // Execute successful operation
            var result = await m_CircuitBreaker.ExecuteAsync(successOperation, "test-operation");

            // Assert
            Assert.Equal("Success", result);
            Assert.Equal(CircuitState.Closed, m_CircuitBreaker.GetState());
        }

        [Fact]
        public void Dispose_DisposesCorrectly()
        {
            // Act
            m_CircuitBreaker.Dispose();

            // Assert - Should not throw when disposed multiple times
            m_CircuitBreaker.Dispose();
        }

        [Fact]
        public async Task ExecuteAsync_AfterDisposal_ThrowsObjectDisposedException()
        {
            // Arrange
            m_CircuitBreaker.Dispose();
            var operation = new Func<Task<string>>(() => Task.FromResult("Success"));

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));
        }

        [Fact]
        public void GetState_AfterDisposal_ReturnsState()
        {
            // Arrange
            m_CircuitBreaker.Dispose();

            // Act
            var state = m_CircuitBreaker.GetState();

            // Assert - Should still return state even after disposal
            Assert.Equal(CircuitState.Closed, state);
        }

        [Fact]
        public void Reset_AfterDisposal_Works()
        {
            // Arrange
            m_CircuitBreaker.Dispose();

            // Act & Assert - Should not throw
            m_CircuitBreaker.Reset();
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellation_ThrowsTaskCanceledException()
        {
            // Arrange
            var operation = new Func<Task<string>>(async () =>
            {
                await Task.Delay(10000); // Longer than timeout
                return "Success";
            });

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));
        }

        [Fact]
        public async Task ExecuteAsync_WithLongRunningOperation_RespectsTimeout()
        {
            // Arrange
            var operation = new Func<Task<string>>(async () =>
            {
                await Task.Delay(6000); // Longer than timeout (5 seconds)
                return "Success";
            });

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));
        }

        [Fact]
        public async Task ExecuteAsync_WithCircuitBreakerOpenException_DoesNotCountAsFailure()
        {
            // Arrange
            var operation = new Func<Task<string>>(() => throw new CircuitBreakerOpenException("Circuit is open"));

            // Act & Assert
            // CircuitBreakerOpenException should not count as a failure
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => m_CircuitBreaker.ExecuteAsync(operation, "test-operation"));

            // State should still be Closed since CircuitBreakerOpenException doesn't count as failure
            Assert.Equal(CircuitState.Closed, m_CircuitBreaker.GetState());
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleOperations_HandlesConcurrency()
        {
            // Arrange
            var operations = new List<Task<string>>();
            var operation = new Func<Task<string>>(() => Task.FromResult("Success"));

            // Act - Execute multiple operations concurrently
            for (int i = 0; i < 10; i++)
            {
                operations.Add(m_CircuitBreaker.ExecuteAsync(operation, $"operation-{i}"));
            }

            var results = await Task.WhenAll(operations);

            // Assert
            Assert.All(results, result => Assert.Equal("Success", result));
            Assert.Equal(CircuitState.Closed, m_CircuitBreaker.GetState());
        }

        [Fact]
        public async Task ExecuteAsync_WithFailingOperations_HandlesConcurrency()
        {
            // Arrange
            var operations = new List<Task<string>>();
            var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));

            // Act - Execute multiple failing operations concurrently
            for (int i = 0; i < 5; i++)
            {
                operations.Add(m_CircuitBreaker.ExecuteAsync(operation, $"operation-{i}"));
            }

            // Assert - All should throw exceptions
            await Assert.ThrowsAsync<InvalidOperationException>(() => Task.WhenAll(operations));

            // Circuit should be open after 3 failures
            Assert.Equal(CircuitState.Open, m_CircuitBreaker.GetState());
        }
    }
}
