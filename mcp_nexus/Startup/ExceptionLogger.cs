namespace mcp_nexus.Startup
{
    /// <summary>
    /// Handles setup and logging of unhandled exceptions.
    /// </summary>
    public static class ExceptionLogger
    {
        /// <summary>
        /// Sets up global exception handlers to catch unhandled exceptions from all sources.
        /// </summary>
        public static void SetupGlobalExceptionHandlers()
        {
            // Handle unhandled exceptions in the current AppDomain
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogFatalException(ex, "AppDomain.UnhandledException", e.IsTerminating);
            };

            // Handle unhandled exceptions in tasks
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogFatalException(e.Exception, "TaskScheduler.UnobservedTaskException", false);
                e.SetObserved(); // Prevent the process from terminating
            };

            // Handle unhandled exceptions in the current thread (for console apps)
            if (!Environment.UserInteractive)
            {
                // For service mode, also handle process exit
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Console.Error.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Process exiting...");
                };
            }
        }

        /// <summary>
        /// Logs fatal exceptions with comprehensive details.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="source">The source of the exception.</param>
        /// <param name="isTerminating">Whether the exception is terminating the process.</param>
        public static void LogFatalException(Exception? ex, string source, bool isTerminating)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // IMMEDIATE console output with flushing
                Console.Error.WriteLine("################################################################################");
                Console.Error.WriteLine($"FATAL UNHANDLED EXCEPTION - {source}");
                Console.Error.WriteLine($"Time: {timestamp}");
                Console.Error.WriteLine($"Terminating: {isTerminating}");
                Console.Error.WriteLine("################################################################################");

                if (ex != null)
                {
                    Console.Error.WriteLine($"Exception Type: {ex.GetType().FullName}");
                    Console.Error.WriteLine($"Message: {ex.Message}");
                    Console.Error.WriteLine($"Source: {ex.Source}");
                    Console.Error.WriteLine($"TargetSite: {ex.TargetSite}");
                    Console.Error.WriteLine("Stack Trace:");
                    Console.Error.WriteLine(ex.StackTrace ?? "No stack trace available");

                    // Handle inner exceptions
                    var innerEx = ex.InnerException;
                    int depth = 1;
                    while (innerEx != null && depth <= 5) // Limit depth to prevent infinite loops
                    {
                        Console.Error.WriteLine($"Inner Exception (Level {depth}):");
                        Console.Error.WriteLine($"  Type: {innerEx.GetType().FullName}");
                        Console.Error.WriteLine($"  Message: {innerEx.Message}");
                        Console.Error.WriteLine($"  Stack Trace: {innerEx.StackTrace}");
                        innerEx = innerEx.InnerException;
                        depth++;
                    }

                    // Handle AggregateException specially
                    if (ex is AggregateException aggEx)
                    {
                        Console.Error.WriteLine($"AggregateException contains {aggEx.InnerExceptions.Count} inner exceptions:");
                        for (int i = 0; i < aggEx.InnerExceptions.Count && i < 10; i++) // Limit to first 10
                        {
                            var innerException = aggEx.InnerExceptions[i];
                            Console.Error.WriteLine($"  [{i}] {innerException.GetType().FullName}: {innerException.Message}");
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLine("Exception object is null");
                }

                Console.Error.WriteLine("################################################################################");
                Console.Error.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
                Console.Error.WriteLine($"OS Version: {Environment.OSVersion}");
                Console.Error.WriteLine($".NET Version: {Environment.Version}");
                Console.Error.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
                Console.Error.WriteLine($"Process ID: {Environment.ProcessId}");
                Console.Error.WriteLine($"Thread ID: {Environment.CurrentManagedThreadId}");
                Console.Error.WriteLine("################################################################################");

                // AGGRESSIVELY write to multiple crash log locations
                var logContent = $"{timestamp} - {source} - Terminating: {isTerminating}\n{ex}\n\n";
                var logLocations = new[]
                {
                    Path.Combine(Environment.CurrentDirectory, "mcp_nexus_crash.log"),
                    Path.Combine(Path.GetTempPath(), "mcp_nexus_crash.log"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "mcp_nexus_crash.log")
                };

                foreach (var logFile in logLocations)
                {
                    try
                    {
                        File.AppendAllText(logFile, logContent);
                        Console.Error.WriteLine($"Crash details written to: {logFile}");
                        break; // Stop after first successful write
                    }
                    catch
                    {
                        // Try next location
                    }
                }

                // Also try Windows Event Log as last resort
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        using var eventLog = new System.Diagnostics.EventLog("Application");
                        eventLog.Source = "MCP Nexus";
                        eventLog.WriteEntry($"FATAL EXCEPTION - {source}: {ex?.Message}", System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                catch
                {
                    // Ignore event log errors
                }
            }
            catch
            {
                // If even our error logging fails, try one last desperate attempt
                try
                {
                    Console.Error.WriteLine($"CRITICAL: Exception in exception handler! Original: {ex?.Message}");
                }
                catch
                {
                    // Give up
                }
            }
        }

        /// <summary>
        /// Handles fatal exceptions during application startup.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="args">Command line arguments for logging.</param>
        /// <returns>A completed task.</returns>
        public static async Task HandleFatalExceptionAsync(Exception ex, string[] args)
        {
            Console.Error.WriteLine($"🚨 FATAL STARTUP ERROR");
            Console.Error.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.Error.WriteLine($"Command Line Args: {string.Join(" ", args)}");

            // Try to write to file for post-mortem debugging
            try
            {
                var logFile = Path.Combine(Environment.CurrentDirectory, "mcp-nexus-startup-error.log");
                await File.WriteAllTextAsync(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - FATAL ERROR: {ex}");
            }
            catch
            {
                // Last resort - do nothing, but at least we tried
            }
        }
    }
}

