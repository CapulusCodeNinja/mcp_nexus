using mcp_nexus.Startup;

namespace mcp_nexus
{
    /// <summary>
    /// Main entry point for the MCP Nexus application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task Main(string[] args)
        {
            // IMMEDIATE startup logging to track how far we get
            try
            {
                Console.Error.WriteLine($" MCP Nexus starting...");
                Console.Error.WriteLine($" Setting up global exception handlers...");
                ExceptionLogger.SetupGlobalExceptionHandlers();
                Console.Error.WriteLine($" Global exception handlers set up.");
            }
            catch (Exception startupEx)
            {
                Console.Error.WriteLine($" STARTUP EXCEPTION: {startupEx}");
                Environment.Exit(1);
            }

            try
            {
                Console.Error.WriteLine($" Setting environment variables...");
                StartupHelper.SetupConsoleEncoding();
                mcp_nexus.Infrastructure.Core.ThreadPoolTuning.Apply();
                StartupHelper.SetEnvironmentForServiceMode(args);

                // Check if this is a help request first
                if (CommandLineParser.IsHelpRequest(args))
                {
                    await HelpDisplay.ShowHelpAsync();
                    return;
                }

                // Parse command line arguments
                var commandLineArgs = CommandLineParser.Parse(args);

                // Handle special commands first (Windows only)
                if (commandLineArgs.Install)
                {
                    await ServiceCommandHandler.HandleInstallCommandAsync();
                    return;
                }

                if (commandLineArgs.Uninstall)
                {
                    await ServiceCommandHandler.HandleUninstallCommandAsync();
                    return;
                }

                if (commandLineArgs.Update)
                {
                    await ServiceCommandHandler.HandleUpdateCommandAsync();
                    return;
                }

                // Determine transport mode
                bool useHttp = commandLineArgs.UseHttp || commandLineArgs.ServiceMode;

                // Validate service mode is only used on Windows
                if (!StartupHelper.ValidateServiceModeOnWindows(commandLineArgs.ServiceMode))
                {
                    return;
                }

                if (useHttp)
                {
                    await ServerRunner.RunHttpServerAsync(args, commandLineArgs);
                }
                else
                {
                    await ServerRunner.RunStdioServerAsync(args, commandLineArgs);
                }
            }
            catch (Exception ex)
            {
                await ExceptionLogger.HandleFatalExceptionAsync(ex, args);
                Environment.Exit(1);
            }
        }
    }
}
