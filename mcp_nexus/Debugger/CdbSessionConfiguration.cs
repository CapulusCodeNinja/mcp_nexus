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
        /// Searches for CDB in standard Windows SDK locations
        /// </summary>
        private string? FindCdbInStandardLocations()
        {
            var architecture = GetCurrentArchitecture();

            var searchPaths = new[]
            {
                // Windows 11 SDK paths
                $@"C:\Program Files (x86)\Windows Kits\10\Debuggers\{architecture}\cdb.exe",
                $@"C:\Program Files\Windows Kits\10\Debuggers\{architecture}\cdb.exe",
                
                // Windows 10 SDK paths
                $@"C:\Program Files (x86)\Windows Kits\8.1\Debuggers\{architecture}\cdb.exe",
                $@"C:\Program Files\Windows Kits\8.1\Debuggers\{architecture}\cdb.exe",
                
                // Legacy paths
                $@"C:\Program Files (x86)\Windows Kits\8.0\Debuggers\{architecture}\cdb.exe",
                $@"C:\Program Files\Windows Kits\8.0\Debuggers\{architecture}\cdb.exe",
                
                // Visual Studio paths
                $@"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\Microsoft\Windows Kits\10.0\Debuggers\{architecture}\cdb.exe",
                $@"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\Windows Kits\10.0\Debuggers\{architecture}\cdb.exe",
                $@"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\Microsoft\Windows Kits\10.0\Debuggers\{architecture}\cdb.exe",
                
                // Try x64 as fallback if current architecture not found
                @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe"
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }
    }
}
