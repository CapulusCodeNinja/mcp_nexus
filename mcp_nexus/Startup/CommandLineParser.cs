using System.CommandLine;

namespace mcp_nexus.Startup
{
    /// <summary>
    /// Handles command line argument parsing for the MCP Nexus application.
    /// </summary>
    public static class CommandLineParser
    {
        /// <summary>
        /// Parses command line arguments using System.CommandLine.
        /// </summary>
        /// <param name="args">The command line arguments to parse.</param>
        /// <returns>A CommandLineArguments object containing the parsed values.</returns>
        public static CommandLineArguments Parse(string[] args)
        {
            var result = new CommandLineArguments();

            var cdbPathOption = new Option<string?>("--cdb-path", "Custom path to CDB.exe debugger executable");
            var httpOption = new Option<bool>("--http", "Use HTTP transport instead of stdio");
            var serviceOption = new Option<bool>("--service", "Run in Windows service mode (implies --http)");
            var installOption = new Option<bool>("--install", "Install MCP Nexus as Windows service");
            var uninstallOption = new Option<bool>("--uninstall", "Uninstall MCP Nexus Windows service");
            var updateOption = new Option<bool>("--update", "Update MCP Nexus service (stop, update files, restart)");
            var portOption = new Option<int?>("--port", "HTTP server port (default: 5117 dev, 5000 production)");
            var hostOption = new Option<string?>("--host", "HTTP server host binding (default: localhost, use 0.0.0.0 for all interfaces)");

            var rootCommand = new RootCommand("MCP Nexus - Comprehensive MCP Server Platform")
            {
                cdbPathOption,
                httpOption,
                serviceOption,
                installOption,
                uninstallOption,
                updateOption,
                portOption,
                hostOption
            };

            var parseResult = rootCommand.Parse(args);
            if (parseResult.Errors.Count == 0)
            {
                result.CustomCdbPath = parseResult.GetValueForOption(cdbPathOption);
                result.UseHttp = parseResult.GetValueForOption(httpOption);
                result.ServiceMode = parseResult.GetValueForOption(serviceOption);
                result.Install = parseResult.GetValueForOption(installOption);
                result.Uninstall = parseResult.GetValueForOption(uninstallOption);
                result.Update = parseResult.GetValueForOption(updateOption);
                result.Port = parseResult.GetValueForOption(portOption);
                result.Host = parseResult.GetValueForOption(hostOption);

                // Track which values came from command line
                result.PortFromCommandLine = result.Port.HasValue;
                result.HostFromCommandLine = result.Host != null;
            }

            return result;
        }

        /// <summary>
        /// Determines if the command line arguments represent a help request.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>True if this is a help request, false otherwise.</returns>
        public static bool IsHelpRequest(string[] args)
        {
            return args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "help");
        }
    }

    /// <summary>
    /// Represents parsed command line arguments for the MCP Nexus application.
    /// </summary>
    public class CommandLineArguments
    {
        /// <summary>
        /// Gets or sets the custom path to the CDB.exe debugger executable.
        /// </summary>
        public string? CustomCdbPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use HTTP transport instead of stdio.
        /// </summary>
        public bool UseHttp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to run in Windows service mode (implies HTTP).
        /// </summary>
        public bool ServiceMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to install MCP Nexus as a Windows service.
        /// </summary>
        public bool Install { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to uninstall the MCP Nexus Windows service.
        /// </summary>
        public bool Uninstall { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to update the MCP Nexus service (stop, update files, restart).
        /// </summary>
        public bool Update { get; set; }

        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Gets or sets the HTTP server host binding address.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the host was specified via command line (for source reporting).
        /// </summary>
        public bool HostFromCommandLine { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the port was specified via command line (for source reporting).
        /// </summary>
        public bool PortFromCommandLine { get; set; }
    }
}

