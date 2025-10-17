using Microsoft.Extensions.Options;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus.CommandQueue.Batching
{
    /// <summary>
    /// Calculates appropriate timeouts for batch command execution
    /// </summary>
    public class BatchTimeoutCalculator
    {
        private readonly int m_BaseTimeoutMs;
        private readonly BatchingConfiguration m_Config;

        /// <summary>
        /// Initializes a new instance of the BatchTimeoutCalculator class
        /// </summary>
        /// <param name="baseTimeoutMs">The base timeout for single commands in milliseconds</param>
        /// <param name="options">The batching configuration options</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when baseTimeoutMs is not positive</exception>
        public BatchTimeoutCalculator(int baseTimeoutMs, IOptions<BatchingConfiguration> options)
        {
            if (options?.Value == null)
                throw new ArgumentNullException(nameof(options));

            if (baseTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseTimeoutMs), "Base timeout must be positive");

            m_BaseTimeoutMs = baseTimeoutMs;
            m_Config = options.Value;
        }

        /// <summary>
        /// Calculates the appropriate timeout for a batch of commands
        /// </summary>
        /// <param name="commands">The list of commands to be batched</param>
        /// <returns>The calculated timeout for the batch</returns>
        /// <exception cref="ArgumentNullException">Thrown when commands is null</exception>
        /// <exception cref="ArgumentException">Thrown when commands list is empty</exception>
        public TimeSpan CalculateBatchTimeout(List<QueuedCommand> commands)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            if (commands.Count == 0)
                throw new ArgumentException("Commands list cannot be empty", nameof(commands));

            // Calculate timeout: base timeout * number of commands * multiplier
            var batchTimeoutMs = (int)(m_BaseTimeoutMs * commands.Count * m_Config.BatchTimeoutMultiplier);

            // Cap at maximum configured timeout
            var maxTimeoutMs = m_Config.MaxBatchTimeoutMinutes * 60 * 1000;
            batchTimeoutMs = Math.Min(batchTimeoutMs, maxTimeoutMs);

            return TimeSpan.FromMilliseconds(batchTimeoutMs);
        }

        /// <summary>
        /// Gets the base timeout used for calculations
        /// </summary>
        /// <returns>The base timeout in milliseconds</returns>
        public int GetBaseTimeoutMs()
        {
            return m_BaseTimeoutMs;
        }

        /// <summary>
        /// Gets the maximum batch timeout from configuration
        /// </summary>
        /// <returns>The maximum batch timeout in minutes</returns>
        public int GetMaxBatchTimeoutMinutes()
        {
            return m_Config.MaxBatchTimeoutMinutes;
        }
    }
}
