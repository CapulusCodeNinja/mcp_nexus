namespace WinAiDbg.Config.Models;

/// <summary>
/// Debugging configuration settings.
/// </summary>
public class DebuggingSettings
{
    /// <summary>
    /// Gets or sets the CDB path.
    /// </summary>
    public string? CdbPath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the command timeout in milliseconds.
    /// </summary>
    public int CommandTimeoutMs { get; set; } = 600000;

    /// <summary>
    /// Gets or sets the idle timeout in milliseconds.
    /// </summary>
    public int IdleTimeoutMs { get; set; } = 300000;

    /// <summary>
    /// Gets or sets the symbol server max retries.
    /// </summary>
    public int SymbolServerMaxRetries { get; set; } = 1;

    /// <summary>
    /// Gets or sets the symbol search path.
    /// </summary>
    public string SymbolSearchPath { get; set; } = "srv*T:\\symbols*https://symbols.int.avast.com/symbols;srv*T:\\symbols*https://msdl.microsoft.com/download/symbols";

    /// <summary>
    /// Gets or sets the startup delay in milliseconds.
    /// </summary>
    public int StartupDelayMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the output reading timeout in milliseconds.
    /// </summary>
    public int OutputReadingTimeoutMs { get; set; } = 300000;

    /// <summary>
    /// Gets or sets a value indicating whether command preprocessing is enabled.
    /// </summary>
    public bool EnableCommandPreprocessing { get; set; } = true;
}
