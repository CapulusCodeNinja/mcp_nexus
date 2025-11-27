using Nexus.Config;
using Nexus.External.Apis.FileSystem;

using NLog;

namespace Nexus.Engine.DumpCheck.Internal;

/// <summary>
/// Resolves the path to the dumpchk executable based on configuration
/// and a small set of common Windows debugger installation locations.
/// </summary>
internal sealed class DumpChkLocator
{
    /// <summary>
    /// Logger for dumpchk location operations.
    /// </summary>
    private readonly Logger m_Logger;

    /// <summary>
    /// Shared application settings.
    /// </summary>
    private readonly ISettings m_Settings;

    /// <summary>
    /// File system abstraction for path probing.
    /// </summary>
    private readonly IFileSystem m_FileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="DumpChkLocator"/> class.
    /// </summary>
    /// <param name="settings">The shared application settings.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="settings"/> or <paramref name="fileSystem"/> is <c>null</c>.
    /// </exception>
    public DumpChkLocator(ISettings settings, IFileSystem fileSystem)
    {
        m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Finds the dumpchk executable path by checking the configured path and common installation locations.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the dumpchk executable path.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the dumpchk executable cannot be found at any known location.
    /// </exception>
    public Task<string> FindDumpChkExecutableAsync()
    {
        var validationSettings = m_Settings.Get().McpNexus.Validation;

        if (!string.IsNullOrWhiteSpace(validationSettings.DumpChkPath) &&
            m_FileSystem.FileExists(validationSettings.DumpChkPath))
        {
            m_Logger.Debug("Using configured dumpchk path: {DumpChkPath}", validationSettings.DumpChkPath);
            return Task.FromResult(validationSettings.DumpChkPath);
        }

        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\dumpchk.exe",
            @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\dumpchk.exe",
            @"C:\Program Files\Windows Kits\10\Debuggers\x64\dumpchk.exe",
            @"C:\Program Files\Windows Kits\10\Debuggers\x86\dumpchk.exe",
        };

        foreach (var path in commonPaths)
        {
            if (m_FileSystem.FileExists(path))
            {
                m_Logger.Debug("Found dumpchk at: {DumpChkPath}", path);
                return Task.FromResult(path);
            }
        }

        throw new InvalidOperationException("Dumpchk executable not found. Please install Windows SDK or specify DumpChkPath in configuration when DumpChkEnabled is true.");
    }
}
