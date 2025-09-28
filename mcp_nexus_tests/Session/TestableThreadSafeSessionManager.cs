using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;

namespace mcp_nexus_tests.Session
{
    /// <summary>
    /// Testable version of ThreadSafeSessionManager that mocks CDB session creation
    /// </summary>
    public class TestableThreadSafeSessionManager : ThreadSafeSessionManager
    {
        private readonly Mock<ICdbSession> _mockCdbSession;
        private readonly Mock<ICommandQueueService> _mockCommandQueue;

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

        protected override ICdbSession CreateCdbSession(ILogger sessionLogger, string sessionId)
        {
            return _mockCdbSession.Object;
        }

        protected override ICommandQueueService CreateCommandQueue(ICdbSession cdbSession, ILogger sessionLogger, string sessionId)
        {
            return _mockCommandQueue.Object;
        }

        public Mock<ICdbSession> MockCdbSession => _mockCdbSession;
        public Mock<ICommandQueueService> MockCommandQueue => _mockCommandQueue;
    }
}
