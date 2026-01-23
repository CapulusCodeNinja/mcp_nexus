namespace Nexus.Config.Models;

/// <summary>
/// Validation configuration settings for dump files and related tooling.
/// </summary>
public class ValidationSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether dumpchk integration is enabled.
    /// When disabled, the engine will not attempt to locate or invoke dumpchk.exe.
    /// </summary>
    public bool DumpChkEnabled
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the optional path to the dumpchk executable.
    /// When not specified, common debugger installation paths will be probed.
    /// </summary>
    public string? DumpChkPath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the timeout for dumpchk operations in milliseconds.
    /// Default is 60 seconds. If dumpchk does not complete within this timeout,
    /// the validation will be skipped with a warning, but session creation will continue.
    /// </summary>
    public int DumpChkTimeoutMs { get; set; } = 60000;
}


