using System.Text.RegularExpressions;

namespace mcp_nexus.Utilities
{
    /// <summary>
    /// Utility class for handling path conversions between WSL and Windows formats.
    /// This class provides centralized logic for converting WSL-style paths (like /mnt/c/...)
    /// to Windows-style paths (like C:\...) for use with Windows debugging tools.
    /// </summary>
    public static class PathHandler
    {
        private static readonly Regex WslPathRegex = new(@"^/mnt/([a-zA-Z])(?:/(.*))?$", RegexOptions.Compiled);

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
        public static string ConvertToWindowsPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            try
            {
                // Check if it's a WSL mount path pattern: /mnt/[drive_letter]/rest/of/path
                var match = WslPathRegex.Match(path);
                if (match.Success)
                {
                    var driveLetter = match.Groups[1].Value.ToUpper();
                    var restOfPath = match.Groups[2].Success ? match.Groups[2].Value.Replace('/', '\\') : "";
                    
                    // Handle empty rest of path (e.g., /mnt/c or /mnt/c/ -> C:\)
                    if (string.IsNullOrEmpty(restOfPath))
                    {
                        return $"{driveLetter}:\\";
                    }
                    
                    return $"{driveLetter}:\\{restOfPath}";
                }

                // If it's already a Windows path or not a WSL mount path, return as-is
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
        /// Converts a Windows-style path to WSL mount format.
        /// Examples:
        /// - "C:\inetpub\wwwroot\uploads\dump.dmp" -> "/mnt/c/inetpub/wwwroot/uploads/dump.dmp"
        /// - "D:\symbols" -> "/mnt/d/symbols"
        /// - "/already/unix/path" -> "/already/unix/path" (unchanged)
        /// </summary>
        /// <param name="path">The input Windows path</param>
        /// <returns>WSL mount format path</returns>
        public static string ConvertToWslPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            try
            {
                // Check if it's a Windows path with drive letter
                if (path.Length >= 2 && path[1] == ':' && char.IsLetter(path[0]))
                {
                    var driveLetter = char.ToLower(path[0]);
                    var restOfPath = path.Length > 2 ? path.Substring(2).Replace('\\', '/') : "";
                    
                    // Remove leading slash if present (e.g., C:\ -> /mnt/c, not /mnt/c/)
                    if (!string.IsNullOrEmpty(restOfPath) && restOfPath.StartsWith("/"))
                    {
                        restOfPath = restOfPath.Substring(1);
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
        /// Determines if a path is in WSL mount format (/mnt/[drive_letter]/...)
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path is a WSL mount path, false otherwise</returns>
        public static bool IsWslMountPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return WslPathRegex.IsMatch(path);
        }

        /// <summary>
        /// Determines if a path is in Windows format (starts with drive letter)
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path is a Windows path, false otherwise</returns>
        public static bool IsWindowsPath(string path)
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
        public static string NormalizeForWindows(string path)
        {
            return ConvertToWindowsPath(path);
        }

        /// <summary>
        /// Normalizes multiple paths for Windows usage.
        /// </summary>
        /// <param name="paths">Array of paths that may be in various formats</param>
        /// <returns>Array of Windows-compatible paths</returns>
        public static string[] NormalizeForWindows(params string[] paths)
        {
            if (paths == null)
            {
                return Array.Empty<string>();
            }

            return paths.Select(NormalizeForWindows).ToArray();
        }
    }
}

