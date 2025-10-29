namespace Nexus.Config.Models;

/// <summary>
/// Service configuration settings.
/// </summary>
public class ServiceSettings
{
    /// <summary>
    /// Gets or sets the service install path.
    /// </summary>
    public string InstallPath { get; set; } = "C:\\Program Files\\MCP-Nexus";

    /// <summary>
    /// Gets or sets the service backup path.
    /// </summary>
    public string BackupPath { get; set; } = "C:\\Program Files\\MCP-Nexus\\backups";

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = "MCP-Nexus";

    /// <summary>
    /// Gets or sets the service display name.
    /// </summary>
    public string DisplayName { get; set; } = "MCP-Nexus Debugging Server";
}
