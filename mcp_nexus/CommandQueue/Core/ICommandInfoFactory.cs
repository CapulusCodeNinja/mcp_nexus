namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Factory interface for creating command information objects
    /// </summary>
    public interface ICommandInfoFactory
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
        ICommandInfo CreateCommandInfo(string commandId, string command, CommandState state,
            DateTime queueTime, int queuePosition = 0);

        /// <summary>
        /// Creates a command info instance for a queued command
        /// </summary>
        /// <param name="queuedCommand">Queued command to create info for</param>
        /// <param name="queuePosition">Queue position</param>
        /// <returns>New command info instance</returns>
        ICommandInfo CreateCommandInfoFromQueuedCommand(IQueuedCommand queuedCommand, int queuePosition = 0);
    }
}
