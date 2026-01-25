using System.Runtime.Versioning;

namespace WinAiDbg.External.Apis.Security;

/// <summary>
/// Interface for checking administrator privileges to enable mocking in tests.
/// </summary>
[SupportedOSPlatform("windows")]
public interface IAdministratorChecker
{
    /// <summary>
    /// Checks if the current process is running with administrator privileges.
    /// </summary>
    /// <returns>True if running as administrator, false otherwise.</returns>
    bool IsRunningAsAdministrator();
}
