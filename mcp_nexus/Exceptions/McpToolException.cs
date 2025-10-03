namespace mcp_nexus.Exceptions
{
    /// <summary>
    /// Exception thrown by MCP tools that should be returned as JSON-RPC error responses.
    /// This exception includes error codes and optional data for structured error handling.
    /// </summary>
    public class McpToolException : Exception
    {
        /// <summary>
        /// Gets the error code associated with this exception.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Gets the optional error data associated with this exception.
        /// </summary>
        public object? ErrorData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpToolException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code for this exception.</param>
        /// <param name="message">The error message.</param>
        /// <param name="errorData">Optional error data. Can be null.</param>
        public McpToolException(int errorCode, string message, object? errorData = null)
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorData = errorData;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpToolException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code for this exception.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        /// <param name="errorData">Optional error data. Can be null.</param>
        public McpToolException(int errorCode, string message, Exception innerException, object? errorData = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            ErrorData = errorData;
        }
    }
}
