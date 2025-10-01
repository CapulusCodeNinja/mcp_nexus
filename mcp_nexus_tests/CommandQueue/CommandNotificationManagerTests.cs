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
        private readonly Mock<IMcpNotificationService> _mockNotificationService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandQueueConfiguration _config;
        private readonly CommandNotificationManager _manager;

        public CommandNotificationManagerTests()
        {
            _mockNotificationService = new Mock<IMcpNotificationService>();
            _mockLogger = new Mock<ILogger>();
            _config = new CommandQueueConfiguration("test-session");
            _manager = new CommandNotificationManager(_mockNotificationService.Object, _mockLogger.Object, _config);
        }

        [Fact]
        public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new CommandNotificationManager(null!, _mockLogger.Object, _config));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new CommandNotificationManager(_mockNotificationService.Object, null!, _config));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new CommandNotificationManager(_mockNotificationService.Object, _mockLogger.Object, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var manager = new CommandNotificationManager(_mockNotificationService.Object, _mockLogger.Object, _config);

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
            _manager.NotifyCommandStatusFireAndForget(queuedCommand, "executing", "result", 50);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandStatusFireAndForget_WithParameters_CallsNotificationService()
        {
            // Arrange
            var commandId = "cmd-1";
            var command = "!analyze -v";
            var status = "executing";
            var result = "result";
            var progress = 50;

            // Act
            _manager.NotifyCommandStatusFireAndForget(commandId, command, status, result, progress);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
            _manager.NotifyCommandStatusFireAndForget(commandId, command, status, null, progress);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandStatusFireAndForget_WithDefaultProgress_CallsNotificationServiceWithZeroProgress()
        {
            // Arrange
            var commandId = "cmd-1";
            var command = "!analyze -v";
            var status = "executing";

            // Act
            _manager.NotifyCommandStatusFireAndForget(commandId, command, status);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandHeartbeatFireAndForget_WithQueuedCommand_CallsNotificationService()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource);
            var elapsed = TimeSpan.FromMinutes(5);

            // Act
            _manager.NotifyCommandHeartbeatFireAndForget(queuedCommand, elapsed);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.Is<string>(s => s.Contains("Executing for 5.0 minutes")), 
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandHeartbeatFireAndForget_WithLongElapsedTime_CapsProgressAt95()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource);
            var elapsed = TimeSpan.FromHours(2); // Very long time

            // Act
            _manager.NotifyCommandHeartbeatFireAndForget(queuedCommand, elapsed);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.Is<int?>(p => p <= 95),
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
            var result = _manager.CreateQueuedStatusMessage(queuePosition, elapsed);

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
            var result = _manager.CalculateQueueProgress(queuePosition, elapsed);

            // Assert
            Assert.True(result >= 0 && result <= 100);
        }

        [Fact]
        public void NotifyQueueEvent_WithValidParameters_LogsEvent()
        {
            // Arrange
            var eventType = "TestEvent";
            var message = "Test message";
            var data = new { test = "data" };

            // Act
            _manager.NotifyQueueEvent(eventType, message, data);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Queue Event [{eventType}]: {message}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void NotifyQueueEvent_WithNullData_LogsEvent()
        {
            // Arrange
            var eventType = "TestEvent";
            var message = "Test message";

            // Act
            _manager.NotifyQueueEvent(eventType, message, null);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockLogger.Verify(x => x.Log(
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
            _manager.NotifyServiceShutdown(reason);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Command queue service shutting down: {reason}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void NotifyServiceStartup_LogsStartupEvent()
        {
            // Arrange
            var sessionId = _config.SessionId;

            // Act
            _manager.NotifyServiceStartup();

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockLogger.Verify(x => x.Log(
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
            _manager.NotifyCommandCancellation(commandId, reason);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Command {commandId} cancelled: {reason}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void NotifyBulkCommandCancellation_WithCountAndReason_LogsBulkCancellationEvent()
        {
            // Arrange
            var count = 5;
            var reason = "Service shutdown";

            // Act
            _manager.NotifyBulkCommandCancellation(count, reason);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Cancelled {count} commands: {reason}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandStatusFireAndForget_WithExceptionInNotificationService_LogsWarning()
        {
            // Arrange
            var commandId = "cmd-1";
            var command = "!analyze -v";
            var status = "executing";
            
            _mockNotificationService.Setup(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Notification failed"));

            // Act
            _manager.NotifyCommandStatusFireAndForget(commandId, command, status);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to send command status notification for {commandId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void NotifyCommandHeartbeatFireAndForget_WithExceptionInNotificationService_LogsTrace()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource);
            var elapsed = TimeSpan.FromMinutes(5);
            
            _mockNotificationService.Setup(x => x.NotifyCommandStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Notification failed"));

            // Act
            _manager.NotifyCommandHeartbeatFireAndForget(queuedCommand, elapsed);

            // Assert
            // Give the Task.Run a moment to execute
            Thread.Sleep(100);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to send heartbeat notification for {queuedCommand.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }
}