using mcp_nexus.Debugger;
using System.Text;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus.CommandQueue.Batching
{
    /// <summary>
    /// Builds batch commands by combining multiple individual commands with separators
    /// </summary>
    public class CommandBatchBuilder
    {
        /// <summary>
        /// Creates a batch command string from a list of queued commands
        /// </summary>
        /// <param name="commands">The list of commands to batch together</param>
        /// <returns>A single command string that can be executed by CDB</returns>
        /// <exception cref="ArgumentNullException">Thrown when commands is null</exception>
        /// <exception cref="ArgumentException">Thrown when commands list is empty</exception>
        public string CreateBatchCommand(List<QueuedCommand> commands)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            if (commands.Count == 0)
                throw new ArgumentException("Commands list cannot be empty", nameof(commands));

            var commandParts = new List<string>();

            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command.Command))
                    continue;

                var upperCommandId = command.Id?.ToUpperInvariant() ?? string.Empty;
                commandParts.Add($".echo {CdbSentinels.CommandSeparator}_{upperCommandId}_START");
                commandParts.Add(command.Command);
                commandParts.Add($".echo {CdbSentinels.CommandSeparator}_{upperCommandId}_END");
            }

            return string.Join("; ", commandParts);
        }
    }
}
