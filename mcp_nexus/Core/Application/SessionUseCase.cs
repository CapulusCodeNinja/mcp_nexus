using mcp_nexus.Core.Domain;
using DomainSession = mcp_nexus.Core.Domain.ISession;

namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Session use case implementation
    /// </summary>
    public class SessionUseCase : ISessionUseCase
    {
        #region Private Fields

        private readonly IServiceLocator m_serviceLocator;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new session use case
        /// </summary>
        /// <param name="serviceLocator">Service locator</param>
        public SessionUseCase(IServiceLocator serviceLocator)
        {
            m_serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new session
        /// </summary>
        /// <param name="dumpPath">Path to dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        /// <returns>Created session</returns>
        public async Task<DomainSession> CreateSessionAsync(string dumpPath, string? symbolsPath = null)
        {
            if (string.IsNullOrEmpty(dumpPath))
                throw new ArgumentException("Dump path cannot be null or empty", nameof(dumpPath));

            // Get required services
            var debuggerService = m_serviceLocator.GetService<IDebuggerService>();
            var sessionRepository = m_serviceLocator.GetService<ISessionRepository>();
            var notificationService = m_serviceLocator.GetService<INotificationService>();

            // Initialize debugger session
            var sessionId = await debuggerService.InitializeSessionAsync(dumpPath, symbolsPath);

            // Create domain session
            var session = new Session(sessionId, dumpPath, symbolsPath);
            session.UpdateStatus(SessionStatus.Active);

            // Save session
            await sessionRepository.SaveAsync(session);

            // Notify session created
            await notificationService.PublishEventAsync("SessionCreated", new { SessionId = sessionId, DumpPath = dumpPath });

            return session;
        }

        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Session or null if not found</returns>
        public async Task<DomainSession?> GetSessionAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            var sessionRepository = m_serviceLocator.GetService<ISessionRepository>();
            return await sessionRepository.GetByIdAsync(sessionId);
        }

        /// <summary>
        /// Closes a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>True if closed successfully</returns>
        public async Task<bool> CloseSessionAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            // Get required services
            var debuggerService = m_serviceLocator.GetService<IDebuggerService>();
            var sessionRepository = m_serviceLocator.GetService<ISessionRepository>();
            var notificationService = m_serviceLocator.GetService<INotificationService>();

            // Get session
            var session = await sessionRepository.GetByIdAsync(sessionId);
            if (session == null)
                return false;

            // Update session status
            session.UpdateStatus(SessionStatus.Disposing);

            // Close debugger session
            var success = await debuggerService.CloseSessionAsync(sessionId);

            if (success)
            {
                // Dispose session
                session.Dispose();
                await sessionRepository.SaveAsync(session);

                // Notify session closed
                await notificationService.PublishEventAsync("SessionClosed", new { SessionId = sessionId });
            }

            return success;
        }

        /// <summary>
        /// Gets all active sessions
        /// </summary>
        /// <returns>Collection of active sessions</returns>
        public async Task<IEnumerable<DomainSession>> GetActiveSessionsAsync()
        {
            var sessionRepository = m_serviceLocator.GetService<ISessionRepository>();
            return await sessionRepository.GetActiveSessionsAsync();
        }

        #endregion
    }
}
