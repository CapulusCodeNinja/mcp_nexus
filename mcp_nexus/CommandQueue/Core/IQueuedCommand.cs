namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Interface for queued command with proper encapsulation
    /// </summary>
    public interface IQueuedCommand : IDisposable
    {
        /// <summary>Gets the command identifier</summary>
        string Id { get; }

        /// <summary>Gets the command text</summary>
        string Command { get; }

        /// <summary>Gets the queue time</summary>
        DateTime QueueTime { get; }

        /// <summary>Gets the completion source</summary>
        TaskCompletionSource<string> CompletionSource { get; }

        /// <summary>Gets the cancellation token source</summary>
        CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>Gets or sets the command state</summary>
        CommandState State { get; set; }

        /// <summary>Gets whether the command is disposed</summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Updates the command state
        /// </summary>
        /// <param name="newState">New command state</param>
        void UpdateState(CommandState newState);

        /// <summary>
        /// Cancels the command
        /// </summary>
        void Cancel();

        /// <summary>
        /// Sets the result of the command
        /// </summary>
        /// <param name="result">Command result</param>
        void SetResult(string result);

        /// <summary>
        /// Sets an exception for the command
        /// </summary>
        /// <param name="exception">Exception to set</param>
        void SetException(Exception exception);
    }
}
