namespace Nexus.Engine.Internal;

/// <summary>
/// Represents a single line of process output with source metadata.
/// </summary>
internal sealed class ProcessOutputLine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessOutputLine"/> class.
    /// </summary>
    /// <param name="text">The text content of the line.</param>
    /// <param name="isError">Indicates whether the line originated from the error stream.</param>
    public ProcessOutputLine(string text, bool isError)
    {
        Text = text ?? string.Empty;
        IsError = isError;
    }

    /// <summary>
    /// Gets the text content of the line.
    /// </summary>
    public string Text
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether the line originated from the error stream.
    /// </summary>
    public bool IsError
    {
        get;
    }
}


