namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Implementation of command execution result
    /// </summary>
    /// <remarks>
    /// Initializes a new command result
    /// </remarks>
    /// <param name="isSuccess">Whether the command executed successfully</param>
    /// <param name="output">Command output</param>
    /// <param name="errorMessage">Error message if execution failed</param>
    /// <param name="duration">Execution duration</param>
    /// <param name="data">Additional result data</param>
    public class CommandResult(bool isSuccess, string output, string? errorMessage = null,
        TimeSpan duration = default, Dictionary<string, object>? data = null) : ICommandResult
    {
        #region Private Fields

        private readonly bool m_IsSuccess = isSuccess;
        private readonly string m_Output = output ?? string.Empty;
        private readonly string? m_ErrorMessage = errorMessage;
        private readonly TimeSpan m_Duration = duration;
        private readonly Dictionary<string, object> m_Data = data ?? [];

        #endregion

        #region Public Properties

        /// <summary>Gets whether the command executed successfully</summary>
        public bool IsSuccess => m_IsSuccess;

        /// <summary>Gets the command output</summary>
        public string Output => m_Output;

        /// <summary>Gets the error message if execution failed</summary>
        public string? ErrorMessage => m_ErrorMessage;

        /// <summary>Gets the execution duration</summary>
        public TimeSpan Duration => m_Duration;

        /// <summary>Gets additional result data</summary>
        public IReadOnlyDictionary<string, object> Data => m_Data.AsReadOnly();

        #endregion
        #region Constructor

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a successful command result
        /// </summary>
        /// <param name="output">Command output</param>
        /// <param name="duration">Execution duration</param>
        /// <param name="data">Additional result data</param>
        /// <returns>Successful command result</returns>
        public static ICommandResult Success(string output, TimeSpan duration = default,
            Dictionary<string, object>? data = null)
        {
            return new CommandResult(true, output, null, duration, data);
        }

        /// <summary>
        /// Creates a failed command result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="duration">Execution duration</param>
        /// <param name="data">Additional result data</param>
        /// <returns>Failed command result</returns>
        public static ICommandResult Failure(string errorMessage, TimeSpan duration = default,
            Dictionary<string, object>? data = null)
        {
            return new CommandResult(false, string.Empty, errorMessage, duration, data);
        }

        #endregion
    }
}
