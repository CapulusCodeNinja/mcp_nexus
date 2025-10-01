namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Detailed command information for type-safe status checking - properly encapsulated
    /// </summary>
    public class CommandInfo
    {
        #region Private Fields

        private readonly string m_commandId;
        private readonly string m_command;
        private CommandState m_state;
        private readonly DateTime m_queueTime;
        private TimeSpan m_elapsed;
        private TimeSpan m_remaining;
        private int m_queuePosition;
        private bool m_isCompleted;

        #endregion

        #region Public Properties

        /// <summary>Gets the command identifier</summary>
        public string CommandId => m_commandId;

        /// <summary>Gets the command text</summary>
        public string Command => m_command;

        /// <summary>Gets or sets the command state</summary>
        public CommandState State
        {
            get => m_state;
            set => m_state = value;
        }

        /// <summary>Gets the queue time</summary>
        public DateTime QueueTime => m_queueTime;

        /// <summary>Gets or sets the elapsed time</summary>
        public TimeSpan Elapsed
        {
            get => m_elapsed;
            set => m_elapsed = value;
        }

        /// <summary>Gets or sets the remaining time</summary>
        public TimeSpan Remaining
        {
            get => m_remaining;
            set => m_remaining = value;
        }

        /// <summary>Gets or sets the queue position</summary>
        public int QueuePosition
        {
            get => m_queuePosition;
            set => m_queuePosition = value;
        }

        /// <summary>Gets or sets whether the command is completed</summary>
        public bool IsCompleted
        {
            get => m_isCompleted;
            set => m_isCompleted = value;
        }

        #endregion

        #region Constructor

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
            m_commandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
            m_command = command ?? throw new ArgumentNullException(nameof(command));
            m_state = state;
            m_queueTime = queueTime;
            m_queuePosition = queuePosition;
            m_elapsed = TimeSpan.Zero;
            m_remaining = TimeSpan.Zero;
            m_isCompleted = false;
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
            m_elapsed = elapsed;
            m_remaining = remaining;
        }

        /// <summary>
        /// Marks the command as completed
        /// </summary>
        public void MarkCompleted()
        {
            m_isCompleted = true;
            m_state = CommandState.Completed;
        }

        /// <summary>
        /// Updates the queue position
        /// </summary>
        /// <param name="position">New queue position</param>
        public void UpdateQueuePosition(int position)
        {
            m_queuePosition = position;
        }

        #endregion
    }
}
