namespace Nexus.Engine.Extensions.Models;

/// <summary>
/// Represents internal tracking information for a running extension process.
/// </summary>
internal class ExtensionStatus
{
    /// <summary>
    /// Gets or sets the command ID for this extension execution.
    /// </summary>
    public required string CommandId
    {
        get; init;
    }

    /// <summary>
    /// Gets or sets the session ID this extension belongs to.
    /// </summary>
    public required string SessionId
    {
        get; init;
    }

    /// <summary>
    /// Gets or sets the name of the extension.
    /// </summary>
    public required string ExtensionName
    {
        get; init;
    }

    /// <summary>
    /// Gets or sets the process ID of the running extension.
    /// </summary>
    public required int ProcessId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the time when the extension was started.
    /// </summary>
    public required DateTime StartTime
    {
        get; init;
    }

    /// <summary>
    /// Gets or sets the cancellation token source for this extension.
    /// </summary>
    public required CancellationTokenSource CancellationTokenSource
    {
        get; init;
    }

    /// <summary>
    /// Gets or sets the parameters passed to the extension.
    /// </summary>
    public object? Parameters
    {
        get; init;
    }
}

