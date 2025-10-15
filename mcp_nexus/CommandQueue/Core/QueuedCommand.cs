namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Represents a queued command with proper encapsulation
    /// </summary>
    public class QueuedCommand : IDisposable
    {
        #region Private Fields

        private readonly string? m_Id;
        private readonly string? m_Command;
        private readonly DateTime m_QueueTime;
        private readonly TaskCompletionSource<string>? m_CompletionSource;
        private readonly CancellationTokenSource? m_CancellationTokenSource;
        private CommandState m_State;
        private volatile bool m_Disposed = false;

        #endregion

        #region Public Properties

        /// <summary>Gets the command identifier</summary>
        public string? Id => m_Id;

        /// <summary>Gets the command text</summary>
        public string? Command => m_Command;

        /// <summary>Gets the queue time</summary>
        public DateTime QueueTime => m_QueueTime;

        /// <summary>Gets the completion source</summary>
        public TaskCompletionSource<string>? CompletionSource => m_CompletionSource;

        /// <summary>Gets the cancellation token source</summary>
        public CancellationTokenSource? CancellationTokenSource => m_CancellationTokenSource;

        /// <summary>Gets or sets the command state</summary>
        public CommandState State
        {
            get => m_State;
            set => m_State = value;
        }

        /// <summary>Gets whether the command is disposed</summary>
        public bool IsDisposed => m_Disposed;

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
            m_Id = id ?? throw new ArgumentNullException(nameof(id));
            m_Command = command ?? throw new ArgumentNullException(nameof(command));
            m_QueueTime = queueTime;
            m_CompletionSource = completionSource ?? throw new ArgumentNullException(nameof(completionSource));
            m_CancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
            m_State = state;
        }


        /// <summary>
        /// Initializes a new queued command with nullable parameters (for test compatibility - allows nulls including ID)
        /// </summary>
        /// <param name="id">Command identifier (can be null)</param>
        /// <param name="command">Command text (can be null)</param>
        /// <param name="queueTime">Queue time</param>
        /// <param name="completionSource">Completion source (can be null)</param>
        /// <param name="cancellationTokenSource">Cancellation token source (can be null)</param>
        public QueuedCommand(string? id, string? command, DateTime queueTime,
            TaskCompletionSource<string>? completionSource, CancellationTokenSource? cancellationTokenSource)
        {
            m_Id = id; // Allow null for test compatibility
            m_Command = command; // Allow null for test compatibility
            m_QueueTime = queueTime;
            m_CompletionSource = completionSource; // Allow null for test compatibility
            m_CancellationTokenSource = cancellationTokenSource; // Allow null for test compatibility
            m_State = CommandState.Queued;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the command state
        /// </summary>
        /// <param name="newState">New command state</param>
        public void UpdateState(CommandState newState)
        {
            if (m_Disposed) return;
            m_State = newState;
        }

        /// <summary>
        /// Cancels the command
        /// </summary>
        public void Cancel()
        {
            if (m_Disposed) return;
            m_CancellationTokenSource?.Cancel();
            m_State = CommandState.Cancelled;
        }

        /// <summary>
        /// Sets the result of the command
        /// </summary>
        /// <param name="result">Command result</param>
        public void SetResult(string result)
        {
            if (m_Disposed) return;
            m_CompletionSource?.SetResult(result);
            m_State = CommandState.Completed;
        }

        /// <summary>
        /// Sets an exception for the command
        /// </summary>
        /// <param name="exception">Exception to set</param>
        public void SetException(Exception exception)
        {
            if (m_Disposed) return;
            m_CompletionSource?.SetException(exception);
            m_State = CommandState.Failed;
        }

        #endregion

        #region Equality Implementation

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is QueuedCommand other)
            {
                return m_Id == other.m_Id &&
                       m_Command == other.m_Command &&
                       m_QueueTime == other.m_QueueTime &&
                       m_State == other.m_State &&
                       ReferenceEquals(m_CompletionSource, other.m_CompletionSource) &&
                       ReferenceEquals(m_CancellationTokenSource, other.m_CancellationTokenSource);
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code for the current object
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(m_Id, m_Command, m_QueueTime, m_State, m_CompletionSource, m_CancellationTokenSource);
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        public static bool operator ==(QueuedCommand? left, QueuedCommand? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        public static bool operator !=(QueuedCommand? left, QueuedCommand? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns a string representation of the queued command
        /// </summary>
        public override string ToString()
        {
            return $"QueuedCommand(Id={m_Id}, Command={m_Command}, State={m_State}, QueueTime={m_QueueTime:yyyy-MM-dd HH:mm:ss})";
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the queued command
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;

            try
            {
                m_CancellationTokenSource?.Dispose();
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
                id ?? m_Id ?? string.Empty,
                command ?? m_Command ?? string.Empty,
                queueTime ?? m_QueueTime,
                completionSource ?? m_CompletionSource ?? new TaskCompletionSource<string>(),
                cancellationTokenSource ?? m_CancellationTokenSource ?? new CancellationTokenSource(),
                state ?? m_State
            );
        }

        /// <summary>
        /// Creates a copy of this command with updated state (for record-like behavior)
        /// </summary>
        public QueuedCommand WithState(CommandState newState)
        {
            return new QueuedCommand(
                m_Id ?? string.Empty,
                m_Command ?? string.Empty,
                m_QueueTime,
                m_CompletionSource ?? new TaskCompletionSource<string>(),
                m_CancellationTokenSource ?? new CancellationTokenSource(),
                newState);
        }

        /// <summary>
        /// Creates a copy of this command with updated completion source (for record-like behavior)
        /// </summary>
        public QueuedCommand WithCompletionSource(TaskCompletionSource<string> newCompletionSource)
        {
            return new QueuedCommand(
                m_Id ?? string.Empty,
                m_Command ?? string.Empty,
                m_QueueTime,
                newCompletionSource,
                m_CancellationTokenSource ?? new CancellationTokenSource(),
                m_State);
        }

        #endregion
    }
}
