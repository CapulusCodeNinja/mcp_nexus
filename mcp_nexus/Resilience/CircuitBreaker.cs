using System.Diagnostics;

namespace mcp_nexus.Resilience
{
    /// <summary>
    /// Circuit breaker pattern implementation for handling external dependency failures
    /// </summary>
    public class CircuitBreaker : IDisposable
    {
        private readonly ILogger<CircuitBreaker> m_logger;
        private readonly int m_failureThreshold;
        private readonly TimeSpan m_timeout;
        private readonly TimeSpan m_retryTimeout;

        private int m_failureCount = 0;
        private DateTime m_lastFailureTime = DateTime.MinValue;
        private CircuitState m_state = CircuitState.Closed;
        private readonly object m_lock = new();
        private bool m_disposed = false;

        public CircuitBreaker(
            ILogger<CircuitBreaker> logger,
            int failureThreshold = 5,
            TimeSpan timeout = default,
            TimeSpan retryTimeout = default)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_failureThreshold = failureThreshold;
            m_timeout = timeout == default ? TimeSpan.FromMinutes(1) : timeout;
            m_retryTimeout = retryTimeout == default ? TimeSpan.FromMinutes(5) : retryTimeout;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "operation")
        {
            ThrowIfDisposed();

            if (m_state == CircuitState.Open)
            {
                if (DateTime.UtcNow - m_lastFailureTime < m_retryTimeout)
                {
                    m_logger.LogWarning("Circuit breaker is OPEN - operation {OperationName} blocked", operationName);
                    throw new CircuitBreakerOpenException($"Circuit breaker is open for {operationName}");
                }
                else
                {
                    m_logger.LogInformation("Circuit breaker attempting to close for {OperationName}", operationName);
                    m_state = CircuitState.HalfOpen;
                }
            }

            try
            {
                using var cts = new CancellationTokenSource(m_timeout);
                var result = await operation().WaitAsync(cts.Token);

                OnSuccess(operationName);
                return result;
            }
            catch (Exception ex) when (!(ex is CircuitBreakerOpenException))
            {
                OnFailure(operationName, ex);
                throw;
            }
        }

        public async Task ExecuteAsync(Func<Task> operation, string operationName = "operation")
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            }, operationName);
        }

        private void OnSuccess(string operationName)
        {
            lock (m_lock)
            {
                if (m_state == CircuitState.HalfOpen)
                {
                    m_logger.LogInformation("Circuit breaker closed for {OperationName}", operationName);
                    m_state = CircuitState.Closed;
                }

                m_failureCount = 0;
            }
        }

        private void OnFailure(string operationName, Exception exception)
        {
            lock (m_lock)
            {
                m_failureCount++;
                m_lastFailureTime = DateTime.UtcNow;

                m_logger.LogWarning(exception, "Circuit breaker failure #{FailureCount} for {OperationName}",
                    m_failureCount, operationName);

                if (m_failureCount >= m_failureThreshold)
                {
                    m_state = CircuitState.Open;
                    m_logger.LogError("Circuit breaker opened for {OperationName} after {FailureCount} failures",
                        operationName, m_failureCount);
                }
            }
        }

        public CircuitState GetState()
        {
            lock (m_lock)
            {
                return m_state;
            }
        }

        public void Reset()
        {
            lock (m_lock)
            {
                m_failureCount = 0;
                m_state = CircuitState.Closed;
                m_logger.LogInformation("Circuit breaker manually reset");
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CircuitBreaker));
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                m_disposed = true;
                m_logger.LogDebug("Circuit breaker disposed");
            }
        }
    }

    public enum CircuitState
    {
        Closed,    // Normal operation
        Open,      // Failing fast
        HalfOpen   // Testing if service is back
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
    }
}
