namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Interface for command information with proper encapsulation
    /// </summary>
    public interface ICommandInfo
    {
        /// <summary>Gets the command identifier</summary>
        string CommandId { get; }

        /// <summary>Gets the command text</summary>
        string Command { get; }

        /// <summary>Gets or sets the command state</summary>
        CommandState State { get; set; }

        /// <summary>Gets the queue time</summary>
        DateTime QueueTime { get; }

        /// <summary>Gets or sets the elapsed time</summary>
        TimeSpan Elapsed { get; set; }

        /// <summary>Gets or sets the remaining time</summary>
        TimeSpan Remaining { get; set; }

        /// <summary>Gets or sets the queue position</summary>
        int QueuePosition { get; set; }

        /// <summary>Gets or sets whether the command is completed</summary>
        bool IsCompleted { get; set; }

        /// <summary>
        /// Updates the elapsed and remaining time
        /// </summary>
        /// <param name="elapsed">Elapsed time</param>
        /// <param name="remaining">Remaining time</param>
        void UpdateTiming(TimeSpan elapsed, TimeSpan remaining);

        /// <summary>
        /// Marks the command as completed
        /// </summary>
        void MarkCompleted();

        /// <summary>
        /// Updates the queue position
        /// </summary>
        /// <param name="position">New queue position</param>
        void UpdateQueuePosition(int position);
    }
}
