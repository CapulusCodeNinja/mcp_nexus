namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Factory for creating command information objects using Factory Pattern
    /// </summary>
    public class CommandInfoFactory
    {
        /// <summary>
        /// Creates a new command info instance
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <param name="command">Command text</param>
        /// <param name="state">Initial command state</param>
        /// <param name="queueTime">Queue time</param>
        /// <param name="queuePosition">Queue position (optional)</param>
        /// <returns>New command info instance</returns>
        public CommandInfo CreateCommandInfo(string commandId, string command, CommandState state,
            DateTime queueTime, int queuePosition = 0)
        {
            return new CommandInfo(commandId, command, state, queueTime, queuePosition);
        }

        /// <summary>
        /// Creates a command info instance for a queued command
        /// </summary>
        /// <param name="queuedCommand">Queued command to create info for</param>
        /// <param name="queuePosition">Queue position</param>
        /// <returns>New command info instance</returns>
        public CommandInfo CreateCommandInfoFromQueuedCommand(QueuedCommand queuedCommand, int queuePosition = 0)
        {
            return new CommandInfo(queuedCommand.Id, queuedCommand.Command, queuedCommand.State,
                queuedCommand.QueueTime, queuePosition);
        }
    }
}
