namespace Nexus.Setup.Models;

/// <summary>
/// Defines how a service starts.
/// </summary>
public enum ServiceStartMode
{
    /// <summary>
    /// Service starts automatically when the system starts.
    /// </summary>
    Automatic,

    /// <summary>
    /// Service must be started manually.
    /// </summary>
    Manual,

    /// <summary>
    /// Service is disabled and cannot be started.
    /// </summary>
    Disabled
}

