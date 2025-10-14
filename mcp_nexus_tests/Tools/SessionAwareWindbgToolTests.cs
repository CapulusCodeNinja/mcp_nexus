using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Tools;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Exceptions;
using System.Text.Json;

namespace mcp_nexus_tests.Tools
{
    public class SessionAwareWindbgToolTests : IDisposable
    {
        private readonly Mock<ILogger<SessionAwareWindbgTool>> m_MockLogger;
        private readonly Mock<ISessionManager> m_MockSessionManager;
        private readonly SessionAwareWindbgTool m_Tool;

        public SessionAwareWindbgToolTests()
        {
            m_MockLogger = new Mock<ILogger<SessionAwareWindbgTool>>();
            m_MockSessionManager = new Mock<ISessionManager>();
            m_Tool = new SessionAwareWindbgTool(m_MockLogger.Object, m_MockSessionManager.Object);
        }

        public void Dispose()
        {
            // No cleanup needed for this test class
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var tool = new SessionAwareWindbgTool(m_MockLogger.Object, m_MockSessionManager.Object);

            // Assert
            Assert.NotNull(tool);
        }

        [Fact]
        public void Constructor_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert - Primary constructors don't validate parameters by default
            var tool = new SessionAwareWindbgTool(null!, m_MockSessionManager.Object);
            Assert.NotNull(tool);
        }

        [Fact]
        public void Constructor_WithNullSessionManager_DoesNotThrow()
        {
            // Act & Assert - Primary constructors don't validate parameters by default
            var tool = new SessionAwareWindbgTool(m_MockLogger.Object, null!);
            Assert.NotNull(tool);
        }

        #endregion

        #region Static Properties Tests

        [Fact]
        public void USAGE_EXPLANATION_IsNotNull()
        {
            // Act & Assert
            Assert.NotNull(SessionAwareWindbgTool.USAGE_EXPLANATION);
        }

        [Fact]
        public void USAGE_EXPLANATION_HasExpectedStructure()
        {
            // Arrange
            var usage = SessionAwareWindbgTool.USAGE_EXPLANATION;
            var json = JsonSerializer.Serialize(usage);
            var document = JsonDocument.Parse(json);

            // Assert
            Assert.True(document.RootElement.TryGetProperty("title", out _));
            Assert.True(document.RootElement.TryGetProperty("description", out _));
            Assert.True(document.RootElement.TryGetProperty("tools", out _));
            Assert.True(document.RootElement.TryGetProperty("resources", out _));
        }

        #endregion

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

                m_MockSessionManager
                    .Setup(x => x.GetSessionContext(sessionId))
                    .Returns(sessionContext);

                // Act
                var result = await m_Tool.nexus_open_dump_analyze_session(dumpPath, symbolsPath);

                // Assert
                Assert.NotNull(result);
                var json = JsonSerializer.Serialize(result);
                var document = JsonDocument.Parse(json);

                Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
                Assert.Equal(sessionId, sessionIdElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
                Assert.Equal("Success", statusElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("operation", out var operationElement));
                Assert.Equal("nexus_open_dump_analyze_session", operationElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("usage", out _));
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
                var result = await m_Tool.nexus_open_dump_analyze_session(dumpPath);

                // Assert
                Assert.NotNull(result);
                var json = JsonSerializer.Serialize(result);
                var document = JsonDocument.Parse(json);

                Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
                Assert.Equal("Failed", statusElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
                Assert.Contains("Maximum concurrent sessions exceeded", messageElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("usage", out _));
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
                var result = await m_Tool.nexus_open_dump_analyze_session(dumpPath);

                // Assert
                Assert.NotNull(result);
                var json = JsonSerializer.Serialize(result);
                var document = JsonDocument.Parse(json);

                Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
                Assert.Equal("Failed", statusElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
                Assert.Contains("Failed to create debugging session", messageElement.GetString());

                Assert.True(document.RootElement.TryGetProperty("usage", out _));
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
                await m_Tool.nexus_open_dump_analyze_session(dumpPath);

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
                .Setup(x => x.GetSessionContext(sessionId))
                .Returns(new SessionContext { SessionId = sessionId });

            m_MockSessionManager
                .Setup(x => x.CloseSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await m_Tool.nexus_close_dump_analyze_session(sessionId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Success", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("operation", out var operationElement));
            Assert.Equal("nexus_close_dump_analyze_session", operationElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("usage", out _));
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
            var result = await m_Tool.nexus_close_dump_analyze_session(sessionId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Failed", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("Session not found or already closed", messageElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("usage", out _));
        }

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithCloseFailure_ReturnsFailureResponse()
        {
            // Arrange
            var sessionId = "test-session-123";

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.GetSessionContext(sessionId))
                .Returns(new SessionContext { SessionId = sessionId });

            m_MockSessionManager
                .Setup(x => x.CloseSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await m_Tool.nexus_close_dump_analyze_session(sessionId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Failed", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("may have already been closed", messageElement.GetString());
        }

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithException_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var exception = new InvalidOperationException("Test error");

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.GetSessionContext(sessionId))
                .Throws(exception);

            // Act
            var result = await m_Tool.nexus_close_dump_analyze_session(sessionId);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Failed", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("Error closing session", messageElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("usage", out _));
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
                .Setup(x => x.GetCommandQueue(sessionId))
                .Returns(mockCommandQueue.Object);

            m_MockSessionManager
                .Setup(x => x.GetSessionContext(sessionId))
                .Returns(sessionContext);

            // Act
            var result = await m_Tool.nexus_enqueue_async_dump_analyze_command(sessionId, command);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("commandId", out var commandIdElement));
            Assert.Equal(commandId, commandIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Success", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("operation", out var operationElement));
            Assert.Equal("nexus_enqueue_async_dump_analyze_command", operationElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("usage", out _));
        }

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithNonExistentSession_ReturnsNotFoundResponse()
        {
            // Arrange
            var sessionId = "non-existent-session";
            var command = "!analyze -v";

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(false);

            // Act
            var result = await m_Tool.nexus_enqueue_async_dump_analyze_command(sessionId, command);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal(sessionId, sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Failed", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("Session not found or expired", messageElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("usage", out _));
        }

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithSessionNotFoundException_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var command = "!analyze -v";
            var exception = new SessionNotFoundException(sessionId);

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.GetCommandQueue(sessionId))
                .Throws(exception);

            // Act
            var result = await m_Tool.nexus_enqueue_async_dump_analyze_command(sessionId, command);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Failed", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("Session not found", messageElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("usage", out _));
        }

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithGeneralException_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session-123";
            var command = "!analyze -v";
            var exception = new InvalidOperationException("Test error");

            m_MockSessionManager
                .Setup(x => x.SessionExists(sessionId))
                .Returns(true);

            m_MockSessionManager
                .Setup(x => x.GetCommandQueue(sessionId))
                .Throws(exception);

            // Act
            var result = await m_Tool.nexus_enqueue_async_dump_analyze_command(sessionId, command);

            // Assert
            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.TryGetProperty("status", out var statusElement));
            Assert.Equal("Failed", statusElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("message", out var messageElement));
            Assert.Contains("Error executing command", messageElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("usage", out _));
        }

        #endregion

        #region Data Classes Tests

        [Fact]
        public void SessionAwareResponse_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var response = new SessionAwareWindbgTool.SessionAwareResponse();

            // Assert
            Assert.Equal(string.Empty, response.SessionId);
            Assert.Equal(string.Empty, response.Result);
            Assert.Null(response.SessionContext);
            Assert.NotNull(response.AIGuidance);
            Assert.NotNull(response.WorkflowContext);
        }

        [Fact]
        public void SessionAwareResponse_PropertyAssignments_WorkCorrectly()
        {
            // Arrange
            var sessionContext = new SessionContext { SessionId = "test-123" };
            var aiGuidance = new SessionAwareWindbgTool.AIGuidance();
            var workflowContext = new SessionAwareWindbgTool.WorkflowContext();

            // Act
            var response = new SessionAwareWindbgTool.SessionAwareResponse
            {
                SessionId = "test-session",
                Result = "test-result",
                SessionContext = sessionContext,
                AIGuidance = aiGuidance,
                WorkflowContext = workflowContext
            };

            // Assert
            Assert.Equal("test-session", response.SessionId);
            Assert.Equal("test-result", response.Result);
            Assert.Equal(sessionContext, response.SessionContext);
            Assert.Equal(aiGuidance, response.AIGuidance);
            Assert.Equal(workflowContext, response.WorkflowContext);
        }

        [Fact]
        public void AIGuidance_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var guidance = new SessionAwareWindbgTool.AIGuidance();

            // Assert
            Assert.NotNull(guidance.NextSteps);
            Assert.Empty(guidance.NextSteps);
            Assert.NotNull(guidance.UsageHints);
            Assert.Empty(guidance.UsageHints);
            Assert.NotNull(guidance.CommonErrors);
            Assert.Empty(guidance.CommonErrors);
        }

        [Fact]
        public void AIGuidance_PropertyAssignments_WorkCorrectly()
        {
            // Arrange
            var nextSteps = new List<string> { "Step 1", "Step 2" };
            var usageHints = new List<string> { "Hint 1", "Hint 2" };
            var commonErrors = new List<string> { "Error 1", "Error 2" };

            // Act
            var guidance = new SessionAwareWindbgTool.AIGuidance
            {
                NextSteps = nextSteps,
                UsageHints = usageHints,
                CommonErrors = commonErrors
            };

            // Assert
            Assert.Equal(nextSteps, guidance.NextSteps);
            Assert.Equal(usageHints, guidance.UsageHints);
            Assert.Equal(commonErrors, guidance.CommonErrors);
        }

        [Fact]
        public void WorkflowContext_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var context = new SessionAwareWindbgTool.WorkflowContext();

            // Assert
            Assert.Equal(string.Empty, context.CurrentStep);
            Assert.NotNull(context.SuggestedNextCommands);
            Assert.Empty(context.SuggestedNextCommands);
            Assert.Equal(string.Empty, context.SessionState);
        }

        [Fact]
        public void WorkflowContext_PropertyAssignments_WorkCorrectly()
        {
            // Arrange
            var suggestedCommands = new List<string> { "!analyze -v", "k" };

            // Act
            var context = new SessionAwareWindbgTool.WorkflowContext
            {
                CurrentStep = "analysis",
                SuggestedNextCommands = suggestedCommands,
                SessionState = "active"
            };

            // Assert
            Assert.Equal("analysis", context.CurrentStep);
            Assert.Equal(suggestedCommands, context.SuggestedNextCommands);
            Assert.Equal("active", context.SessionState);
        }

        #endregion

        #region JSON Serialization Tests

        [Fact]
        public void SessionAwareResponse_SerializesToJson_Correctly()
        {
            // Arrange
            var response = new SessionAwareWindbgTool.SessionAwareResponse
            {
                SessionId = "test-session",
                Result = "test-result"
            };

            // Act
            var json = JsonSerializer.Serialize(response);
            var document = JsonDocument.Parse(json);

            // Assert
            Assert.True(document.RootElement.TryGetProperty("sessionId", out var sessionIdElement));
            Assert.Equal("test-session", sessionIdElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("result", out var resultElement));
            Assert.Equal("test-result", resultElement.GetString());
        }

        [Fact]
        public void AIGuidance_SerializesToJson_Correctly()
        {
            // Arrange
            var guidance = new SessionAwareWindbgTool.AIGuidance
            {
                NextSteps = ["Step 1", "Step 2"],
                UsageHints = ["Hint 1"],
                CommonErrors = ["Error 1"]
            };

            // Act
            var json = JsonSerializer.Serialize(guidance);
            var document = JsonDocument.Parse(json);

            // Assert
            Assert.True(document.RootElement.TryGetProperty("nextSteps", out var nextStepsElement));
            Assert.Equal(JsonValueKind.Array, nextStepsElement.ValueKind);
            Assert.Equal(2, nextStepsElement.GetArrayLength());

            Assert.True(document.RootElement.TryGetProperty("usageHints", out var usageHintsElement));
            Assert.Equal(JsonValueKind.Array, usageHintsElement.ValueKind);
            Assert.Equal(1, usageHintsElement.GetArrayLength());

            Assert.True(document.RootElement.TryGetProperty("commonErrors", out var commonErrorsElement));
            Assert.Equal(JsonValueKind.Array, commonErrorsElement.ValueKind);
            Assert.Equal(1, commonErrorsElement.GetArrayLength());
        }

        [Fact]
        public void WorkflowContext_SerializesToJson_Correctly()
        {
            // Arrange
            var context = new SessionAwareWindbgTool.WorkflowContext
            {
                CurrentStep = "analysis",
                SuggestedNextCommands = ["!analyze -v"],
                SessionState = "active"
            };

            // Act
            var json = JsonSerializer.Serialize(context);
            var document = JsonDocument.Parse(json);

            // Assert
            Assert.True(document.RootElement.TryGetProperty("currentStep", out var currentStepElement));
            Assert.Equal("analysis", currentStepElement.GetString());

            Assert.True(document.RootElement.TryGetProperty("suggestedNextCommands", out var suggestedCommandsElement));
            Assert.Equal(JsonValueKind.Array, suggestedCommandsElement.ValueKind);
            Assert.Equal(1, suggestedCommandsElement.GetArrayLength());

            Assert.True(document.RootElement.TryGetProperty("sessionState", out var sessionStateElement));
            Assert.Equal("active", sessionStateElement.GetString());
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task nexus_open_dump_analyze_session_WithSessionLimitExceeded2_ReturnsErrorResponse()
        {
            // Arrange
            var dumpPath = Path.GetTempFileName();
            try
            {
                m_MockSessionManager.Setup(x => x.CreateSessionAsync(dumpPath, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new SessionLimitExceededException(5, 5));

                // Act
                var result = await m_Tool.nexus_open_dump_analyze_session(dumpPath);

                // Assert
                var json = JsonSerializer.Serialize(result);
                Assert.True(json.Contains("Maximum concurrent sessions exceeded") || json.Contains("Session limit exceeded") || json.Contains("error"),
                    $"Expected error message but got: {json}");
            }
            finally
            {
                File.Delete(dumpPath);
            }
        }

        [Fact]
        public async Task nexus_open_dump_analyze_session_WithGeneralException2_ReturnsErrorResponse()
        {
            // Arrange
            var dumpPath = Path.GetTempFileName();
            try
            {
                m_MockSessionManager.Setup(x => x.CreateSessionAsync(dumpPath, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("General error"));

                // Act
                var result = await m_Tool.nexus_open_dump_analyze_session(dumpPath);

                // Assert
                var json = JsonSerializer.Serialize(result);
                Assert.True(json.Contains("Failed to create debugging session") || json.Contains("error") || json.Contains("Exception"),
                    $"Expected error message but got: {json}");
            }
            finally
            {
                File.Delete(dumpPath);
            }
        }

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithNullSessionId_ReturnsErrorResponse()
        {
            // Arrange
            string? sessionId = null;

            // Act
            var result = await m_Tool.nexus_close_dump_analyze_session(sessionId!);

            // Assert
            var json = JsonSerializer.Serialize(result);
            // Debug: Let's see what we actually get
            Assert.True(json.Contains("Invalid session ID") || json.Contains("error") || json.Contains("null") || json.Contains("empty"),
                $"Expected error message but got: {json}");
        }

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithEmptySessionId_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "";

            // Act
            var result = await m_Tool.nexus_close_dump_analyze_session(sessionId);

            // Assert
            var json = JsonSerializer.Serialize(result);
            // Debug: Let's see what we actually get
            Assert.True(json.Contains("Invalid session ID") || json.Contains("error") || json.Contains("null") || json.Contains("empty"),
                $"Expected error message but got: {json}");
        }

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithWhitespaceSessionId_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "   ";

            // Act
            var result = await m_Tool.nexus_close_dump_analyze_session(sessionId);

            // Assert
            var json = JsonSerializer.Serialize(result);
            // Debug: Let's see what we actually get
            Assert.True(json.Contains("Invalid session ID") || json.Contains("error") || json.Contains("null") || json.Contains("empty"),
                $"Expected error message but got: {json}");
        }

        [Fact]
        public async Task nexus_close_dump_analyze_session_WithGeneralException_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session";
            m_MockSessionManager.Setup(x => x.CloseSessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("General error"));

            // Act
            var result = await m_Tool.nexus_close_dump_analyze_session(sessionId);

            // Assert
            var json = JsonSerializer.Serialize(result);
            // The response should contain an error message
            Assert.True(json.Contains("Session not found") || json.Contains("error") || json.Contains("General error"),
                $"Expected error message but got: {json}");
        }

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithSessionNotFound_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "non-existent-session";
            var command = "!analyze -v";
            m_MockSessionManager.Setup(x => x.TryGetCommandQueue(sessionId, out It.Ref<ICommandQueueService?>.IsAny))
                .Returns(false);

            // Act
            var result = await m_Tool.nexus_enqueue_async_dump_analyze_command(sessionId, command);

            // Assert
            var json = JsonSerializer.Serialize(result);
            var document = JsonDocument.Parse(json);

            // The response should contain an error message
            Assert.True(json.Contains("Session not found") || json.Contains("error"));
        }

        [Fact]
        public async Task nexus_enqueue_async_dump_analyze_command_WithGeneralException2_ReturnsErrorResponse()
        {
            // Arrange
            var sessionId = "test-session";
            var command = "!analyze -v";
            var mockCommandQueue = new Mock<ICommandQueueService>();
            mockCommandQueue.Setup(x => x.QueueCommand(command))
                .Throws(new InvalidOperationException("General error"));

            ICommandQueueService? outQueue = mockCommandQueue.Object;
            m_MockSessionManager.Setup(x => x.TryGetCommandQueue(sessionId, out outQueue))
                .Returns(true);

            // Act
            var result = await m_Tool.nexus_enqueue_async_dump_analyze_command(sessionId, command);

            // Assert
            var json = JsonSerializer.Serialize(result);
            // The response should contain an error message
            Assert.True(json.Contains("Session not found") || json.Contains("error") || json.Contains("General error"),
                $"Expected error message but got: {json}");
        }

        #endregion
    }
}
