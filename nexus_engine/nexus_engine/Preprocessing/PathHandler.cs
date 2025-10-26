using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Nexus.Engine.Preprocessing;

/// <summary>
/// Handles path operations, particularly WSL to Windows path conversion.
/// Supports caching and fstab mapping for efficient and accurate path translation.
/// </summary>
internal partial class PathHandler
{
    private readonly ConcurrentDictionary<string, string> m_FstabMountMap = new();
    private DateTime m_FstabLastLoadedUtc = DateTime.MinValue;
    private readonly object m_FstabLock = new();
    private readonly ConcurrentDictionary<string, (string Converted, DateTime ExpiresUtc)> m_ConversionCache = new();
    private readonly TimeSpan m_ConversionTtl = TimeSpan.FromMinutes(5);
    private readonly WslPathConverter m_WslConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathHandler"/> class.
    /// </summary>
    public PathHandler()
    {
        m_WslConverter = new WslPathConverter();
    }

    /// <summary>
    /// Converts a WSL path (e.g., /mnt/c/...) to a Windows path (e.g., C:\...).
    /// </summary>
    /// <param name="wslPath">The WSL path to convert.</param>
    /// <returns>The converted Windows path, or the original path if not a WSL path.</returns>
    public string ConvertToWindowsPath(string wslPath)
    {
        if (string.IsNullOrWhiteSpace(wslPath))
        {
            return wslPath;
        }

        try
        {
            // Short-circuit for Windows and UNC paths
            if (IsWindowsPath(wslPath) || wslPath.StartsWith("\\\\"))
            {
                return wslPath;
            }

            // Only attempt WSL-related conversions for Unix-style inputs
            if (!wslPath.StartsWith("/"))
            {
                return wslPath;
            }

            // Cache lookup (only conversions are cached, not passthrough)
            if (m_ConversionCache.TryGetValue(wslPath, out var cached) && cached.ExpiresUtc > DateTime.Now)
            {
                return cached.Converted;
            }

            // Use fstab mapping (cached) before invoking wslpath
            if (TryConvertWithFstabMapping(wslPath, out var fstabConverted))
            {
                m_ConversionCache[wslPath] = (fstabConverted, DateTime.Now + m_ConversionTtl);
                return fstabConverted;
            }

            // Fallback: wslpath call with short timeout (only for /mnt/<letter>/...)
            if (wslPath.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase) &&
                m_WslConverter.TryConvertToWindowsPath(wslPath, out var wslConverted))
            {
                m_ConversionCache[wslPath] = (wslConverted, DateTime.Now + m_ConversionTtl);
                return wslConverted;
            }

            // If it's already a Windows path or not a recognized WSL mount path, return as-is
            return wslPath;
        }
        catch (Exception)
        {
            // If any error occurs during conversion, return the original path
            // This ensures the system remains functional even with unexpected input
            return wslPath;
        }
    }

    /// <summary>
    /// Determines whether a path is a valid Windows path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a valid Windows path, false otherwise.</returns>
    public bool IsWindowsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Check for Windows path patterns (C:\, \\server\share, etc.)
        return WindowsPathRegex().IsMatch(path);
    }

    /// <summary>
    /// Attempts to convert using a mapping derived from '/etc/fstab' (drvfs entries) via WSL.
    /// This supports custom mount points, e.g. mapping \\server\share to /mnt/share.
    /// </summary>
    /// <param name="path">The WSL path to convert.</param>
    /// <param name="windowsPath">The converted Windows path.</param>
    /// <returns>True if conversion succeeded, false otherwise.</returns>
    private bool TryConvertWithFstabMapping(string path, out string windowsPath)
    {
        windowsPath = path;

        try
        {
            EnsureFstabMountMapLoaded();

            if (m_FstabMountMap.Count == 0)
            {
                return false;
            }

            // Find the longest matching mount prefix
            var match = m_FstabMountMap
                .OrderByDescending(kvp => kvp.Key.Length)
                .FirstOrDefault(kvp => path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(match.Key))
            {
                var relative = path[match.Key.Length..].TrimStart('/');
                var root = match.Value;

                if (root.StartsWith("\\\\"))
                {
                    // UNC root
                    windowsPath = root + (relative.Length > 0 ? "\\" + relative.Replace('/', '\\') : string.Empty);
                    return true;
                }

                if (root.Length >= 2 && root[1] == ':' && char.IsLetter(root[0]))
                {
                    // Drive letter root - ensure proper path separator
                    // If root already ends with backslash, don't add another
                    if (relative.Length > 0)
                    {
                        var separator = root.EndsWith('\\') ? string.Empty : "\\";
                        windowsPath = root + separator + relative.Replace('/', '\\');
                    }
                    else
                    {
                        windowsPath = root;
                    }
                    return true;
                }
            }
        }
        catch
        {
            // Ignore and fall back
        }

        return false;
    }

    /// <summary>
    /// Ensures the fstab mount map is loaded and up to date.
    /// Refreshes at most every 5 minutes.
    /// </summary>
    private void EnsureFstabMountMapLoaded()
    {
        // Refresh map at most every 5 minutes
        if ((DateTime.Now - m_FstabLastLoadedUtc) < TimeSpan.FromMinutes(5) && m_FstabMountMap.Count > 0)
        {
            return;
        }

        lock (m_FstabLock)
        {
            if ((DateTime.Now - m_FstabLastLoadedUtc) < TimeSpan.FromMinutes(5) && m_FstabMountMap.Count > 0)
            {
                return;
            }

            try
            {
                var newMappings = m_WslConverter.LoadFstabMappings();

                m_FstabMountMap.Clear();
                foreach (var kvp in newMappings)
                {
                    m_FstabMountMap[kvp.Key] = kvp.Value;
                }

                m_FstabLastLoadedUtc = DateTime.Now;
            }
            catch
            {
                // Ignore failures; leave map empty
            }
        }
    }

    /// <summary>
    /// Regular expression to match Windows paths (e.g., C:\ or \\server\share).
    /// </summary>
    /// <returns>Compiled regex.</returns>
    [GeneratedRegex(@"^([a-zA-Z]:\\|\\\\)", RegexOptions.Compiled)]
    private static partial Regex WindowsPathRegex();
}
