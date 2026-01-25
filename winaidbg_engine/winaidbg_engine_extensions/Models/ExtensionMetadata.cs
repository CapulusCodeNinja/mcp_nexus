namespace WinAiDbg.Engine.Extensions.Models;

/// <summary>
/// Represents metadata for an extension script.
/// </summary>
internal class ExtensionMetadata
{
    /// <summary>
    /// Gets or sets the name of the extension.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the extension.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the extension.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the author of the extension.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the script file name.
    /// </summary>
    public string ScriptFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path to the script file.
    /// </summary>
    public string FullScriptPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the script type (e.g., "PowerShell").
    /// </summary>
    public string ScriptType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 300000; // 5 minutes default

    /// <summary>
    /// Gets or sets the required parameters for the extension.
    /// </summary>
    public List<ExtensionParameter> RequiredParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional parameters for the extension.
    /// </summary>
    public List<ExtensionParameter> OptionalParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the extension path (directory containing the metadata file).
    /// </summary>
    public string ExtensionPath { get; set; } = string.Empty;
}
