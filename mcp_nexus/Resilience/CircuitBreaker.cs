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
        private long m_lastFailureTimeTicks = DateTime.MinValue.Ticks;
        private int m_state = (int)CircuitState.Closed; // Use int for Interlocked operations
        private bool m_disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording circuit breaker operations and errors.</param>
        /// <param name="failureThreshold">The number of consecutive failures before opening the circuit. Default is 5.</param>
        /// <param name="timeout">The timeout for individual operations. Default is 1 minute.</param>
        /// <param name="retryTimeout">The timeout for retry attempts when the circuit is half-open. Default is 5 minutes.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
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

        /// <summary>
        /// Executes an operation through the circuit breaker.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging purposes. Default is "operation".</param>
        /// <returns>A task that represents the asynchronous operation and contains the result.</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit breaker is open and the operation cannot be executed.</exception>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "operation")
        {
            ThrowIfDisposed();

            var currentState = (CircuitState)Interlocked.CompareExchange(ref m_state, m_state, m_state);

            if (currentState == CircuitState.Open)
            {
                var lastFailureTime = new DateTime(Interlocked.Read(ref m_lastFailureTimeTicks));
                if (DateTime.UtcNow - lastFailureTime < m_retryTimeout)
                {
                    m_logger.LogWarning("Circuit breaker is OPEN - operation {OperationName} blocked", operationName);
                    throw new CircuitBreakerOpenException($"Circuit breaker is open for {operationName}");
                }
                else
                {
                    m_logger.LogInformation("Circuit breaker attempting to close for {OperationName}", operationName);
                    Interlocked.Exchange(ref m_state, (int)CircuitState.HalfOpen);
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
            // Use Interlocked for lock-free state updates
            var currentState = (CircuitState)Interlocked.CompareExchange(ref m_state, m_state, m_state);

            if (currentState == CircuitState.HalfOpen)
            {
                m_logger.LogInformation("Circuit breaker closed for {OperationName}", operationName);
                Interlocked.Exchange(ref m_state, (int)CircuitState.Closed);
            }

            Interlocked.Exchange(ref m_failureCount, 0);
        }

        private void OnFailure(string operationName, Exception exception)
        {
            // Use Interlocked for lock-free updates
            var newCount = Interlocked.Increment(ref m_failureCount);
            Interlocked.Exchange(ref m_lastFailureTimeTicks, DateTime.UtcNow.Ticks);

            m_logger.LogWarning(exception, "Circuit breaker failure #{FailureCount} for {OperationName}",
                newCount, operationName);

            if (newCount >= m_failureThreshold)
            {
                Interlocked.Exchange(ref m_state, (int)CircuitState.Open);
                m_logger.LogError("Circuit breaker opened for {OperationName} after {FailureCount} failures",
                    operationName, newCount);
            }
        }

        public CircuitState GetState()
        {
            return (CircuitState)Interlocked.CompareExchange(ref m_state, m_state, m_state);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref m_failureCount, 0);
            Interlocked.Exchange(ref m_state, (int)CircuitState.Closed);
            m_logger.LogInformation("Circuit breaker manually reset");
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
