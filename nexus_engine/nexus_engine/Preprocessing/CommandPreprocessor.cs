using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using Nexus.Config;
using Nexus.External.Apis.FileSystem;

namespace Nexus.Engine.Preprocessing;

/// <summary>
/// Service for preprocessing WinDBG commands for path conversion and directory creation.
/// This class ONLY handles WSL to Windows path conversion and ensures directories exist.
/// NO syntax fixing, NO adding quotes, NO adding semicolons.
/// </summary>
internal partial class CommandPreprocessor
{
    /// <summary>
    /// Path handler for WSL to Windows path conversion.
    /// </summary>
    private readonly PathHandler m_PathHandler = new();

    /// <summary>
    /// File system abstraction for directory operations.
    /// </summary>
    private readonly IFileSystem m_FileSystem;

    /// <summary>
    /// Cache for preprocessed commands to avoid repeated regex/IO operations.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> m_CommandCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPreprocessor"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction for directory operations.</param>
    public CommandPreprocessor(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <summary>
    /// Preprocesses a WinDBG command to convert WSL paths to Windows paths and ensure directories exist.
    /// ONLY does path conversion - NO syntax changes.
    /// </summary>
    /// <param name="command">The original command.</param>
    /// <returns>The command with paths converted.</returns>
    public string PreprocessCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return command;
        }

        var preprocessingEnabled = Settings.Instance.Get().McpNexus.Debugging.EnableCommandPreprocessing;
        if (!preprocessingEnabled)
        {
            return command;
        }

        // Cache exact command string to avoid repeated regex/IO for identical inputs
        return m_CommandCache.GetOrAdd(command, ProcessInternal);
    }

    /// <summary>
    /// Internal method to process a command.
    /// </summary>
    /// <param name="original">The original command.</param>
    /// <returns>The preprocessed command.</returns>
    private string ProcessInternal(string original)
    {
        // Find and convert all WSL paths in the command (generic /mnt/<drive>/...)
        var result = WslPathPattern().Replace(original, match =>
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
            result = ProcessSrcPathCommand(result);
        }

        // Handle .sympath (set symbol path) - ensure local directories exist, skip srv*/http tokens
        else if (result.StartsWith(".sympath", StringComparison.OrdinalIgnoreCase))
        {
            result = ProcessSymPathCommand(result);
        }

        // Handle .symfix (set default symbol path with optional downstream store) - ensure local store exists
        else if (result.StartsWith(".symfix", StringComparison.OrdinalIgnoreCase))
        {
            result = ProcessSymFixCommand(result);
        }

        // Handle !homedir (set home directory for extensions and configuration) - ensure directory exists
        else if (result.StartsWith("!homedir", StringComparison.OrdinalIgnoreCase))
        {
            result = ProcessHomeDirCommand(result);
        }

        return result;
    }

    /// <summary>
    /// Processes .srcpath command to convert paths and ensure directories exist.
    /// </summary>
    /// <param name="command">The .srcpath command.</param>
    /// <returns>The processed command.</returns>
    private string ProcessSrcPathCommand(string command)
    {
        // Convert srv*/mnt/... to srv*<WindowsPath>
        var result = SrvMntToken().Replace(command, match =>
        {
            var wsl = match.Groups[1].Value.Replace('\\', '/');
            var win = m_PathHandler.ConvertToWindowsPath(wsl);
            return "srv*" + win;
        });

        // Ensure directories for non-srv tokens
        var match = SrcPathLine().Match(result);
        if (match.Success)
        {
            var pathArg = match.Groups[1].Value;
            foreach (var path in SplitPathTokens(pathArg))
            {
                if (!path.StartsWith("srv", StringComparison.OrdinalIgnoreCase))
                {
                    EnsureDirectoryExists(path);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Processes .sympath command to ensure local directories exist.
    /// </summary>
    /// <param name="command">The .sympath command.</param>
    /// <returns>The processed command.</returns>
    private string ProcessSymPathCommand(string command)
    {
        var match = SymPathLine().Match(command);
        if (match.Success)
        {
            var pathArg = match.Groups[1].Value;
            foreach (var part in SplitPathTokens(pathArg))
            {
                if (!ShouldSkipSymbolPathToken(part))
                {
                    EnsureDirectoryExists(part);
                }
            }
        }

        return command;
    }

    /// <summary>
    /// Processes .symfix command to ensure local directories exist.
    /// </summary>
    /// <param name="command">The .symfix command.</param>
    /// <returns>The processed command.</returns>
    private string ProcessSymFixCommand(string command)
    {
        var match = SymFixLine().Match(command);
        if (match.Success)
        {
            var arg = match.Groups[1].Value;
            foreach (var part in SplitPathTokens(arg))
            {
                if (!ShouldSkipSymbolPathToken(part))
                {
                    EnsureDirectoryExists(part);
                }
            }
        }

        return command;
    }

    /// <summary>
    /// Processes !homedir command to convert backslashes to forward slashes and ensure directory exists.
    /// </summary>
    /// <param name="command">The !homedir command.</param>
    /// <returns>The processed command.</returns>
    private string ProcessHomeDirCommand(string command)
    {
        var match = HomeDirLine().Match(command);
        if (!match.Success)
        {
            return command;
        }

        var commandPrefix = match.Groups[1].Value;
        var pathArg = match.Groups[2].Value;
        var hasQuotes = pathArg.StartsWith('"') && pathArg.EndsWith('"');
        var path = pathArg.Trim().Trim('"').Trim('\'');

        // Ensure directory exists before converting slashes
        EnsureDirectoryExists(path);

        // Convert backslashes to forward slashes (CDB handles these better)
        var pathWithForwardSlashes = path.Replace('\\', '/');

        // Reconstruct command with forward slashes
        return hasQuotes
            ? $"{commandPrefix}\"{pathWithForwardSlashes}\""
            : $"{commandPrefix}{pathWithForwardSlashes}";
    }

    /// <summary>
    /// Splits path tokens from a command argument.
    /// </summary>
    /// <param name="raw">The raw path argument string.</param>
    /// <returns>List of path tokens.</returns>
    private static List<string> SplitPathTokens(string raw)
    {
        var tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return tokens;
        }

        var span = raw.AsSpan();
        var start = 0;
        for (var i = 0; i <= span.Length; i++)
        {
            var atEnd = i == span.Length;
            if (atEnd || span[i] == ';' || span[i] == ' ')
            {
                if (i > start)
                {
                    var token = span[start..i].Trim();
                    token = token.Trim('"');
                    token = token.Trim('\'');
                    if (!token.IsEmpty)
                    {
                        tokens.Add(new string(token));
                    }
                }

                start = i + 1;
            }
        }

        return tokens;
    }

    /// <summary>
    /// Ensures that a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">The directory path to ensure exists.</param>
    private void EnsureDirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

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
                if (!m_FileSystem.DirectoryExists(path))
                {
                    m_FileSystem.CreateDirectory(path);
                }
            }
        }
        catch (Exception)
        {
            // Silently ignore directory creation failures
            // The command execution will handle the error appropriately
        }
    }

    /// <summary>
    /// Determines whether a symbol path token should be skipped for directory creation.
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>True if the token should be skipped, false otherwise.</returns>
    private static bool ShouldSkipSymbolPathToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        // Skip symbol server specifiers and URLs
        if (token.StartsWith("srv", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (token.StartsWith("symsrv", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (token.StartsWith("cache", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (token.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Skip UNC paths
        return token.StartsWith("\\\\");
    }

    /// <summary>
    /// Regular expression to match WSL paths (e.g., /mnt/c/path).
    /// </summary>
    /// <returns>Compiled regex.</returns>
    [GeneratedRegex(@"/mnt/[^/\s;""]+/[^\s;""]*", RegexOptions.Compiled)]
    private static partial Regex WslPathPattern();

    /// <summary>
    /// Regular expression to match .srcpath command line.
    /// </summary>
    /// <returns>Compiled regex.</returns>
    [GeneratedRegex(@"^\.srcpath\+?\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex SrcPathLine();

    /// <summary>
    /// Regular expression to match .sympath command line.
    /// </summary>
    /// <returns>Compiled regex.</returns>
    [GeneratedRegex(@"^\.sympath\+?\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex SymPathLine();

    /// <summary>
    /// Regular expression to match .symfix command line.
    /// </summary>
    /// <returns>Compiled regex.</returns>
    [GeneratedRegex(@"^\.symfix\+?\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex SymFixLine();

    /// <summary>
    /// Regular expression to match !homedir command line.
    /// </summary>
    /// <returns>Compiled regex.</returns>
    [GeneratedRegex(@"^(!homedir\s+)(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex HomeDirLine();

    /// <summary>
    /// Regular expression to match srv* tokens with WSL paths.
    /// </summary>
    /// <returns>Compiled regex.</returns>
    [GeneratedRegex(@"srv\*(/mnt/[^"";\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex SrvMntToken();
}
