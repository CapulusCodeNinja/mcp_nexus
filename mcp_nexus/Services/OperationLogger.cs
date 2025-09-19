using Microsoft.Extensions.Logging;

namespace mcp_nexus.Services
{
    /// <summary>
    /// Provides operation-based logging with consistent formatting across the application.
    /// Adds operation prefixes like [Install], [Uninstall], [Cleanup] to log messages.
    /// </summary>
    public static class OperationLogger
    {
        /// <summary>
        /// Logs an information message with operation prefix
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="operation">The operation name (e.g., "Install", "Uninstall")</param>
        /// <param name="message">The message template</param>
        /// <param name="args">Message arguments</param>
        public static void LogInfo(ILogger? logger, string operation, string message, params object[] args)
        {
            logger?.LogInformation($"[{operation}] {message}", args);
        }

        /// <summary>
        /// Logs a warning message with operation prefix
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="operation">The operation name (e.g., "Install", "Uninstall")</param>
        /// <param name="message">The message template</param>
        /// <param name="args">Message arguments</param>
        public static void LogWarning(ILogger? logger, string operation, string message, params object[] args)
        {
            logger?.LogWarning($"[{operation}] {message}", args);
        }

        /// <summary>
        /// Logs an error message with operation prefix
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="operation">The operation name (e.g., "Install", "Uninstall")</param>
        /// <param name="message">The message template</param>
        /// <param name="args">Message arguments</param>
        public static void LogError(ILogger? logger, string operation, string message, params object[] args)
        {
            logger?.LogError($"[{operation}] {message}", args);
        }

        /// <summary>
        /// Logs an error message with exception and operation prefix
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="operation">The operation name (e.g., "Install", "Uninstall")</param>
        /// <param name="ex">The exception</param>
        /// <param name="message">The message template</param>
        /// <param name="args">Message arguments</param>
        public static void LogError(ILogger? logger, string operation, Exception ex, string message, params object[] args)
        {
            logger?.LogError(ex, $"[{operation}] {message}", args);
        }

        /// <summary>
        /// Logs a debug message with operation prefix
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="operation">The operation name (e.g., "Install", "Uninstall")</param>
        /// <param name="message">The message template</param>
        /// <param name="args">Message arguments</param>
        public static void LogDebug(ILogger? logger, string operation, string message, params object[] args)
        {
            logger?.LogDebug($"[{operation}] {message}", args);
        }

        /// <summary>
        /// Logs a trace message with operation prefix
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="operation">The operation name (e.g., "Install", "Uninstall")</param>
        /// <param name="message">The message template</param>
        /// <param name="args">Message arguments</param>
        public static void LogTrace(ILogger? logger, string operation, string message, params object[] args)
        {
            logger?.LogTrace($"[{operation}] {message}", args);
        }

        /// <summary>
        /// Common operation names for consistency across the application
        /// </summary>
        public static class Operations
        {
            public const string Install = "Install";
            public const string Uninstall = "Uninstall";
            public const string ForceUninstall = "ForceUninstall";
            public const string Update = "Update";
            public const string Build = "Build";
            public const string Copy = "Copy";
            public const string Registry = "Registry";
            public const string Service = "Service";
            public const string Cleanup = "Cleanup";
            public const string Http = "HTTP";
            public const string Stdio = "Stdio";
            public const string MCP = "MCP";
            public const string Tool = "Tool";
            public const string Protocol = "Protocol";
            public const string Debug = "Debug";
            public const string Startup = "Startup";
            public const string Shutdown = "Shutdown";
        }
    }
}
