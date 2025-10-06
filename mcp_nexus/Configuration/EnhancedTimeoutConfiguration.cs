namespace mcp_nexus.Configuration
{
    /// <summary>
    /// Provides enhanced timeout configuration for complex debugging operations.
    /// Includes adaptive timeouts based on command complexity and system performance.
    /// </summary>
    public class EnhancedTimeoutConfiguration
    {
        /// <summary>
        /// Gets the base command timeout in milliseconds.
        /// </summary>
        public int BaseCommandTimeoutMs { get; }

        /// <summary>
        /// Gets the complex command timeout in milliseconds (for operations like !analyze -v).
        /// </summary>
        public int ComplexCommandTimeoutMs { get; }


        /// <summary>
        /// Gets the output reading timeout in milliseconds.
        /// </summary>
        public int OutputReadingTimeoutMs { get; }

        /// <summary>
        /// Gets the idle timeout in milliseconds.
        /// </summary>
        public int IdleTimeoutMs { get; }

        /// <summary>
        /// Gets the startup delay in milliseconds.
        /// </summary>
        public int StartupDelayMs { get; }

        /// <summary>
        /// Gets the maximum number of retries for symbol server operations.
        /// </summary>
        public int SymbolServerMaxRetries { get; }

        /// <summary>
        /// Gets a value indicating whether adaptive timeouts are enabled.
        /// </summary>
        public bool EnableAdaptiveTimeouts { get; }

        /// <summary>
        /// Gets the performance multiplier for adaptive timeouts.
        /// </summary>
        public double PerformanceMultiplier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedTimeoutConfiguration"/> class.
        /// </summary>
        /// <param name="baseCommandTimeoutMs">The base command timeout in milliseconds. Default is 600000ms (10 minutes).</param>
        /// <param name="complexCommandTimeoutMs">The complex command timeout in milliseconds. Default is 1800000ms (30 minutes).</param>
        /// <param name="outputReadingTimeoutMs">The output reading timeout in milliseconds. Default is 60000ms (1 minute).</param>
        /// <param name="idleTimeoutMs">The idle timeout in milliseconds. Default is 300000ms (5 minutes).</param>
        /// <param name="startupDelayMs">The startup delay in milliseconds. Default is 2000ms (2 seconds).</param>
        /// <param name="symbolServerMaxRetries">The maximum number of retries for symbol server operations. Default is 3.</param>
        /// <param name="enableAdaptiveTimeouts">Whether to enable adaptive timeouts based on system performance. Default is true.</param>
        /// <param name="performanceMultiplier">The performance multiplier for adaptive timeouts. Default is 1.0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any of the timeout or retry parameters are invalid.</exception>
        public EnhancedTimeoutConfiguration(
            int baseCommandTimeoutMs = 600000,
            int complexCommandTimeoutMs = 1800000,
            int outputReadingTimeoutMs = 60000,
            int idleTimeoutMs = 300000,
            int startupDelayMs = 2000,
            int symbolServerMaxRetries = 3,
            bool enableAdaptiveTimeouts = true,
            double performanceMultiplier = 1.0)
        {
            ValidateParameters(baseCommandTimeoutMs, complexCommandTimeoutMs,
                outputReadingTimeoutMs, idleTimeoutMs, startupDelayMs, symbolServerMaxRetries, performanceMultiplier);

            BaseCommandTimeoutMs = baseCommandTimeoutMs;
            ComplexCommandTimeoutMs = complexCommandTimeoutMs;
            OutputReadingTimeoutMs = outputReadingTimeoutMs;
            IdleTimeoutMs = idleTimeoutMs;
            StartupDelayMs = startupDelayMs;
            SymbolServerMaxRetries = symbolServerMaxRetries;
            EnableAdaptiveTimeouts = enableAdaptiveTimeouts;
            PerformanceMultiplier = performanceMultiplier;
        }

        /// <summary>
        /// Validates configuration parameters to ensure they are within acceptable ranges.
        /// </summary>
        /// <param name="baseCommandTimeoutMs">The base command timeout in milliseconds.</param>
        /// <param name="complexCommandTimeoutMs">The complex command timeout in milliseconds.</param>
        /// <param name="outputReadingTimeoutMs">The output reading timeout in milliseconds.</param>
        /// <param name="idleTimeoutMs">The idle timeout in milliseconds.</param>
        /// <param name="startupDelayMs">The startup delay in milliseconds.</param>
        /// <param name="symbolServerMaxRetries">The maximum number of retries for symbol server operations.</param>
        /// <param name="performanceMultiplier">The performance multiplier for adaptive timeouts.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any of the parameters are invalid.</exception>
        public static void ValidateParameters(
            int baseCommandTimeoutMs,
            int complexCommandTimeoutMs,
            int outputReadingTimeoutMs,
            int idleTimeoutMs,
            int startupDelayMs,
            int symbolServerMaxRetries,
            double performanceMultiplier)
        {
            if (baseCommandTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseCommandTimeoutMs), "Base command timeout must be positive");

            if (complexCommandTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(complexCommandTimeoutMs), "Complex command timeout must be positive");


            if (outputReadingTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputReadingTimeoutMs), "Output reading timeout must be positive");

            if (idleTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(idleTimeoutMs), "Idle timeout must be positive");

            if (startupDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(startupDelayMs), "Startup delay cannot be negative");

            if (symbolServerMaxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(symbolServerMaxRetries), "Symbol server max retries cannot be negative");

            if (performanceMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(performanceMultiplier), "Performance multiplier must be positive");

            if (complexCommandTimeoutMs < baseCommandTimeoutMs)
                throw new ArgumentOutOfRangeException(nameof(complexCommandTimeoutMs), "Complex command timeout must be greater than or equal to base command timeout");
        }

        /// <summary>
        /// Gets the appropriate timeout for a command based on its complexity.
        /// </summary>
        /// <param name="command">The command to get timeout for.</param>
        /// <returns>The timeout in milliseconds for the command.</returns>
        public int GetCommandTimeout(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return BaseCommandTimeoutMs;

            var normalizedCommand = command.Trim().ToLowerInvariant();

            // Complex commands that typically take longer
            var complexCommands = new[]
            {
                "!analyze -v",
                "!analyze",
                "!heap",
                "!address",
                "!process",
                "!thread",
                "!locks",
                "!handle",
                "!gflags",
                "!ext",
                "!sym",
                "!peb",
                "!teb"
            };

            var isComplexCommand = complexCommands.Any(cmd => normalizedCommand.StartsWith(cmd));

            var baseTimeout = isComplexCommand ? ComplexCommandTimeoutMs : BaseCommandTimeoutMs;

            if (!EnableAdaptiveTimeouts)
                return baseTimeout;

            // Apply performance multiplier for adaptive timeouts
            return (int)(baseTimeout * PerformanceMultiplier);
        }

        /// <summary>
        /// Gets the timeout for output reading operations.
        /// </summary>
        /// <returns>The output reading timeout in milliseconds.</returns>
        public int GetOutputReadingTimeout()
        {
            if (!EnableAdaptiveTimeouts)
                return OutputReadingTimeoutMs;

            return (int)(OutputReadingTimeoutMs * PerformanceMultiplier);
        }


        /// <summary>
        /// Gets the idle timeout for command execution.
        /// </summary>
        /// <returns>The idle timeout in milliseconds.</returns>
        public int GetIdleTimeout()
        {
            if (!EnableAdaptiveTimeouts)
                return IdleTimeoutMs;

            return (int)(IdleTimeoutMs * PerformanceMultiplier);
        }
    }
}
