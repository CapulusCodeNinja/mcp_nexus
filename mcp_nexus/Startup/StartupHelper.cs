using NLog;

namespace mcp_nexus.Startup
{
    /// <summary>
    /// Provides helper methods for application startup and initialization.
    /// </summary>
    public static class StartupHelper
    {
        /// <summary>
        /// Sets up console encoding to UTF-8 for proper character handling.
        /// </summary>
        public static void SetupConsoleEncoding()
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
            try { Console.InputEncoding = System.Text.Encoding.UTF8; } catch { }
        }

        /// <summary>
        /// Sets the ASPNETCORE_ENVIRONMENT variable based on command line arguments.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void SetEnvironmentForServiceMode(string[] args)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
            {
                // Check if we're running in service mode
                if (args.Contains("--service") || args.Contains("--install") || args.Contains("--uninstall") || args.Contains("--update"))
                {
                    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Service");
                    Console.Error.WriteLine($" Environment set to Service mode");
                }
                else
                {
                    // Default to Production for non-development builds
                    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
                    Console.Error.WriteLine($" Environment set to Production mode");
                }
            }
        }

        /// <summary>
        /// Validates that service mode is only used on Windows.
        /// </summary>
        /// <param name="serviceMode">Whether service mode is requested.</param>
        /// <returns>True if validation passes, false otherwise.</returns>
        public static bool ValidateServiceModeOnWindows(bool serviceMode)
        {
            if (serviceMode && !OperatingSystem.IsWindows())
            {
                var logger = LogManager.GetCurrentClassLogger();
                Console.Error.WriteLineAsync("ERROR: Service mode is only supported on Windows.").Wait();
                logger.Error("Service mode is only supported on Windows");
                return false;
            }
            return true;
        }
    }
}

