using System.Diagnostics;
using System.Text.RegularExpressions;

namespace mcp_nexus.Utilities
{
    /// <summary>
    /// Real implementation of WSL path conversion using wsl.exe.
    /// </summary>
    public class WslPathConverter : IWslPathConverter
    {
        private const int WslHelperTimeoutMs = 2000;

        /// <summary>
        /// Attempts to convert a Linux path to a Windows path using 'wsl.exe wslpath -w'.
        /// Uses a short timeout and fails silently to avoid blocking.
        /// </summary>
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
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    return false;
                }

                if (!proc.WaitForExit(WslHelperTimeoutMs))
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
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
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                    return mappings;

                if (!proc.WaitForExit(WslHelperTimeoutMs))
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
                    return mappings;
                }

                var text = proc.StandardOutput.ReadToEnd();

                foreach (var rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var line = rawLine.Trim();
                    if (line.StartsWith("#")) continue;

                    // fstab format: <src> <mount> <type> <opts> <dump> <pass>
                    var parts = Regex.Split(line, @"\s+");
                    if (parts.Length < 3) continue;

                    var src = parts[0];
                    var mount = parts[1];
                    var type = parts[2];

                    if (!string.Equals(type, "drvfs", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Map mount -> windows root
                    string? root = null;
                    if (src.Length >= 2 && src[1] == ':' && char.IsLetter(src[0]))
                    {
                        // e.g., C:
                        root = char.ToUpperInvariant(src[0]) + ":\\";
                    }
                    else if (src.StartsWith("//") || src.StartsWith("\\\\"))
                    {
                        // UNC like //server/share
                        var unc = src.Replace('/', '\\');
                        if (!unc.StartsWith("\\\\")) unc = "\\\\" + unc.TrimStart('\\');
                        root = unc;
                    }

                    if (root != null)
                    {
                        // Normalize mount key (no trailing slash)
                        var key = mount.EndsWith('/') ? mount.TrimEnd('/') : mount;
                        if (!mappings.ContainsKey(key))
                            mappings[key] = root;
                    }
                }
            }
            catch
            {
                // Ignore failures; return empty mappings
            }

            return mappings;
        }
    }
}

