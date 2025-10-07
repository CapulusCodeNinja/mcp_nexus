using System.Text.RegularExpressions;

namespace mcp_nexus.Utilities
{
    /// <summary>
    /// Preprocesses WinDBG commands for path conversion and directory creation only.
    /// This class ONLY handles WSL to Windows path conversion and ensures directories exist.
    /// NO syntax fixing, NO adding quotes, NO adding semicolons.
    /// </summary>
    public static class CommandPreprocessor
    {
        private static readonly Regex PathPattern = new(
            @"/mnt/[a-zA-Z]/[^\s;""]+",
            RegexOptions.Compiled);

        /// <summary>
        /// Preprocesses a WinDBG command to convert WSL paths to Windows paths and ensure directories exist.
        /// ONLY does path conversion - NO syntax changes.
        /// </summary>
        /// <param name="command">The original command</param>
        /// <returns>The command with paths converted</returns>
        public static string PreprocessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return command;
            }

            // Find and convert all WSL paths in the command
            var result = PathPattern.Replace(command, match =>
            {
                var wslPath = match.Value;
                var windowsPath = PathHandler.ConvertToWindowsPath(wslPath);
                
                // Ensure directory exists
                EnsureDirectoryExists(windowsPath);
                
                return windowsPath;
            });

            // Also ensure any Windows paths that are directory arguments exist
            // Look for .srcpath commands and create the target directory
            if (result.StartsWith(".srcpath", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(result, @"^\.srcpath\+?\s+(.+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var pathArg = match.Groups[1].Value;
                    // Extract potential paths from the argument (handle semicolon-separated lists)
                    var paths = pathArg.Split(new[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var path in paths)
                    {
                        var cleanPath = path.Trim().Trim('"').Trim('\'');
                        // Skip srv* and similar special paths
                        if (!cleanPath.StartsWith("srv", StringComparison.OrdinalIgnoreCase))
                        {
                            EnsureDirectoryExists(cleanPath);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Ensures that a directory exists, creating it if necessary.
        /// </summary>
        /// <param name="path">The directory path to ensure exists</param>
        private static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                // Skip source server paths and UNC paths
                if (path.StartsWith("srv", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Check if it's a valid Windows path
                if (PathHandler.IsWindowsPath(path))
                {
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        // Note: We can't use ILogger here since this is a static utility class
                        // Directory creation will be logged at the command execution level
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore directory creation failures
                // The .srcpath command will handle the error appropriately
            }
        }
    }
}
