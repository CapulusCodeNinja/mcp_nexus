using System.ComponentModel.DataAnnotations;

namespace mcp_nexus.CommandQueue.Batching
{
    /// <summary>
    /// Configuration for command batching functionality
    /// </summary>
    public class BatchingConfiguration
    {
        /// <summary>
        /// Gets or sets the session identifier for this batching configuration
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether command batching is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of commands to batch together
        /// </summary>
        [Range(1, 10, ErrorMessage = "MaxBatchSize must be between 1 and 10")]
        public int MaxBatchSize { get; set; } = 5;

        /// <summary>
        /// Gets or sets the timeout in milliseconds to wait for more commands before executing a partial batch
        /// </summary>
        [Range(100, 10000, ErrorMessage = "BatchWaitTimeoutMs must be between 100 and 10000 milliseconds")]
        public int BatchWaitTimeoutMs { get; set; } = 2000;

        /// <summary>
        /// Gets or sets the multiplier for batch timeout calculation
        /// </summary>
        [Range(0.1, 5.0, ErrorMessage = "BatchTimeoutMultiplier must be between 0.1 and 5.0")]
        public double BatchTimeoutMultiplier { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the maximum batch timeout in minutes
        /// </summary>
        [Range(1, 60, ErrorMessage = "MaxBatchTimeoutMinutes must be between 1 and 60 minutes")]
        public int MaxBatchTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets the list of commands that should never be batched
        /// </summary>
        public string[] ExcludedCommands { get; set; } = new[]
        {
            "!analyze",
            "!dump",
            "!heap",
            "!memusage",
            "!runaway",
            "~*k",
            "!locks",
            "!cs",
            "!gchandles"
        };
    }
}
