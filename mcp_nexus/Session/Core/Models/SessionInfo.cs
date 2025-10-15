using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus.Session.Core.Models
{
    /// <summary>
    /// Thread-safe session information container with proper encapsulation
    /// </summary>
    public class SessionInfo : IDisposable
    {
        #region Private Fields

        /// <summary>Unique session identifier</summary>
        private readonly string m_SessionId;

        /// <summary>CDB session for this debugging session</summary>
        private readonly ICdbSession m_CdbSession;

        /// <summary>Command queue service for this session</summary>
        private readonly ICommandQueueService m_CommandQueue;

        /// <summary>Session creation time (local)</summary>
        private readonly DateTime m_CreatedAt;

        /// <summary>Path to the dump file being debugged</summary>
        private readonly string m_DumpPath;

        /// <summary>Optional path to symbol files</summary>
        private readonly string? m_SymbolsPath;

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
        public string SessionId { get => m_SessionId; set { } } // Read-only from external perspective

        /// <summary>CDB session for this debugging session</summary>
        public ICdbSession CdbSession { get => m_CdbSession; set { } } // Read-only from external perspective

        /// <summary>Command queue service for this session</summary>
        public ICommandQueueService CommandQueue { get => m_CommandQueue; set { } } // Read-only from external perspective

        /// <summary>Session creation time (local)</summary>
        public DateTime CreatedAt { get => m_CreatedAt; set { } } // Read-only from external perspective

        /// <summary>Path to the dump file being debugged</summary>
        public string DumpPath { get => m_DumpPath; set { } } // Read-only from external perspective

        /// <summary>Optional path to symbol files</summary>
        public string? SymbolsPath { get => m_SymbolsPath; set { } } // Read-only from external perspective

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
            m_SessionId = string.Empty;
            m_CdbSession = null!;
            m_CommandQueue = null!;
            m_CreatedAt = DateTime.Now;
            m_DumpPath = string.Empty;
            m_SymbolsPath = null;
            m_processId = null;
            LastActivity = DateTime.Now;
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
            m_SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_CommandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
            m_CreatedAt = DateTime.Now;
            m_DumpPath = dumpPath ?? throw new ArgumentNullException(nameof(dumpPath));
            m_SymbolsPath = symbolsPath;
            m_processId = processId;
            LastActivity = DateTime.Now;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Thread-safe disposal of session resources.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed) return;

            m_disposed = true;
            Status = SessionStatus.Disposing;

            try
            {
                // Dispose command queue first to stop new commands
                m_CommandQueue?.Dispose();

                // Then dispose CDB session
                m_CdbSession?.Dispose();

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
}
