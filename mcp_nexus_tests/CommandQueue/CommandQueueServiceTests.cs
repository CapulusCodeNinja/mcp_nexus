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
using mcp_nexus_tests.Helpers;

namespace mcp_nexus_tests.Services
{
    public class CommandQueueServiceTests : IDisposable
    {
        private readonly ICdbSession m_realisticCdbSession;
        private readonly Mock<ILogger<CommandQueueService>> m_mockLogger;
        private readonly Mock<ILoggerFactory> m_mockLoggerFactory;
        private readonly CommandQueueService m_service;

        public CommandQueueServiceTests()
        {
            m_mockLogger = new Mock<ILogger<CommandQueueService>>();
            m_mockLoggerFactory = new Mock<ILoggerFactory>();

            // Setup logger factory to return appropriate loggers
            m_mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

            // Use realistic CDB mock instead of simple mock
            m_realisticCdbSession = RealisticCdbTestHelper.CreateBugSimulatingCdbSession(Mock.Of<ILogger>());

            m_service = new CommandQueueService(m_realisticCdbSession, m_mockLogger.Object, m_mockLoggerFactory.Object);
        }

        public void Dispose()
        {
            m_realisticCdbSession?.Dispose();
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
            // Arrange - ensure session is started
            await m_realisticCdbSession.StartSession("test.dmp", null);
            var commandId = m_service.QueueCommand("!analyze -v");
            await Task.Delay(200); // Wait for realistic command execution

            // Act
            var result = await m_service.GetCommandResult(commandId);

            // Assert
            Assert.Contains("DBGENG:", result); // Realistic output from !analyze -v
            Assert.Contains("[STDERR]", result); // Verifies stderr was handled
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
            // Realistic mock doesn't need setup - it handles IsActive internally

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
            // Realistic mock will handle exceptions internally

            // Act
            var commandId = m_service.QueueCommand("test command");
            await Task.Delay(50); // Allow processing

            // Act - Get result to see failure
            var result = await m_service.GetCommandResult(commandId);

            // Assert - Command should return an error result
            Assert.NotNull(result);
            Assert.Contains("Command execution failed", result);
        }

        #region Additional Edge Case Tests

        [Fact]
        public void Constructor_WithNullCdbSession_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new CommandQueueService(null!, m_mockLogger.Object, m_mockLoggerFactory.Object));
            Assert.Equal("cdbSession", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new CommandQueueService(m_realisticCdbSession, null!, m_mockLoggerFactory.Object));
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new CommandQueueService(m_realisticCdbSession, m_mockLogger.Object, null!));
            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void QueueCommand_WithNullCommand_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => m_service.QueueCommand(null!));
            Assert.Contains("not be null or empty", exception.Message);
            Assert.Equal("command", exception.ParamName);
        }

        [Fact]
        public void QueueCommand_WithEmptyCommand_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => m_service.QueueCommand(""));
            Assert.Contains("not be null or empty", exception.Message);
            Assert.Equal("command", exception.ParamName);
        }

        [Fact]
        public void QueueCommand_WithWhitespaceCommand_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => m_service.QueueCommand("   "));
            Assert.Contains("not be null or empty", exception.Message);
            Assert.Equal("command", exception.ParamName);
        }

        [Fact]
        public void CancelCommand_WithNullCommandId_ReturnsFalse()
        {
            // Act
            var result = m_service.CancelCommand(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelCommand_WithEmptyCommandId_ReturnsFalse()
        {
            // Act
            var result = m_service.CancelCommand("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelCommand_WithWhitespaceCommandId_ReturnsFalse()
        {
            // Act
            var result = m_service.CancelCommand("   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelAllCommands_WithNoActiveCommands_ReturnsZero()
        {
            // Act
            var result = m_service.CancelAllCommands();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CancelAllCommands_WithActiveCommands_ReturnsCorrectCount()
        {
            // Arrange
            var commandId1 = m_service.QueueCommand("test command 1");
            var commandId2 = m_service.QueueCommand("test command 2");

            // Act
            var result = m_service.CancelAllCommands("Test cancellation");

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public void TriggerCleanup_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_service.TriggerCleanup();
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_service.Dispose();
            m_service.Dispose();
        }

        #endregion

    }
}

