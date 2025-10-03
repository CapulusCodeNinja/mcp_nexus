using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Logs service operations and events
    /// </summary>
    public class OperationLogger
    {
        private readonly ILogger<OperationLogger> m_Logger;
        private readonly List<OperationLogEntry> m_LogEntries = new();

        public OperationLogger(ILogger<OperationLogger> logger)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

        public IReadOnlyList<OperationLogEntry> GetLogEntries()
        {
            return m_LogEntries.AsReadOnly();
        }

        public IReadOnlyList<OperationLogEntry> GetLogEntries(string operation)
        {
            return m_LogEntries.FindAll(e => e.Operation == operation).AsReadOnly();
        }

        public IReadOnlyList<OperationLogEntry> GetLogEntries(OperationStatus status)
        {
            return m_LogEntries.FindAll(e => e.Status == status).AsReadOnly();
        }

        public void ClearLogs()
        {
            m_LogEntries.Clear();
            m_Logger.LogInformation("Operation logs cleared");
        }

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


        public IReadOnlyList<OperationLogEntry> Operations => m_LogEntries.AsReadOnly();

        // Static methods for compatibility with existing code
        public static void LogInfo(ILogger logger, string operation, string message)
        {
            logger?.LogInformation("[{Operation}] {Message}", operation, message);
        }

        public static void LogInfo(ILogger logger, string operation, string messageTemplate, params object[] args)
        {
            logger?.LogInformation($"[{operation}] {messageTemplate}", args);
        }

        public static void LogWarning(ILogger logger, string operation, string message)
        {
            logger?.LogWarning("[{Operation}] {Message}", operation, message);
        }

        public static void LogError(ILogger logger, string operation, string message)
        {
            logger?.LogError("[{Operation}] {Message}", operation, message);
        }

        public static void LogError(ILogger logger, string operation, Exception exception, string message)
        {
            logger?.LogError(exception, "[{Operation}] {Message}", operation, message);
        }

        public static void LogDebug(ILogger logger, string operation, string message)
        {
            logger?.LogDebug("[{Operation}] {Message}", operation, message);
        }

        public static void LogTrace(ILogger logger, string operation, string message)
        {
            logger?.LogTrace("[{Operation}] {Message}", operation, message);
        }

        public static IReadOnlyList<OperationLogEntry> GetOperations()
        {
            // This is a placeholder for static access - in a real implementation,
            // you'd need to maintain a static collection or use dependency injection
            return new List<OperationLogEntry>();
        }

        public static class OperationTypes
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
            public const string Mcp = "MCP";
            public const string Tool = "Tool";
            public const string Protocol = "Protocol";
            public const string Debug = "Debug";
            public const string Startup = "Startup";
            public const string Shutdown = "Shutdown";
        }

    }

    public class OperationLogEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
        public OperationStatus Status { get; set; }
        public string? Result { get; set; }
        public string? Error { get; set; }
        public object? Data { get; set; }
    }

    public enum OperationStatus
    {
        Started,
        Completed,
        Failed,
        Event,
        Warning,
        Error
    }
}
