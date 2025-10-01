namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Command interface using Command Pattern
    /// </summary>
    public interface ICommand
    {
        /// <summary>Gets the command identifier</summary>
        string CommandId { get; }

        /// <summary>Gets the command name</summary>
        string CommandName { get; }

        /// <summary>Gets the command description</summary>
        string Description { get; }

        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Command execution result</returns>
        Task<ICommandResult> ExecuteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the command before execution
        /// </summary>
        /// <returns>Validation result</returns>
        ICommandValidationResult Validate();
    }
}
