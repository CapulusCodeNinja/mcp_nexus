namespace Nexus.Engine.Extensions.Models;

/// <summary>
/// Information about a running extension process.
/// </summary>
public class ExtensionProcessInfo
{
    /// <summary>
    /// Command ID of the extension.
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Extension name.
    /// </summary>
    public string ExtensionName { get; set; } = string.Empty;

    /// <summary>
    /// Session ID the extension is running for.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Process ID of the extension.
    /// </summary>
    public int? ProcessId
    {
        get; set;
    }

    /// <summary>
    /// When the extension started.
    /// </summary>
    public DateTime StartedAt
    {
        get; set;
    }

    /// <summary>
    /// Whether the extension is still running.
    /// </summary>
    public bool IsRunning
    {
        get; set;
    }
}

