using Xunit;
using Microsoft.Extensions.Logging;
using mcp_nexus.Services;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace mcp_nexus_tests.Services
{
    public class CommandTimeoutServiceTests : IDisposable
    {
        private readonly CommandTimeoutService m_service;
        private readonly ILogger<CommandTimeoutService> m_logger;

        public CommandTimeoutServiceTests()
        {
            m_logger = LoggerFactory.Create(b => { }).CreateLogger<CommandTimeoutService>();
            m_service = new CommandTimeoutService(m_logger);
        }

        public void Dispose()
        {
            m_service?.Dispose();
        }

        [Fact]
        public void StartCommandTimeout_ValidParameters_DoesNotThrow()
        {
            // Arrange
            var commandId = "test-command-1";
            var timeout = TimeSpan.FromMilliseconds(100);

            // Act & Assert
            var exception = Record.Exception(() => 
                m_service.StartCommandTimeout(commandId, timeout, async () => 
                {
                    await Task.CompletedTask;
                }));

            Assert.Null(exception);
            // Note: We can't easily verify the timeout was called without waiting
            // This test just ensures the method doesn't throw
        }

        [Fact]
        public async Task StartCommandTimeout_ShortTimeout_CallsTimeoutHandler()
        {
            // Arrange
            var commandId = "test-command-2";
            var timeout = TimeSpan.FromMilliseconds(50);
            var timeoutCalled = false;
            var timeoutCallTime = DateTime.MinValue;

            // Act
            var startTime = DateTime.UtcNow;
            m_service.StartCommandTimeout(commandId, timeout, async () => 
            {
                timeoutCalled = true;
                timeoutCallTime = DateTime.UtcNow;
                await Task.CompletedTask;
            });

            // Wait slightly longer than timeout
            await Task.Delay(150);

            // Assert
            Assert.True(timeoutCalled, "Timeout handler should have been called");
            var elapsed = timeoutCallTime - startTime;
            Assert.True(elapsed >= timeout, $"Timeout should have occurred after {timeout.TotalMilliseconds}ms, but was {elapsed.TotalMilliseconds}ms");
            Assert.True(elapsed < timeout.Add(TimeSpan.FromMilliseconds(100)), "Timeout should not be significantly delayed");
        }

        [Fact]
        public async Task CancelCommandTimeout_BeforeTimeout_PreventsTimeoutCall()
        {
            // Arrange
            var commandId = "test-command-3";
            var timeout = TimeSpan.FromMilliseconds(100);
            var timeoutCalled = false;

            // Act
            m_service.StartCommandTimeout(commandId, timeout, async () => 
            {
                timeoutCalled = true;
                await Task.CompletedTask;
            });

            // Cancel immediately
            m_service.CancelCommandTimeout(commandId);

            // Wait longer than timeout
            await Task.Delay(200);

            // Assert
            Assert.False(timeoutCalled, "Timeout handler should not have been called after cancellation");
        }

        [Fact]
        public void CancelCommandTimeout_NonExistentCommand_DoesNotThrow()
        {
            // Arrange
            var commandId = "non-existent-command";

            // Act & Assert
            var exception = Record.Exception(() => m_service.CancelCommandTimeout(commandId));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ExtendCommandTimeout_ExistingCommand_PreservesOriginalHandlerAndCallsIt()
        {
            // Arrange
            var commandId = "test-command-4";
            var initialTimeout = TimeSpan.FromMilliseconds(50);
            var extension = TimeSpan.FromMilliseconds(100);
            var originalTimeoutCalled = false;

            // Act
            m_service.StartCommandTimeout(commandId, initialTimeout, async () => 
            {
                originalTimeoutCalled = true;
                await Task.CompletedTask;
            });

            // Wait half the initial timeout, then extend
            await Task.Delay(25);
            m_service.ExtendCommandTimeout(commandId, extension);

            // Wait for original timeout period (should not fire due to extension)
            await Task.Delay(50);
            Assert.False(originalTimeoutCalled, "Original timeout should have been cancelled by extension");

            // Wait for extension period to complete
            await Task.Delay(120);

            // Assert - the original handler should have been called after the extension expires
            Assert.True(originalTimeoutCalled, "Original timeout handler should have been called after extension period");
        }

        [Fact]
        public void ExtendCommandTimeout_NonExistentCommand_DoesNotThrow()
        {
            // Arrange
            var commandId = "non-existent-command";
            var extension = TimeSpan.FromMilliseconds(100);

            // Act & Assert
            var exception = Record.Exception(() => m_service.ExtendCommandTimeout(commandId, extension));
            Assert.Null(exception);
        }

        [Fact]
        public async Task StartCommandTimeout_MultipleCommands_HandlesIndependently()
        {
            // Arrange
            var command1Id = "test-command-5a";
            var command2Id = "test-command-5b";
            var timeout1 = TimeSpan.FromMilliseconds(50);
            var timeout2 = TimeSpan.FromMilliseconds(100);
            var timeout1Called = false;
            var timeout2Called = false;

            // Act
            m_service.StartCommandTimeout(command1Id, timeout1, async () => 
            {
                timeout1Called = true;
                await Task.CompletedTask;
            });

            m_service.StartCommandTimeout(command2Id, timeout2, async () => 
            {
                timeout2Called = true;
                await Task.CompletedTask;
            });

            // Wait for first timeout
            await Task.Delay(75);
            Assert.True(timeout1Called, "First timeout should have been called");
            Assert.False(timeout2Called, "Second timeout should not have been called yet");

            // Wait for second timeout
            await Task.Delay(50);
            Assert.True(timeout2Called, "Second timeout should have been called");
        }

        [Fact]
        public async Task StartCommandTimeout_ExceptionInHandler_DoesNotCrashService()
        {
            // Arrange
            var commandId = "test-command-6";
            var timeout = TimeSpan.FromMilliseconds(50);
            var handlerCalled = false;

            // Act
            m_service.StartCommandTimeout(commandId, timeout, async () => 
            {
                handlerCalled = true;
                await Task.CompletedTask;
                throw new InvalidOperationException("Test exception in timeout handler");
            });

            // Wait for timeout
            await Task.Delay(100);

            // Assert - handler should have been called despite exception
            Assert.True(handlerCalled, "Timeout handler should have been called even if it throws");

            // Service should still be functional
            var anotherCommandId = "test-command-7";
            
            var exception = Record.Exception(() => 
                m_service.StartCommandTimeout(anotherCommandId, TimeSpan.FromMilliseconds(50), async () => 
                {
                    await Task.CompletedTask;
                }));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Dispose_CancelsAllTimeouts()
        {
            // Arrange
            var commandId = "test-command-8";
            var timeout = TimeSpan.FromMilliseconds(50);
            var timeoutCalled = false;

            m_service.StartCommandTimeout(commandId, timeout, async () => 
            {
                timeoutCalled = true;
                await Task.CompletedTask;
            });

            // Act
            m_service.Dispose();

            // Wait longer than timeout
            await Task.Delay(100);

            // Assert
            Assert.False(timeoutCalled, "Timeout should not have been called after disposal");
        }

        [Fact]
        public async Task StartCommandTimeout_AfterDispose_DoesNotStartTimeout()
        {
            // Arrange
            m_service.Dispose();
            var commandId = "test-command-9";
            var timeout = TimeSpan.FromMilliseconds(50);
            var timeoutCalled = false;

            // Act
            m_service.StartCommandTimeout(commandId, timeout, async () => 
            {
                timeoutCalled = true;
                await Task.CompletedTask;
            });

            // Wait longer than timeout
            await Task.Delay(100);

            // Assert
            Assert.False(timeoutCalled, "Timeout should not have been called after service disposal");
        }

        [Fact]
        public async Task StartCommandTimeout_DuplicateCommandId_ReplacesExisting()
        {
            // Arrange
            var commandId = "duplicate-command";
            var firstTimeout = TimeSpan.FromMilliseconds(50);
            var secondTimeout = TimeSpan.FromMilliseconds(100);
            var firstTimeoutCalled = false;
            var secondTimeoutCalled = false;

            // Act
            m_service.StartCommandTimeout(commandId, firstTimeout, async () => 
            {
                firstTimeoutCalled = true;
                await Task.CompletedTask;
            });

            // Wait a bit, then start another timeout with same ID (should replace the first)
            await Task.Delay(25);
            m_service.StartCommandTimeout(commandId, secondTimeout, async () => 
            {
                secondTimeoutCalled = true;
                await Task.CompletedTask;
            });

            // Wait for original first timeout period
            await Task.Delay(50);
            
            // First timeout should have been cancelled and not called
            Assert.False(firstTimeoutCalled, "First timeout should have been cancelled and not called");
            
            // Wait for second timeout
            await Task.Delay(100);
            
            // Second timeout should have been called
            Assert.True(secondTimeoutCalled, "Second timeout should have been called");
        }
    }
}
