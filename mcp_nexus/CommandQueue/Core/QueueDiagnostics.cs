namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Diagnostic information about a command queue service
    /// </summary>
    public class QueueDiagnostics
    {
        /// <summary>
        /// Gets or sets the session identifier
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the service has been disposed
        /// </summary>
        public bool IsDisposed { get; set; }

        /// <summary>
        /// Gets or sets whether cancellation has been requested
        /// </summary>
        public bool IsCancellationRequested { get; set; }

        /// <summary>
        /// Gets or sets the current task status
        /// </summary>
        public string TaskStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the processing task is faulted
        /// </summary>
        public bool TaskIsFaulted { get; set; }

        /// <summary>
        /// Gets or sets the number of times the task has been restarted
        /// </summary>
        public int TaskRestartCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of task restarts allowed
        /// </summary>
        public int MaxTaskRestarts { get; set; }

        /// <summary>
        /// Gets or sets the last task exception message
        /// </summary>
        public string? LastTaskException { get; set; }

        /// <summary>
        /// Gets or sets the current queue count
        /// </summary>
        public int QueueCount { get; set; }

        /// <summary>
        /// Gets or sets whether the queue is completed
        /// </summary>
        public bool IsQueueCompleted { get; set; }

        /// <summary>
        /// Gets or sets the performance statistics
        /// </summary>
        public (long Total, long Completed, long Failed, long Cancelled) PerformanceStats { get; set; }
    }
}

