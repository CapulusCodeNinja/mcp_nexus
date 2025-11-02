using System.Diagnostics;
using System.Text.RegularExpressions;

using Nexus.External.Apis.ProcessManagement;

namespace Nexus.Engine.Preprocessing;

/// <summary>
/// Handles WSL path conversion using wsl.exe for accurate path translation.
/// </summary>
internal partial class WslPathConverter
{
    private const int WslHelperTimeoutMs = 2000;
    private readonly IProcessManager m_ProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WslPathConverter"/> class.
    /// </summary>
    /// <param name="processManager">The process manager for executing wsl.exe.</param>
    public WslPathConverter(IProcessManager processManager)
    {
        m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
    }

    /// <summary>
    /// Attempts to convert a Linux path to a Windows path using 'wsl.exe wslpath -w'.
    /// Uses a short timeout and fails silently to avoid blocking.
    /// </summary>
    /// <param name="wslPath">The WSL path to convert.</param>
    /// <param name="windowsPath">The converted Windows path.</param>
    /// <returns>True if conversion succeeded, false otherwise.</returns>
    public bool TryConvertToWindowsPath(string wslPath, out string windowsPath)
    {
        windowsPath = wslPath;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-e wslpath -w \"{wslPath.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = m_ProcessManager.StartProcess(psi);
            if (proc == null)
            {
                return false;
            }

            if (!m_ProcessManager.WaitForProcessExit(proc, WslHelperTimeoutMs))
            {
                try
                {
                    m_ProcessManager.KillProcess(proc);
                }
                catch
                {
                    // Ignore kill failures
                }

                return false;
            }

            if (proc.ExitCode == 0)
            {
                var output = proc.StandardOutput.ReadToEnd().Trim();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    windowsPath = output.Replace('/', '\\');
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
    /// Attempts to load mount mappings from '/etc/fstab' via WSL.
    /// This supports custom mount points, e.g. mapping \\server\share to /mnt/share.
    /// </summary>
    /// <returns>Dictionary of mount point to Windows root mappings.</returns>
    public Dictionary<string, string> LoadFstabMappings()
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-e cat /etc/fstab",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = m_ProcessManager.StartProcess(psi);
            if (proc == null)
            {
                return mappings;
            }

            if (!m_ProcessManager.WaitForProcessExit(proc, WslHelperTimeoutMs))
            {
                try
                {
                    m_ProcessManager.KillProcess(proc);
                }
                catch
                {
                    // Ignore kill failures
                }

                return mappings;
            }

            var text = proc.StandardOutput.ReadToEnd();

            foreach (var rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.StartsWith("#"))
                {
                    continue;
                }

                // fstab format: <src> <mount> <type> <opts> <dump> <pass>
                var parts = WsSplitRegex().Split(line);
                if (parts.Length < 3)
                {
                    continue;
                }

                var src = parts[0];
                var mount = parts[1];
                var type = parts[2];

                if (!string.Equals(type, "drvfs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Map mount -> windows root
                string? root = null;
                if (src.Length >= 2 && src[1] == ':' && char.IsLetter(src[0]))
                {
                    // Handle both "C:" and "C:/some/path"
                    root = src.Replace('/', '\\');

                    // Ensure drive letter is uppercase
                    root = char.ToUpperInvariant(root[0]) + root[1..];

                    // Ensure backslash after drive letter
                    if (root.Length == 2 || (root.Length > 2 && root[2] != '\\'))
                    {
                        root = root[..2] + '\\' + root[2..].TrimStart('\\');
                    }
                }
                else if (src.StartsWith("//") || src.StartsWith("\\\\"))
                {
                    // UNC like //server/share
                    var unc = src.Replace('/', '\\');
                    if (!unc.StartsWith("\\\\"))
                    {
                        unc = "\\\\" + unc.TrimStart('\\');
                    }

                    root = unc;
                }

                if (root != null)
                {
                    // Normalize mount key (no trailing slash)
                    var key = mount.EndsWith('/') ? mount.TrimEnd('/') : mount;
                    if (!mappings.ContainsKey(key))
                    {
                        mappings[key] = root;
                    }
                }
            }
        }
        catch
        {
            // Ignore failures; return empty mappings
        }

        return mappings;
    }

    /// <summary>
    /// Regular expression for splitting whitespace in fstab lines.
    /// </summary>
    /// <returns>Compiled regex.</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex WsSplitRegex();
}
