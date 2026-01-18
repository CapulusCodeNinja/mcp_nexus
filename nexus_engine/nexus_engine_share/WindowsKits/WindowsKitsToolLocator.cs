using System.Runtime.InteropServices;

using Nexus.External.Apis.FileSystem;

namespace Nexus.Engine.Share.WindowsKits;

/// <summary>
/// Locates Windows Debugging Tools (for example <c>cdb.exe</c> and <c>dumpchk.exe</c>) by probing
/// installed Windows Kits debugger directories and selecting the best match for the current OS architecture.
/// </summary>
public sealed class WindowsKitsToolLocator
{
    private readonly IFileSystem m_FileSystem;
    private readonly IReadOnlyList<string> m_WindowsKitsBaseDirectories;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsKitsToolLocator"/> class.
    /// </summary>
    /// <param name="fileSystem">File system abstraction used for probing.</param>
    /// <param name="windowsKitsBaseDirectories">
    /// Optional override for Windows Kits base directories to probe (for example <c>C:\Program Files\Windows Kits</c>).
    /// When not provided, the locator probes both Program Files and Program Files (x86).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileSystem"/> is <c>null</c>.</exception>
    public WindowsKitsToolLocator(IFileSystem fileSystem, IEnumerable<string>? windowsKitsBaseDirectories = null)
    {
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_WindowsKitsBaseDirectories = NormalizeWindowsKitsBaseDirectories(windowsKitsBaseDirectories);
    }

    /// <summary>
    /// Finds a debugging tool executable path by first honoring a configured override and then probing
    /// installed Windows Kits debugger folders.
    /// </summary>
    /// <param name="toolFileName">The tool executable file name (for example <c>cdb.exe</c>).</param>
    /// <param name="configuredPath">An optional configured absolute path override.</param>
    /// <param name="osArchitecture">
    /// Optional OS architecture override. When not specified, uses <see cref="RuntimeInformation.OSArchitecture"/>.
    /// </param>
    /// <returns>The resolved absolute path to the tool executable.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toolFileName"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no matching executable can be found.</exception>
    public string FindToolExecutablePath(string toolFileName, string? configuredPath, Architecture? osArchitecture = null)
    {
        if (string.IsNullOrWhiteSpace(toolFileName))
        {
            throw new ArgumentException("Tool file name cannot be null or whitespace.", nameof(toolFileName));
        }

        var normalizedToolFileName = toolFileName.Trim();

        if (!string.IsNullOrWhiteSpace(configuredPath) &&
            m_FileSystem.FileExists(configuredPath))
        {
            return configuredPath;
        }

        var effectiveOsArchitecture = osArchitecture ?? RuntimeInformation.OSArchitecture;

        var debuggerRoots = FindDebuggerRootDirectories();

        var bestMatch = TryFindBestMatchInDebuggerRoots(debuggerRoots, normalizedToolFileName, effectiveOsArchitecture);
        if (!string.IsNullOrWhiteSpace(bestMatch))
        {
            return bestMatch;
        }

        var anyMatch = TryFindAnyMatchInDebuggerRoots(debuggerRoots, normalizedToolFileName);
        if (!string.IsNullOrWhiteSpace(anyMatch))
        {
            return anyMatch;
        }

        // As an ultimate fallback (for environments where directory enumeration is restricted),
        // probe a small set of known common locations.
        var legacyMatch = TryFindLegacyKnownPath(normalizedToolFileName, effectiveOsArchitecture);
        return !string.IsNullOrWhiteSpace(legacyMatch)
            ? legacyMatch
            : throw new InvalidOperationException(
                $"'{normalizedToolFileName}' executable not found. Please install Windows Debugging Tools or configure an explicit path.");
    }

    /// <summary>
    /// Normalizes the set of Windows Kits base directories to probe.
    /// </summary>
    /// <param name="windowsKitsBaseDirectories">An optional override set.</param>
    /// <returns>A normalized, de-duplicated list of base directories.</returns>
    private static IReadOnlyList<string> NormalizeWindowsKitsBaseDirectories(IEnumerable<string>? windowsKitsBaseDirectories)
    {
        if (windowsKitsBaseDirectories != null)
        {
            return windowsKitsBaseDirectories
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim().TrimEnd('\\'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Trim().TrimEnd('\\');
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).Trim().TrimEnd('\\');

        return new[]
        {
            Path.Combine(programFiles, "Windows Kits"),
            Path.Combine(programFilesX86, "Windows Kits"),
        }
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    }

    /// <summary>
    /// Enumerates Windows Kits version folders and returns any <c>Debuggers</c> directories found.
    /// </summary>
    /// <returns>A list of debugger root directories (for example <c>...\Windows Kits\10\Debuggers</c>).</returns>
    private IReadOnlyList<string> FindDebuggerRootDirectories()
    {
        var debuggerRoots = new List<string>();

        foreach (var kitsBase in m_WindowsKitsBaseDirectories)
        {
            if (!m_FileSystem.DirectoryExists(kitsBase))
            {
                continue;
            }

            foreach (var versionDirectoryPath in m_FileSystem.GetDirectories(kitsBase))
            {
                var debuggersDirectory = m_FileSystem.CombinePaths(versionDirectoryPath, "Debuggers");
                if (m_FileSystem.DirectoryExists(debuggersDirectory))
                {
                    debuggerRoots.Add(debuggersDirectory);
                }
            }
        }

        return debuggerRoots
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Attempts to find the best match for the provided OS architecture by probing well-known architecture
    /// subdirectories (for example <c>arm64</c>, <c>x64</c>, <c>x86</c>) under each debugger root.
    /// </summary>
    /// <param name="debuggerRoots">The debugger root directories to probe.</param>
    /// <param name="toolFileName">The tool executable file name.</param>
    /// <param name="osArchitecture">The OS architecture to match.</param>
    /// <returns>The resolved executable path if found; otherwise <c>null</c>.</returns>
    private string? TryFindBestMatchInDebuggerRoots(IReadOnlyList<string> debuggerRoots, string toolFileName, Architecture osArchitecture)
    {
        var preferredArchitectures = GetPreferredArchitectureFolderNames(osArchitecture);

        foreach (var root in debuggerRoots)
        {
            foreach (var archFolder in preferredArchitectures)
            {
                var candidate = m_FileSystem.CombinePaths(root, archFolder, toolFileName);
                if (m_FileSystem.FileExists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to find any available tool executable under the provided debugger roots, regardless of architecture.
    /// </summary>
    /// <param name="debuggerRoots">The debugger root directories to probe.</param>
    /// <param name="toolFileName">The tool executable file name.</param>
    /// <returns>The resolved executable path if found; otherwise <c>null</c>.</returns>
    private string? TryFindAnyMatchInDebuggerRoots(IReadOnlyList<string> debuggerRoots, string toolFileName)
    {
        foreach (var root in debuggerRoots)
        {
            if (!m_FileSystem.DirectoryExists(root))
            {
                continue;
            }

            foreach (var architectureDirectoryPath in m_FileSystem.GetDirectories(root))
            {
                var candidate = m_FileSystem.CombinePaths(architectureDirectoryPath, toolFileName);
                if (m_FileSystem.FileExists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Probes a small fixed list of legacy paths as a last-resort fallback.
    /// </summary>
    /// <param name="toolFileName">The tool executable file name.</param>
    /// <param name="osArchitecture">The OS architecture to match.</param>
    /// <returns>The resolved executable path if found; otherwise <c>null</c>.</returns>
    private string? TryFindLegacyKnownPath(string toolFileName, Architecture osArchitecture)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Trim().TrimEnd('\\');
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).Trim().TrimEnd('\\');

        var preferredArchitectures = GetPreferredArchitectureFolderNames(osArchitecture);
        var legacyRoots = new[]
        {
            Path.Combine(programFilesX86, @"Windows Kits\10\Debuggers"),
            Path.Combine(programFilesX86, @"Windows Kits\11\Debuggers"),
            Path.Combine(programFiles, @"Windows Kits\10\Debuggers"),
            Path.Combine(programFiles, @"Windows Kits\11\Debuggers"),
        };

        foreach (var root in legacyRoots)
        {
            foreach (var arch in preferredArchitectures)
            {
                var candidate = Path.Combine(root, arch, toolFileName);
                if (m_FileSystem.FileExists(candidate))
                {
                    return candidate;
                }
            }
        }

        foreach (var root in legacyRoots)
        {
            foreach (var arch in new[] { "arm64", "x64", "x86", "arm" })
            {
                var candidate = Path.Combine(root, arch, toolFileName);
                if (m_FileSystem.FileExists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets an ordered list of debugger architecture folder names to probe for a given OS architecture.
    /// </summary>
    /// <param name="osArchitecture">The OS architecture.</param>
    /// <returns>Ordered folder names to probe.</returns>
    private static IReadOnlyList<string> GetPreferredArchitectureFolderNames(Architecture osArchitecture)
    {
        return osArchitecture switch
        {
            Architecture.Arm64 => new[] { "arm64", "x64", "x86", "arm" },
            Architecture.X64 => new[] { "x64", "x86" },
            Architecture.X86 => new[] { "x86" },
            Architecture.Arm => new[] { "arm", "x86" },
            _ => new[] { "x64", "arm64", "x86", "arm" },
        };
    }
}

