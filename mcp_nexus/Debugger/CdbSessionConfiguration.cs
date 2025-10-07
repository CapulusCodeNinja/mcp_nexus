using System.Runtime.InteropServices;
using System.Diagnostics;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles CDB session configuration, validation, and path resolution.
    /// Provides configuration settings for CDB debugging sessions including timeouts, paths, and retry settings.
    /// </summary>
    public class CdbSessionConfiguration
    {
        /// <summary>
        /// Gets the command timeout in milliseconds.
        /// </summary>
        public int CommandTimeoutMs { get; }

        /// <summary>
        /// Gets the idle timeout in milliseconds.
        /// </summary>
        public int IdleTimeoutMs { get; }

        /// <summary>
        /// Gets the custom CDB executable path, if specified.
        /// </summary>
        public string? CustomCdbPath { get; }


        /// <summary>
        /// Gets the maximum number of retries for symbol server operations.
        /// </summary>
        public int SymbolServerMaxRetries { get; }

        /// <summary>
        /// Gets the symbol search path for CDB.
        /// </summary>
        public string? SymbolSearchPath { get; }

        /// <summary>
        /// Gets the startup delay in milliseconds.
        /// </summary>
        public int StartupDelayMs { get; }

        /// <summary>
        /// Gets the output reading timeout in milliseconds.
        /// </summary>
        public int OutputReadingTimeoutMs { get; }

        /// <summary>
        /// Gets a value indicating whether command preprocessing is enabled.
        /// When enabled, commands like .srcpath will be automatically fixed and paths will be normalized.
        /// When disabled, commands are sent to CDB as-is without any preprocessing.
        /// </summary>
        public bool EnableCommandPreprocessing { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbSessionConfiguration"/> class.
        /// </summary>
        /// <param name="commandTimeoutMs">The command timeout in milliseconds. Default is 30000ms (30 seconds).</param>
        /// <param name="idleTimeoutMs">The idle timeout in milliseconds. Default is 180000ms (3 minutes).</param>
        /// <param name="customCdbPath">Optional custom path to the CDB executable. If null, uses the default path.</param>
        /// <param name="symbolServerMaxRetries">The maximum number of retries for symbol server operations. Default is 1.</param>
        /// <param name="symbolSearchPath">Optional symbol search path for CDB. If null, uses the default path.</param>
        /// <param name="startupDelayMs">The startup delay in milliseconds. Default is 1000ms (1 second).</param>
        /// <param name="outputReadingTimeoutMs">The output reading timeout in milliseconds. Default is 60000ms (1 minute).</param>
        /// <param name="enableCommandPreprocessing">Whether to enable command preprocessing. Default is true.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any of the timeout or retry parameters are invalid.</exception>
        public CdbSessionConfiguration(
            int commandTimeoutMs = 30000,
            int idleTimeoutMs = 180000,
            string? customCdbPath = null,
            int symbolServerMaxRetries = 1,
            string? symbolSearchPath = null,
            int startupDelayMs = 1000,
            int outputReadingTimeoutMs = 60000,
            bool enableCommandPreprocessing = true)
        {
            ValidateParameters(commandTimeoutMs, idleTimeoutMs, symbolServerMaxRetries, startupDelayMs, outputReadingTimeoutMs);

            CommandTimeoutMs = commandTimeoutMs;
            IdleTimeoutMs = idleTimeoutMs;
            CustomCdbPath = customCdbPath;
            SymbolServerMaxRetries = symbolServerMaxRetries;
            SymbolSearchPath = symbolSearchPath;
            StartupDelayMs = startupDelayMs;
            OutputReadingTimeoutMs = outputReadingTimeoutMs;
            EnableCommandPreprocessing = enableCommandPreprocessing;
        }

        /// <summary>
        /// Validates configuration parameters to ensure they are within acceptable ranges.
        /// </summary>
        /// <param name="commandTimeoutMs">The command timeout in milliseconds.</param>
        /// <param name="idleTimeoutMs">The idle timeout in milliseconds.</param>
        /// <param name="symbolServerMaxRetries">The maximum number of retries for symbol server operations.</param>
        /// <param name="startupDelayMs">The startup delay in milliseconds.</param>
        /// <param name="outputReadingTimeoutMs">The output reading timeout in milliseconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any of the parameters are invalid.</exception>
        public static void ValidateParameters(
            int commandTimeoutMs,
            int idleTimeoutMs,
            int symbolServerMaxRetries,
            int startupDelayMs,
            int outputReadingTimeoutMs)
        {
            if (commandTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(commandTimeoutMs), "Command timeout must be positive");

            if (idleTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(idleTimeoutMs), "Idle timeout must be positive");


            if (symbolServerMaxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(symbolServerMaxRetries), "Symbol server max retries cannot be negative");

            if (startupDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(startupDelayMs), "Startup delay cannot be negative");

            if (outputReadingTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputReadingTimeoutMs), "Output reading timeout must be positive");
        }

        /// <summary>
        /// Finds the CDB executable path using custom path or standard locations.
        /// This method searches for CDB in the configured custom path first, then falls back to standard locations.
        /// </summary>
        /// <returns>
        /// The path to the CDB executable if found; otherwise, <c>null</c>.
        /// </returns>
        /// <exception cref="FileNotFoundException">Thrown when a custom CDB path is specified but the file does not exist.</exception>
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
        /// Gets the current system architecture for CDB selection.
        /// This method determines the appropriate CDB executable architecture based on the current process architecture.
        /// </summary>
        /// <returns>
        /// A string representing the current architecture ("x64", "x86", "arm64", or "arm").
        /// Returns "x64" as a fallback for unknown architectures.
        /// </returns>
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
