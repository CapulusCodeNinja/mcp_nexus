namespace mcp_nexus.Startup
{
    /// <summary>
    /// Handles displaying help information to users.
    /// </summary>
    public static class HelpDisplay
    {
        /// <summary>
        /// Displays help information about the application and its usage.
        /// </summary>
        /// <returns>A completed task.</returns>
        public static async Task ShowHelpAsync()
        {
            Console.WriteLine("MCP Nexus - Comprehensive MCP Server Platform");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  mcp_nexus [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("DESCRIPTION:");
            Console.WriteLine("  MCP Nexus is a Model Context Protocol (MCP) server that provides various tools");
            Console.WriteLine("  and utilities for development and debugging. It supports both stdio and HTTP transports.");
            Console.WriteLine();
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  --http                 Use HTTP transport instead of stdio");
            Console.WriteLine("  --port <PORT>          HTTP server port (default: 5117 dev, 5000 production)");
            Console.WriteLine("  --service              Run in Windows service mode (implies --http)");
            Console.WriteLine("  --cdb-path <PATH>      Custom path to CDB.exe debugger executable");
            Console.WriteLine();
            Console.WriteLine("SERVICE MANAGEMENT (Windows only):");
            Console.WriteLine("  --install              Install MCP Nexus as Windows service");
            Console.WriteLine("  --uninstall            Uninstall MCP Nexus Windows service");
            Console.WriteLine("  --update               Update MCP Nexus service (stop, update files, restart)");
            Console.WriteLine();
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("  mcp_nexus                          # Run in stdio mode");
            Console.WriteLine("  mcp_nexus --http                   # Run HTTP server on default port");
            Console.WriteLine("  mcp_nexus --http --port 8080       # Run HTTP server on port 8080");
            Console.WriteLine("  mcp_nexus --install                # Install as Windows service");
            Console.WriteLine("  mcp_nexus --install --port 9000    # Install service on port 9000");
            Console.WriteLine("  mcp_nexus --update                 # Update installed service");
            Console.WriteLine("  mcp_nexus --cdb-path \"C:\\WinDbg\"   # Use custom debugger path");
            Console.WriteLine();
            Console.WriteLine("NOTES:");
            Console.WriteLine("  - Service commands require administrator privileges on Windows");
            Console.WriteLine("  - Updates create backups in: C:\\Program Files\\MCP-Nexus\\backups\\[timestamp]");
            Console.WriteLine("  - HTTP mode runs on localhost:5000/ (or custom port if specified)");
            Console.WriteLine();
            Console.WriteLine("For more information, visit: https://github.com/your-repo/mcp_nexus");
            await Task.CompletedTask;
        }
    }
}

