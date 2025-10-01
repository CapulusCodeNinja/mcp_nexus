namespace mcp_nexus.Core.Domain
{
    /// <summary>
    /// Core domain implementation of command - no external dependencies
    /// </summary>
    public class Command : ICommand
    {
        #region Private Fields

        private readonly string m_commandId;
        private readonly string m_commandText;
        private readonly DateTime m_createdAt;
        private CommandState m_state;
        private bool m_isCompleted;

        #endregion

        #region Public Properties

        /// <summary>Gets the command identifier</summary>
        public string CommandId => m_commandId;

        /// <summary>Gets the command text</summary>
        public string CommandText => m_commandText;

        /// <summary>Gets the command creation time</summary>
        public DateTime CreatedAt => m_createdAt;

        /// <summary>Gets the current command state</summary>
        public CommandState State => m_state;

        /// <summary>Gets whether the command is completed</summary>
        public bool IsCompleted => m_isCompleted;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new command
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <param name="commandText">Command text</param>
        public Command(string commandId, string commandText)
        {
            m_commandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
            m_commandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
            m_createdAt = DateTime.UtcNow;
            m_state = CommandState.Queued;
            m_isCompleted = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the command state
        /// </summary>
        /// <param name="state">New state</param>
        public void UpdateState(CommandState state)
        {
            m_state = state;
            
            if (state == CommandState.Completed || state == CommandState.Failed || state == CommandState.Cancelled)
            {
                m_isCompleted = true;
            }
        }

        /// <summary>
        /// Marks the command as completed
        /// </summary>
        public void MarkCompleted()
        {
            m_state = CommandState.Completed;
            m_isCompleted = true;
        }

        #endregion
    }
}
