namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Represents a queued command with proper encapsulation
    /// </summary>
    public class QueuedCommand : IDisposable
    {
        #region Private Fields

        private readonly string m_id;
        private readonly string m_command;
        private readonly DateTime m_queueTime;
        private readonly TaskCompletionSource<string> m_completionSource;
        private readonly CancellationTokenSource m_cancellationTokenSource;
        private CommandState m_state;
        private volatile bool m_disposed = false;

        #endregion

        #region Public Properties

        /// <summary>Gets the command identifier</summary>
        public string Id => m_id;

        /// <summary>Gets the command text</summary>
        public string Command => m_command;

        /// <summary>Gets the queue time</summary>
        public DateTime QueueTime => m_queueTime;

        /// <summary>Gets the completion source</summary>
        public TaskCompletionSource<string> CompletionSource => m_completionSource;

        /// <summary>Gets the cancellation token source</summary>
        public CancellationTokenSource CancellationTokenSource => m_cancellationTokenSource;

        /// <summary>Gets or sets the command state</summary>
        public CommandState State
        {
            get => m_state;
            set => m_state = value;
        }

        /// <summary>Gets whether the command is disposed</summary>
        public bool IsDisposed => m_disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new queued command
        /// </summary>
        /// <param name="id">Command identifier</param>
        /// <param name="command">Command text</param>
        /// <param name="queueTime">Queue time</param>
        /// <param name="completionSource">Completion source</param>
        /// <param name="cancellationTokenSource">Cancellation token source</param>
        /// <param name="state">Initial command state</param>
        public QueuedCommand(string id, string command, DateTime queueTime,
            TaskCompletionSource<string> completionSource, CancellationTokenSource cancellationTokenSource,
            CommandState state = CommandState.Queued)
        {
            m_id = id ?? throw new ArgumentNullException(nameof(id));
            m_command = command ?? throw new ArgumentNullException(nameof(command));
            m_queueTime = queueTime;
            m_completionSource = completionSource ?? throw new ArgumentNullException(nameof(completionSource));
            m_cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
            m_state = state;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the command state
        /// </summary>
        /// <param name="newState">New command state</param>
        public void UpdateState(CommandState newState)
        {
            if (m_disposed) return;
            m_state = newState;
        }

        /// <summary>
        /// Cancels the command
        /// </summary>
        public void Cancel()
        {
            if (m_disposed) return;
            m_cancellationTokenSource.Cancel();
            m_state = CommandState.Cancelled;
        }

        /// <summary>
        /// Sets the result of the command
        /// </summary>
        /// <param name="result">Command result</param>
        public void SetResult(string result)
        {
            if (m_disposed) return;
            m_completionSource.SetResult(result);
            m_state = CommandState.Completed;
        }

        /// <summary>
        /// Sets an exception for the command
        /// </summary>
        /// <param name="exception">Exception to set</param>
        public void SetException(Exception exception)
        {
            if (m_disposed) return;
            m_completionSource.SetException(exception);
            m_state = CommandState.Failed;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the queued command
        /// </summary>
        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            try
            {
                m_cancellationTokenSource?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        /// <summary>
        /// Creates a copy of this command with updated values (for record-like behavior)
        /// </summary>
        public QueuedCommand With(string? id = null, string? command = null, DateTime? queueTime = null, TaskCompletionSource<string>? completionSource = null, CancellationTokenSource? cancellationTokenSource = null, CommandState? state = null)
        {
            return new QueuedCommand(
                id ?? m_id,
                command ?? m_command,
                queueTime ?? m_queueTime,
                completionSource ?? m_completionSource,
                cancellationTokenSource ?? m_cancellationTokenSource,
                state ?? m_state
            );
        }

        /// <summary>
        /// Creates a copy of this command with updated state (for record-like behavior)
        /// </summary>
        public QueuedCommand WithState(CommandState newState)
        {
            return new QueuedCommand(m_id, m_command, m_queueTime, m_completionSource, m_cancellationTokenSource, newState);
        }

        /// <summary>
        /// Creates a copy of this command with updated completion source (for record-like behavior)
        /// </summary>
        public QueuedCommand WithCompletionSource(TaskCompletionSource<string> newCompletionSource)
        {
            return new QueuedCommand(m_id, m_command, m_queueTime, newCompletionSource, m_cancellationTokenSource, m_state);
        }

        #endregion
    }
}
