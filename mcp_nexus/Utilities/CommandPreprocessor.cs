using System.Text.RegularExpressions;

namespace mcp_nexus.Utilities
{
    /// <summary>
    /// Service for preprocessing WinDBG commands for path conversion and directory creation.
    /// This class ONLY handles WSL to Windows path conversion and ensures directories exist.
    /// NO syntax fixing, NO adding quotes, NO adding semicolons.
    /// Injectable service that supports proper dependency injection and testing.
    /// </summary>
    public partial class CommandPreprocessor : ICommandPreprocessor
    {
        private static readonly Regex s_PathPattern = MyRegex();
        private readonly IPathHandler m_PathHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPreprocessor"/> class.
        /// </summary>
        /// <param name="pathHandler">The path handler for WSL/Windows path conversions.</param>
        public CommandPreprocessor(IPathHandler pathHandler)
        {
            m_PathHandler = pathHandler ?? throw new ArgumentNullException(nameof(pathHandler));
        }

        /// <summary>
        /// Preprocesses a WinDBG command to convert WSL paths to Windows paths and ensure directories exist.
        /// ONLY does path conversion - NO syntax changes.
        /// </summary>
        /// <param name="command">The original command</param>
        /// <returns>The command with paths converted</returns>
        public string PreprocessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return command;
            }

            // Find and convert all WSL paths in the command (generic /mnt/<drive>/...)
            var result = s_PathPattern.Replace(command, match =>
            {
                var wslPath = match.Value;
                var windowsPath = m_PathHandler.ConvertToWindowsPath(wslPath);

                // Ensure directory exists
                EnsureDirectoryExists(windowsPath);

                return windowsPath;
            });

            // .srcpath: convert embedded srv* /mnt/... tokens and ensure local directories exist
            if (result.StartsWith(".srcpath", StringComparison.OrdinalIgnoreCase))
            {
                // Convert srv*/mnt/... to srv*<WindowsPath>
                result = Regex.Replace(result, @"srv\*(/mnt/[^"";\s]+)",
                    (Match m) =>
                    {
                        var wsl = m.Groups[1].Value.Replace('\\', '/');
                        var win = m_PathHandler.ConvertToWindowsPath(wsl);
                        return "srv*" + win;
                    },
                    RegexOptions.IgnoreCase);

                // Ensure directories for non-srv tokens
                var match = Regex.Match(result, @"^\.srcpath\+?\s+(.+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var pathArg = match.Groups[1].Value;
                    var paths = pathArg.Split([';', ' '], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var path in paths)
                    {
                        var cleanPath = path.Trim().Trim('"').Trim('\'');
                        if (!cleanPath.StartsWith("srv", StringComparison.OrdinalIgnoreCase))
                        {
                            EnsureDirectoryExists(cleanPath);
                        }
                    }
                }
            }

            // Handle .sympath (set symbol path) - ensure local directories exist, skip srv*/http tokens
            else if (result.StartsWith(".sympath", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(result, @"^\.sympath\+?\s+(.+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var pathArg = match.Groups[1].Value;
                    var parts = pathArg.Split([';', ' '], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var clean = part.Trim().Trim('"').Trim('\'');
                        if (ShouldSkipSymbolPathToken(clean))
                            continue;
                        EnsureDirectoryExists(clean);
                    }
                }
            }

            // Handle .symfix (set default symbol path with optional downstream store) - ensure local store exists
            else if (result.StartsWith(".symfix", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(result, @"^\.symfix\+?\s+(.+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var arg = match.Groups[1].Value;
                    var parts = arg.Split([';', ' '], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var clean = part.Trim().Trim('"').Trim('\'');
                        if (ShouldSkipSymbolPathToken(clean))
                            continue;
                        EnsureDirectoryExists(clean);
                    }
                }
            }

            // Handle !homedir (set home directory for extensions and configuration) - ensure directory exists
            // Convert backslashes to forward slashes to work around CDB's path handling quirks
            else if (result.StartsWith("!homedir", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(result, @"^(!homedir\s+)(.+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var commandPrefix = match.Groups[1].Value;
                    var pathArg = match.Groups[2].Value;
                    var hasQuotes = pathArg.StartsWith('"') && pathArg.EndsWith('"');
                    var path = pathArg.Trim().Trim('"').Trim('\'');

                    // Ensure directory exists before converting slashes
                    EnsureDirectoryExists(path);

                    // Convert backslashes to forward slashes (CDB handles these better)
                    var pathWithForwardSlashes = path.Replace('\\', '/');

                    // Reconstruct command with forward slashes
                    if (hasQuotes)
                    {
                        result = $"{commandPrefix}\"{pathWithForwardSlashes}\"";
                    }
                    else
                    {
                        result = $"{commandPrefix}{pathWithForwardSlashes}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Ensures that a directory exists, creating it if necessary.
        /// </summary>
        /// <param name="path">The directory path to ensure exists</param>
        private void EnsureDirectoryExists(string path)
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
                if (m_PathHandler.IsWindowsPath(path))
                {
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        // Directory creation is logged at the command execution level if needed
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore directory creation failures
                // The .srcpath command will handle the error appropriately
            }
        }

        private static bool ShouldSkipSymbolPathToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return true;

            // Skip symbol server specifiers and URLs
            if (token.StartsWith("srv", StringComparison.OrdinalIgnoreCase)) return true;
            if (token.StartsWith("symsrv", StringComparison.OrdinalIgnoreCase)) return true;
            if (token.StartsWith("cache", StringComparison.OrdinalIgnoreCase)) return true;
            if (token.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return true;

            // Skip UNC paths (handled upstream as disallowed for creation here)
            if (token.StartsWith("\\\\")) return true;

            return false;
        }

        [GeneratedRegex(@"/mnt/[^/\s;""]+/[^\s;""]*", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
    }
}
