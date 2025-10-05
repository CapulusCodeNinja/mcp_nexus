using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests that would have caught the command ID mismatch and cache retrieval bugs
    /// </summary>
    public class CommandIdCompatibilityTests : IDisposable
    {
        private readonly Mock<ICdbSession> _mockCdbSession;
        private readonly Mock<ILogger<IsolatedCommandQueueService>> _mockIsolatedLogger;
        private readonly Mock<ILogger<CommandQueueService>> _mockQueueLogger;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<IMcpNotificationService> _mockNotificationService;
        private readonly IsolatedCommandQueueService _isolatedService;
        private readonly CommandQueueService _queueService;

        public CommandIdCompatibilityTests()
        {
            _mockCdbSession = new Mock<ICdbSession>();
            _mockIsolatedLogger = new Mock<ILogger<IsolatedCommandQueueService>>();
            _mockQueueLogger = new Mock<ILogger<CommandQueueService>>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockNotificationService = new Mock<IMcpNotificationService>();
            _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

            // Setup mock CDB session
            _mockCdbSession.Setup(x => x.IsActive).Returns(true);
            _mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync("Mock result");

            // Create services with different ID generation strategies
            _isolatedService = new IsolatedCommandQueueService(
                _mockCdbSession.Object,
                _mockIsolatedLogger.Object,
                _mockNotificationService.Object,
                "test-session-123");

            _queueService = new CommandQueueService(
                _mockCdbSession.Object,
                _mockQueueLogger.Object,
                _mockLoggerFactory.Object);
        }

        public void Dispose()
        {
            _isolatedService?.Dispose();
        }

        [Fact]
        public void QueueCommand_DifferentServices_GenerateDifferentIdFormats()
        {
            // This test would have caught the command ID format mismatch
            // Arrange & Act
            var isolatedId = _isolatedService.QueueCommand("test command");
            var queueId = _queueService.QueueCommand("test command");

            // Assert
            Assert.StartsWith("cmd-test-session-123-", isolatedId);
            Assert.DoesNotContain("cmd-test-session-123-", queueId); // Should be GUID format
            Assert.NotEqual(isolatedId, queueId);
        }

        [Fact]
        public async Task GetCommandResult_WithCrossServiceId_HandlesCorrectly()
        {
            // This test would have caught the command ID mismatch bug
            // Arrange
            var isolatedId = _isolatedService.QueueCommand("test command");
            var queueId = _queueService.QueueCommand("test command");

            // Wait for commands to complete
            await Task.Delay(100);

            // Act - Try to get results with wrong service
            var isolatedResult = await _isolatedService.GetCommandResult(isolatedId);
            var queueResult = await _queueService.GetCommandResult(queueId);

            // Try cross-service lookups (these should fail)
            var crossResult1 = await _isolatedService.GetCommandResult(queueId);
            var crossResult2 = await _queueService.GetCommandResult(isolatedId);

            // Assert
            Assert.Contains("Mock result", isolatedResult);
            Assert.Contains("Mock result", queueResult);
            Assert.Contains("Command not found", crossResult1);
            Assert.Contains("Command not found", crossResult2);
        }

        [Fact]
        public async Task GetCommandResult_AfterCleanup_RetrievesFromCache()
        {
            // This test would have caught the cache retrieval bug
            // Arrange
            var commandId = _isolatedService.QueueCommand("test command");

            // Wait for completion
            await Task.Delay(100);

            // Act - Get result immediately
            var result1 = await _isolatedService.GetCommandResult(commandId);

            // Simulate cleanup (command removed from active tracker but still in cache)
            // Wait a bit more
            await Task.Delay(100);

            // Try to get result again (should work from cache)
            var result2 = await _isolatedService.GetCommandResult(commandId);

            // Assert
            Assert.Equal(result1, result2);
            Assert.Contains("Mock result", result1);
        }

        [Fact]
        public void QueueCommand_MultipleCommands_GenerateSequentialIds()
        {
            // This test would have caught the command ID generation issues
            // Arrange & Act
            var id1 = _isolatedService.QueueCommand("command 1");
            var id2 = _isolatedService.QueueCommand("command 2");
            var id3 = _isolatedService.QueueCommand("command 3");

            // Assert
            Assert.StartsWith("cmd-test-session-123-", id1);
            Assert.StartsWith("cmd-test-session-123-", id2);
            Assert.StartsWith("cmd-test-session-123-", id3);
            Assert.Contains("0001", id1);
            Assert.Contains("0002", id2);
            Assert.Contains("0003", id3);
            Assert.NotEqual(id1, id2);
            Assert.NotEqual(id2, id3);
        }

        [Fact]
        public async Task GetCommandResult_WithInvalidId_ReturnsErrorMessage()
        {
            // This test would have caught the error handling issues
            // Arrange
            var invalidId = "invalid-command-id";

            // Act
            var result = await _isolatedService.GetCommandResult(invalidId);

            // Assert
            Assert.Contains("Command not found", result);
        }

        [Fact]
        public async Task GetCommandResult_WithNullId_ReturnsErrorMessage()
        {
            // This test would have caught the null handling issues
            // Arrange & Act
            var result = await _isolatedService.GetCommandResult(null!);

            // Assert
            Assert.Contains("Command ID cannot be null or empty", result);
        }
    }
}
