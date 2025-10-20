using mcp_nexus.Utilities.PathHandling;

namespace mcp_nexus_unit_tests.Mocks
{
    /// <summary>
    /// Mock implementation of IWslPathConverter for testing PathHandler without requiring actual WSL.
    /// This makes tests portable and deterministic across all systems.
    /// </summary>
    public class MockWslPathConverter : IWslPathConverter
    {
        /// <summary>
        /// Mock implementation that converts standard /mnt/ paths to Windows format
        /// without invoking wsl.exe.
        /// </summary>
        public bool TryConvertToWindowsPath(string wslPath, out string windowsPath)
        {
            windowsPath = wslPath;

            // Handle standard /mnt/<letter>/ format
            if (wslPath.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase) && wslPath.Length >= 6)
            {
                var driveLetter = wslPath[5];
                if (char.IsLetter(driveLetter))
                {
                    // Check for trailing slash or path continuation
                    if (wslPath.Length == 6 || wslPath[6] == '/')
                    {
                        var remaining = wslPath.Length > 7 ? wslPath[7..].Replace('/', '\\') : string.Empty;
                        windowsPath = $"{char.ToUpper(driveLetter)}:\\{remaining}";
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Mock implementation that returns standard drive mount mappings
        /// without invoking wsl.exe.
        /// </summary>
        public Dictionary<string, string> LoadFstabMappings()
        {
            // Return standard /mnt/c -> C:\ and /mnt/d -> D:\ mappings
            // Also include multi-character mount points for testing
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "/mnt/c", "C:\\" },
                { "/mnt/d", "D:\\" },
                { "/mnt/e", "E:\\" },
                { "/mnt/f", "F:\\" },
                { "/mnt/analysis", "C:\\analysis" },
                { "/mnt/share", "C:\\share" }
            };
        }
    }
}

