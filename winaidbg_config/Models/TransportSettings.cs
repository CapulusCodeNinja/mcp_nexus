namespace WinAiDbg.Config.Models;

/// <summary>
/// Transport configuration settings.
/// </summary>
public class TransportSettings
{
    /// <summary>
    /// Gets or sets the transport mode.
    /// </summary>
    public string Mode { get; set; } = "http";

    /// <summary>
    /// Gets or sets a value indicating whether service mode is enabled.
    /// </summary>
    public bool ServiceMode { get; set; } = true;
}
