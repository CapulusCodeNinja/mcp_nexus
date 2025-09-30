using System.Runtime.InteropServices;
using System.Diagnostics;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles CDB session configuration, validation, and path resolution
    /// </summary>
    public class CdbSessionConfiguration
    {
        public int CommandTimeoutMs { get; }
        public string? CustomCdbPath { get; }
        public int SymbolServerTimeoutMs { get; }
        public int SymbolServerMaxRetries { get; }
        public string? SymbolSearchPath { get; }
        public int StartupDelayMs { get; }

        public CdbSessionConfiguration(
            int commandTimeoutMs = 30000,
            string? customCdbPath = null,
            int symbolServerTimeoutMs = 30000,
            int symbolServerMaxRetries = 1,
            string? symbolSearchPath = null,
            int startupDelayMs = 2000)
        {
            ValidateParameters(commandTimeoutMs, symbolServerTimeoutMs, symbolServerMaxRetries, startupDelayMs);

            CommandTimeoutMs = commandTimeoutMs;
            CustomCdbPath = customCdbPath;
            SymbolServerTimeoutMs = symbolServerTimeoutMs;
            SymbolServerMaxRetries = symbolServerMaxRetries;
            SymbolSearchPath = symbolSearchPath;
            StartupDelayMs = startupDelayMs;
        }

        /// <summary>
        /// Validates configuration parameters
        /// </summary>
        public static void ValidateParameters(
            int commandTimeoutMs,
            int symbolServerTimeoutMs,
            int symbolServerMaxRetries,
            int startupDelayMs)
        {
            if (commandTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(commandTimeoutMs), "Command timeout must be positive");

            if (symbolServerTimeoutMs < 0)
                throw new ArgumentOutOfRangeException(nameof(symbolServerTimeoutMs), "Symbol server timeout cannot be negative");

            if (symbolServerMaxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(symbolServerMaxRetries), "Symbol server max retries cannot be negative");

            if (startupDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(startupDelayMs), "Startup delay cannot be negative");
        }

        /// <summary>
        /// Finds the CDB executable path
        /// </summary>
        public string? FindCdbPath()
        {
            // Use custom path if provided
            if (!string.IsNullOrWhiteSpace(CustomCdbPath))
            {
                if (File.Exists(CustomCdbPath))
                    return CustomCdbPath;

                throw new FileNotFoundException($"Custom CDB path not found: {CustomCdbPath}");
            }

            return FindCdbInStandardLocations();
        }

        /// <summary>
        /// Gets the current system architecture for CDB selection
        /// </summary>
        public string GetCurrentArchitecture()
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                _ => "x64" // Default fallback
            };
        }

        /// <summary>
        /// Searches for CDB using the exact same logic as the original working tag 1.0.4
        /// </summary>
        private string? FindCdbInStandardLocations()
        {
            var currentArch = GetCurrentArchitecture();

            // Create prioritized list based on current architecture (like original tag 1.0.4)
            var possiblePaths = new List<string>();

            // Add paths for current architecture first
            switch (currentArch)
            {
                case "x64":
                    possiblePaths.AddRange(new[]
                    {
                        @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                        @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
                        @"C:\Program Files (x86)\Debugging Tools for Windows (x64)\cdb.exe",
                        @"C:\Program Files\Debugging Tools for Windows (x64)\cdb.exe"
                    });
                    break;
                case "x86":
                    possiblePaths.AddRange(new[]
                    {
                        @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe",
                        @"C:\Program Files\Windows Kits\10\Debuggers\x86\cdb.exe",
                        @"C:\Program Files (x86)\Debugging Tools for Windows (x86)\cdb.exe",
                        @"C:\Program Files\Debugging Tools for Windows (x86)\cdb.exe"
                    });
                    break;
                case "arm64":
                    possiblePaths.AddRange(new[]
                    {
                        @"C:\Program Files (x86)\Windows Kits\10\Debuggers\arm64\cdb.exe",
                        @"C:\Program Files\Windows Kits\10\Debuggers\arm64\cdb.exe"
                    });
                    break;
            }

            // Add fallback paths for other architectures (like original code)
            if (currentArch != "x64")
            {
                possiblePaths.AddRange(new[]
                {
                    @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                    @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
                    @"C:\Program Files (x86)\Debugging Tools for Windows (x64)\cdb.exe",
                    @"C:\Program Files\Debugging Tools for Windows (x64)\cdb.exe"
                });
            }

            if (currentArch != "x86")
            {
                possiblePaths.AddRange(new[]
                {
                    @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe",
                    @"C:\Program Files\Windows Kits\10\Debuggers\x86\cdb.exe",
                    @"C:\Program Files (x86)\Debugging Tools for Windows (x86)\cdb.exe",
                    @"C:\Program Files\Debugging Tools for Windows (x86)\cdb.exe"
                });
            }

            if (currentArch != "arm64")
            {
                possiblePaths.AddRange(new[]
                {
                    @"C:\Program Files (x86)\Windows Kits\10\Debuggers\arm64\cdb.exe",
                    @"C:\Program Files\Windows Kits\10\Debuggers\arm64\cdb.exe"
                });
            }

            // Check all possible paths
            foreach (var path in possiblePaths)
            {
                try
                {
                    if (File.Exists(path))
                        return path;
                }
                catch
                {
                    // Continue searching if path check fails
                }
            }

            // Try to find in PATH using 'where' command (like original tag 1.0.4)
            try
            {
                using var result = Process.Start(new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "cdb.exe",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (result != null && result.WaitForExit(5000)) // 5 second timeout
                {
                    var output = result.StandardOutput.ReadToEnd();
                    if (result.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            return lines[0].Trim();
                        }
                    }
                }
            }
            catch
            {
                // PATH search failed, return null
            }

            return null;
        }

    }
}

}
