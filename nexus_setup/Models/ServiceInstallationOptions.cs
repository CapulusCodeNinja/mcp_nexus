namespace Nexus.Setup.Models;

/// <summary>
/// Options for service installation.
/// </summary>
internal class ServiceInstallationOptions
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = "Nexus";

    /// <summary>
    /// Gets or sets the service display name.
    /// </summary>
    public string DisplayName { get; set; } = "Nexus Debugging Server";

    /// <summary>
    /// Gets or sets the service description.
    /// </summary>
    public string Description { get; set; } = "Model Context Protocol server for Windows debugging tools";

    /// <summary>
    /// Gets or sets the path to the executable.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service start mode.
    /// </summary>
    public ServiceStartMode StartMode { get; set; } = ServiceStartMode.Automatic;

    /// <summary>
    /// Gets or sets the service account.
    /// </summary>
    public ServiceAccount Account { get; set; } = ServiceAccount.LocalSystem;

    /// <summary>
    /// Gets or sets the service account username (if using custom account).
    /// </summary>
    public string? AccountUsername
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the service account password (if using custom account).
    /// </summary>
    public string? AccountPassword
    {
        get; set;
    }
}
