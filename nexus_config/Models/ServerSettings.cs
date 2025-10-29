namespace Nexus.Config.Models;

/// <summary>
/// Server configuration settings.
/// </summary>
public class ServerSettings
{
    /// <summary>
    /// Gets or sets the server host.
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// Gets or sets the server port.
    /// </summary>
    public int Port { get; set; } = 5511;
}
