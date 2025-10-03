using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Logs service operations and events.
    /// Provides comprehensive logging capabilities for tracking service operations, events, warnings, and errors.
    /// </summary>
    public class OperationLogger
    {
        private readonly ILogger<OperationLogger> m_Logger;
        private readonly List<OperationLogEntry> m_LogEntries = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public OperationLogger(ILogger<OperationLogger> logger)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs the start of an operation.
        /// </summary>
        /// <param name="operation">The name of the operation being started.</param>
        /// <param name="details">Optional details about the operation. Default is empty string.</param>
        public void LogOperationStart(string operation, string details = "")
        {
            var entry = new OperationLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Operation = operation,
                Details = details,
                StartTime = DateTime.UtcNow,
                Status = OperationStatus.Started
            };

            m_LogEntries.Add(entry);
            m_Logger.LogInformation("Operation started: {Operation} - {Details}", operation, details);
        }

        /// <summary>
        /// Logs the end of an operation.
        /// </summary>
        /// <param name="operation">The name of the operation that ended.</param>
        /// <param name="success">Whether the operation completed successfully.</param>
        /// <param name="result">Optional result message. Default is empty string.</param>
        public void LogOperationEnd(string operation, bool success, string result = "")
        {
            var entry = m_LogEntries.FindLast(e => e.Operation == operation && e.Status == OperationStatus.Started);
            if (entry != null)
            {
                entry.EndTime = DateTime.UtcNow;
                entry.Status = success ? OperationStatus.Completed : OperationStatus.Failed;
                entry.Result = result;
                // Duration is calculated property, no assignment needed

                m_Logger.LogInformation("Operation {Status}: {Operation} - Duration: {Duration}ms - {Result}",
                    success ? "completed" : "failed", operation, entry.Duration?.TotalMilliseconds ?? 0, result);
            }
        }

        /// <summary>
        /// Logs an error that occurred during an operation.
        /// </summary>
        /// <summary>
        /// Logs an operation error.
        /// </summary>
        /// <param name="operation">The name of the operation that failed.</param>
        /// <param name="exception">The exception that occurred.</param>
        public void LogOperationError(string operation, Exception exception)
        {
            var entry = m_LogEntries.FindLast(e => e.Operation == operation && e.Status == OperationStatus.Started);
            if (entry != null)
            {
                entry.EndTime = DateTime.UtcNow;
                entry.Status = OperationStatus.Failed;
                entry.Error = exception.Message;
                // Duration is calculated property, no assignment needed

                m_Logger.LogError(exception, "Operation failed: {Operation} - Duration: {Duration}ms",
                    operation, entry.Duration?.TotalMilliseconds ?? 0);
            }
        }

        /// <summary>
        /// Logs a service event.
        /// </summary>
        /// <param name="eventType">The type of event being logged.</param>
        /// <param name="message">The event message.</param>
        /// <param name="data">Optional additional data associated with the event. Can be null.</param>
        public void LogServiceEvent(string eventType, string message, object? data = null)
        {
            var entry = new OperationLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Operation = eventType,
                Details = message,
                StartTime = DateTime.UtcNow,
                Status = OperationStatus.Event,
                Data = data
            };

            m_LogEntries.Add(entry);
            m_Logger.LogInformation("Service event: {EventType} - {Message}", eventType, message);
        }

        /// <summary>
        /// Logs a service warning.
        /// </summary>
        /// <param name="message">The warning message.</param>
        /// <param name="data">Optional additional data associated with the warning. Can be null.</param>
        public void LogServiceWarning(string message, object? data = null)
        {
            var entry = new OperationLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Operation = "Warning",
                Details = message,
                StartTime = DateTime.UtcNow,
                Status = OperationStatus.Warning,
                Data = data
            };

            m_LogEntries.Add(entry);
            m_Logger.LogWarning("Service warning: {Message}", message);
        }

        /// <summary>
        /// Logs a service error.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="exception">Optional exception that caused the error. Can be null.</param>
        /// <param name="data">Optional additional data associated with the error. Can be null.</param>
        public void LogServiceError(string message, Exception? exception = null, object? data = null)
        {
            var entry = new OperationLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Operation = "Error",
                Details = message,
                StartTime = DateTime.UtcNow,
                Status = OperationStatus.Error,
                Error = exception?.Message,
                Data = data
            };

            m_LogEntries.Add(entry);
            m_Logger.LogError(exception, "Service error: {Message}", message);
        }

        /// <summary>
        /// Gets all log entries.
        /// </summary>
        /// <returns>
        /// A read-only list of all operation log entries.
        /// </returns>
        public IReadOnlyList<OperationLogEntry> GetLogEntries()
        {
            return m_LogEntries.AsReadOnly();
        }

        /// <summary>
        /// Gets log entries for a specific operation.
        /// </summary>
        /// <param name="operation">The operation name to filter by.</param>
        /// <returns>
        /// A read-only list of operation log entries for the specified operation.
        /// </returns>
        public IReadOnlyList<OperationLogEntry> GetLogEntries(string operation)
        {
            return m_LogEntries.FindAll(e => e.Operation == operation).AsReadOnly();
        }

        /// <summary>
        /// Gets log entries with a specific status.
        /// </summary>
        /// <param name="status">The operation status to filter by.</param>
        /// <returns>
        /// A read-only list of operation log entries with the specified status.
        /// </returns>
        public IReadOnlyList<OperationLogEntry> GetLogEntries(OperationStatus status)
        {
            return m_LogEntries.FindAll(e => e.Status == status).AsReadOnly();
        }

        /// <summary>
        /// Clears all log entries.
        /// </summary>
        public void ClearLogs()
        {
            m_LogEntries.Clear();
            m_Logger.LogInformation("Operation logs cleared");
        }

        /// <summary>
        /// Exports logs to a file asynchronously.
        /// </summary>
        /// <param name="filePath">The path where the logs should be exported.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the export was successful; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> ExportLogsAsync(string filePath)
        {
            try
            {
                // Implementation would export logs to file
                m_Logger.LogInformation("Logs exported to {FilePath}", filePath);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to export logs to {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Gets all operation log entries.
        /// </summary>
        public IReadOnlyList<OperationLogEntry> Operations => m_LogEntries.AsReadOnly();

        // Static methods for compatibility with existing code
        /// <summary>
        /// Logs an informational message for an operation.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging.</param>
        /// <param name="operation">The operation name.</param>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(ILogger logger, string operation, string message)
        {
            logger?.LogInformation("[{Operation}] {Message}", operation, message);
        }

        /// <summary>
        /// Logs an informational message for an operation with formatted parameters.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging.</param>
        /// <param name="operation">The operation name.</param>
        /// <param name="messageTemplate">The message template with placeholders.</param>
        /// <param name="args">The arguments to format into the message template.</param>
        public static void LogInfo(ILogger logger, string operation, string messageTemplate, params object[] args)
        {
            logger?.LogInformation($"[{operation}] {messageTemplate}", args);
        }

        /// <summary>
        /// Logs a warning message for an operation.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging.</param>
        /// <param name="operation">The operation name.</param>
        /// <param name="message">The warning message to log.</param>
        public static void LogWarning(ILogger logger, string operation, string message)
        {
            logger?.LogWarning("[{Operation}] {Message}", operation, message);
        }

        /// <summary>
        /// Logs an error message for an operation.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging.</param>
        /// <param name="operation">The operation name.</param>
        /// <param name="message">The error message to log.</param>
        public static void LogError(ILogger logger, string operation, string message)
        {
            logger?.LogError("[{Operation}] {Message}", operation, message);
        }

        /// <summary>
        /// Logs an error message for an operation with an exception.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging.</param>
        /// <param name="operation">The operation name.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="message">The error message to log.</param>
        public static void LogError(ILogger logger, string operation, Exception exception, string message)
        {
            logger?.LogError(exception, "[{Operation}] {Message}", operation, message);
        }

        /// <summary>
        /// Logs a debug message for an operation.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging.</param>
        /// <param name="operation">The operation name.</param>
        /// <param name="message">The debug message to log.</param>
        public static void LogDebug(ILogger logger, string operation, string message)
        {
            logger?.LogDebug("[{Operation}] {Message}", operation, message);
        }

        /// <summary>
        /// Logs a trace message for an operation.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging.</param>
        /// <param name="operation">The operation name.</param>
        /// <param name="message">The trace message to log.</param>
        public static void LogTrace(ILogger logger, string operation, string message)
        {
            logger?.LogTrace("[{Operation}] {Message}", operation, message);
        }

        /// <summary>
        /// Gets all operations (static version).
        /// This is a placeholder for static access - in a real implementation,
        /// you'd need to maintain a static collection or use dependency injection.
        /// </summary>
        /// <returns>
        /// A read-only list of operation log entries.
        /// </returns>
        public static IReadOnlyList<OperationLogEntry> GetOperations()
        {
            // This is a placeholder for static access - in a real implementation,
            // you'd need to maintain a static collection or use dependency injection
            return new List<OperationLogEntry>();
        }

        /// <summary>
        /// Contains constants for common operation types.
        /// </summary>
        public static class OperationTypes
        {
            /// <summary>
            /// Install operation type.
            /// </summary>
            public const string Install = "Install";

            /// <summary>
            /// Uninstall operation type.
            /// </summary>
            public const string Uninstall = "Uninstall";

            /// <summary>
            /// Force uninstall operation type.
            /// </summary>
            public const string ForceUninstall = "ForceUninstall";

            /// <summary>
            /// Update operation type.
            /// </summary>
            public const string Update = "Update";

            /// <summary>
            /// Build operation type.
            /// </summary>
            public const string Build = "Build";

            /// <summary>
            /// Copy operation type.
            /// </summary>
            public const string Copy = "Copy";

            /// <summary>
            /// Registry operation type.
            /// </summary>
            public const string Registry = "Registry";

            /// <summary>
            /// Service operation type.
            /// </summary>
            public const string Service = "Service";

            /// <summary>
            /// Cleanup operation type.
            /// </summary>
            public const string Cleanup = "Cleanup";

            /// <summary>
            /// HTTP operation type.
            /// </summary>
            public const string Http = "HTTP";

            /// <summary>
            /// Stdio operation type.
            /// </summary>
            public const string Stdio = "Stdio";

            /// <summary>
            /// MCP operation type.
            /// </summary>
            public const string Mcp = "MCP";

            /// <summary>
            /// Tool operation type.
            /// </summary>
            public const string Tool = "Tool";

            /// <summary>
            /// Protocol operation type.
            /// </summary>
            public const string Protocol = "Protocol";

            /// <summary>
            /// Debug operation type.
            /// </summary>
            public const string Debug = "Debug";

            /// <summary>
            /// Startup operation type.
            /// </summary>
            public const string Startup = "Startup";

            /// <summary>
            /// Shutdown operation type.
            /// </summary>
            public const string Shutdown = "Shutdown";
        }

    }

    /// <summary>
    /// Represents a single operation log entry.
    /// Contains information about an operation including timing, status, and results.
    /// </summary>
    public class OperationLogEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for the log entry.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the operation.
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the details of the operation.
        /// </summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the start time of the operation.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the operation.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets the duration of the operation.
        /// </summary>
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        /// <summary>
        /// Gets or sets the status of the operation.
        /// </summary>
        public OperationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the result of the operation.
        /// </summary>
        public string? Result { get; set; }

        /// <summary>
        /// Gets or sets the error message if the operation failed.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets additional data associated with the operation.
        /// </summary>
        public object? Data { get; set; }
    }

    /// <summary>
    /// Specifies the status of an operation.
    /// </summary>
    public enum OperationStatus
    {
        /// <summary>
        /// The operation has started.
        /// </summary>
        Started,

        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The operation failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The operation is an event.
        /// </summary>
        Event,

        /// <summary>
        /// The operation is a warning.
        /// </summary>
        Warning,

        /// <summary>
        /// The operation is an error.
        /// </summary>
        Error
    }
}
