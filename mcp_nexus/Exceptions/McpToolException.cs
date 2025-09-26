namespace mcp_nexus.Exceptions
{
    /// <summary>
    /// Exception thrown by MCP tools that should be returned as JSON-RPC error responses
    /// </summary>
    public class McpToolException : Exception
    {
        public int ErrorCode { get; }
        public object? ErrorData { get; }

        public McpToolException(int errorCode, string message, object? errorData = null)
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorData = errorData;
        }

        public McpToolException(int errorCode, string message, Exception innerException, object? errorData = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            ErrorData = errorData;
        }
    }
}
