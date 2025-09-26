using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Session;
using mcp_nexus.Tools;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using Xunit;

namespace mcp_nexus_tests.Tools
{
    public class SessionManagementCommandsTests
    {
        private readonly Mock<ILogger<SessionAwareWindbgTool>> _mockLogger;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly SessionAwareWindbgTool _tool;

        public NewCommandsTests()
        {
            _mockLogger = new Mock<ILogger<SessionAwareWindbgTool>>();
            _mockSessionManager = new Mock<ISessionManager>();
            _tool = new SessionAwareWindbgTool(_mockLogger.Object, _mockSessionManager.Object);
        }

        [Fact]
        public async Task nexus_list_dump_analyze_sessions_ReturnsSessionsList()
        {
            // Arrange
            var sessions = new List<SessionInfo>
            {
                new SessionInfo
                {
                    SessionId = "sess-000001-abc12345-12345678-0001",
                    Status = SessionStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    DumpPath = "C:\\test\\dump1.dmp"
                },
                new SessionInfo
                {
                    SessionId = "sess-000002-def67890-87654321-0002",
                    Status = SessionStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    DumpPath = "C:\\test\\dump2.dmp"
                }
            };

            _mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            _mockSessionManager.Setup(x => x.GetSessionContext(It.IsAny<string>()))
                .Returns(new SessionContext
                {
                    SessionId = "test-session",
                    DumpPath = "C:\\test\\dump.dmp",
                    CommandsProcessed = 5,
                    ActiveCommands = 2
                });

            // Act
            var result = await _tool.nexus_list_dump_analyze_sessions();

            // Assert
            Assert.NotNull(result);
            
            // Verify the result is a proper response object
            var resultType = result.GetType();
            var successProperty = resultType.GetProperty("success");
            Assert.NotNull(successProperty);
            
            var successValue = successProperty.GetValue(result);
            Assert.True((bool)successValue);
        }

        [Fact]
        public async Task nexus_list_dump_analyze_session_async_commands_WithValidSession_ReturnsCommandsList()
        {
            // Arrange
            var sessionId = "sess-000001-abc12345-12345678-0001";
            var mockCommandQueue = new Mock<ICommandQueueService>();
            
            _mockSessionManager.Setup(x => x.SessionExists(sessionId)).Returns(true);
            _mockSessionManager.Setup(x => x.GetCommandQueue(sessionId)).Returns(mockCommandQueue.Object);
            _mockSessionManager.Setup(x => x.GetSessionContext(sessionId))
                .Returns(new SessionContext
                {
                    SessionId = sessionId,
                    DumpPath = "C:\\test\\dump.dmp"
                });

            // Act
            var result = await _tool.nexus_list_dump_analyze_session_async_commands(sessionId);

            // Assert
            Assert.NotNull(result);
            
            // Verify the result is a proper response object
            var resultType = result.GetType();
            var successProperty = resultType.GetProperty("success");
            Assert.NotNull(successProperty);
            
            var successValue = successProperty.GetValue(result);
            Assert.True((bool)successValue);
        }

        [Fact]
        public async Task nexus_list_dump_analyze_session_async_commands_WithInvalidSession_ReturnsError()
        {
            // Arrange
            var sessionId = "invalid-session-id";
            
            _mockSessionManager.Setup(x => x.SessionExists(sessionId)).Returns(false);

            // Act
            var result = await _tool.nexus_list_dump_analyze_session_async_commands(sessionId);

            // Assert
            Assert.NotNull(result);
            
            // Verify the result is an error response
            var resultType = result.GetType();
            var successProperty = resultType.GetProperty("success");
            Assert.NotNull(successProperty);
            
            var successValue = successProperty.GetValue(result);
            Assert.False((bool)successValue);
        }

        [Fact]
        public async Task nexus_list_dump_analyze_sessions_WithException_ReturnsError()
        {
            // Arrange
            _mockSessionManager.Setup(x => x.GetAllSessions())
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _tool.nexus_list_dump_analyze_sessions();

            // Assert
            Assert.NotNull(result);
            
            // Verify the result is an error response
            var resultType = result.GetType();
            var successProperty = resultType.GetProperty("success");
            Assert.NotNull(successProperty);
            
            var successValue = successProperty.GetValue(result);
            Assert.False((bool)successValue);
        }
    }
}
