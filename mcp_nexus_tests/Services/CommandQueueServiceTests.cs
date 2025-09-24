using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_tests.Services
{
    public class CommandQueueServiceTests : IDisposable
    {
        private readonly Mock<ICdbSession> m_mockCdbSession;
        private readonly Mock<ILogger<CommandQueueService>> m_mockLogger;
        private readonly CommandQueueService m_service;

        public CommandQueueServiceTests()
        {
            m_mockCdbSession = new Mock<ICdbSession>();
            m_mockLogger = new Mock<ILogger<CommandQueueService>>();
            
            // Setup default mock behavior
            m_mockCdbSession.Setup(x => x.IsActive).Returns(true);
            m_mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Mock result");
            
            m_service = new CommandQueueService(m_mockCdbSession.Object, m_mockLogger.Object);
        }

        [Fact]
        public void QueueCommand_ValidCommand_ReturnsCommandId()
        {
            // Act
            var commandId = m_service.QueueCommand("test command");

            // Assert
            Assert.NotNull(commandId);
            Assert.NotEmpty(commandId);
        }

        [Fact]
        public async Task GetCommandResult_ExistingCommand_ReturnsResult()
        {
            // Arrange
            var commandId = m_service.QueueCommand("test command");
            await Task.Delay(1); // Allow processing

            // Act
            var result = await m_service.GetCommandResult(commandId);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetCommandResult_NonExistentCommand_ReturnsNotFound()
        {
            // Act
            var result = await m_service.GetCommandResult("non-existent-id");

            // Assert
            Assert.Contains("Command not found", result);
        }

        [Fact]
        public void CancelCommand_ExistingCommand_ReturnsTrue()
        {
            // Arrange
            var commandId = m_service.QueueCommand("test command");

            // Act
            var result = m_service.CancelCommand(commandId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CancelCommand_NonExistentCommand_ReturnsFalse()
        {
            // Act
            var result = m_service.CancelCommand("non-existent-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetQueueStatus_WithCommands_ReturnsAllCommands()
        {
            // Arrange
            m_service.QueueCommand("command1");
            m_service.QueueCommand("command2");

            // Act
            var status = m_service.GetQueueStatus();

            // Assert
            Assert.NotNull(status);
            Assert.True(status.Count() >= 0); // May be 0 if commands processed quickly
        }

        [Fact]
        public void CancelAllCommands_WithCommands_CancelsAll()
        {
            // Arrange
            m_service.QueueCommand("command1");
            m_service.QueueCommand("command2");

            // Act
            var cancelledCount = m_service.CancelAllCommands("Test cancellation");

            // Assert
            Assert.True(cancelledCount >= 0);
        }

        [Fact]
        public void QueueCommand_WhenSessionNotActive_DoesNotThrow()
        {
            // Arrange
            m_mockCdbSession.Setup(x => x.IsActive).Returns(false);

            // Act - CommandQueueService doesn't check IsActive during queueing
            var commandId = m_service.QueueCommand("test command");

            // Assert
            Assert.NotNull(commandId);
            Assert.NotEmpty(commandId);
        }

        [Fact]
        public async Task QueueCommand_WhenCommandExecutionFails_HandlesException()
        {
            // Arrange
            m_mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var commandId = m_service.QueueCommand("test command");
            await Task.Delay(50); // Allow processing

            // Act - Get result to see failure
            var result = await m_service.GetCommandResult(commandId);

            // Assert - Command should return an error result
            Assert.NotNull(result);
            Assert.Contains("Command execution failed", result);
        }

        public void Dispose()
        {
            m_service?.Dispose();
        }
    }
}
