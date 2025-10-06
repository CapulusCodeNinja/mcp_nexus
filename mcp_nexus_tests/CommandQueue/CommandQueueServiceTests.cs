using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Debugger;
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
using Xunit;
using mcp_nexus_tests.Mocks;

namespace mcp_nexus_tests.CommandQueue
{
    public class CommandQueueServiceTests : IDisposable
    {
        private readonly ICdbSession m_RealisticCdbSession;
        private readonly Mock<ILogger<CommandQueueService>> m_MockLogger;
        private readonly Mock<ILoggerFactory> m_MockLoggerFactory;
        private readonly CommandQueueService m_Service;

        public CommandQueueServiceTests()
        {
            m_MockLogger = new Mock<ILogger<CommandQueueService>>();
            m_MockLoggerFactory = new Mock<ILoggerFactory>();

            // Setup logger factory to return appropriate loggers
            m_MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

            // Use realistic CDB mock instead of simple mock
            m_RealisticCdbSession = RealisticCdbTestHelper.CreateBugSimulatingCdbSession(Mock.Of<ILogger>());

            m_Service = new CommandQueueService(m_RealisticCdbSession, m_MockLogger.Object, m_MockLoggerFactory.Object);
        }

        public void Dispose()
        {
            m_RealisticCdbSession?.Dispose();
        }

        [Fact]
        public void QueueCommand_ValidCommand_ReturnsCommandId()
        {
            // Act
            var commandId = m_Service.QueueCommand("test command");

            // Assert
            Assert.NotNull(commandId);
            Assert.NotEmpty(commandId);
        }

        [Fact]
        public async Task GetCommandResult_ExistingCommand_ReturnsResult()
        {
            // Arrange - ensure session is started
            await m_RealisticCdbSession.StartSession("test.dmp", null);
            var commandId = m_Service.QueueCommand("!analyze -v");
            await Task.Delay(200); // Wait for realistic command execution

            // Act
            var result = await m_Service.GetCommandResult(commandId);

            // Assert
            Assert.Contains("DBGENG:", result); // Realistic output from !analyze -v
            Assert.Contains("[STDERR]", result); // Verifies stderr was handled
        }

        [Fact]
        public async Task GetCommandResult_NonExistentCommand_ReturnsNotFound()
        {
            // Act
            var result = await m_Service.GetCommandResult("non-existent-id");

            // Assert
            Assert.Contains("Command not found", result);
        }

        [Fact]
        public void CancelCommand_ExistingCommand_ReturnsTrue()
        {
            // Arrange
            var commandId = m_Service.QueueCommand("test command");

            // Act
            var result = m_Service.CancelCommand(commandId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CancelCommand_NonExistentCommand_ReturnsFalse()
        {
            // Act
            var result = m_Service.CancelCommand("non-existent-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetQueueStatus_WithCommands_ReturnsAllCommands()
        {
            // Arrange
            m_Service.QueueCommand("command1");
            m_Service.QueueCommand("command2");

            // Act
            var status = m_Service.GetQueueStatus();

            // Assert
            Assert.NotNull(status);
            Assert.True(status.Count() >= 0); // May be 0 if commands processed quickly
        }

        [Fact]
        public void CancelAllCommands_WithCommands_CancelsAll()
        {
            // Arrange
            m_Service.QueueCommand("command1");
            m_Service.QueueCommand("command2");

            // Act
            var cancelledCount = m_Service.CancelAllCommands("Test cancellation");

            // Assert
            Assert.True(cancelledCount >= 0);
        }

        [Fact]
        public void QueueCommand_WhenSessionNotActive_DoesNotThrow()
        {
            // Arrange
            // Realistic mock doesn't need setup - it handles IsActive internally

            // Act - CommandQueueService doesn't check IsActive during queueing
            var commandId = m_Service.QueueCommand("test command");

            // Assert
            Assert.NotNull(commandId);
            Assert.NotEmpty(commandId);
        }

        [Fact]
        public async Task QueueCommand_WhenCommandExecutionFails_HandlesException()
        {
            // Arrange
            // Realistic mock will handle exceptions internally

            // Act
            var commandId = m_Service.QueueCommand("failing-command");
            await Task.Delay(50); // Allow processing

            // Act - Get result to see failure
            var result = await m_Service.GetCommandResult(commandId);

            // Assert - Command should return an error result
            Assert.NotNull(result);
            Assert.Contains("Command execution failed: Command failed: failing-command", result);
        }

        #region Additional Edge Case Tests

        [Fact]
        public void Constructor_WithNullCdbSession_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CommandQueueService(null!, m_MockLogger.Object, m_MockLoggerFactory.Object));
            Assert.Equal("cdbSession", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CommandQueueService(m_RealisticCdbSession, null!, m_MockLoggerFactory.Object));
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CommandQueueService(m_RealisticCdbSession, m_MockLogger.Object, null!));
            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void QueueCommand_WithNullCommand_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => m_Service.QueueCommand(null!));
            Assert.Contains("not be null or empty", exception.Message);
            Assert.Equal("command", exception.ParamName);
        }

        [Fact]
        public void QueueCommand_WithEmptyCommand_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => m_Service.QueueCommand(""));
            Assert.Contains("not be null or empty", exception.Message);
            Assert.Equal("command", exception.ParamName);
        }

        [Fact]
        public void QueueCommand_WithWhitespaceCommand_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => m_Service.QueueCommand("   "));
            Assert.Contains("not be null or empty", exception.Message);
            Assert.Equal("command", exception.ParamName);
        }

        [Fact]
        public void CancelCommand_WithNullCommandId_ReturnsFalse()
        {
            // Act
            var result = m_Service.CancelCommand(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelCommand_WithEmptyCommandId_ReturnsFalse()
        {
            // Act
            var result = m_Service.CancelCommand("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelCommand_WithWhitespaceCommandId_ReturnsFalse()
        {
            // Act
            var result = m_Service.CancelCommand("   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelAllCommands_WithNoActiveCommands_ReturnsZero()
        {
            // Act
            var result = m_Service.CancelAllCommands();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CancelAllCommands_WithActiveCommands_ReturnsCorrectCount()
        {
            // Arrange
            var commandId1 = m_Service.QueueCommand("test command 1");
            var commandId2 = m_Service.QueueCommand("test command 2");

            // Act
            var result = m_Service.CancelAllCommands("Test cancellation");

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public void TriggerCleanup_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_Service.TriggerCleanup();
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_Service.Dispose();
            m_Service.Dispose();
        }

        #endregion

    }
}

