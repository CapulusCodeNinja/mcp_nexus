using System.Runtime.InteropServices;

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
        /// Searches for CDB in standard Windows SDK locations and PATH
        /// </summary>
        private string? FindCdbInStandardLocations()
        {
            // First try PATH environment variable (like the original working code)
            var pathResult = FindCdbInPath();
            if (pathResult != null)
                return pathResult;

            // Then try standard SDK locations
            var architecture = GetCurrentArchitecture();

            var searchPaths = new[]
            {
                // Use environment variables like the original working code
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles(x86)%\Windows Kits\10\Debuggers\{architecture}\cdb.exe"),
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles%\Windows Kits\10\Debuggers\{architecture}\cdb.exe"),
                
                // Windows 10 SDK paths
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles(x86)%\Windows Kits\8.1\Debuggers\{architecture}\cdb.exe"),
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles%\Windows Kits\8.1\Debuggers\{architecture}\cdb.exe"),
                
                // Legacy paths
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles(x86)%\Windows Kits\8.0\Debuggers\{architecture}\cdb.exe"),
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles%\Windows Kits\8.0\Debuggers\{architecture}\cdb.exe"),
                
                // Visual Studio paths
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\Microsoft\Windows Kits\10.0\Debuggers\{architecture}\cdb.exe"),
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles%\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\Windows Kits\10.0\Debuggers\{architecture}\cdb.exe"),
                Environment.ExpandEnvironmentVariables($@"%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\Microsoft\Windows Kits\10.0\Debuggers\{architecture}\cdb.exe"),
                
                // Try x64 as fallback if current architecture not found
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Windows Kits\10\Debuggers\x64\cdb.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Windows Kits\10\Debuggers\x64\cdb.exe")
            };

            foreach (var path in searchPaths)
            {
                try
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        return path;
                }
                catch
                {
                    // Continue searching if path expansion fails
                }
            }

            return null;
        }

        /// <summary>
        /// Searches for CDB in PATH environment variable (like the original working code)
        /// </summary>
        private string? FindCdbInPath()
        {
            try
            {
                // Try to find cdb.exe in PATH
                var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
                var pathDirs = pathVar.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var dir in pathDirs)
                {
                    try
                    {
                        var cdbPath = Path.Combine(dir.Trim(), "cdb.exe");
                        if (File.Exists(cdbPath))
                            return cdbPath;
                    }
                    catch
                    {
                        // Continue searching if path is invalid
                    }
                }
            }
            catch
            {
                // PATH search failed, continue with standard locations
            }

            return null;
        }
    }
}
