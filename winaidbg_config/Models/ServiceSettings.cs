namespace WinAiDbg.Config.Models;

/// <summary>
/// Service configuration settings.
/// </summary>
public class ServiceSettings
{
    /// <summary>
    /// Gets or sets the service install path.
    /// </summary>
    public string InstallPath { get; set; } = "C:\\Program Files\\WinAiDbg";

    /// <summary>
    /// Gets or sets the service backup path.
    /// </summary>
    public string BackupPath { get; set; } = "C:\\Program Files\\WinAiDbg\\backups";

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = "WinAiDbg";

    /// <summary>
    /// Gets or sets the service display name.
    /// </summary>
    public string DisplayName { get; set; } = "WinAiDbg Debugging Server";
}
