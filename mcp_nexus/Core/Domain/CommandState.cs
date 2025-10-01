namespace mcp_nexus.Core.Domain
{
    /// <summary>
    /// Represents the state of a command
    /// </summary>
    public enum CommandState
    {
        /// <summary>Command is queued</summary>
        Queued,
        /// <summary>Command is executing</summary>
        Executing,
        /// <summary>Command completed successfully</summary>
        Completed,
        /// <summary>Command failed</summary>
        Failed,
        /// <summary>Command was cancelled</summary>
        Cancelled
    }
}
