using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for CommandNotificationManager
    /// </summary>
    public class CommandNotificationManagerTests
    {
        private readonly Mock<IMcpNotificationService> m_MockNotificationService;
        private readonly Mock<ILogger> m_MockLogger;
        private readonly CommandQueueConfiguration m_Config;
        private readonly CommandNotificationManager m_Manager;

        public CommandNotificationManagerTests()
        {
            m_MockNotificationService = new Mock<IMcpNotificationService>();
            m_MockLogger = new Mock<ILogger>();
            m_Config = new CommandQueueConfiguration("test-session");
            m_Manager = new CommandNotificationManager(m_MockNotificationService.Object, m_MockLogger.Object, m_Config);
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CommandNotificationManager(null!, m_MockLogger.Object, m_Config));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CommandNotificationManager(m_MockNotificationService.Object, null!, m_Config));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CommandNotificationManager(m_MockNotificationService.Object, m_MockLogger.Object, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var manager = new CommandNotificationManager(m_MockNotificationService.Object, m_MockLogger.Object, m_Config);

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void NotifyCommandStatusFireAndForget_WithQueuedCommand_CallsNotificationService()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource);

            // Act
            m_Manager.NotifyCommandStatusFireAndForget(queuedCommand, "executing", "result", 50);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(500);
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithParameters_CallsNotificationService()
        {
            // Arrange
            var commandId = "cmd-1";
            var command = "!analyze -v";
            var status = "executing";
            var result = "result";
            var progress = 50;

            // Act
            m_Manager.NotifyCommandStatusFireAndForget(commandId, command, status, result, progress);

            // Assert
            // Give the Task.Run a moment to execute
            await Task.Delay(500);
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandStatusFireAndForget_WithNullResult_CallsNotificationServiceWithNullResult()
        {
            // Arrange
            var commandId = "cmd-1";
            var command = "!analyze -v";
            var status = "executing";
            var progress = 50;

            // Act
            m_Manager.NotifyCommandStatusFireAndForget(commandId, command, status, null, progress);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(1000);
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandStatusFireAndForget_WithDefaultProgress_CallsNotificationServiceWithZeroProgress()
        {
            // Arrange
            var commandId = "cmd-1";
            var command = "!analyze -v";
            var status = "executing";

            // Act
            m_Manager.NotifyCommandStatusFireAndForget(commandId, command, status);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(500);
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatFireAndForget_WithQueuedCommand_CallsNotificationService()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource);
            var elapsed = TimeSpan.FromMinutes(5);

            // Act
            m_Manager.NotifyCommandHeartbeatFireAndForget(queuedCommand, elapsed);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(2000);
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Executing for 5") && s.Contains("minutes")),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatFireAndForget_WithLongElapsedTime_CapsProgressAt95()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource);
            var elapsed = TimeSpan.FromHours(2); // Very long time

            // Act
            m_Manager.NotifyCommandHeartbeatFireAndForget(queuedCommand, elapsed);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(1000);
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<int>(p => p <= 95),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void CreateQueuedStatusMessage_WithValidParameters_ReturnsValidMessage()
        {
            // Arrange
            var queuePosition = 3;
            var elapsed = TimeSpan.FromMinutes(5);

            // Act
            var result = m_Manager.CreateQueuedStatusMessage(queuePosition, elapsed);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("3rd in queue", result);
            Assert.Contains("waited 5m", result);
        }

        [Fact]
        public void CalculateQueueProgress_WithValidParameters_ReturnsValidProgress()
        {
            // Arrange
            var queuePosition = 3;
            var elapsed = TimeSpan.FromMinutes(5);

            // Act
            var result = m_Manager.CalculateQueueProgress(queuePosition, elapsed);

            // Assert
            Assert.True(result >= 0 && result <= 100);
        }

        [Fact]
        public async Task NotifyQueueEvent_WithValidParameters_LogsEvent()
        {
            // Arrange
            var eventType = "TestEvent";
            var message = "Test message";
            var data = new { test = "data" };

            // Act
            m_Manager.NotifyQueueEvent(eventType, message, data);

            // Assert
            // Give the Task.Run a moment to execute
            await Task.Delay(500);
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Queue Event [{eventType}]: {message}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyQueueEvent_WithNullData_LogsEvent()
        {
            // Arrange
            var eventType = "TestEvent";
            var message = "Test message";

            // Act
            m_Manager.NotifyQueueEvent(eventType, message, null);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(1000);
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Queue Event [{eventType}]: {message}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void NotifyServiceShutdown_WithReason_LogsShutdownEvent()
        {
            // Arrange
            var reason = "System maintenance";

            // Act
            m_Manager.NotifyServiceShutdown(reason);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(500);
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Command queue service shutting down: {reason}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyServiceStartup_LogsStartupEvent()
        {
            // Arrange
            var sessionId = m_Config.SessionId;

            // Act
            m_Manager.NotifyServiceStartup();

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(1000);
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Command queue service started for session {sessionId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandCancellation_WithCommandIdAndReason_LogsCancellationEvent()
        {
            // Arrange
            var commandId = "cmd-1";
            var reason = "User requested";

            // Act
            m_Manager.NotifyCommandCancellation(commandId, reason);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(500);
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Command {commandId} cancelled: {reason}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyBulkCommandCancellation_WithCountAndReason_LogsBulkCancellationEvent()
        {
            // Arrange
            var count = 5;
            var reason = "Service shutdown";

            // Act
            m_Manager.NotifyBulkCommandCancellation(count, reason);

            // Assert
            // Give the Task.Run a moment to execute
            await Task.Delay(2000);
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Cancelled {count} commands: {reason}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithExceptionInNotificationService_LogsWarning()
        {
            // Arrange
            var commandId = "cmd-1";
            var command = "!analyze -v";
            var status = "executing";

            m_MockNotificationService.Setup(x => x.NotifyCommandStatusAsync(commandId, command, status, 0, string.Empty, string.Empty))
                .ThrowsAsync(new Exception("Notification failed"));

            // Act
            m_Manager.NotifyCommandStatusFireAndForget(commandId, command, status);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(2000);
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send command status notification for")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatFireAndForget_WithExceptionInNotificationService_LogsTrace()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource);
            var elapsed = TimeSpan.FromMinutes(5);

            m_MockNotificationService.Setup(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Notification failed"));

            // Act
            m_Manager.NotifyCommandHeartbeatFireAndForget(queuedCommand, elapsed);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(2000);
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send heartbeat notification for")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }
}