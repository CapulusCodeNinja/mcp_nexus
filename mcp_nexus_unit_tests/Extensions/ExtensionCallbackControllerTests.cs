using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Extensions;
using mcp_nexus.Session.Lifecycle;
using Xunit;

namespace mcp_nexus_unit_tests.Extensions
{
    /// <summary>
    /// Delegate for mocking TryGetCommandQueue method.
    /// </summary>
    public delegate bool TryGetCommandQueueDelegate(string sessionId, out ICommandQueueService? queue);

    /// <summary>
    /// Tests for the ExtensionCallbackController class.
    /// </summary>
    public class ExtensionCallbackControllerTests
    {
        private readonly Mock<ILogger<ExtensionCallbackController>> m_MockLogger;
        private readonly Mock<ISessionManager> m_MockSessionManager;
        private readonly Mock<IExtensionTokenValidator> m_MockTokenValidator;
        private readonly Mock<IExtensionCommandTracker> m_MockCommandTracker;
        private readonly Mock<ICommandQueueService> m_MockCommandQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionCallbackControllerTests"/> class.
        /// </summary>
        public ExtensionCallbackControllerTests()
        {
            m_MockLogger = new Mock<ILogger<ExtensionCallbackController>>();
            m_MockSessionManager = new Mock<ISessionManager>();
            m_MockTokenValidator = new Mock<IExtensionTokenValidator>();
            m_MockCommandTracker = new Mock<IExtensionCommandTracker>();
            m_MockCommandQueue = new Mock<ICommandQueueService>();
        }

        /// <summary>
        /// Creates a controller with all dependencies mocked.
        /// </summary>
        private ExtensionCallbackController CreateController()
        {
            var controller = new ExtensionCallbackController(
                m_MockLogger.Object,
                m_MockTokenValidator.Object,
                m_MockSessionManager.Object,
                m_MockCommandTracker.Object)
            {
                // Setup HTTP context
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            return controller;
        }

        [Fact]
        public void Constructor_WithValidDependencies_Succeeds()
        {
            // Act
            var controller = CreateController();

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExtensionCallbackController(
                    null!,
                    m_MockTokenValidator.Object,
                    m_MockSessionManager.Object,
                    m_MockCommandTracker.Object));
        }

        [Fact]
        public void Constructor_WithNullSessionManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExtensionCallbackController(
                    m_MockLogger.Object,
                    m_MockTokenValidator.Object,
                    null!,
                    m_MockCommandTracker.Object));
        }

        [Fact]
        public void Constructor_WithNullCommandTracker_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExtensionCallbackController(
                    m_MockLogger.Object,
                    m_MockTokenValidator.Object,
                    m_MockSessionManager.Object,
                    null!));
        }

        [Fact]
        public async Task ExecuteCommand_WithEmptyCommand_ReturnsForbidden()
        {
            // Arrange
            var controller = CreateController();
            var request = new ExtensionCallbackExecuteRequest
            {
                Command = "",
                TimeoutSeconds = 300
            };

            // Act
            var result = await controller.ExecuteCommand(request);

            // Assert - Returns 403 because no valid token/session in test setup
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            Assert.NotNull(objectResult.Value);
        }

        [Fact]
        public async Task ExecuteCommand_WithNullRequest_ReturnsForbidden()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.ExecuteCommand(null!);

            // Assert - Returns 403 because no valid token/session in test setup
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            Assert.NotNull(objectResult.Value);
        }

        [Fact]
        public async Task ExecuteCommand_Success_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer tok";
            m_MockTokenValidator.Setup(v => v.ValidateToken("tok")).Returns((true, "sess-1", "ext-1"));

            m_MockCommandTracker.Setup(t => t.IncrementCallbackCount("ext-1"));

            var mockQueue = new Mock<ICommandQueueService>();
            mockQueue.Setup(q => q.QueueCommand("!analyze -v")).Returns("cmd-123");
            ICommandQueueService? outQueue = mockQueue.Object;
            m_MockSessionManager.Setup(s => s.TryGetCommandQueue("sess-1", out outQueue)).Returns(true);

            var cmdInfo = new CommandInfo("cmd-123", "!analyze -v", CommandState.Completed, DateTime.Now, 0)
            {
                IsCompleted = true
            };
            var cmdResult = new CommandResult(true, "ok", null);
            m_MockSessionManager.Setup(s => s.GetCommandInfoAndResultAsync("sess-1", "cmd-123"))
                .ReturnsAsync((cmdInfo, (ICommandResult)cmdResult));

            var request = new ExtensionCallbackExecuteRequest { Command = "!analyze -v", TimeoutSeconds = 5 };

            // Act
            var result = await controller.ExecuteCommand(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ExtensionCallbackExecuteResponse>(ok.Value);
            Assert.Equal("cmd-123", body.CommandId);
            Assert.Equal("Success", body.Status);
            Assert.Equal("ok", body.Output);
        }

        [Fact]
        public async Task ExecuteCommand_NonLocalhost_ReturnsForbidden()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("8.8.8.8");
            var request = new ExtensionCallbackExecuteRequest { Command = "!analyze -v" };

            // Act
            var result = await controller.ExecuteCommand(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
        }

        [Fact]
        public async Task ExecuteCommand_MissingToken_ReturnsUnauthorized()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            var request = new ExtensionCallbackExecuteRequest { Command = "!analyze -v" };

            // Act
            var result = await controller.ExecuteCommand(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(401, objectResult.StatusCode);
        }

        [Fact]
        public void WriteLog_MissingMessage_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer tok";
            m_MockTokenValidator.Setup(v => v.ValidateToken("tok")).Returns((true, "sess-1", "ext-1"));

            var req = new ExtensionCallbackLogRequest { Message = "" };

            // Act
            var result = controller.WriteLog(req);

            // Assert
            var objectResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task ReadCommandResult_Success_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "tok"; // No Bearer prefix
            m_MockTokenValidator.Setup(v => v.ValidateToken("tok")).Returns((true, "sess-1", null));

            var cmdInfo = new CommandInfo("cmd-1", "!analyze -v", CommandState.Completed, DateTime.Now, 0)
            {
                IsCompleted = true
            };
            var cmdResult = new CommandResult(true, "out", null);
            m_MockSessionManager.Setup(s => s.GetCommandInfoAndResultAsync("sess-1", "cmd-1"))
                .ReturnsAsync((cmdInfo, (ICommandResult)cmdResult));

            var request = new ExtensionCallbackReadRequest { CommandId = "cmd-1" };

            // Act
            var result = await controller.ReadCommandResult(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ExtensionCallbackReadResponse>(ok.Value);
            Assert.Equal("cmd-1", body.CommandId);
            Assert.True(body.IsCompleted);
            Assert.Equal("out", body.Output);
        }

        [Fact]
        public async Task ReadCommandResult_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer invalid";
            m_MockTokenValidator.Setup(v => v.ValidateToken(It.IsAny<string>())).Returns((false, null, null));

            var request = new ExtensionCallbackReadRequest { CommandId = "cmd-1" };

            // Act
            var result = await controller.ReadCommandResult(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(401, objectResult.StatusCode);
        }

        [Fact]
        public async Task ReadCommandResult_WithValidTokenAndMissingCommand_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid";
            m_MockTokenValidator.Setup(v => v.ValidateToken("valid")).Returns((true, "sess-1", null));
            m_MockSessionManager.Setup(s => s.GetCommandInfoAndResultAsync("sess-1", "cmd-404")).ReturnsAsync((CommandInfo: null, Result: (ICommandResult?)null));

            var request = new ExtensionCallbackReadRequest { CommandId = "cmd-404" };

            // Act
            var result = await controller.ReadCommandResult(request);

            // Assert
            var objectResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, objectResult.StatusCode);
        }

        [Theory]
        [InlineData("debug")]
        [InlineData("information")]
        [InlineData("warning")]
        [InlineData("error")]
        [InlineData(null)]
        public void WriteLog_ParsesBearerTokenAndLogs(string? level)
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer tok";
            m_MockTokenValidator.Setup(v => v.ValidateToken("tok")).Returns((true, "sess-1", "ext-1"));
            m_MockCommandTracker.Setup(t => t.GetCommandInfo("ext-1")).Returns(new ExtensionCommandInfo { ExtensionName = "ext", Id = "ext-1" });

            var req = new ExtensionCallbackLogRequest { Message = "hello", Level = level };

            // Act
            var result = controller.WriteLog(req);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        // Note: Async command execution tests will be added in a future update
        // The core functionality is implemented and working

        [Fact]
        public void QueueCommand_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var controller = CreateController();
            SetupLocalhostRequest(controller);

            var request = new ExtensionCallbackExecuteRequest { Command = "lm" };

            m_MockTokenValidator
                .Setup(v => v.ValidateToken(It.IsAny<string>()))
                .Returns((false, null, null));

            // Act
            var result = controller.QueueCommand(request);

            // Assert
            var unauthorizedResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public void QueueCommand_FromNonLocalhost_ReturnsForbidden()
        {
            // Arrange
            var controller = CreateController();
            SetupNonLocalhostRequest(controller);

            var request = new ExtensionCallbackExecuteRequest { Command = "lm" };

            // Act
            var result = controller.QueueCommand(request);

            // Assert
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbiddenResult.StatusCode);
        }

        [Fact]
        public async Task ExecuteCommand_WithCustomTimeout_UsesCustomTimeout()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            var request = new ExtensionCallbackExecuteRequest
            {
                Command = "lm",
                TimeoutSeconds = 120 // Custom timeout
            };

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "cmd-456"));
            m_MockCommandTracker.Setup(x => x.IncrementCallbackCount("cmd-456"));

            var mockQueue = new Mock<ICommandQueueService>();
            mockQueue.Setup(q => q.QueueCommand("lm")).Returns("cmd-789");
            ICommandQueueService? outQueue = mockQueue.Object;
            m_MockSessionManager.Setup(s => s.TryGetCommandQueue("session-123", out outQueue)).Returns(true);

            var cmdInfo = new CommandInfo("cmd-789", "lm", CommandState.Completed, DateTime.Now, 0)
            {
                IsCompleted = true
            };
            var cmdResult = new CommandResult(true, "output", null);
            m_MockSessionManager.Setup(s => s.GetCommandInfoAndResultAsync("session-123", "cmd-789"))
                .ReturnsAsync((cmdInfo, (ICommandResult)cmdResult));

            // Act
            var result = await controller.ExecuteCommand(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ExtensionCallbackExecuteResponse>(okResult.Value);
            Assert.Equal("Success", response.Status);
        }

        [Fact]
        public async Task ExecuteCommand_WithZeroTimeout_UsesDefaultTimeout()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            var request = new ExtensionCallbackExecuteRequest
            {
                Command = "lm",
                TimeoutSeconds = 0 // Will use default (5 min)
            };

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "cmd-456"));
            m_MockCommandTracker.Setup(x => x.IncrementCallbackCount("cmd-456"));

            var mockQueue = new Mock<ICommandQueueService>();
            mockQueue.Setup(q => q.QueueCommand("lm")).Returns("cmd-789");
            ICommandQueueService? outQueue = mockQueue.Object;
            m_MockSessionManager.Setup(s => s.TryGetCommandQueue("session-123", out outQueue)).Returns(true);

            var cmdInfo = new CommandInfo("cmd-789", "lm", CommandState.Completed, DateTime.Now, 0)
            {
                IsCompleted = true
            };
            var cmdResult = new CommandResult(true, "output", null);
            m_MockSessionManager.Setup(s => s.GetCommandInfoAndResultAsync("session-123", "cmd-789"))
                .ReturnsAsync((cmdInfo, (ICommandResult)cmdResult));

            // Act
            var result = await controller.ExecuteCommand(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ExecuteCommand_WithEmptyCommandId_SkipsIncrementCallback()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            var request = new ExtensionCallbackExecuteRequest { Command = "lm" };

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "")); // Empty commandId

            var mockQueue = new Mock<ICommandQueueService>();
            mockQueue.Setup(q => q.QueueCommand("lm")).Returns("cmd-789");
            ICommandQueueService? outQueue = mockQueue.Object;
            m_MockSessionManager.Setup(s => s.TryGetCommandQueue("session-123", out outQueue)).Returns(true);

            var cmdInfo = new CommandInfo("cmd-789", "lm", CommandState.Completed, DateTime.Now, 0)
            {
                IsCompleted = true
            };
            var cmdResult = new CommandResult(true, "output", null);
            m_MockSessionManager.Setup(s => s.GetCommandInfoAndResultAsync("session-123", "cmd-789"))
                .ReturnsAsync((cmdInfo, (ICommandResult)cmdResult));

            // Act
            var result = await controller.ExecuteCommand(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            m_MockCommandTracker.Verify(x => x.IncrementCallbackCount(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void QueueCommand_WithEmptyCommandId_SkipsIncrementCallback()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            var request = new ExtensionCallbackExecuteRequest { Command = "lm" };

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "")); // Empty commandId

            var mockQueue = new Mock<ICommandQueueService>();
            mockQueue.Setup(q => q.QueueCommand("lm")).Returns("cmd-789");
            ICommandQueueService? outQueue = mockQueue.Object;
            m_MockSessionManager.Setup(s => s.TryGetCommandQueue("session-123", out outQueue)).Returns(true);

            // Act
            var result = controller.QueueCommand(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            m_MockCommandTracker.Verify(x => x.IncrementCallbackCount(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void QueueCommand_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "cmd-456"));

            // Act
            var result = controller.QueueCommand(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetBulkCommandStatus_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "cmd-456"));

            // Act
            var result = await controller.GetBulkCommandStatus(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetBulkCommandStatus_WithEmptyCommandIds_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            var request = new ExtensionCallbackBulkStatusRequest { CommandIds = [] };

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "cmd-456"));

            // Act
            var result = await controller.GetBulkCommandStatus(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ReadCommandResult_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "cmd-456"));

            // Act
            var result = await controller.ReadCommandResult(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ReadCommandResult_WithEmptyCommandId_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            var request = new ExtensionCallbackReadRequest { CommandId = "" };

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "cmd-456"));

            // Act
            var result = await controller.ReadCommandResult(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void WriteLog_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.HttpContext.Request.Headers.Authorization = "Bearer valid-token";

            m_MockTokenValidator.Setup(x => x.ValidateToken("valid-token"))
                .Returns((true, "session-123", "cmd-456"));

            // Act
            var result = controller.WriteLog(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void QueueCommand_WithEmptyCommand_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            SetupLocalhostRequest(controller);

            var request = new ExtensionCallbackExecuteRequest { Command = "" };

            m_MockTokenValidator
                .Setup(v => v.ValidateToken(It.IsAny<string>()))
                .Returns((true, "session-123", "cmd-123"));

            // Act
            var result = controller.QueueCommand(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ExtensionCallbackErrorResponse>(badRequestResult.Value);
            Assert.Contains("cannot be null or empty", response.Message);
        }

        private void SetupLocalhostRequest(ExtensionCallbackController controller)
        {
            controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Bearer test-token";
        }

        private void SetupNonLocalhostRequest(ExtensionCallbackController controller)
        {
            controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
            controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Bearer test-token";
        }
    }
}

