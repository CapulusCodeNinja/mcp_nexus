using System.Runtime.Versioning;

namespace WinAiDbg.External.Apis.Security;

/// <summary>
/// Default implementation of <see cref="IAdministratorChecker"/> that checks Windows administrator privileges.
/// </summary>
[SupportedOSPlatform("windows")]
public class AdministratorChecker : IAdministratorChecker
{
    /// <summary>
    /// Checks if the current process is running with administrator privileges.
    /// </summary>
    /// <returns>True if running as administrator, false otherwise.</returns>
    public bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
