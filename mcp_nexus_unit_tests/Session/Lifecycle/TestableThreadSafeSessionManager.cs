using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.Session.Lifecycle;
using mcp_nexus.Session.Core;
using mcp_nexus.Session.Core.Models;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Notifications;
using System.Collections.Concurrent;
using mcp_nexus_unit_tests.Mocks;

namespace mcp_nexus_unit_tests.Session.Lifecycle
{
    /// <summary>
    /// Testable version of ThreadSafeSessionManager that mocks CDB session creation
    /// </summary>
    public class TestableThreadSafeSessionManager : ThreadSafeSessionManager
    {
        private readonly ICdbSession m_RealisticCdbSession;
        private readonly Mock<ICommandQueueService> m_MockCommandQueue;
        private readonly Dictionary<string, SessionInfo> m_MockSessionInfos = [];
        private readonly Dictionary<string, SessionContext> m_MockSessionContexts = [];
        private readonly SessionManagerConfiguration m_Config;
        private int _sessionCounter = 0;

        public TestableThreadSafeSessionManager(
            ILogger<ThreadSafeSessionManager> logger,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IMcpNotificationService notificationService,
            IOptions<SessionConfiguration>? config = null,
            IOptions<CdbSessionOptions>? cdbOptions = null)
            : base(logger, serviceProvider, loggerFactory, notificationService, config, cdbOptions)
        {
            m_Config = new SessionManagerConfiguration(config ?? Options.Create(new SessionConfiguration()), cdbOptions ?? Options.Create(new CdbSessionOptions()));
            m_RealisticCdbSession = RealisticCdbTestHelper.CreateBugSimulatingCdbSession(Mock.Of<ILogger>());
            m_MockCommandQueue = new Mock<ICommandQueueService>();

            // Setup mock command queue
            m_MockCommandQueue.Setup(x => x.GetQueueStatus())
                .Returns(new List<(string Id, string Command, DateTime QueueTime, string Status)>());
        }

        /// <summary>
        /// Override to create mock sessions instead of real CDB sessions
        /// </summary>
        public override async Task<string> CreateSessionAsync(string dumpPath, string? symbolsPath = null, CancellationToken cancellationToken = default)
        {
            // Validate parameters like the base class would
            ArgumentNullException.ThrowIfNull(dumpPath);

            if (string.IsNullOrWhiteSpace(dumpPath))
                throw new ArgumentException("Dump path cannot be empty", nameof(dumpPath));

            // Check session count limits (same as base class)
            if (m_Config.WouldExceedSessionLimit(m_MockSessionInfos.Count))
            {
                throw new SessionLimitExceededException(m_MockSessionInfos.Count, m_Config.Config.MaxConcurrentSessions);
            }

            // Generate session ID
            var sessionNumber = Interlocked.Increment(ref _sessionCounter);
            var sessionId = $"session-{sessionNumber:D3}";

            // Create mock session info
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = dumpPath,
                SymbolsPath = symbolsPath,
                CreatedAt = DateTime.Now,
                Status = SessionStatus.Active,
                CdbSession = m_RealisticCdbSession,
                CommandQueue = m_MockCommandQueue.Object
            };

            // Create mock session context
            var sessionContext = new SessionContext
            {
                SessionId = sessionId,
                DumpPath = dumpPath,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                Status = "Active",
                Description = $"Mock session for {Path.GetFileName(dumpPath)}"
            };

            m_MockSessionInfos[sessionId] = sessionInfo;
            m_MockSessionContexts[sessionId] = sessionContext;

            await Task.Delay(1, cancellationToken); // Simulate async work
            return sessionId;
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override SessionContext GetSessionContext(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            if (m_MockSessionContexts.TryGetValue(sessionId, out var context))
            {
                if (context.Status == "Disposed")
                    throw new SessionNotFoundException(sessionId, "Session has been disposed");
                return context;
            }

            throw new SessionNotFoundException(sessionId);
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override IEnumerable<SessionContext> GetActiveSessions()
        {
            return m_MockSessionContexts.Values.Where(s => s.Status == "Active");
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override IEnumerable<SessionInfo> GetAllSessions()
        {
            // Based on the test expectation, GetAllSessions should only return active sessions
            return m_MockSessionInfos.Values.Where(s => s.Status == SessionStatus.Active);
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override bool SessionExists(string sessionId)
        {
            return m_MockSessionInfos.ContainsKey(sessionId) &&
                   m_MockSessionInfos[sessionId].Status == SessionStatus.Active;
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override async Task<bool> CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            // Validate parameters like the base class would
            ArgumentNullException.ThrowIfNull(sessionId);

            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID cannot be empty or whitespace", nameof(sessionId));

            if (m_MockSessionInfos.TryGetValue(sessionId, out var info) &&
                m_MockSessionContexts.TryGetValue(sessionId, out var context))
            {
                info.Status = SessionStatus.Disposed;
                context.Status = "Disposed";
                await Task.Delay(1, cancellationToken); // Simulate async work
                return true;
            }
            return false;
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override void UpdateActivity(string sessionId)
        {
            if (m_MockSessionContexts.TryGetValue(sessionId, out var context))
            {
                context.LastActivity = DateTime.Now;
            }
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        // Note: TryGetCommandQueue is not virtual, so we can't override it
        // The test will need to work with the base implementation

        public ICdbSession RealisticCdbSession => m_RealisticCdbSession;
        public Mock<ICommandQueueService> MockCommandQueue => m_MockCommandQueue;

        public new void Dispose()
        {
            m_RealisticCdbSession?.Dispose();
            base.Dispose();
        }
    }
}
