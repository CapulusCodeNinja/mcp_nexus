namespace WinAiDbg.Config.Models;

/// <summary>
/// Extensions configuration settings.
/// </summary>
public class ExtensionsSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether extensions are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the extensions path.
    /// </summary>
    public string ExtensionsPath { get; set; } = "extensions";

    /// <summary>
    /// Gets or sets the callback port.
    /// </summary>
    public int CallbackPort { get; set; } = 0;

    /// <summary>
    /// Gets or sets the graceful termination timeout in milliseconds.
    /// </summary>
    public int GracefulTerminationTimeoutMs { get; set; } = 2000;
}
