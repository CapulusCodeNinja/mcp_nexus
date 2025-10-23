namespace nexus.CommandLine;

/// <summary>
/// Represents the command line context for the application.
/// </summary>
public class CommandLineContext
{
    private readonly string[] m_Args;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineContext"/> class.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public CommandLineContext(string[] args)
    {
        m_Args = args ?? throw new ArgumentNullException(nameof(args));
    }

    /// <summary>
    /// Gets a value indicating whether the application should run in HTTP mode.
    /// </summary>
    public bool IsHttpMode => HasArgument("--http");

    /// <summary>
    /// Gets a value indicating whether the application should run in stdio mode.
    /// </summary>
    public bool IsStdioMode => HasArgument("--stdio");

    /// <summary>
    /// Gets a value indicating whether the application should run in service mode.
    /// </summary>
    public bool IsServiceMode => HasArgument("--service");

    /// <summary>
    /// Gets a value indicating whether the application should run install command.
    /// </summary>
    public bool IsInstallMode => HasArgument("--install");

    /// <summary>
    /// Gets a value indicating whether the application should run update command.
    /// </summary>
    public bool IsUpdateMode => HasArgument("--update");

    /// <summary>
    /// Gets a value indicating whether the application should run uninstall command.
    /// </summary>
    public bool IsUninstallMode => HasArgument("--uninstall");

    /// <summary>
    /// Gets the raw command line arguments.
    /// </summary>
    public string[] Args => m_Args;

    /// <summary>
    /// Checks if a specific argument is present in the command line.
    /// </summary>
    /// <param name="argument">The argument to check for.</param>
    /// <returns>True if the argument is present, false otherwise.</returns>
    private bool HasArgument(string argument)
    {
        return m_Args.Contains(argument, StringComparer.OrdinalIgnoreCase);
    }
}
