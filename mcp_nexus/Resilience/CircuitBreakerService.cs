using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Resilience
{
    /// <summary>
    /// Advanced circuit breaker pattern implementation for fault tolerance
    /// </summary>
    public class CircuitBreakerService : IDisposable
    {
        #region Private Fields

        private readonly ILogger<CircuitBreakerService> m_logger;
        private readonly ConcurrentDictionary<string, CircuitBreakerState> m_circuits = new();
        private readonly Timer m_monitoringTimer;
        private volatile bool m_disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the CircuitBreakerService class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Monitor circuits every 10 seconds
            m_monitoringTimer = new Timer(MonitorCircuits, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            m_logger.LogInformation("🔧 CircuitBreakerService initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes an operation with circuit breaker protection
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="circuitName">The name of the circuit</param>
        /// <param name="operation">The operation to execute</param>
        /// <param name="failureThreshold">The number of failures before opening the circuit</param>
        /// <param name="timeout">The operation timeout</param>
        /// <param name="recoveryTimeout">The time to wait before attempting recovery</param>
        /// <returns>The result of the operation</returns>
        public async Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> operation,
            int failureThreshold = 5, TimeSpan? timeout = null, TimeSpan? recoveryTimeout = null)
        {
            if (m_disposed) throw new ObjectDisposedException(nameof(CircuitBreakerService));

            var circuit = GetOrCreateCircuit(circuitName, failureThreshold, recoveryTimeout ?? TimeSpan.FromMinutes(1));
            var timeoutValue = timeout ?? TimeSpan.FromSeconds(30);

            return await ExecuteWithCircuitBreaker(circuit, operation, timeoutValue);
        }

        public async Task ExecuteAsync(string circuitName, Func<Task> operation,
            int failureThreshold = 5, TimeSpan? timeout = null, TimeSpan? recoveryTimeout = null)
        {
            if (m_disposed) throw new ObjectDisposedException(nameof(CircuitBreakerService));

            var circuit = GetOrCreateCircuit(circuitName, failureThreshold, recoveryTimeout ?? TimeSpan.FromMinutes(1));
            var timeoutValue = timeout ?? TimeSpan.FromSeconds(30);

            await ExecuteWithCircuitBreaker(circuit, async () =>
            {
                await operation();
                return true; // Dummy return for void operations
            }, timeoutValue);
        }

        /// <summary>
        /// Gets an existing circuit breaker or creates a new one for the specified circuit name.
        /// </summary>
        /// <param name="circuitName">The name of the circuit breaker.</param>
        /// <param name="failureThreshold">The number of failures before opening the circuit.</param>
        /// <param name="recoveryTimeout">The timeout before attempting to close the circuit.</param>
        /// <returns>The circuit breaker state for the specified circuit name.</returns>
        private CircuitBreakerState GetOrCreateCircuit(string circuitName, int failureThreshold, TimeSpan recoveryTimeout)
        {
            return m_circuits.GetOrAdd(circuitName, _ => new CircuitBreakerState
            {
                Name = circuitName,
                FailureThreshold = failureThreshold,
                RecoveryTimeout = recoveryTimeout,
                State = AdvancedCircuitState.Closed,
                FailureCount = 0,
                LastFailureTime = null,
                NextAttemptTime = null
            });
        }

        /// <summary>
        /// Executes an operation with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="circuit">The circuit breaker state.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="timeout">The timeout for the operation.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="AdvancedCircuitBreakerOpenException">Thrown when the circuit is open.</exception>
        private async Task<T> ExecuteWithCircuitBreaker<T>(CircuitBreakerState circuit, Func<Task<T>> operation, TimeSpan timeout)
        {
            // Check if circuit is open and should remain open
            if (circuit.State == AdvancedCircuitState.Open)
            {
                if (DateTime.UtcNow < circuit.NextAttemptTime)
                {
                    throw new AdvancedCircuitBreakerOpenException($"Circuit '{circuit.Name}' is open. Next attempt allowed at {circuit.NextAttemptTime}");
                }

                // Try to transition to half-open
                circuit.State = AdvancedCircuitState.HalfOpen;
                m_logger.LogInformation("🔧 Circuit '{CircuitName}' transitioning to Half-Open", circuit.Name);
            }

            try
            {
                using var cts = new CancellationTokenSource(timeout);
                var result = await operation();

                // Success - reset circuit if it was half-open
                if (circuit.State == AdvancedCircuitState.HalfOpen)
                {
                    circuit.State = AdvancedCircuitState.Closed;
                    circuit.FailureCount = 0;
                    circuit.LastFailureTime = null;
                    circuit.NextAttemptTime = null;
                    m_logger.LogInformation("🔧 Circuit '{CircuitName}' reset to Closed after successful operation", circuit.Name);
                }

                return result;
            }
            catch (Exception ex)
            {
                // Record failure
                circuit.FailureCount++;
                circuit.LastFailureTime = DateTime.UtcNow;

                m_logger.LogWarning(ex, "🔧 Circuit '{CircuitName}' recorded failure #{FailureCount}",
                    circuit.Name, circuit.FailureCount);

                // Check if we should open the circuit
                if (circuit.FailureCount >= circuit.FailureThreshold)
                {
                    circuit.State = AdvancedCircuitState.Open;
                    circuit.NextAttemptTime = DateTime.UtcNow.Add(circuit.RecoveryTimeout);

                    m_logger.LogError("🔧 Circuit '{CircuitName}' opened after {FailureCount} failures. Next attempt at {NextAttemptTime}",
                        circuit.Name, circuit.FailureCount, circuit.NextAttemptTime);
                }

                throw;
            }
        }

        /// <summary>
        /// Monitors all circuit breakers and performs periodic maintenance.
        /// </summary>
        /// <param name="state">The timer state (unused).</param>
        private void MonitorCircuits(object? state)
        {
            if (m_disposed) return;

            try
            {
                var now = DateTime.UtcNow;
                var circuitsToLog = new List<string>();

                foreach (var circuit in m_circuits.Values)
                {
                    if (circuit.State == AdvancedCircuitState.Open && now >= circuit.NextAttemptTime)
                    {
                        circuit.State = AdvancedCircuitState.HalfOpen;
                        circuitsToLog.Add(circuit.Name);
                    }
                }

                if (circuitsToLog.Count > 0)
                {
                    m_logger.LogInformation("🔧 Circuits transitioning to Half-Open: {Circuits}", string.Join(", ", circuitsToLog));
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error monitoring circuits");
            }
        }

        /// <summary>
        /// Gets the current status of a specific circuit breaker.
        /// </summary>
        /// <param name="circuitName">The name of the circuit breaker to check.</param>
        /// <returns>The current status of the circuit breaker, or null if not found.</returns>
        public CircuitBreakerStatus GetCircuitStatus(string circuitName)
        {
            if (m_circuits.TryGetValue(circuitName, out var circuit))
            {
                return new CircuitBreakerStatus
                {
                    Name = circuit.Name,
                    State = circuit.State,
                    FailureCount = circuit.FailureCount,
                    LastFailureTime = circuit.LastFailureTime,
                    NextAttemptTime = circuit.NextAttemptTime,
                    IsHealthy = circuit.State == AdvancedCircuitState.Closed
                };
            }

            return new CircuitBreakerStatus { Name = circuitName, State = AdvancedCircuitState.Closed, IsHealthy = true };
        }

        /// <summary>
        /// Gets the status of all circuit breakers.
        /// </summary>
        /// <returns>
        /// A dictionary containing the status of all circuit breakers, keyed by circuit name.
        /// </returns>
        public Dictionary<string, CircuitBreakerStatus> GetAllCircuitStatuses()
        {
            return m_circuits.ToDictionary(
                kvp => kvp.Key,
                kvp => new CircuitBreakerStatus
                {
                    Name = kvp.Value.Name,
                    State = kvp.Value.State,
                    FailureCount = kvp.Value.FailureCount,
                    LastFailureTime = kvp.Value.LastFailureTime,
                    NextAttemptTime = kvp.Value.NextAttemptTime,
                    IsHealthy = kvp.Value.State == AdvancedCircuitState.Closed
                });
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the circuit breaker service.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            m_monitoringTimer?.Dispose();
            m_logger.LogInformation("🔧 CircuitBreakerService disposed");
        }

        #endregion
    }

    #region Enums and Data Classes

    /// <summary>
    /// Represents the state of a circuit breaker
    /// </summary>
    public enum AdvancedCircuitState
    {
        Closed,    // Normal operation
        Open,      // Circuit is open, requests fail fast
        HalfOpen   // Testing if service has recovered
    }

    /// <summary>
    /// Represents the internal state of a circuit breaker
    /// </summary>
    public class CircuitBreakerState
    {
        /// <summary>
        /// The name of the circuit
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The current state of the circuit
        /// </summary>
        public AdvancedCircuitState State { get; set; }

        /// <summary>
        /// The number of failures before opening the circuit
        /// </summary>
        public int FailureThreshold { get; set; }

        /// <summary>
        /// The time to wait before attempting recovery
        /// </summary>
        public TimeSpan RecoveryTimeout { get; set; }

        /// <summary>
        /// The current failure count
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// The time of the last failure
        /// </summary>
        public DateTime? LastFailureTime { get; set; }

        /// <summary>
        /// The time when the next attempt is allowed
        /// </summary>
        public DateTime? NextAttemptTime { get; set; }
    }

    /// <summary>
    /// Represents the status of a circuit breaker
    /// </summary>
    public class CircuitBreakerStatus
    {
        /// <summary>
        /// The name of the circuit
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The current state of the circuit
        /// </summary>
        public AdvancedCircuitState State { get; set; }

        /// <summary>
        /// The current failure count
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// The time of the last failure
        /// </summary>
        public DateTime? LastFailureTime { get; set; }

        /// <summary>
        /// The time when the next attempt is allowed
        /// </summary>
        public DateTime? NextAttemptTime { get; set; }

        /// <summary>
        /// Whether the circuit is healthy
        /// </summary>
        public bool IsHealthy { get; set; }
    }

    /// <summary>
    /// Exception thrown when a circuit breaker is open
    /// </summary>
    public class AdvancedCircuitBreakerOpenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the AdvancedCircuitBreakerOpenException class
        /// </summary>
        /// <param name="message">The error message</param>
        public AdvancedCircuitBreakerOpenException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the AdvancedCircuitBreakerOpenException class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public AdvancedCircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}
