using nexus.CommandLine;

namespace nexus.Hosting;

/// <summary>
/// Context for the current server mode.
/// </summary>
/// <param name="mode">The server mode.</param>
public class ServerModeContext(ServerMode mode)
{
    /// <summary>
    /// Gets the current server mode.
    /// </summary>
    public ServerMode Mode { get; } = mode;
}

