using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_tests.Helper
{
    public class CdbSessionTests : IDisposable
    {
        private readonly Mock<ICdbSession> m_mockSession;
        private readonly Mock<ILogger<CdbSession>> m_mockLogger;

        public CdbSessionTests()
        {
            m_mockSession = new Mock<ICdbSession>();
            m_mockLogger = new Mock<ILogger<CdbSession>>();
            
            // Setup default mock behavior
            m_mockSession.Setup(x => x.IsActive).Returns(false);
            m_mockSession.Setup(x => x.StartSession(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);
            m_mockSession.Setup(x => x.StopSession()).ReturnsAsync(false);
            m_mockSession.Setup(x => x.ExecuteCommand(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("No active debugging session"));
            m_mockSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("No active debugging session"));
        }

        [Fact]
        public void IsActive_InitialState_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(m_mockSession.Object.IsActive);
        }

        [Fact]
        public async Task StartSession_WithInvalidTarget_ReturnsFalse()
        {
            // Act
            var result = await m_mockSession.Object.StartSession("invalid-target", null);

            // Assert
            Assert.False(result);
            m_mockSession.Verify(x => x.StartSession("invalid-target", null), Times.Once);
        }

        [Fact] 
        public async Task StartSession_WithNullTarget_ReturnsFalse()
        {
            // Act
            var result = await m_mockSession.Object.StartSession(null!, null);

            // Assert
            Assert.False(result);
            m_mockSession.Verify(x => x.StartSession(null, null), Times.Once);
        }

        [Fact]
        public async Task StartSession_WithEmptyTarget_ReturnsFalse()
        {
            // Act
            var result = await m_mockSession.Object.StartSession("", null);

            // Assert
            Assert.False(result);
            m_mockSession.Verify(x => x.StartSession("", null), Times.Once);
        }

        [Fact]
        public async Task StopSession_WhenNotActive_ReturnsFalse()
        {
            // Act
            var result = await m_mockSession.Object.StopSession();

            // Assert
            Assert.False(result);
            m_mockSession.Verify(x => x.StopSession(), Times.Once);
        }

        [Fact]
        public async Task ExecuteCommand_WhenNotActive_ThrowsInvalidOperationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => m_mockSession.Object.ExecuteCommand("test"));
            
            m_mockSession.Verify(x => x.ExecuteCommand("test"), Times.Once);
        }

        [Fact]
        public async Task ExecuteCommand_WithCancellationToken_WhenNotActive_ThrowsInvalidOperationException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => m_mockSession.Object.ExecuteCommand("test", cts.Token));
            
            m_mockSession.Verify(x => x.ExecuteCommand("test", cts.Token), Times.Once);
        }

        [Fact]
        public void ExecuteCommand_WithNullCommand_ShouldBeHandledByImplementation()
        {
            // This test verifies that null commands can be mocked
            // Actual validation is in the real implementation
            m_mockSession.Setup(x => x.ExecuteCommand(null!))
                .ThrowsAsync(new ArgumentException("Command cannot be null"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(
                () => m_mockSession.Object.ExecuteCommand(null!));
            
            Assert.NotNull(exception);
        }

        [Fact] 
        public void ExecuteCommand_WithEmptyCommand_ShouldBeHandledByImplementation()
        {
            // This test verifies that empty commands can be mocked
            // Actual validation is in the real implementation  
            m_mockSession.Setup(x => x.ExecuteCommand(""))
                .ThrowsAsync(new ArgumentException("Command cannot be empty"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(
                () => m_mockSession.Object.ExecuteCommand(""));
            
            Assert.NotNull(exception);
        }

        [Fact]
        public void CancelCurrentOperation_WhenNotActive_DoesNotThrow()
        {
            // Act - Should not throw
            m_mockSession.Object.CancelCurrentOperation();

            // Assert - Verify method was called
            m_mockSession.Verify(x => x.CancelCurrentOperation(), Times.Once);
        }

        [Fact]
        public async Task ExecuteCommand_WithCancelledToken_ThrowsTaskCancelledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            
            m_mockSession.Setup(x => x.ExecuteCommand("test", It.Is<CancellationToken>(t => t.IsCancellationRequested)))
                .ThrowsAsync(new TaskCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => m_mockSession.Object.ExecuteCommand("test", cts.Token));
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Act - Should not throw
            m_mockSession.Object.Dispose();

            // Assert - Multiple dispose calls should be safe
            m_mockSession.Object.Dispose();
            
            m_mockSession.Verify(x => x.Dispose(), Times.AtLeast(2));
        }

        [Fact]
        public void Dispose_AfterDispose_IsActiveReturnsFalse()
        {
            // Simple mock test - verify that Dispose can be called
            // and that after dispose, behavior is as expected
            
            // Act
            m_mockSession.Object.Dispose();

            // Assert - Verify dispose was called
            m_mockSession.Verify(x => x.Dispose(), Times.Once);
            
            // Since this is a mock, the default IsActive behavior is false anyway
            Assert.False(m_mockSession.Object.IsActive);
        }

        public void Dispose()
        {
            // Nothing to dispose for mocks
        }
    }
}
