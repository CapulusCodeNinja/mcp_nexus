using System;
using System.Collections.Generic;
using Xunit;
using mcp_nexus.Session.Models;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using Moq;

namespace mcp_nexus_tests.Session.Models
{
    /// <summary>
    /// Tests for Session model classes - simple data containers
    /// </summary>
    public class SessionModelsTests
    {
        [Fact]
        public void SessionStatus_EnumValues_AreCorrect()
        {
            // Assert
            Assert.Equal(0, (int)SessionStatus.Initializing);
            Assert.Equal(1, (int)SessionStatus.Active);
            Assert.Equal(2, (int)SessionStatus.Disposing);
            Assert.Equal(3, (int)SessionStatus.Disposed);
            Assert.Equal(4, (int)SessionStatus.Error);
        }

        [Fact]
        public void SessionInfo_DefaultValues_AreCorrect()
        {
            // Act
            var sessionInfo = new SessionInfo();

            // Assert
            Assert.Equal(string.Empty, sessionInfo.SessionId);
            Assert.Null(sessionInfo.CdbSession);
            Assert.Null(sessionInfo.CommandQueue);
            Assert.Equal(DateTime.MinValue, sessionInfo.CreatedAt);
            Assert.Equal(string.Empty, sessionInfo.DumpPath);
            Assert.Null(sessionInfo.SymbolsPath);
        }

        [Fact]
        public void SessionInfo_WithValues_SetsProperties()
        {
            // Arrange
            var mockCdbSession = new Mock<ICdbSession>();
            var mockCommandQueue = new Mock<ICommandQueueService>();
            var createdAt = DateTime.UtcNow;
            const string sessionId = "test-session-123";
            const string dumpPath = "C:\\test.dmp";
            const string symbolsPath = "C:\\symbols";

            // Act
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                CdbSession = mockCdbSession.Object,
                CommandQueue = mockCommandQueue.Object,
                CreatedAt = createdAt,
                DumpPath = dumpPath,
                SymbolsPath = symbolsPath
            };

            // Assert
            Assert.Equal(sessionId, sessionInfo.SessionId);
            Assert.Equal(mockCdbSession.Object, sessionInfo.CdbSession);
            Assert.Equal(mockCommandQueue.Object, sessionInfo.CommandQueue);
            Assert.Equal(createdAt, sessionInfo.CreatedAt);
            Assert.Equal(dumpPath, sessionInfo.DumpPath);
            Assert.Equal(symbolsPath, sessionInfo.SymbolsPath);
        }

        [Fact]
        public void SessionInfo_Dispose_DoesNotThrow()
        {
            // Arrange
            var sessionInfo = new SessionInfo();

            // Act & Assert
            sessionInfo.Dispose(); // Should not throw
        }

        [Fact]
        public void SessionContext_DefaultValues_AreCorrect()
        {
            // Act
            var context = new SessionContext();

            // Assert
            Assert.Equal(string.Empty, context.SessionId);
            Assert.Equal(string.Empty, context.Description);
            Assert.Equal(DateTime.MinValue, context.CreatedAt);
            Assert.Equal(string.Empty, context.Status);
            Assert.Equal(0, context.CommandsProcessed);
            Assert.Equal(0, context.ActiveCommands);
        }

        [Fact]
        public void SessionContext_WithValues_SetsProperties()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            const string sessionId = "context-session-456";
            const string description = "Test session context";

            // Act
            var context = new SessionContext
            {
                SessionId = sessionId,
                Description = description,
                CreatedAt = createdAt,
                Status = "Active",
                CommandsProcessed = 5,
                ActiveCommands = 2
            };

            // Assert
            Assert.Equal(sessionId, context.SessionId);
            Assert.Equal(description, context.Description);
            Assert.Equal(createdAt, context.CreatedAt);
            Assert.Equal("Active", context.Status);
            Assert.Equal(5, context.CommandsProcessed);
            Assert.Equal(2, context.ActiveCommands);
        }

        [Fact]
        public void CdbSessionOptions_DefaultValues_AreCorrect()
        {
            // Act
            var options = new CdbSessionOptions();

            // Assert
            Assert.Equal(30000, options.CommandTimeoutMs);
            Assert.Equal(30000, options.SymbolServerTimeoutMs);
            Assert.Equal(1, options.SymbolServerMaxRetries);
            Assert.Null(options.SymbolSearchPath);
            Assert.Null(options.CustomCdbPath);
        }

        [Fact]
        public void CdbSessionOptions_WithValues_SetsProperties()
        {
            // Arrange
            const string symbolSearchPath = "C:\\symbols";
            const string customCdbPath = "C:\\cdb.exe";

            // Act
            var options = new CdbSessionOptions
            {
                CommandTimeoutMs = 60000,
                SymbolServerTimeoutMs = 45000,
                SymbolServerMaxRetries = 3,
                SymbolSearchPath = symbolSearchPath,
                CustomCdbPath = customCdbPath
            };

            // Assert
            Assert.Equal(60000, options.CommandTimeoutMs);
            Assert.Equal(45000, options.SymbolServerTimeoutMs);
            Assert.Equal(3, options.SymbolServerMaxRetries);
            Assert.Equal(symbolSearchPath, options.SymbolSearchPath);
            Assert.Equal(customCdbPath, options.CustomCdbPath);
        }

        [Fact]
        public void SessionConfiguration_DefaultValues_AreCorrect()
        {
            // Act
            var config = new SessionConfiguration();

            // Assert
            Assert.Equal(1000, config.MaxConcurrentSessions);
            Assert.Equal(TimeSpan.FromMinutes(30), config.SessionTimeout);
            Assert.Equal(TimeSpan.FromMinutes(5), config.CleanupInterval);
            Assert.Equal(TimeSpan.FromSeconds(30), config.DisposalTimeout);
            Assert.Equal(TimeSpan.FromMinutes(10), config.DefaultCommandTimeout);
            Assert.Equal(1_000_000_000L, config.MemoryCleanupThresholdBytes);
        }

        [Fact]
        public void SessionConfiguration_WithValues_SetsProperties()
        {
            // Arrange
            var sessionTimeout = TimeSpan.FromMinutes(60);
            var cleanupInterval = TimeSpan.FromMinutes(10);
            var disposalTimeout = TimeSpan.FromSeconds(60);
            var commandTimeout = TimeSpan.FromMinutes(15);

            // Act
            var config = new SessionConfiguration
            {
                MaxConcurrentSessions = 500,
                SessionTimeout = sessionTimeout,
                CleanupInterval = cleanupInterval,
                DisposalTimeout = disposalTimeout,
                DefaultCommandTimeout = commandTimeout,
                MemoryCleanupThresholdBytes = 2_000_000_000L
            };

            // Assert
            Assert.Equal(500, config.MaxConcurrentSessions);
            Assert.Equal(sessionTimeout, config.SessionTimeout);
            Assert.Equal(cleanupInterval, config.CleanupInterval);
            Assert.Equal(disposalTimeout, config.DisposalTimeout);
            Assert.Equal(commandTimeout, config.DefaultCommandTimeout);
            Assert.Equal(2_000_000_000L, config.MemoryCleanupThresholdBytes);
        }


        [Fact]
        public void SessionLimitExceededException_WithCounts_SetsProperties()
        {
            // Arrange
            const int currentSessions = 5;
            const int maxSessions = 3;

            // Act
            var exception = new SessionLimitExceededException(currentSessions, maxSessions);

            // Assert
            Assert.Equal(currentSessions, exception.CurrentSessions);
            Assert.Equal(maxSessions, exception.MaxSessions);
            Assert.Contains("Maximum concurrent sessions exceeded: 5/3", exception.Message);
        }

        [Fact]
        public void SessionNotFoundException_WithSessionId_SetsProperties()
        {
            // Arrange
            const string sessionId = "session-123";

            // Act
            var exception = new SessionNotFoundException(sessionId);

            // Assert
            Assert.Equal(sessionId, exception.SessionId);
            Assert.Contains($"Session '{sessionId}' not found or has expired", exception.Message);
        }

        [Fact]
        public void SessionNotFoundException_WithSessionIdAndMessage_SetsProperties()
        {
            // Arrange
            const string sessionId = "session-456";
            const string message = "Custom session not found message";

            // Act
            var exception = new SessionNotFoundException(sessionId, message);

            // Assert
            Assert.Equal(sessionId, exception.SessionId);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void SessionNotFoundException_WithSessionIdMessageAndInnerException_SetsProperties()
        {
            // Arrange
            const string sessionId = "session-789";
            const string message = "Session not found with inner exception";
            var innerException = new ArgumentException("Inner error");

            // Act
            var exception = new SessionNotFoundException(sessionId, message, innerException);

            // Assert
            Assert.Equal(sessionId, exception.SessionId);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }
    }
}
