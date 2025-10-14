namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Implementation of command validation result
    /// </summary>
    /// <remarks>
    /// Initializes a new command validation result
    /// </remarks>
    /// <param name="isValid">Whether the command is valid</param>
    /// <param name="errors">Validation error messages</param>
    /// <param name="warnings">Validation warnings</param>
    public class CommandValidationResult(bool isValid, List<string>? errors = null, List<string>? warnings = null) : ICommandValidationResult
    {
        #region Private Fields

        private readonly bool m_isValid = isValid;
        private readonly List<string> m_errors = errors ?? [];
        private readonly List<string> m_warnings = warnings ?? [];

        #endregion

        #region Public Properties

        /// <summary>Gets whether the command is valid</summary>
        public bool IsValid => m_isValid;

        /// <summary>Gets validation error messages</summary>
        public IReadOnlyList<string> Errors => m_errors.AsReadOnly();

        /// <summary>Gets validation warnings</summary>
        public IReadOnlyList<string> Warnings => m_warnings.AsReadOnly();

        #endregion
        #region Constructor

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a valid command validation result
        /// </summary>
        /// <param name="warnings">Optional warnings</param>
        /// <returns>Valid command validation result</returns>
        public static ICommandValidationResult Valid(List<string>? warnings = null)
        {
            return new CommandValidationResult(true, null, warnings);
        }

        /// <summary>
        /// Creates an invalid command validation result
        /// </summary>
        /// <param name="errors">Validation errors</param>
        /// <param name="warnings">Optional warnings</param>
        /// <returns>Invalid command validation result</returns>
        public static ICommandValidationResult Invalid(List<string> errors, List<string>? warnings = null)
        {
            return new CommandValidationResult(false, errors, warnings);
        }

        /// <summary>
        /// Creates an invalid command validation result with a single error
        /// </summary>
        /// <param name="error">Validation error</param>
        /// <returns>Invalid command validation result</returns>
        public static ICommandValidationResult Invalid(string error)
        {
            return new CommandValidationResult(false, [error]);
        }

        #endregion
    }
}
