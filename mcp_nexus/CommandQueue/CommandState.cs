namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Represents the current state of a command in the command queue.
    /// </summary>
    public enum CommandState
    {
        /// <summary>
        /// Command is queued and waiting to be executed.
        /// </summary>
        Queued,

        /// <summary>
        /// Command is currently being executed.
        /// </summary>
        Executing,

        /// <summary>
        /// Command has completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Command was cancelled before completion.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Command failed during execution.
        /// </summary>
        Failed
    }
}
