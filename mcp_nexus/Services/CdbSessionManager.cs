using System.Collections.Concurrent;
using mcp_nexus.Helper;

namespace mcp_nexus.Services
{
    public record SessionInfo(
        string SessionId,
        string Target,
        DateTime CreatedAt,
        CdbSession CdbSession,
        CommandQueueService CommandQueue
    );

    public class CdbSessionManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, SessionInfo> m_sessions = new();
        private readonly ILogger<CdbSessionManager> m_logger;
        private readonly IServiceProvider m_serviceProvider;
        private readonly string? m_customCdbPath;
        private readonly int m_commandTimeoutMs;
        private readonly int m_symbolServerTimeoutMs;
        private readonly int m_symbolServerMaxRetries;
        private readonly string? m_symbolSearchPath;

        public CdbSessionManager(
            ILogger<CdbSessionManager> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            string? customCdbPath = null)
        {
            m_logger = logger;
            m_serviceProvider = serviceProvider;
            m_customCdbPath = customCdbPath;
            
            // Read configuration values
            m_commandTimeoutMs = configuration.GetValue("McpNexus:Debugging:CommandTimeoutMs", 30000);
            m_symbolServerTimeoutMs = configuration.GetValue("McpNexus:Debugging:SymbolServerTimeoutMs", 30000);
            m_symbolServerMaxRetries = configuration.GetValue("McpNexus:Debugging:SymbolServerMaxRetries", 1);
            m_symbolSearchPath = configuration.GetValue<string?>("McpNexus:Debugging:SymbolSearchPath");
            
            m_logger.LogInformation("CdbSessionManager initialized - parallel sessions supported");
        }

        public async Task<(bool Success, string SessionId)> CreateSession(string target, string? sessionName = null)
        {
            var sessionId = Guid.NewGuid().ToString();
            var displayName = sessionName ?? $"Session-{sessionId[..8]}";
            
            m_logger.LogInformation("Creating new CDB session {SessionId} ({DisplayName}) for target: {Target}", 
                sessionId, displayName, target);

            try
            {
                // Create a new CdbSession instance
                var sessionLogger = m_serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<CdbSession>();
                var cdbSession = new CdbSession(
                    sessionLogger,
                    m_commandTimeoutMs,
                    m_customCdbPath,
                    m_symbolServerTimeoutMs,
                    m_symbolServerMaxRetries,
                    m_symbolSearchPath);

                // Create a dedicated command queue for this session
                var queueLogger = m_serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<CommandQueueService>();
                var commandQueue = new CommandQueueService(cdbSession, queueLogger);

                // Start the CDB session
                var success = await cdbSession.StartSession(target);
                if (success)
                {
                    var sessionInfo = new SessionInfo(sessionId, target, DateTime.UtcNow, cdbSession, commandQueue);
                    m_sessions[sessionId] = sessionInfo;
                    
                    m_logger.LogInformation("✅ Successfully created CDB session {SessionId} ({DisplayName}). Active sessions: {Count}", 
                        sessionId, displayName, m_sessions.Count);
                    
                    return (true, sessionId);
                }
                else
                {
                    m_logger.LogError("❌ Failed to start CDB session {SessionId} ({DisplayName})", sessionId, displayName);
                    commandQueue.Dispose();
                    cdbSession.Dispose();
                    return (false, string.Empty);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Error creating CDB session {SessionId} ({DisplayName})", sessionId, displayName);
                return (false, string.Empty);
            }
        }

        public async Task<bool> CloseSession(string sessionId)
        {
            m_logger.LogInformation("Closing CDB session {SessionId}", sessionId);

            if (m_sessions.TryRemove(sessionId, out var sessionInfo))
            {
                try
                {
                    m_logger.LogInformation("🔄 Stopping session {SessionId}...", sessionId);
                    
                    // Stop the command queue first
                    sessionInfo.CommandQueue.Dispose();
                    
                    // Then stop the CDB session
                    var success = await sessionInfo.CdbSession.StopSession();
                    sessionInfo.CdbSession.Dispose();
                    
                    m_logger.LogInformation("✅ Successfully closed CDB session {SessionId}. Remaining sessions: {Count}", 
                        sessionId, m_sessions.Count);
                    
                    return success;
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "💥 Error closing CDB session {SessionId}", sessionId);
                    
                    // Ensure cleanup even on error
                    try { sessionInfo.CommandQueue.Dispose(); } catch { }
                    try { sessionInfo.CdbSession.Dispose(); } catch { }
                    
                    return false;
                }
            }
            else
            {
                m_logger.LogWarning("⚠️ Session {SessionId} not found for closing", sessionId);
                return false;
            }
        }

        public SessionInfo? GetSession(string sessionId)
        {
            m_sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public CommandQueueService? GetCommandQueue(string sessionId)
        {
            return GetSession(sessionId)?.CommandQueue;
        }

        public IEnumerable<SessionInfo> GetActiveSessions()
        {
            return m_sessions.Values.ToList();
        }

        public int GetActiveSessionCount()
        {
            return m_sessions.Count;
        }

        public bool HasActiveSessions()
        {
            return m_sessions.Count > 0;
        }

        public string GetSessionsStatus()
        {
            if (m_sessions.Count == 0)
            {
                return "No active sessions";
            }

            var status = new List<string>();
            foreach (var session in m_sessions.Values.OrderBy(s => s.CreatedAt))
            {
                var duration = DateTime.UtcNow - session.CreatedAt;
                var isActive = session.CdbSession.IsActive;
                var queuedCommands = session.CommandQueue.GetQueueStatus().Count(s => s.Status == "Queued");
                var executingCommands = session.CommandQueue.GetQueueStatus().Count(s => s.Status == "Executing");
                
                status.Add($"Session {session.SessionId[..8]}: {session.Target} " +
                          $"(Age: {duration.TotalMinutes:F1}m, Active: {isActive}, " +
                          $"Queue: {queuedCommands} queued, {executingCommands} executing)");
            }

            return string.Join("\n", status);
        }

        public void Dispose()
        {
            m_logger.LogInformation("🗑️ Disposing CdbSessionManager with {SessionCount} active sessions", m_sessions.Count);

            var sessions = m_sessions.Values.ToList();
            foreach (var sessionInfo in sessions)
            {
                try
                {
                    m_logger.LogInformation("🔄 Auto-closing session {SessionId} during shutdown", sessionInfo.SessionId);
                    
                    sessionInfo.CommandQueue.Dispose();
                    sessionInfo.CdbSession.StopSession().Wait(TimeSpan.FromSeconds(5));
                    sessionInfo.CdbSession.Dispose();
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "💥 Error disposing session {SessionId} during shutdown", sessionInfo.SessionId);
                }
            }

            m_sessions.Clear();
            m_logger.LogInformation("✅ CdbSessionManager disposed");
        }
    }
}
