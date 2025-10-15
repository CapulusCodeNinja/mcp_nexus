using Microsoft.Extensions.Options;

namespace mcp_nexus.CommandQueue.Batching
{
    /// <summary>
    /// Filters commands to determine which ones can be batched together
    /// </summary>
    public class BatchCommandFilter
    {
        private readonly HashSet<string> m_ExcludedCommands;

        /// <summary>
        /// Initializes a new instance of the BatchCommandFilter class
        /// </summary>
        /// <param name="options">The batching configuration options</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
        public BatchCommandFilter(IOptions<BatchingConfiguration> options)
        {
            if (options?.Value == null)
                throw new ArgumentNullException(nameof(options));

            var excludedCommands = options.Value.ExcludedCommands ?? Array.Empty<string>();
            m_ExcludedCommands = new HashSet<string>(excludedCommands, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether a command can be batched with other commands
        /// </summary>
        /// <param name="command">The command to check</param>
        /// <returns>True if the command can be batched, false if it should be executed immediately</returns>
        public bool CanBatchCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            var trimmedCommand = command.Trim();

            // Check if command starts with any excluded pattern
            foreach (var excluded in m_ExcludedCommands)
            {
                if (trimmedCommand.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the list of excluded commands for debugging/logging purposes
        /// </summary>
        /// <returns>A read-only collection of excluded command patterns</returns>
        public IReadOnlyCollection<string> GetExcludedCommands()
        {
            return m_ExcludedCommands.ToList().AsReadOnly();
        }
    }
}
