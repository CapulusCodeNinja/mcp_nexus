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
}


