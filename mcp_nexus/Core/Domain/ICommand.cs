namespace mcp_nexus.Core.Domain
{
    /// <summary>
    /// Core domain interface for command - no external dependencies
    /// </summary>
    public interface ICommand
    {
        /// <summary>Gets the command identifier</summary>
        string CommandId { get; }

        /// <summary>Gets the command text</summary>
        string CommandText { get; }

        /// <summary>Gets the command creation time</summary>
        DateTime CreatedAt { get; }

        /// <summary>Gets the current command state</summary>
        CommandState State { get; }

        /// <summary>Gets whether the command is completed</summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Updates the command state
        /// </summary>
        /// <param name="state">New state</param>
        void UpdateState(CommandState state);

        /// <summary>
        /// Marks the command as completed
        /// </summary>
        void MarkCompleted();
    }
}
