using mcp_nexus.Constants;
using NLog;

namespace mcp_nexus.Startup
{
    /// <summary>
    /// Handles formatting and displaying the application startup banner.
    /// </summary>
    public static class StartupBanner
    {
        /// <summary>
        /// Logs the startup banner with application information and configuration details.
        /// </summary>
        /// <param name="args">The parsed command line arguments.</param>
        /// <param name="host">The host address the application will bind to.</param>
        /// <param name="port">The port number the application will listen on.</param>
        public static void LogStartupBanner(CommandLineArguments args, string host, int? port)
        {
            var logger = LogManager.GetCurrentClassLogger();
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            const int bannerWidth = 69; // Total width including asterisks
            const int contentWidth = bannerWidth - 4; // Width for content (excluding "* " and " *")

            var banner = new System.Text.StringBuilder();
            banner.AppendLine("*********************************************************************");
            banner.AppendLine(FormatCenteredBannerLine("MCP NEXUS", contentWidth));
            banner.AppendLine(FormatCenteredBannerLine("Model Context Protocol Server", contentWidth));
            banner.AppendLine("*********************************************************************");
            banner.AppendLine(FormatBannerLine("Version:", version, contentWidth));
            banner.AppendLine(FormatBannerLine("Environment:", environment, contentWidth));
            banner.AppendLine(FormatBannerLine("Started:", timestamp, contentWidth));
            banner.AppendLine(FormatBannerLine("PID:", Environment.ProcessId.ToString(), contentWidth));

            if (host == "stdio")
            {
                banner.AppendLine(FormatBannerLine("Transport:", "STDIO Mode", contentWidth));
            }
            else
            {
                var transport = args.ServiceMode ? "HTTP (Service Mode)" : "HTTP (Interactive)";
                banner.AppendLine(FormatBannerLine("Transport:", transport, contentWidth));
                banner.AppendLine(FormatBannerLine("Host:", host, contentWidth));
                banner.AppendLine(FormatBannerLine("Port:", port?.ToString() ?? "Default", contentWidth));
            }

            // Show configuration sources
            var configSources = new List<string>();
            if (args.HostFromCommandLine || args.PortFromCommandLine)
                configSources.Add("Command Line");
            configSources.Add("Configuration File");
            banner.AppendLine(FormatBannerLine("Config:", string.Join(", ", configSources), contentWidth));

            // Show custom CDB path if specified
            if (!string.IsNullOrEmpty(args.CustomCdbPath))
            {
                var cdbPath = args.CustomCdbPath.Length > (contentWidth - 12) ?
                    ApplicationConstants.PathTruncationPrefix + args.CustomCdbPath[^(contentWidth - 15)..] :
                    args.CustomCdbPath;
                banner.AppendLine(FormatBannerLine("CDB Path:", cdbPath, contentWidth));
            }

            banner.AppendLine("*********************************************************************");

            // Log the banner to both console and log file
            var bannerText = banner.ToString();
            Console.WriteLine(bannerText);

            // Log a clean startup message instead of the messy formatted banner

            logger.Info("╔═══════════════════════════════════════════════════════════════════╗");
            logger.Info("                            MCP NEXUS STARTUP");
            logger.Info("");
            logger.Info($"  Version:     {version}");
            logger.Info($"  Environment: {environment}");
            logger.Info($"  Process ID:  {Environment.ProcessId}");
            if (host == "stdio")
            {
                logger.Info("  Transport:   STDIO Mode");
            }
            else
            {
                var transport = args.ServiceMode ? "HTTP (Service Mode)" : "HTTP (Interactive)";
                logger.Info($"  Transport:   {transport}");
                logger.Info($"  Host:        {host}");
                logger.Info($"  Port:        {port?.ToString() ?? "Default"}");
            }
            logger.Info($"  Started:     {timestamp}");
            logger.Info("╚═══════════════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Formats a banner line with a label and value, truncating if necessary.
        /// </summary>
        /// <param name="label">The label for the banner line.</param>
        /// <param name="value">The value to display.</param>
        /// <param name="contentWidth">The maximum width for the content.</param>
        /// <returns>A formatted banner line string.</returns>
        public static string FormatBannerLine(string label, string value, int contentWidth)
        {
            var content = $"{label,-12} {value}";
            if (content.Length > contentWidth)
            {
                content = content[..contentWidth];
            }
            return $"* {content.PadRight(contentWidth)} *";
        }

        /// <summary>
        /// Formats a centered banner line with the specified text.
        /// </summary>
        /// <param name="text">The text to center in the banner line.</param>
        /// <param name="contentWidth">The maximum width for the content.</param>
        /// <returns>A formatted centered banner line string.</returns>
        public static string FormatCenteredBannerLine(string text, int contentWidth)
        {
            if (text.Length > contentWidth)
            {
                text = text[..contentWidth];
            }
            // Center the text within the content width
            var totalPadding = contentWidth - text.Length;
            var leftPadding = totalPadding / 2;
            var rightPadding = totalPadding - leftPadding;
            var centeredContent = new string(' ', leftPadding) + text + new string(' ', rightPadding);
            return $"* {centeredContent} *";
        }
    }
}

