using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using mcp_nexus.Tools;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Exceptions;
using System.Text.Json;

namespace mcp_nexus_tests.Tools
{
    public class McpNexusToolsTests
    {
        private readonly IServiceProvider m_ServiceProvider;
        private readonly Mock<ILogger> m_MockLogger;
        private readonly Mock<ISessionManager> m_MockSessionManager;

        public McpNexusToolsTests()
        {
            m_MockLogger = new Mock<ILogger>();
            m_MockSessionManager = new Mock<ISessionManager>();

            // Create a real service provider with mocked services
            var services = new ServiceCollection();
            services.AddSingleton(m_MockSessionManager.Object);

            // Add logging services
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

            // Use NullLogger to avoid the internal Program class issue
            services.AddSingleton<ILogger<mcp_nexus.Program>>(NullLogger<mcp_nexus.Program>.Instance);

            m_ServiceProvider = services.BuildServiceProvider();
        }

        #region nexus_open_dump_analyze_session Tests

        [Fact]
        public async Task nexus_open_dump_analyze_session_WithValidParameters_ReturnsSuccessResponse()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var dumpPath = Path.Combine(tempDir, "test_dump.dmp");
            var symbolsPath = @"C:\test\symbols";
            var sessionId = "test-session-123";

            // Create a temporary dump file
            File.WriteAllText(dumpPath, "fake dump content");

            try
            {
                var sessionContext = new SessionContext
                {
                    SessionId = sessionId,
                    DumpPath = dumpPath,
                    Description = "Test session"
                };

                m_MockSessionManager
                    .Setup(x => x.CreateSessionAsync(dumpPath, symbolsPath, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sessionId);

                // Act
                var result = await McpNexusTools.nexus_open_dump_analyze_session(
                    m_ServiceProvider, dumpPath, symbolsPath);

                // Assert
                Assert.NotNull(result);
                var json = JsonSerializer.Serialize(result);
                var document = JsonDocument.Parse(json);

                Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
                Assert.Equal(sessionId, sessionIdElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
                Assert.True(successElement.GetBoolean());

                Assert.True(document.RootElement.TryGetProperty("operation", out var operationElement));
                Assert.Equal("nexus_open_dump_analyze_session", operationElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("dumpFile", out var dumpFileElement));
                Assert.Equal("test_dump.dmp", dumpFileElement.GetString());

                // Verify service calls
                m_MockSessionManager.Verify(x => x.CreateSessionAsync(dumpPath, symbolsPath, It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(dumpPath))
                    File.Delete(dumpPath);
            }
        }

        [Fact]
        public async Task nexus_open_dump_analyze_session_WithNullSymbolsPath_CallsCreateSessionWithNull()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var dumpPath = Path.Combine(tempDir, "test_dump.dmp");
            var sessionId = "test-session-123";

            // Create a temporary dump file
            File.WriteAllText(dumpPath, "fake dump content");

            try
            {
                m_MockSessionManager
                    .Setup(x => x.CreateSessionAsync(dumpPath, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sessionId);

                m_MockSessionManager
                    .Setup(x => x.GetSessionContext(sessionId))
                    .Returns(new SessionContext { SessionId = sessionId });

                // Act
                await McpNexusTools.nexus_open_dump_analyze_session(m_ServiceProvider, dumpPath);

                // Assert
                m_MockSessionManager.Verify(x => x.CreateSessionAsync(dumpPath, null, It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(dumpPath))
                    File.Delete(dumpPath);
            }
        }

        [Fact]
        public async Task nexus_open_dump_analyze_session_WithSessionLimitExceeded_ReturnsErrorResponse()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var dumpPath = Path.Combine(tempDir, "test_dump.dmp");
            var exception = new SessionLimitExceededException(5, 3);

            // Create a temporary dump file
            File.WriteAllText(dumpPath, "fake dump content");

            try
            {
                m_MockSessionManager
                    .Setup(x => x.CreateSessionAsync(dumpPath, null, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);

                // Act
                var result = await McpNexusTools.nexus_open_dump_analyze_session(
                    m_ServiceProvider, dumpPath);

                // Assert
                Assert.NotNull(result);
                var json = JsonSerializer.Serialize(result);
                var document = JsonDocument.Parse(json);

                Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
                Assert.False(successElement.GetBoolean());

                Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
                Assert.Contains("Maximum concurrent sessions exceeded", messageElement.GetString());
                Assert.Contains("5/3", messageElement.GetString());
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(dumpPath))
                    File.Delete(dumpPath);
            }
        }

        [Fact]
        public async Task nexus_open_dump_analyze_session_WithGeneralException_ReturnsErrorResponse()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var dumpPath = Path.Combine(tempDir, "test_dump.dmp");
            var exception = new InvalidOperationException("Test error");

            // Create a temporary dump file
            File.WriteAllText(dumpPath, "fake dump content");

            try
            {
                m_MockSessionManager
                    .Setup(x => x.CreateSessionAsync(dumpPath, null, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);

                // Act
                var result = await McpNexusTools.nexus_open_dump_analyze_session(
                    m_ServiceProvider, dumpPath);

                // Assert
                Assert.NotNull(result);
                var json = JsonSerializer.Serialize(result);
                var document = JsonDocument.Parse(json);

                Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
                Assert.False(successElement.GetBoolean());

                Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
                Assert.Contains("Failed to create debugging session", messageElement.GetString());
                Assert.Contains("Test error", messageElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("dumpFile", out var dumpFileElement));
                Assert.Equal("test_dump.dmp", dumpFileElement.GetString());
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(dumpPath))
                    File.Delete(dumpPath);
            }
        }

        #endregion

        #region nexus_close_dump_analyze_session Tests

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithValidSessionId_ReturnsSuccessResponse()
        {
            // Arrange
            var sessionId = "test-session-123";

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.CloseSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await McpNexusTools.nexus_close_dump_analyze_session(
                m_ServiceProvider, sessionId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.True(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("operation", out var operationElement));
            Assert.Equal("nexus_close_dump_analyze_session", operationElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("closed successfully", messageElement.GetString());

            // Verify service calls
            m_MockSessionManager.Verify(x => x.SessionExists(sessionId), Times.Once);
            m_MockSessionManager.Verify(x => x.CloseSessionAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithNonExistentSession_ReturnsNotFoundResponse()
        {
            // Arrange
            var sessionId = "non-existent-session";

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(false);

            // Act
            var result = await McpNexusTools.nexus_close_dump_analyze_session(
                m_ServiceProvider, sessionId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.False(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("not found", messageElement.GetString());

            // Verify that CloseSessionAsync was not called
            m_MockSessionManager.Verify(x => x.CloseSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithException_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var exception = new InvalidOperationException("Test error");

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Throws(exception);

            // Act
            var result = await McpNexusTools.nexus_close_dump_analyze_session(
                m_ServiceProvider, sessionId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.False(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("Failed to close session", messageElement.GetString());
            Assert.Contains("Test error", messageElement.GetString());
        }

        #endregion

        #region nexus_enqueue_async_dump_analyze_command Tests

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithValidParameters_ReturnsSuccessResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var command = "!analyze -v";
            var commandId = "cmd-123";

            var mockCommandQueue = new Mock<ICommandQueueService>();
            mockCommandQueue
                .Setup(x => x.QueueCommand(command))
                .Returns(commandId);

            var sessionContext = new SessionContext
            {
                SessionId = sessionId,
                DumpPath = @"C:\test\dump.dmp"
            };

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.GetSessionContext(sessionId))
                .Returns(sessionContext);

            ICommandQueueService? outQueue = mockCommandQueue.Object;
            m_MockSessionManager
                .Setup(x => x.TryGetCommandQueue(sessionId, out outQueue))
                .Returns(true);

            // Act
            var result = await McpNexusTools.nexus_enqueue_async_dump_analyze_command(
                m_ServiceProvider, sessionId, command);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("commandId", out var commandIdElement));
            Assert.Equal(commandId, commandIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("command", out var commandElement));
            Assert.Equal(command, commandElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.True(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("operation", out var operationElement));
            Assert.Equal("nexus_enqueue_async_dump_analyze_command", operationElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("queued", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("timeoutMinutes", out var timeoutElement));
            Assert.Equal(10, timeoutElement.GetInt32());

            // Verify service calls
            m_MockSessionManager.Verify(x => x.TryGetCommandQueue(sessionId, out It.Ref<ICommandQueueService?>.IsAny), Times.Once);
            mockCommandQueue.Verify(x => x.QueueCommand(command), Times.Once);
        }

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithNonExistentSession_ReturnsNotFoundResponse()
        {
            // Arrange
            var sessionId = "non-existent-session";
            var command = "!analyze -v";

            m_MockSessionManager
                .Setup(x => x.TryGetCommandQueue(sessionId, out It.Ref<ICommandQueueService?>.IsAny))
                .Returns(false);

            // Act
            var result = await McpNexusTools.nexus_enqueue_async_dump_analyze_command(
                m_ServiceProvider, sessionId, command);

            // Assert
            Assert.NotNull(result);

            // Check if result is a Task (which it shouldn't be after await)
            if (result is Task<object> taskObject)
            {
                result = await taskObject;
            }

            // Use dynamic to access anonymous type properties
            dynamic dynamicResult = result;

            Assert.Equal(sessionId, dynamicResult.sessionId);
            Assert.False(dynamicResult.success);
            Assert.Contains("not ready", (string)dynamicResult.message);
            Assert.Null(dynamicResult.commandId);
        }

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithNullSessionContext_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var command = "!analyze -v";

            m_MockSessionManager
                .Setup(x => x.TryGetCommandQueue(sessionId, out It.Ref<ICommandQueueService?>.IsAny))
                .Returns(false);

            // Act
            var result = await McpNexusTools.nexus_enqueue_async_dump_analyze_command(
                m_ServiceProvider, sessionId, command);

            // Assert
            Assert.NotNull(result);

            // Check if result is a Task (which it shouldn't be after await)
            if (result is Task<object> taskObject)
            {
                result = await taskObject;
            }

            // Use dynamic to access anonymous type properties
            dynamic dynamicResult = result;

            Assert.False(dynamicResult.success);
            Assert.Contains("not ready", (string)dynamicResult.message);
        }

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithException_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var command = "!analyze -v";
            var exception = new InvalidOperationException("Test error");

            m_MockSessionManager
                .Setup(x => x.TryGetCommandQueue(sessionId, out It.Ref<ICommandQueueService?>.IsAny))
                .Throws(exception);

            // Act
            var result = await McpNexusTools.nexus_enqueue_async_dump_analyze_command(
                m_ServiceProvider, sessionId, command);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.False(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("Failed to queue command", messageElement.GetString());
            Assert.Contains("Test error", messageElement.GetString());
        }

        #endregion

        #region nexus_read_dump_analyze_command_result Tests

        [Fact]
        public async Task nexus_read_dump_analyze_command_result_WithCompletedCommand_ReturnsSuccessResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var commandId = "cmd-123";
            var commandResult = "Command output here";

            var mockCommandQueue = new Mock<ICommandQueueService>();

            var commandInfo = new CommandInfo
            {
                CommandId = commandId,
                Command = "!analyze -v",
                State = CommandState.Completed,
                QueueTime = DateTime.UtcNow.AddMinutes(-5),
                Elapsed = TimeSpan.FromMinutes(3),
                Remaining = TimeSpan.Zero,
                QueuePosition = 0,
                IsCompleted = true
            };

            mockCommandQueue
                .Setup(x => x.GetCommandInfo(commandId))
                .Returns(commandInfo);

            mockCommandQueue
                .Setup(x => x.GetCommandResult(commandId))
                .ReturnsAsync(commandResult);

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.GetCommandQueue(sessionId))
                .Returns(mockCommandQueue.Object);

            // Act
            var result = await McpNexusTools.nexus_read_dump_analyze_command_result(
                m_ServiceProvider, sessionId, commandId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("commandId", out var commandIdElement));
            Assert.Equal(commandId, commandIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.True(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Completed", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("result", out var resultElement));
            Assert.Equal(commandResult, resultElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("completedAt", out _));
            Assert.True(document.RootElement.TryGetProperty("progress", out _));
            Assert.True(document.RootElement.TryGetProperty("usage", out _));
        }

        [Fact]
        public async Task nexus_read_dump_analyze_command_result_WithQueuedCommand_ReturnsQueuedResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var commandId = "cmd-123";

            var mockCommandQueue = new Mock<ICommandQueueService>();

            var commandInfo = new CommandInfo
            {
                CommandId = commandId,
                Command = "!analyze -v",
                State = CommandState.Queued,
                QueueTime = DateTime.UtcNow.AddMinutes(-1),
                Elapsed = TimeSpan.FromMinutes(1),
                Remaining = TimeSpan.FromMinutes(9),
                QueuePosition = 2,
                IsCompleted = false
            };

            mockCommandQueue
                .Setup(x => x.GetCommandInfo(commandId))
                .Returns(commandInfo);

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.GetCommandQueue(sessionId))
                .Returns(mockCommandQueue.Object);

            // Act
            var result = await McpNexusTools.nexus_read_dump_analyze_command_result(
                m_ServiceProvider, sessionId, commandId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("commandId", out var commandIdElement));
            Assert.Equal(commandId, commandIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.False(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Queued", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("result", out var resultElement));
            Assert.Equal(JsonValueKind.Null, resultElement.ValueKind);

            Assert.True(document.RootElement.TryGetProperty("completedAt", out var completedAtElement));
            Assert.Equal(JsonValueKind.Null, completedAtElement.ValueKind);

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.NotEqual(JsonValueKind.Null, messageElement.ValueKind);

            Assert.True(document.RootElement.TryGetProperty("progress", out var progressElement));
            Assert.True(progressElement.TryGetProperty("queuePosition", out var queuePositionElement));
            Assert.Equal(2, queuePositionElement.GetInt32());
        }

        [Fact]
        public async Task nexus_read_dump_analyze_command_result_WithNonExistentSession_ReturnsNotFoundResponse()
        {
            // Arrange
            var sessionId = "non-existent-session";
            var commandId = "cmd-123";

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(false);

            // Act
            var result = await McpNexusTools.nexus_read_dump_analyze_command_result(
                m_ServiceProvider, sessionId, commandId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("commandId", out var commandIdElement));
            Assert.Equal(commandId, commandIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.False(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("error", out var errorElement));
            Assert.Contains("Session", errorElement.GetString());
            Assert.Contains("not found", errorElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("usage", out _));
        }

        [Fact]
        public async Task nexus_read_dump_analyze_command_result_WithNonExistentCommand_ReturnsNotFoundResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var commandId = "non-existent-cmd";

            var mockCommandQueue = new Mock<ICommandQueueService>();
            mockCommandQueue
                .Setup(x => x.GetCommandInfo(commandId))
                .Returns((CommandInfo?)null);

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.GetCommandQueue(sessionId))
                .Returns(mockCommandQueue.Object);

            // Act
            var result = await McpNexusTools.nexus_read_dump_analyze_command_result(
                m_ServiceProvider, sessionId, commandId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.False(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("error", out var errorElement));
            Assert.Contains("Command", errorElement.GetString());
            Assert.Contains("not found", errorElement.GetString());
        }

        [Fact]
        public async Task nexus_read_dump_analyze_command_result_WithException_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var commandId = "cmd-123";
            var exception = new InvalidOperationException("Test error");

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Throws(exception);

            // Act
            var result = await McpNexusTools.nexus_read_dump_analyze_command_result(
                m_ServiceProvider, sessionId, commandId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("success", out var successElement));
            Assert.False(successElement.GetBoolean());

            Assert.True(document.RootElement.TryGetProperty("error", out var errorElement));
            Assert.Contains("Failed to get command result", errorElement.GetString());
            Assert.Contains("Test error", errorElement.GetString());
        }

        #endregion

        #region Helper Methods Tests

        [Theory]
        [InlineData("", null)]
        [InlineData("No elapsed time", null)]
        [InlineData("Command completed. Elapsed: 2.5min", "2.5min")]
        [InlineData("Processing... Elapsed: 10.0min remaining", "10.0min")]
        [InlineData("Multiple Elapsed: 1.2min and Elapsed: 3.4min values", "1.2min")]
        public void GetElapsedTime_WithVariousInputs_ReturnsExpectedResult(string input, string? expected)
        {
            // Use reflection to access the private method
            var method = typeof(McpNexusTools).GetMethod("GetElapsedTime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { input });

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("No ETA time", null)]
        [InlineData("Command queued. ETA: 5min 30s", "5min 30s")]
        [InlineData("Processing... ETA: <1min", "<1min")]
        [InlineData("Multiple ETA: 2min 15s and ETA: 8min 45s values", "2min 15s")]
        public void GetEtaTime_WithStringInput_ReturnsExpectedResult(string input, string? expected)
        {
            // Use reflection to access the private method
            var method = typeof(McpNexusTools).GetMethods(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetEtaTime" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { input });

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0, 0, 0, "<1min")]   // 0 minutes = <1min
        [InlineData(0, 0, 30, "0min 30s")]  // 30 seconds = 0min 30s
        [InlineData(0, 2, 30, "2min 30s")]
        [InlineData(0, 5, 0, "5min 0s")]
        [InlineData(1, 30, 45, "90min 45s")]
        public void GetEtaTime_WithTimeSpanInput_ReturnsExpectedResult(int hours, int minutes, int seconds, string expected)
        {
            // Use reflection to access the private method
            var method = typeof(McpNexusTools).GetMethods(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetEtaTime" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(TimeSpan));
            Assert.NotNull(method);

            var timeSpan = new TimeSpan(hours, minutes, seconds);

            // Act
            var result = method.Invoke(null, new object[] { timeSpan });

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0, 0, 50, 50)]  // Queue pos 0: (10-0)*5 = 50%, no time
        [InlineData(1, 0, 45, 45)]  // Queue pos 1: (10-1)*5 = 45%, no time
        [InlineData(2, 0, 40, 40)]  // Queue pos 2: (10-2)*5 = 40%, no time
        [InlineData(5, 0, 25, 25)]  // Queue pos 5: (10-5)*5 = 25%, no time
        [InlineData(10, 0, 0, 0)]   // Queue pos 10: (10-10)*5 = 0%, no time
        [InlineData(0, 1, 52, 62)]  // Queue pos 0: 50% + time: 2% + min: 30s*0.5% = 15%
        [InlineData(0, 5, 60, 95)]  // Queue pos 0: 50% + time: 10% + min: 300s*0.5% = 95%
        public void CalculateProgressPercentage_WithVariousInputs_ReturnsExpectedRange(int queuePosition, int elapsedMinutes, int expectedMin, int expectedMax)
        {
            // Use reflection to access the private method
            var method = typeof(McpNexusTools).GetMethod("CalculateProgressPercentage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            var elapsed = TimeSpan.FromMinutes(elapsedMinutes);

            // Act
            var result = method.Invoke(null, new object[] { queuePosition, elapsed });

            // Assert
            Assert.IsType<int>(result);
            var percentage = (int)result;
            Assert.InRange(percentage, expectedMin, expectedMax);
            Assert.InRange(percentage, 0, 100);
        }

        [Theory]
        [InlineData(CommandState.Queued, 0, "3-5 seconds")]
        [InlineData(CommandState.Queued, 1, "5-10 seconds")]
        [InlineData(CommandState.Queued, 2, "10-15 seconds")]
        [InlineData(CommandState.Queued, 15, "1-2 minutes")]
        [InlineData(CommandState.Executing, 0, "10-30 seconds")]
        [InlineData(CommandState.Cancelled, 0, "No need to check")]
        [InlineData(CommandState.Failed, 0, "No need to check")]
        public void GetNextCheckInRecommendation_WithVariousStates_ReturnsExpectedRecommendation(CommandState state, int queuePosition, string expectedContains)
        {
            // Use reflection to access the private method
            var method = typeof(McpNexusTools).GetMethod("GetNextCheckInRecommendation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { state, queuePosition });

            // Assert
            Assert.IsType<string>(result);
            var recommendation = (string)result;
            Assert.Contains(expectedContains, recommendation);
        }

        [Theory]
        [InlineData(0, 0, "next in queue")]
        [InlineData(1, 0, "2nd in queue")]
        [InlineData(2, 0, "3rd in queue")]
        [InlineData(4, 0, "5th in queue")]
        [InlineData(9, 0, "10th in queue")]
        [InlineData(15, 0, "position 16 in queue")]
        public void GetBaseMessage_WithVariousPositions_ReturnsExpectedMessage(int queuePosition, int elapsedSeconds, string expectedContains)
        {
            // Use reflection to access the private method
            var method = typeof(McpNexusTools).GetMethod("GetBaseMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            var elapsed = TimeSpan.FromSeconds(elapsedSeconds);

            // Act
            var result = method.Invoke(null, new object[] { queuePosition, elapsed });

            // Assert
            Assert.IsType<string>(result);
            var message = (string)result;
            Assert.Contains(expectedContains, message);
        }

        [Fact]
        public void GetQueuedStatusMessage_WithValidInputs_ReturnsFormattedMessage()
        {
            // Use reflection to access the private method
            var method = typeof(McpNexusTools).GetMethod("GetQueuedStatusMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            var queuePosition = 2;
            var elapsed = TimeSpan.FromMinutes(1.5);
            var remaining = TimeSpan.FromMinutes(8.5);

            // Act
            var result = method.Invoke(null, new object[] { queuePosition, elapsed, remaining });

            // Assert
            Assert.IsType<string>(result);
            var message = (string)result;
            Assert.Contains("3rd in queue", message);
            Assert.Contains("Progress:", message);
            Assert.Contains("Elapsed:", message);
            // The message might be truncated in the test output, so just check it's not empty
            Assert.NotEmpty(message);
            Assert.Contains("ETA:", message);
        }

        #endregion
    }
}
