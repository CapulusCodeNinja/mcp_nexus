namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Application layer implementation of command result
    /// </summary>
    public class CommandResult : ICommandResult
    {
        #region Private Fields

        private readonly bool m_isSuccess;
        private readonly string m_output;
        private readonly string? m_errorMessage;
        private readonly TimeSpan m_duration;

        #endregion

        #region Public Properties

        /// <summary>Gets whether the command executed successfully</summary>
        public bool IsSuccess => m_isSuccess;

        /// <summary>Gets the command output</summary>
        public string Output => m_output;

        /// <summary>Gets the error message if execution failed</summary>
        public string? ErrorMessage => m_errorMessage;

        /// <summary>Gets the execution duration</summary>
        public TimeSpan Duration => m_duration;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new command result
        /// </summary>
        /// <param name="isSuccess">Whether the command executed successfully</param>
        /// <param name="output">Command output</param>
        /// <param name="errorMessage">Error message if execution failed</param>
        /// <param name="duration">Execution duration</param>
        public CommandResult(bool isSuccess, string output, string? errorMessage = null, TimeSpan duration = default)
        {
            m_isSuccess = isSuccess;
            m_output = output ?? string.Empty;
            m_errorMessage = errorMessage;
            m_duration = duration;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a successful command result
        /// </summary>
        /// <param name="output">Command output</param>
        /// <param name="duration">Execution duration</param>
        /// <returns>Successful command result</returns>
        public static ICommandResult Success(string output, TimeSpan duration = default)
        {
            return new CommandResult(true, output, null, duration);
        }

        /// <summary>
        /// Creates a failed command result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="duration">Execution duration</param>
        /// <returns>Failed command result</returns>
        public static ICommandResult Failure(string errorMessage, TimeSpan duration = default)
        {
            return new CommandResult(false, string.Empty, errorMessage, duration);
        }

        #endregion
    }
}
