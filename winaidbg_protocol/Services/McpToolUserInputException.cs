namespace WinAiDbg.Protocol.Services;

/// <summary>
/// Represents a tool invocation failure caused by invalid user-provided inputs.
/// This exception is intended to be caught by the MCP tool call handler to return
/// a structured <c>isError: true</c> response with actionable feedback.
/// </summary>
internal class McpToolUserInputException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolUserInputException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the input problem.</param>
    public McpToolUserInputException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolUserInputException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the input problem.</param>
    /// <param name="innerException">The underlying exception.</param>
    public McpToolUserInputException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

