namespace WinAiDbg.Setup.Models;

/// <summary>
/// Defines the account under which a service runs.
/// </summary>
public enum ServiceAccount
{
    /// <summary>
    /// Runs under the Local System account (highest privileges).
    /// </summary>
    LocalSystem,

    /// <summary>
    /// Runs under the Local Service account (limited privileges).
    /// </summary>
    LocalService,

    /// <summary>
    /// Runs under the Network Service account.
    /// </summary>
    NetworkService,

    /// <summary>
    /// Runs under a custom user account.
    /// </summary>
    Custom,
}
