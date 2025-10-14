namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Error response model for extension callbacks.
    /// </summary>
    public class ExtensionCallbackErrorResponse
    {
        /// <summary>
        /// The error type.
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// The error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional hint for resolving the error.
        /// </summary>
        public string? Hint { get; set; }
    }
}


