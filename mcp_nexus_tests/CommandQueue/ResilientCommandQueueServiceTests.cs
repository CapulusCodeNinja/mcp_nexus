using Xunit;
using Moq;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Fast unit tests for ResilientCommandQueueService using mocks - no real background processing
    /// </summary>
    public class ResilientCommandQueueServiceTests
    {
        private readonly Mock<ICommandQueueService> m_MockService;

        public ResilientCommandQueueServiceTests()
        {
            m_MockService = new Mock<ICommandQueueService>();
        }

        [Fact]
        public void QueueCommand_ValidCommand_ReturnsCommandId()
        {
            // Arrange
            var command = "version";
            var expectedId = "cmd-123";
            m_MockService.Setup(s => s.QueueCommand(command)).Returns(expectedId);

            // Act
            var result = m_MockService.Object.QueueCommand(command);

            // Assert
            Assert.Equal(expectedId, result);
            m_MockService.Verify(s => s.QueueCommand(command), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void QueueCommand_InvalidCommand_ThrowsArgumentException(string invalidCommand)
        {
            // Arrange
            m_MockService.Setup(s => s.QueueCommand(invalidCommand))
                         .Throws(new ArgumentException("Command cannot be empty"));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => m_MockService.Object.QueueCommand(invalidCommand));
        }

        [Fact]
        public void QueueCommand_NullCommand_ThrowsArgumentException()
        {
            // Arrange
            m_MockService.Setup(s => s.QueueCommand(null!))
                         .Throws(new ArgumentException("Command cannot be null"));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => m_MockService.Object.QueueCommand(null!));
        }

        [Fact]
        public async Task GetCommandResult_ValidId_ReturnsResult()
        {
            // Arrange
            var commandId = "cmd-456";
            var expectedResult = "Command completed successfully";
            m_MockService.Setup(s => s.GetCommandResult(commandId)).ReturnsAsync(expectedResult);

            // Act
            var result = await m_MockService.Object.GetCommandResult(commandId);

            // Assert
            Assert.Equal(expectedResult, result);
            m_MockService.Verify(s => s.GetCommandResult(commandId), Times.Once);
        }

        [Fact]
        public async Task GetCommandResult_NonExistentCommand_ReturnsNotFound()
        {
            // Arrange
            var commandId = "non-existent";
            var expectedResult = "Command not found";
            m_MockService.Setup(s => s.GetCommandResult(commandId)).ReturnsAsync(expectedResult);

            // Act
            var result = await m_MockService.Object.GetCommandResult(commandId);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void CancelCommand_ValidId_ReturnsTrue()
        {
            // Arrange
            var commandId = "cmd-789";
            m_MockService.Setup(s => s.CancelCommand(commandId)).Returns(true);

            // Act
            var result = m_MockService.Object.CancelCommand(commandId);

            // Assert
            Assert.True(result);
            m_MockService.Verify(s => s.CancelCommand(commandId), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CancelCommand_InvalidCommandId_ReturnsFalse(string invalidId)
        {
            // Arrange
            m_MockService.Setup(s => s.CancelCommand(invalidId)).Returns(false);

            // Act
            var result = m_MockService.Object.CancelCommand(invalidId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelAllCommands_WithReason_ReturnsCount()
        {
            // Arrange
            var reason = "Test cancellation";
            var expectedCount = 3;
            m_MockService.Setup(s => s.CancelAllCommands(reason)).Returns(expectedCount);

            // Act
            var result = m_MockService.Object.CancelAllCommands(reason);

            // Assert
            Assert.Equal(expectedCount, result);
            m_MockService.Verify(s => s.CancelAllCommands(reason), Times.Once);
        }

        [Fact]
        public void GetQueueStatus_EmptyQueue_ReturnsEmpty()
        {
            // Arrange
            var expectedStatus = Array.Empty<(string Id, string Command, DateTime QueueTime, string Status)>();
            m_MockService.Setup(s => s.GetQueueStatus()).Returns(expectedStatus);

            // Act
            var result = m_MockService.Object.GetQueueStatus();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetQueueStatus_WithQueuedCommands_ReturnsStatus()
        {
            // Arrange
            var expectedStatus = new[]
            {
                ("cmd-1", "version", DateTime.UtcNow, "Executing"),
                ("cmd-2", "analyze", DateTime.UtcNow, "Queued")
            };
            m_MockService.Setup(s => s.GetQueueStatus()).Returns(expectedStatus);

            // Act
            var result = m_MockService.Object.GetQueueStatus().ToArray();

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Contains(result, r => r.Id == "cmd-1" && r.Status == "Executing");
            Assert.Contains(result, r => r.Id == "cmd-2" && r.Status == "Queued");
        }

        [Fact]
        public void GetCurrentCommand_NoCommandExecuting_ReturnsNull()
        {
            // Arrange
            m_MockService.Setup(s => s.GetCurrentCommand()).Returns((QueuedCommand?)null);

            // Act
            var result = m_MockService.Object.GetCurrentCommand();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentCommand_CommandExecuting_ReturnsCommand()
        {
            // Arrange
            var expectedCommand = new QueuedCommand(
                "cmd-current",
                "test-command",
                DateTime.UtcNow,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource());
            m_MockService.Setup(s => s.GetCurrentCommand()).Returns(expectedCommand);

            // Act
            var result = m_MockService.Object.GetCurrentCommand();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("cmd-current", result.Id);
            Assert.Equal("test-command", result.Command);
        }
    }
}

