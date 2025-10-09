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
    }
}

