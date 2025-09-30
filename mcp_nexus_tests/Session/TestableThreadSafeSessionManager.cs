using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using System.Collections.Concurrent;

namespace mcp_nexus_tests.Session
{
    /// <summary>
    /// Testable version of ThreadSafeSessionManager that mocks CDB session creation
    /// </summary>
    public class TestableThreadSafeSessionManager : ThreadSafeSessionManager
    {
        private readonly Mock<ICdbSession> _mockCdbSession;
        private readonly Mock<ICommandQueueService> _mockCommandQueue;
        private readonly Dictionary<string, SessionInfo> _mockSessionInfos = new();
        private readonly Dictionary<string, SessionContext> _mockSessionContexts = new();
        private int _sessionCounter = 0;

        public TestableThreadSafeSessionManager(
            ILogger<ThreadSafeSessionManager> logger,
            IServiceProvider serviceProvider,
            IMcpNotificationService notificationService,
            IOptions<SessionConfiguration>? config = null,
            IOptions<CdbSessionOptions>? cdbOptions = null)
            : base(logger, serviceProvider, notificationService, config, cdbOptions)
        {
            _mockCdbSession = new Mock<ICdbSession>();
            _mockCommandQueue = new Mock<ICommandQueueService>();

            // Setup mock CDB session to return success
            _mockCdbSession.Setup(x => x.IsActive).Returns(true);
            _mockCdbSession.Setup(x => x.StartSession(It.IsAny<string>(), It.IsAny<string?>()))
                .Returns(Task.FromResult(true));
            _mockCdbSession.Setup(x => x.StopSession())
                .Returns(Task.FromResult(true));

            // Setup mock command queue
            _mockCommandQueue.Setup(x => x.GetQueueStatus())
                .Returns(new List<(string Id, string Command, DateTime QueueTime, string Status)>());
        }

        /// <summary>
        /// Override to create mock sessions instead of real CDB sessions
        /// </summary>
        public override async Task<string> CreateSessionAsync(string dumpPath, string? symbolsPath = null, CancellationToken cancellationToken = default)
        {
            // Validate parameters like the base class would
            if (dumpPath == null)
                throw new ArgumentNullException(nameof(dumpPath));

            if (string.IsNullOrWhiteSpace(dumpPath))
                throw new ArgumentException("Dump path cannot be empty", nameof(dumpPath));

            // Generate session ID
            var sessionNumber = Interlocked.Increment(ref _sessionCounter);
            var sessionId = $"session-{sessionNumber:D3}";

            // Create mock session info
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                DumpPath = dumpPath,
                SymbolsPath = symbolsPath,
                CreatedAt = DateTime.UtcNow,
                Status = SessionStatus.Active,
                CdbSession = _mockCdbSession.Object,
                CommandQueue = _mockCommandQueue.Object
            };

            // Create mock session context
            var sessionContext = new SessionContext
            {
                SessionId = sessionId,
                DumpPath = dumpPath,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                Status = "Active",
                Description = $"Mock session for {Path.GetFileName(dumpPath)}"
            };

            _mockSessionInfos[sessionId] = sessionInfo;
            _mockSessionContexts[sessionId] = sessionContext;

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

            if (_mockSessionContexts.TryGetValue(sessionId, out var context))
                return context;

            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override IEnumerable<SessionContext> GetActiveSessions()
        {
            return _mockSessionContexts.Values.Where(s => s.Status == "Active");
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override IEnumerable<SessionInfo> GetAllSessions()
        {
            // Based on the test expectation, GetAllSessions should only return active sessions
            return _mockSessionInfos.Values.Where(s => s.Status == SessionStatus.Active);
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override bool SessionExists(string sessionId)
        {
            return _mockSessionInfos.ContainsKey(sessionId) &&
                   _mockSessionInfos[sessionId].Status == SessionStatus.Active;
        }

        /// <summary>
        /// Override to work with mock sessions
        /// </summary>
        public override async Task<bool> CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            // Validate parameters like the base class would
            if (sessionId == null)
                throw new ArgumentNullException(nameof(sessionId));

            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID cannot be empty or whitespace", nameof(sessionId));

            if (_mockSessionInfos.TryGetValue(sessionId, out var info) &&
                _mockSessionContexts.TryGetValue(sessionId, out var context))
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
            if (_mockSessionContexts.TryGetValue(sessionId, out var context))
            {
                context.LastActivity = DateTime.UtcNow;
            }
        }

        public Mock<ICdbSession> MockCdbSession => _mockCdbSession;
        public Mock<ICommandQueueService> MockCommandQueue => _mockCommandQueue;
    }
}
