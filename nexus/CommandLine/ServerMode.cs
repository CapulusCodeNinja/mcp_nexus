namespace Nexus.CommandLine;

/// <summary>
/// Defines the server execution mode.
/// </summary>
public enum ServerMode
{
    /// <summary>
    /// HTTP server mode.
    /// </summary>
    Http,

    /// <summary>
    /// Standard input/output mode.
    /// </summary>
    Stdio,

    /// <summary>
    /// Windows Service mode.
    /// </summary>
    Service,

    /// <summary>
    /// Install command mode.
    /// </summary>
    Install,

    /// <summary>
    /// Update command mode.
    /// </summary>
    Update,

    /// <summary>
    /// Uninstall command mode.
    /// </summary>
    Uninstall
}

