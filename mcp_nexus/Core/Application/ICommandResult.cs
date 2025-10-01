namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Application layer interface for command results
    /// </summary>
    public interface ICommandResult
    {
        /// <summary>Gets whether the command executed successfully</summary>
        bool IsSuccess { get; }

        /// <summary>Gets the command output</summary>
        string Output { get; }

        /// <summary>Gets the error message if execution failed</summary>
        string? ErrorMessage { get; }

        /// <summary>Gets the execution duration</summary>
        TimeSpan Duration { get; }
    }
}
