namespace WinAiDbg.Config.Models;

/// <summary>
/// Configuration settings controlling whether process statistics are emitted to the logs.
/// </summary>
public class ProcessStatisticsSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether process statistics logging is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

