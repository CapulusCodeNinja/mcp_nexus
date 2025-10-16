using mcp_nexus.Debugger;
using System.Text;

namespace mcp_nexus.CommandQueue
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

            var sb = new StringBuilder();
            sb.AppendLine(CdbSentinels.BatchStart);

            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command.Command))
                    continue;

                // Use echo commands with unique IDs to mark the start and end of each command's output within the batch
                sb.AppendLine($"echo {CdbSentinels.CommandSeparator}_{command.Id}");
                sb.AppendLine(command.Command);
                sb.AppendLine($"echo {CdbSentinels.CommandSeparator}_{command.Id}_END");
            }

            sb.AppendLine(CdbSentinels.BatchEnd);
            return sb.ToString();
        }
    }
}
