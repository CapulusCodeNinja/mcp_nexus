namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Interface for command execution results
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

        /// <summary>Gets additional result data</summary>
        IReadOnlyDictionary<string, object> Data { get; }
    }
}
