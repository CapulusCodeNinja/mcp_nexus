using mcp_nexus.Models;
using mcp_nexus.Session.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Builds standardized notification messages for different event types
    /// </summary>
    public class NotificationMessageBuilder
    {
        private readonly ILogger m_logger;
        private readonly DateTime m_serverStartTime;

        public NotificationMessageBuilder(ILogger logger, DateTime serverStartTime)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_serverStartTime = serverStartTime;
        }

        /// <summary>
        /// Creates a command status notification
        /// </summary>
        public McpCommandStatusNotification CreateCommandStatusNotification(
            string commandId,
            string command,
            string status,
            int? progress = null,
            string? message = null,
            string? result = null,
            string? error = null)
        {
            return new McpCommandStatusNotification
            {
                CommandId = commandId,
                Command = command,
                Status = status,
                Progress = progress,
                Message = message,
                Result = result,
                Error = error,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a session-aware command status notification
        /// </summary>
        public McpCommandStatusNotification CreateSessionCommandStatusNotification(
            string sessionId,
            string commandId,
            string command,
            string status,
            string? result = null,
            int? progress = null,
            string? message = null,
            string? error = null)
        {
            return new McpCommandStatusNotification
            {
                SessionId = sessionId,
                CommandId = commandId,
                Command = command,
                Status = status,
                Progress = progress,
                Message = message,
                Result = result,
                Error = error,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a command heartbeat notification
        /// </summary>
        public McpCommandHeartbeatNotification CreateCommandHeartbeatNotification(
            string commandId,
            string command,
            TimeSpan elapsed,
            string? details = null)
        {
            var elapsedDisplay = FormatElapsedTime(elapsed);

            return new McpCommandHeartbeatNotification
            {
                CommandId = commandId,
                Command = command,
                ElapsedSeconds = elapsed.TotalSeconds,
                ElapsedDisplay = elapsedDisplay,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a session-aware command heartbeat notification
        /// </summary>
        public McpCommandHeartbeatNotification CreateSessionCommandHeartbeatNotification(
            string sessionId,
            string commandId,
            string command,
            TimeSpan elapsed,
            string? details = null)
        {
            var elapsedDisplay = FormatElapsedTime(elapsed);

            return new McpCommandHeartbeatNotification
            {
                SessionId = sessionId,
                CommandId = commandId,
                Command = command,
                ElapsedSeconds = elapsed.TotalSeconds,
                ElapsedDisplay = elapsedDisplay,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a session recovery notification
        /// </summary>
        public McpSessionRecoveryNotification CreateSessionRecoveryNotification(
            string reason,
            string recoveryStep,
            bool success,
            string message,
            string[]? affectedCommands = null)
        {
            return new McpSessionRecoveryNotification
            {
                Reason = reason,
                RecoveryStep = recoveryStep,
                Success = success,
                Message = message,
                AffectedCommands = affectedCommands,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a session event notification
        /// </summary>
        public McpSessionEventNotification CreateSessionEventNotification(
            string sessionId,
            string eventType,
            string message,
            SessionContext? context = null)
        {
            return new McpSessionEventNotification
            {
                SessionId = sessionId,
                EventType = eventType,
                Message = message,
                Context = context,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a server health notification
        /// </summary>
        public McpServerHealthNotification CreateServerHealthNotification(
            string status,
            bool cdbSessionActive,
            int queueSize,
            int activeCommands,
            TimeSpan? uptime = null)
        {
            return new McpServerHealthNotification
            {
                Status = status,
                CdbSessionActive = cdbSessionActive,
                QueueSize = queueSize,
                ActiveCommands = activeCommands,
                Uptime = uptime ?? (DateTime.UtcNow - m_serverStartTime),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Formats elapsed time for display
        /// </summary>
        private static string FormatElapsedTime(TimeSpan elapsed)
        {
            return elapsed.TotalMinutes >= 1
                ? $"{elapsed.TotalMinutes.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}m"
                : $"{elapsed.TotalSeconds.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}s";
        }
    }
}
