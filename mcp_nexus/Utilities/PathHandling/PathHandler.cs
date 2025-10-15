using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace mcp_nexus.Utilities.PathHandling
{
    /// <summary>
    /// Service for handling path conversions between WSL and Windows formats.
    /// This class provides centralized logic for converting WSL-style paths (like /mnt/c/...)
    /// to Windows-style paths (like C:\...) for use with Windows debugging tools.
    /// Injectable service that supports proper dependency injection and testing.
    /// </summary>
    public class PathHandler : IPathHandler
    {
        private readonly ConcurrentDictionary<string, string> m_FstabMountMap = new();
        private DateTime m_FstabLastLoadedUtc = DateTime.MinValue;
        private readonly object m_FstabLock = new();
        private readonly ConcurrentDictionary<string, (string Converted, DateTime ExpiresUtc)> m_ConversionCache = new();
        private readonly TimeSpan m_ConversionTtl = TimeSpan.FromMinutes(5);
        private readonly IWslPathConverter m_WslConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathHandler"/> class.
        /// </summary>
        /// <param name="wslConverter">The WSL path converter to use for path conversions.</param>
        public PathHandler(IWslPathConverter wslConverter)
        {
            m_WslConverter = wslConverter ?? throw new ArgumentNullException(nameof(wslConverter));
        }

        /// <summary>
        /// Converts a WSL-style path to Windows format if needed.
        /// Examples:
        /// - "/mnt/c/inetpub/wwwroot/uploads/dump.dmp" -> "C:\inetpub\wwwroot\uploads\dump.dmp"
        /// - "/mnt/d/symbols" -> "D:\symbols"
        /// - "C:\already\windows\path" -> "C:\already\windows\path" (unchanged)
        /// - "/unix/path" -> "/unix/path" (unchanged - not WSL mount)
        /// </summary>
        /// <param name="path">The input path that may be in WSL format</param>
        /// <returns>Windows-compatible path</returns>
        public string ConvertToWindowsPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            // SECURITY: Prevent path traversal attacks
            ValidatePathSecurity(path);

            try
            {
                // Short-circuit for Windows and UNC paths
                if (IsWindowsPath(path) || path.StartsWith("\\\\"))
                {
                    return path;
                }

                // Only attempt WSL-related conversions for Unix-style inputs
                if (!path.StartsWith("/"))
                {
                    return path;
                }

                // Cache lookup (only conversions are cached, not passthrough)
                if (m_ConversionCache.TryGetValue(path, out var cached) && cached.ExpiresUtc > DateTime.Now)
                {
                    return cached.Converted;
                }

                // Use fstab mapping (cached) before invoking wslpath
                if (TryConvertWithFstabMapping(path, out var fstabConverted))
                {
                    m_ConversionCache[path] = (fstabConverted, DateTime.Now + m_ConversionTtl);
                    return fstabConverted;
                }

                // Fallback: wslpath call with short timeout (only for /mnt/<letter>/...)
                if (path.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase) && m_WslConverter.TryConvertToWindowsPath(path, out var wslConverted))
                {
                    m_ConversionCache[path] = (wslConverted, DateTime.Now + m_ConversionTtl);
                    return wslConverted;
                }

                // If it's already a Windows path or not a recognized WSL mount path, return as-is
                return path;
            }
            catch (Exception)
            {
                // If any error occurs during conversion, return the original path
                // This ensures the system remains functional even with unexpected input
                return path;
            }
        }

        /// <summary>
        /// Attempts to convert using a mapping derived from '/etc/fstab' (drvfs entries) via WSL.
        /// This supports custom mount points, e.g. mapping \\server\share to /mnt/share.
        /// </summary>
        private bool TryConvertWithFstabMapping(string path, out string windowsPath)
        {
            windowsPath = path;

            try
            {
                EnsureFstabMountMapLoaded();

                if (m_FstabMountMap.Count == 0)
                    return false;

                // Find the longest matching mount prefix
                var match = m_FstabMountMap
                    .OrderByDescending(kvp => kvp.Key.Length)
                    .FirstOrDefault(kvp => path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(match.Key))
                {
                    var relative = path.Substring(match.Key.Length).TrimStart('/');
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

        private void EnsureFstabMountMapLoaded()
        {
            // Refresh map at most every 5 minutes
            if ((DateTime.Now - m_FstabLastLoadedUtc) < TimeSpan.FromMinutes(5) && m_FstabMountMap.Count > 0)
                return;

            lock (m_FstabLock)
            {
                if ((DateTime.Now - m_FstabLastLoadedUtc) < TimeSpan.FromMinutes(5) && m_FstabMountMap.Count > 0)
                    return;

                try
                {
                    var newMappings = m_WslConverter.LoadFstabMappings();

                    m_FstabMountMap.Clear();
                    foreach (var kvp in newMappings)
                        m_FstabMountMap[kvp.Key] = kvp.Value;

                    m_FstabLastLoadedUtc = DateTime.Now;
                }
                catch
                {
                    // Ignore failures; leave map empty
                }
            }
        }

        /// <summary>
        /// Converts a Windows-style path to WSL mount format.
        /// Examples:
        /// - "C:\inetpub\wwwroot\uploads\dump.dmp" -> "/mnt/c/inetpub/wwwroot/uploads/dump.dmp"
        /// - "D:\symbols" -> "/mnt/d/symbols"
        /// - "/already/unix/path" -> "/already/unix/path" (unchanged)
        /// </summary>
        /// <param name="path">The input Windows path</param>
        /// <returns>WSL mount format path</returns>
        public string ConvertToWslPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            // SECURITY: Prevent path traversal attacks
            ValidatePathSecurity(path);

            try
            {
                // Check if it's a Windows path with drive letter
                if (path.Length >= 2 && path[1] == ':' && char.IsLetter(path[0]))
                {
                    var driveLetter = char.ToLower(path[0]);
                    var restOfPath = path.Length > 2 ? path[2..].Replace('\\', '/') : "";

                    // Remove leading slash if present (e.g., C:\ -> /mnt/c, not /mnt/c/)
                    if (!string.IsNullOrEmpty(restOfPath) && restOfPath.StartsWith("/"))
                    {
                        restOfPath = restOfPath[1..];
                    }

                    return string.IsNullOrEmpty(restOfPath) ? $"/mnt/{driveLetter}" : $"/mnt/{driveLetter}/{restOfPath}";
                }

                // If it's already a Unix-style path, just normalize slashes
                return path.Replace('\\', '/');
            }
            catch (Exception)
            {
                // If any error occurs during conversion, return the original path with normalized slashes
                return path.Replace('\\', '/');
            }
        }

        /// <summary>
        /// Determines if a path is in Windows format (starts with drive letter)
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path is a Windows path, false otherwise</returns>
        public bool IsWindowsPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length < 2)
            {
                return false;
            }

            return char.IsLetter(path[0]) && path[1] == ':';
        }

        /// <summary>
        /// Normalizes a path for Windows usage by converting WSL paths to Windows format.
        /// This is the main method that should be used by MCP tools when receiving path parameters.
        /// </summary>
        /// <param name="path">The input path that may be in various formats</param>
        /// <returns>Windows-compatible path suitable for file operations</returns>
        public string NormalizeForWindows(string path)
        {
            return ConvertToWindowsPath(path);
        }

        /// <summary>
        /// Normalizes multiple paths for Windows usage.
        /// </summary>
        /// <param name="paths">Array of paths that may be in various formats</param>
        /// <returns>Array of Windows-compatible paths</returns>
        public string[] NormalizeForWindows(params string[] paths)
        {
            if (paths == null)
            {
                return [];
            }

            return [.. paths.Select(NormalizeForWindows)];
        }

        // Intentionally no IsWslMountPath in production to avoid unused public API.

        /// <summary>
        /// Clears the internal path conversion cache.
        /// Useful for testing or when mount configuration changes.
        /// </summary>
        public void ClearCache()
        {
            m_ConversionCache.Clear();
            lock (m_FstabLock)
            {
                m_FstabMountMap.Clear();
                m_FstabLastLoadedUtc = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Validates path security to prevent traversal attacks and malicious paths
        /// </summary>
        /// <param name="path">The path to validate</param>
        /// <exception cref="ArgumentException">Thrown if path contains security risks</exception>
        private static void ValidatePathSecurity(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            // SECURITY: Check for path traversal patterns
            var normalizedPath = path.Replace('\\', '/').ToLowerInvariant();

            // Dangerous patterns that could be used for path traversal
            string[] dangerousPatterns = [
                "../",      // Parent directory traversal
                "..\\",     // Windows parent directory traversal
                "~",        // Home directory expansion
                "%",        // Environment variable expansion
                "$",        // Variable expansion (Unix/PowerShell)
                "\r",       // Carriage return
                "\n"        // Line feed
            ];

            foreach (var pattern in dangerousPatterns)
            {
                if (normalizedPath.Contains(pattern, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ArgumentException($"Path contains potentially dangerous pattern '{pattern}': {path}", nameof(path));
                }
            }

            // SECURITY: Check for UNC paths (\\server\share) which could be used for network attacks
            if (path.StartsWith("\\\\") || path.StartsWith("//"))
            {
                throw new ArgumentException($"UNC paths are not allowed for security reasons: {path}", nameof(path));
            }

            // SECURITY: Check for excessively long paths that could cause buffer overflows
            const int MaxPathLength = 260; // Windows MAX_PATH
            if (path.Length > MaxPathLength)
            {
                throw new ArgumentException($"Path exceeds maximum allowed length ({MaxPathLength} characters): {path.Length}", nameof(path));
            }

            // SECURITY: Check for control characters that could be used for injection
            if (path.Any(c => char.IsControl(c) && c != '\t'))
            {
                throw new ArgumentException($"Path contains invalid control characters: {path}", nameof(path));
            }
        }
    }
}

