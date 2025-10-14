using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.CommandQueue;
using mcp_nexus.Extensions;
using mcp_nexus.Session;
using Xunit;

namespace mcp_nexus_tests.Extensions
{
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
                m_MockCommandTracker.Object);

            // Setup HTTP context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
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
            controller.HttpContext.Request.Headers["Authorization"] = "Bearer tok";
            m_MockTokenValidator.Setup(v => v.ValidateToken("tok")).Returns((true, "sess-1", "ext-1"));

            m_MockCommandTracker.Setup(t => t.IncrementCallbackCount("ext-1"));

            var mockQueue = new Mock<ICommandQueueService>();
            mockQueue.Setup(q => q.QueueCommand("!analyze -v")).Returns("cmd-123");
            ICommandQueueService? outQueue = mockQueue.Object;
            m_MockSessionManager.Setup(s => s.TryGetCommandQueue("sess-1", out outQueue)).Returns(true);

            var cmdInfo = new CommandInfo("cmd-123", "!analyze -v", CommandState.Completed, DateTime.UtcNow, 0);
            cmdInfo.IsCompleted = true;
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
            controller.HttpContext.Request.Headers["Authorization"] = "Bearer tok";
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
            controller.HttpContext.Request.Headers["Authorization"] = "tok"; // No Bearer prefix
            m_MockTokenValidator.Setup(v => v.ValidateToken("tok")).Returns((true, "sess-1", null));

            var cmdInfo = new CommandInfo("cmd-1", "!analyze -v", CommandState.Completed, DateTime.UtcNow, 0);
            cmdInfo.IsCompleted = true;
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
            controller.HttpContext.Request.Headers["Authorization"] = "Bearer invalid";
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
            controller.HttpContext.Request.Headers["Authorization"] = "Bearer valid";
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
            controller.HttpContext.Request.Headers["Authorization"] = "Bearer tok";
            m_MockTokenValidator.Setup(v => v.ValidateToken("tok")).Returns((true, "sess-1", "ext-1"));
            m_MockCommandTracker.Setup(t => t.GetCommandInfo("ext-1")).Returns(new ExtensionCommandInfo { ExtensionName = "ext", Id = "ext-1" });

            var req = new ExtensionCallbackLogRequest { Message = "hello", Level = level };

            // Act
            var result = controller.WriteLog(req);

            // Assert
            Assert.IsType<OkResult>(result);
        }
    }
}

