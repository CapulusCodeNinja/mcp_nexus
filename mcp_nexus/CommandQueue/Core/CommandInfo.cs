namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Detailed command information for type-safe status checking - properly encapsulated
    /// </summary>
    public class CommandInfo
    {
        #region Private Fields

        private readonly string m_CommandId;
        private readonly string m_Command;
        private CommandState m_State;
        private readonly DateTime m_QueueTime;
        private TimeSpan m_Elapsed;
        private TimeSpan m_Remaining;
        private int m_QueuePosition;
        private bool m_IsCompleted;

        #endregion

        #region Public Properties

        /// <summary>Gets or sets the command identifier</summary>
        public string CommandId { get => m_CommandId; set { } } // Read-only from external perspective

        /// <summary>Gets or sets the command text</summary>
        public string Command { get => m_Command; set { } } // Read-only from external perspective

        /// <summary>Gets or sets the command state</summary>
        public CommandState State
        {
            get => m_State;
            set => m_State = value;
        }

        /// <summary>Gets or sets the queue time</summary>
        public DateTime QueueTime { get => m_QueueTime; set { } } // Read-only from external perspective

        /// <summary>Gets or sets the elapsed time</summary>
        public TimeSpan Elapsed
        {
            get => m_Elapsed;
            set => m_Elapsed = value;
        }

        /// <summary>Gets or sets the remaining time</summary>
        public TimeSpan Remaining
        {
            get => m_Remaining;
            set => m_Remaining = value;
        }

        /// <summary>Gets or sets the queue position</summary>
        public int QueuePosition
        {
            get => m_QueuePosition;
            set => m_QueuePosition = value;
        }

        /// <summary>Gets or sets whether the command is completed</summary>
        public bool IsCompleted
        {
            get => m_IsCompleted;
            set => m_IsCompleted = value;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new command info instance with default values
        /// </summary>
        public CommandInfo()
        {
            m_CommandId = string.Empty;
            m_Command = string.Empty;
            m_State = CommandState.Queued;
            m_QueueTime = DateTime.Now;
            m_QueuePosition = 0;
            m_Elapsed = TimeSpan.Zero;
            m_Remaining = TimeSpan.Zero;
            m_IsCompleted = false;
        }

        /// <summary>
        /// Initializes a new command info instance
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <param name="command">Command text</param>
        /// <param name="state">Initial command state</param>
        /// <param name="queueTime">Queue time</param>
        /// <param name="queuePosition">Queue position</param>
        public CommandInfo(string commandId, string command, CommandState state, DateTime queueTime, int queuePosition = 0)
        {
            m_CommandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
            m_Command = command ?? throw new ArgumentNullException(nameof(command));
            m_State = state;
            m_QueueTime = queueTime;
            m_QueuePosition = queuePosition;
            m_Elapsed = TimeSpan.Zero;
            m_Remaining = TimeSpan.Zero;
            m_IsCompleted = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the elapsed and remaining time
        /// </summary>
        /// <param name="elapsed">Elapsed time</param>
        /// <param name="remaining">Remaining time</param>
        public void UpdateTiming(TimeSpan elapsed, TimeSpan remaining)
        {
            m_Elapsed = elapsed;
            m_Remaining = remaining;
        }

        /// <summary>
        /// Marks the command as completed.
        /// </summary>
        public void MarkCompleted()
        {
            m_IsCompleted = true;
            m_State = CommandState.Completed;
        }

        /// <summary>
        /// Updates the queue position of the command.
        /// </summary>
        /// <param name="position">New queue position</param>
        public void UpdateQueuePosition(int position)
        {
            m_QueuePosition = position;
        }

        #endregion
    }
}
