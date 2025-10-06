using System.Text.RegularExpressions;

namespace mcp_nexus.Utilities
{
    /// <summary>
    /// Preprocesses WinDBG commands to fix common syntax issues and path problems.
    /// This class handles command-specific fixes like .srcpath path conversion and syntax correction.
    /// </summary>
    public static class CommandPreprocessor
    {
        private static readonly Regex SrcPathCommandRegex = new(
            @"^\.srcpath\s*(?:\+)?\s*(.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Preprocesses a WinDBG command to fix common issues.
        /// Currently handles .srcpath command path conversion and syntax fixes.
        /// </summary>
        /// <param name="command">The original command</param>
        /// <returns>The preprocessed command with fixes applied</returns>
        public static string PreprocessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return command;
            }

            var trimmedCommand = command.Trim();

            // Handle .srcpath commands
            if (trimmedCommand.StartsWith(".srcpath", StringComparison.OrdinalIgnoreCase))
            {
                return PreprocessSrcPathCommand(trimmedCommand);
            }

            // Return original command if no preprocessing needed
            return command;
        }

        /// <summary>
        /// Preprocesses .srcpath commands to fix path issues and syntax problems.
        /// </summary>
        /// <param name="command">The .srcpath command</param>
        /// <returns>The fixed .srcpath command</returns>
        private static string PreprocessSrcPathCommand(string command)
        {
            var match = SrcPathCommandRegex.Match(command);
            if (!match.Success)
            {
                return command; // Return original if we can't parse it
            }

            var pathArgument = match.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(pathArgument))
            {
                return command; // No paths to process
            }

            // Parse and fix the paths
            var fixedPaths = ParseAndFixSrcPathArgument(pathArgument);

            // Reconstruct the command
            var commandPrefix = command.Substring(0, match.Groups[1].Index).TrimEnd();
            return $"{commandPrefix} {fixedPaths}";
        }

        /// <summary>
        /// Parses the .srcpath argument and fixes path issues.
        /// </summary>
        /// <param name="pathArgument">The path argument from .srcpath command</param>
        /// <returns>The fixed path argument</returns>
        private static string ParseAndFixSrcPathArgument(string pathArgument)
        {
            // Remove outer quotes if present
            var unquoted = pathArgument.Trim();
            if ((unquoted.StartsWith('"') && unquoted.EndsWith('"')) ||
                (unquoted.StartsWith('\'') && unquoted.EndsWith('\'')))
            {
                unquoted = unquoted.Substring(1, unquoted.Length - 2);
            }

            // Split by semicolon to handle multiple paths
            var pathParts = unquoted.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var fixedParts = new List<string>();

            foreach (var part in pathParts)
            {
                var trimmedPart = part.Trim();
                if (string.IsNullOrEmpty(trimmedPart))
                    continue;

                var fixedPart = FixSrcPathPart(trimmedPart);
                if (!string.IsNullOrEmpty(fixedPart))
                {
                    fixedParts.Add(fixedPart);
                }
            }

            // Join with semicolons and quote the entire argument
            var result = string.Join(";", fixedParts);
            return $"\"{result}\"";
        }

        /// <summary>
        /// Fixes individual path parts in .srcpath commands.
        /// </summary>
        /// <param name="pathPart">The individual path part</param>
        /// <returns>The fixed path part</returns>
        private static string FixSrcPathPart(string pathPart)
        {
            var trimmed = pathPart.Trim();

            // Handle source server paths (srv*)
            if (trimmed.StartsWith("srv", StringComparison.OrdinalIgnoreCase))
            {
                // Fix common issues with srv* paths
                if (trimmed.StartsWith("srv\\*", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if there's more content after srv\*
                    var afterSrv = trimmed.Substring(5); // Remove "srv\*"
                    if (!string.IsNullOrEmpty(afterSrv))
                    {
                        // This is actually a malformed path like "srv\*/mnt/c/..." 
                        // We need to split it properly
                        var convertedPath = PathHandler.ConvertToWindowsPath(afterSrv);
                        var normalizedPath = convertedPath.Replace('/', '\\');
                        
                        // Ensure the directory exists
                        EnsureDirectoryExists(normalizedPath);
                        
                        return $"srv*;{normalizedPath}";
                    }
                    return "srv*"; // Just srv\* without additional path
                }
                else if (trimmed.StartsWith("srv*", StringComparison.OrdinalIgnoreCase))
                {
                    return "srv*"; // Already correct
                }
                else if (trimmed.StartsWith("srv", StringComparison.OrdinalIgnoreCase))
                {
                    return "srv*"; // Add missing asterisk
                }
            }

            // Handle regular file paths - use existing PathHandler for conversion
            var windowsPath = PathHandler.ConvertToWindowsPath(trimmed);

            // Normalize path separators to Windows style
            windowsPath = windowsPath.Replace('/', '\\');

            // Ensure the directory exists
            EnsureDirectoryExists(windowsPath);

            return windowsPath;
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
