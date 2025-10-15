namespace mcp_nexus.CommandQueue.Validation
{
    /// <summary>
    /// Interface for command validation results
    /// </summary>
    public interface ICommandValidationResult
    {
        /// <summary>Gets whether the command is valid</summary>
        bool IsValid { get; }

        /// <summary>Gets validation error messages</summary>
        IReadOnlyList<string> Errors { get; }

        /// <summary>Gets validation warnings</summary>
        IReadOnlyList<string> Warnings { get; }
    }
}
