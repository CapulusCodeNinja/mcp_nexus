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
        public int IdleTimeoutMs { get; }
        public string? CustomCdbPath { get; }
        public int SymbolServerTimeoutMs { get; }
        public int SymbolServerMaxRetries { get; }
        public string? SymbolSearchPath { get; }
        public int StartupDelayMs { get; }

        public CdbSessionConfiguration(
            int commandTimeoutMs = 30000,
            int idleTimeoutMs = 180000,
            string? customCdbPath = null,
            int symbolServerTimeoutMs = 30000,
            int symbolServerMaxRetries = 1,
            string? symbolSearchPath = null,
            int startupDelayMs = 2000)
        {
            ValidateParameters(commandTimeoutMs, idleTimeoutMs, symbolServerTimeoutMs, symbolServerMaxRetries, startupDelayMs);

            CommandTimeoutMs = commandTimeoutMs;
            IdleTimeoutMs = idleTimeoutMs;
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
            int idleTimeoutMs,
            int symbolServerTimeoutMs,
            int symbolServerMaxRetries,
            int startupDelayMs)
        {
            if (commandTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(commandTimeoutMs), "Command timeout must be positive");

            if (idleTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(idleTimeoutMs), "Idle timeout must be positive");

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

            var result = FindCdbInStandardLocations();
            if (result == null)
            {
                Console.WriteLine("🔍 CDB DETECTION FAILED - No CDB found in standard locations or PATH");
            }
            else
            {
                Console.WriteLine($"✅ CDB FOUND: {result}");
            }
            return result;
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
                        @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
                        @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                        @"C:\Program Files\Debugging Tools for Windows (x64)\cdb.exe",
                        @"C:\Program Files (x86)\Debugging Tools for Windows (x64)\cdb.exe"
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
                    @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
                    @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                    @"C:\Program Files\Debugging Tools for Windows (x64)\cdb.exe",
                    @"C:\Program Files (x86)\Debugging Tools for Windows (x64)\cdb.exe"
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
            Console.WriteLine($"🔍 Checking {possiblePaths.Count} hardcoded CDB paths for architecture: {currentArch}");
            foreach (var path in possiblePaths)
            {
                try
                {
                    Console.WriteLine($"   Checking: {path}");
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"   ✅ FOUND: {path}");
                        return path;
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Not found: {path}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️ Error checking {path}: {ex.Message}");
                    // Continue searching if path check fails
                }
            }

            // Try to find in PATH using 'where' command (like original tag 1.0.4)
            Console.WriteLine("🔍 Searching for CDB in PATH using 'where cdb.exe' command...");
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
                    var errorOutput = result.StandardError.ReadToEnd();

                    Console.WriteLine($"   'where cdb.exe' exit code: {result.ExitCode}");
                    Console.WriteLine($"   'where cdb.exe' output: '{output.Trim()}'");
                    if (!string.IsNullOrEmpty(errorOutput))
                        Console.WriteLine($"   'where cdb.exe' error: '{errorOutput.Trim()}'");

                    if (result.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            var foundPath = lines[0].Trim();
                            Console.WriteLine($"   ✅ CDB found in PATH: {foundPath}");
                            return foundPath;
                        }
                    }
                    else
                    {
                        Console.WriteLine("   ❌ CDB not found in PATH");
                    }
                }
                else
                {
                    Console.WriteLine("   ⚠️ 'where cdb.exe' command timed out or failed to start");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Error running 'where cdb.exe': {ex.Message}");
                // PATH search failed, return null
            }

            return null;
        }
    }
}
