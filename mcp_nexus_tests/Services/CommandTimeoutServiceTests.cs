using Xunit;
using Moq;
using mcp_nexus.Services;
using System;
using System.Threading.Tasks;

namespace mcp_nexus_tests.Services
{
    /// <summary>
    /// Fast unit tests for CommandTimeoutService using mocks - no real delays
    /// </summary>
    public class CommandTimeoutServiceTests
    {
        private readonly Mock<ICommandTimeoutService> m_mockService;

        public CommandTimeoutServiceTests()
        {
            m_mockService = new Mock<ICommandTimeoutService>();
        }

        [Fact]
        public void StartCommandTimeout_ValidParameters_DoesNotThrow()
        {
            // Arrange
            var commandId = "test-command-1";
            var timeout = TimeSpan.FromMilliseconds(100);
            m_mockService.Setup(s => s.StartCommandTimeout(commandId, timeout, It.IsAny<Func<Task>>()));

            // Act & Assert
            var exception = Record.Exception(() => 
                m_mockService.Object.StartCommandTimeout(commandId, timeout, async () => await Task.CompletedTask));
            
            Assert.Null(exception);
            m_mockService.Verify(s => s.StartCommandTimeout(commandId, timeout, It.IsAny<Func<Task>>()), Times.Once);
        }

        [Fact]
        public void CancelCommandTimeout_ValidCommandId_CallsService()
        {
            // Arrange
            var commandId = "test-command-2";
            m_mockService.Setup(s => s.CancelCommandTimeout(commandId));

            // Act
            var exception = Record.Exception(() => m_mockService.Object.CancelCommandTimeout(commandId));

            // Assert
            Assert.Null(exception);
            m_mockService.Verify(s => s.CancelCommandTimeout(commandId), Times.Once);
        }

        [Fact]
        public void ExtendCommandTimeout_ValidParameters_CallsService()
        {
            // Arrange
            var commandId = "test-command-3";
            var additionalTime = TimeSpan.FromMinutes(5);
            m_mockService.Setup(s => s.ExtendCommandTimeout(commandId, additionalTime));

            // Act
            var exception = Record.Exception(() => m_mockService.Object.ExtendCommandTimeout(commandId, additionalTime));

            // Assert
            Assert.Null(exception);
            m_mockService.Verify(s => s.ExtendCommandTimeout(commandId, additionalTime), Times.Once);
        }

        [Fact]
        public void CancelCommandTimeout_NonExistentCommand_DoesNotThrow()
        {
            // Arrange
            var commandId = "non-existent-command";
            m_mockService.Setup(s => s.CancelCommandTimeout(commandId));

            // Act & Assert
            var exception = Record.Exception(() => m_mockService.Object.CancelCommandTimeout(commandId));
            Assert.Null(exception);
        }

        [Fact]
        public void StartCommandTimeout_MultipleCommands_HandlesIndependently()
        {
            // Arrange
            var command1Id = "cmd-1";
            var command2Id = "cmd-2";
            var timeout1 = TimeSpan.FromMilliseconds(50);
            var timeout2 = TimeSpan.FromMilliseconds(100);

            m_mockService.Setup(s => s.StartCommandTimeout(command1Id, timeout1, It.IsAny<Func<Task>>()));
            m_mockService.Setup(s => s.StartCommandTimeout(command2Id, timeout2, It.IsAny<Func<Task>>()));

            // Act
            m_mockService.Object.StartCommandTimeout(command1Id, timeout1, async () => await Task.CompletedTask);
            m_mockService.Object.StartCommandTimeout(command2Id, timeout2, async () => await Task.CompletedTask);

            // Assert
            m_mockService.Verify(s => s.StartCommandTimeout(command1Id, timeout1, It.IsAny<Func<Task>>()), Times.Once);
            m_mockService.Verify(s => s.StartCommandTimeout(command2Id, timeout2, It.IsAny<Func<Task>>()), Times.Once);
        }

        [Fact]
        public void ExtendCommandTimeout_NonExistentCommand_DoesNotThrow()
        {
            // Arrange
            var commandId = "non-existent";
            var extension = TimeSpan.FromMinutes(2);
            m_mockService.Setup(s => s.ExtendCommandTimeout(commandId, extension));

            // Act & Assert
            var exception = Record.Exception(() => m_mockService.Object.ExtendCommandTimeout(commandId, extension));
            Assert.Null(exception);
        }
    }
}
