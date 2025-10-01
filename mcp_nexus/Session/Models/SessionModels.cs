using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;

namespace mcp_nexus.Session.Models
{
    /// <summary>
    /// Represents the current status of a debugging session
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>Session is initializing</summary>
        Initializing,
        /// <summary>Session is active and ready for commands</summary>
        Active,
        /// <summary>Session is being cleaned up</summary>
        Disposing,
        /// <summary>Session has been disposed</summary>
        Disposed,
        /// <summary>Session encountered an error</summary>
        Error
    }

    /// <summary>
    /// Thread-safe session information container with proper encapsulation
    /// </summary>
    public class SessionInfo : IDisposable
    {
        #region Private Fields

        /// <summary>Unique session identifier</summary>
        private readonly string m_sessionId;

        /// <summary>CDB session for this debugging session</summary>
        private readonly ICdbSession m_cdbSession;

        /// <summary>Command queue service for this session</summary>
        private readonly ICommandQueueService m_commandQueue;

        /// <summary>Session creation time (UTC)</summary>
        private readonly DateTime m_createdAt;

        /// <summary>Path to the dump file being debugged</summary>
        private readonly string m_dumpPath;

        /// <summary>Optional path to symbol files</summary>
        private readonly string? m_symbolsPath;

        /// <summary>Process ID of the CDB debugger process</summary>
        private readonly int? m_processId;

        /// <summary>Current session status - using volatile int for thread-safe atomic operations</summary>
        private volatile int m_status = (int)SessionStatus.Initializing;

        /// <summary>Thread-safe last activity tracking using volatile read/write for lock-free access</summary>
        private long m_lastActivityTicks;

        /// <summary>Disposal state tracking</summary>
        private volatile bool m_disposed = false;

        #endregion

        #region Public Properties

        /// <summary>Unique session identifier</summary>
        public string SessionId { get => m_sessionId; set { } } // Read-only from external perspective

        /// <summary>CDB session for this debugging session</summary>
        public ICdbSession CdbSession { get => m_cdbSession; set { } } // Read-only from external perspective

        /// <summary>Command queue service for this session</summary>
        public ICommandQueueService CommandQueue { get => m_commandQueue; set { } } // Read-only from external perspective

        /// <summary>Session creation time (UTC)</summary>
        public DateTime CreatedAt { get => m_createdAt; set { } } // Read-only from external perspective

        /// <summary>Path to the dump file being debugged</summary>
        public string DumpPath { get => m_dumpPath; set { } } // Read-only from external perspective

        /// <summary>Optional path to symbol files</summary>
        public string? SymbolsPath { get => m_symbolsPath; set { } } // Read-only from external perspective

        /// <summary>Process ID of the CDB debugger process</summary>
        public int? ProcessId { get => m_processId; set { } } // Read-only from external perspective

        /// <summary>Thread-safe session status property</summary>
        public SessionStatus Status
        {
            get => (SessionStatus)m_status;
            set => m_status = (int)value;
        }

        /// <summary>Thread-safe way to get/set last activity time using volatile operations</summary>
        public DateTime LastActivity
        {
            get => new(Volatile.Read(ref m_lastActivityTicks));
            set => Volatile.Write(ref m_lastActivityTicks, value.Ticks);
        }

        /// <summary>Check if session is disposed</summary>
        public bool IsDisposed => m_disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize session with current time as last activity
        /// </summary>
        public SessionInfo()
        {
            m_sessionId = string.Empty;
            m_cdbSession = null!;
            m_commandQueue = null!;
            m_createdAt = DateTime.UtcNow;
            m_dumpPath = string.Empty;
            m_symbolsPath = null;
            m_processId = null;
            LastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// Initialize session with provided parameters
        /// </summary>
        /// <param name="sessionId">Unique session identifier</param>
        /// <param name="cdbSession">CDB session for this debugging session</param>
        /// <param name="commandQueue">Command queue service for this session</param>
        /// <param name="dumpPath">Path to the dump file being debugged</param>
        /// <param name="symbolsPath">Optional path to symbol files</param>
        /// <param name="processId">Process ID of the CDB debugger process</param>
        public SessionInfo(string sessionId, ICdbSession cdbSession, ICommandQueueService commandQueue, 
            string dumpPath, string? symbolsPath = null, int? processId = null)
        {
            m_sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_commandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
            m_createdAt = DateTime.UtcNow;
            m_dumpPath = dumpPath ?? throw new ArgumentNullException(nameof(dumpPath));
            m_symbolsPath = symbolsPath;
            m_processId = processId;
            LastActivity = DateTime.UtcNow;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Thread-safe disposal of session resources
        /// </summary>
        public void Dispose()
        {
            if (m_disposed) return;

            m_disposed = true;
            Status = SessionStatus.Disposing;

            try
            {
                // Dispose command queue first to stop new commands
                m_commandQueue?.Dispose();

                // Then dispose CDB session
                m_cdbSession?.Dispose();

                Status = SessionStatus.Disposed;
            }
            catch
            {
                Status = SessionStatus.Error;
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// Context information about a session for AI client guidance
    /// </summary>
    public class SessionContext
    {
        /// <summary>Session identifier</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Human-readable session description</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Path to the dump file being debugged</summary>
        public string? DumpPath { get; set; }

        /// <summary>Session creation time</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Last activity time</summary>
        public DateTime LastActivity { get; set; }

        /// <summary>Current session status</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Number of commands processed in this session</summary>
        public int CommandsProcessed { get; set; }

        /// <summary>Number of active/pending commands</summary>
        public int ActiveCommands { get; set; }

        /// <summary>Time until session expires due to inactivity</summary>
        public TimeSpan? TimeUntilExpiry { get; set; }

        /// <summary>Helpful hints for AI client about session usage</summary>
        public List<string> UsageHints { get; set; } = new();
    }

    /// <summary>
    /// Exception thrown when a session is not found
    /// </summary>
    public class SessionNotFoundException : Exception
    {
        public string SessionId { get; }

        public SessionNotFoundException(string sessionId)
            : base($"Session '{sessionId}' not found or has expired")
        {
            SessionId = sessionId;
        }

        public SessionNotFoundException(string sessionId, string message)
            : base(message)
        {
            SessionId = sessionId;
        }

        public SessionNotFoundException(string sessionId, string message, Exception innerException)
            : base(message, innerException)
        {
            SessionId = sessionId;
        }
    }

    /// <summary>
    /// Exception thrown when session limit is exceeded
    /// </summary>
    public class SessionLimitExceededException : Exception
    {
        public int CurrentSessions { get; }
        public int MaxSessions { get; }

        public SessionLimitExceededException(int currentSessions, int maxSessions)
            : base($"Maximum concurrent sessions exceeded: {currentSessions}/{maxSessions}")
        {
            CurrentSessions = currentSessions;
            MaxSessions = maxSessions;
        }
    }

    /// <summary>
    /// Configuration for session management
    /// </summary>
    public class SessionConfiguration
    {
        /// <summary>Maximum number of concurrent sessions</summary>
        public int MaxConcurrentSessions { get; set; } = 1000;

        /// <summary>Session timeout due to inactivity</summary>
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>How often to check for expired sessions</summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>How long to wait for session disposal during cleanup</summary>
        public TimeSpan DisposalTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Default command timeout per session</summary>
        public TimeSpan DefaultCommandTimeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>Memory threshold in bytes for triggering cleanup (default: 1GB)</summary>
        public long MemoryCleanupThresholdBytes { get; set; } = 1_000_000_000; // 1GB
    }

    /// <summary>
    /// Configuration for CDB session creation
    /// </summary>
    public class CdbSessionOptions
    {
        /// <summary>Command timeout in milliseconds</summary>
        public int CommandTimeoutMs { get; set; } = 30000;

        /// <summary>Symbol server timeout in milliseconds</summary>
        public int SymbolServerTimeoutMs { get; set; } = 30000;

        /// <summary>Maximum symbol server retries</summary>
        public int SymbolServerMaxRetries { get; set; } = 1;

        /// <summary>Symbol search path</summary>
        public string? SymbolSearchPath { get; set; }

        /// <summary>Custom CDB executable path</summary>
        public string? CustomCdbPath { get; set; }
    }
}

