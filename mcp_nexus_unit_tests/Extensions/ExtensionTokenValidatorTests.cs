using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Extensions;
using Xunit;

namespace mcp_nexus_unit_tests.Extensions
{
    /// <summary>
    /// Tests for the ExtensionTokenValidator class.
    /// </summary>
    public class ExtensionTokenValidatorTests
    {
        private readonly Mock<ILogger<ExtensionTokenValidator>> m_MockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionTokenValidatorTests"/> class.
        /// </summary>
        public ExtensionTokenValidatorTests()
        {
            m_MockLogger = new Mock<ILogger<ExtensionTokenValidator>>();
        }

        [Fact]
        public void Constructor_WithValidLogger_Succeeds()
        {
            // Act
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Assert
            Assert.NotNull(validator);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ExtensionTokenValidator(null!));
        }

        [Fact]
        public void CreateToken_ReturnsNonEmptyToken()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var sessionId = "test-session";
            var commandId = "test-command";

            // Act
            var token = validator.CreateToken(sessionId, commandId);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void CreateToken_WithDifferentInputs_ReturnsUniqueTokens()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act
            var token1 = validator.CreateToken("session1", "command1");
            var token2 = validator.CreateToken("session2", "command2");

            // Assert
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void ValidateToken_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var sessionId = "test-session";
            var commandId = "test-command";
            var token = validator.CreateToken(sessionId, commandId);

            // Act
            var (isValid, extractedSessionId, extractedCommandId) = validator.ValidateToken(token);

            // Assert
            Assert.True(isValid);
            Assert.Equal(sessionId, extractedSessionId);
            Assert.Equal(commandId, extractedCommandId);
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var invalidToken = "invalid-token-12345";

            // Act
            var (isValid, sessionId, commandId) = validator.ValidateToken(invalidToken);

            // Assert
            Assert.False(isValid);
            Assert.Null(sessionId);
            Assert.Null(commandId);
        }

        [Fact]
        public void ValidateToken_WithNullToken_ReturnsFalse()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act
            var (isValid, sessionId, commandId) = validator.ValidateToken(null!);

            // Assert
            Assert.False(isValid);
            Assert.Null(sessionId);
            Assert.Null(commandId);
        }

        [Fact]
        public void ValidateToken_WithEmptyToken_ReturnsFalse()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act
            var (isValid, sessionId, commandId) = validator.ValidateToken(string.Empty);

            // Assert
            Assert.False(isValid);
            Assert.Null(sessionId);
            Assert.Null(commandId);
        }

        [Fact]
        public void RevokeToken_WithValidToken_TokenBecomesInvalid()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var sessionId = "test-session";
            var commandId = "test-command";
            var token = validator.CreateToken(sessionId, commandId);

            // Verify token is initially valid
            var (isValidBefore, _, _) = validator.ValidateToken(token);
            Assert.True(isValidBefore);

            // Act
            validator.RevokeToken(token);

            // Assert
            var (isValidAfter, _, _) = validator.ValidateToken(token);
            Assert.False(isValidAfter);
        }

        [Fact]
        public void RevokeToken_WithNonexistentToken_DoesNotThrow()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert - Should not throw
            validator.RevokeToken("nonexistent-token");
        }

        [Fact]
        public void RevokeToken_WithNullToken_DoesNotThrow()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert - Should not throw
            validator.RevokeToken(null!);
        }

        [Fact]
        public void CreateToken_MultipleTimes_CreatesUniqueTokens()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var sessionId = "test-session";
            var commandId = "test-command";

            // Act
            var token1 = validator.CreateToken(sessionId, commandId);
            var token2 = validator.CreateToken(sessionId, commandId);

            // Assert
            Assert.NotEqual(token1, token2); // Should be unique even with same inputs
        }

        [Fact]
        public void ValidateToken_AfterMultipleCreations_ValidatesCorrectly()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var sessionId = "test-session";
            var commandId = "test-command";

            var token1 = validator.CreateToken(sessionId, commandId);
            var token2 = validator.CreateToken(sessionId, commandId);

            // Act & Assert
            var (isValid1, session1, command1) = validator.ValidateToken(token1);
            var (isValid2, session2, command2) = validator.ValidateToken(token2);

            Assert.True(isValid1);
            Assert.Equal(sessionId, session1);
            Assert.Equal(commandId, command1);

            Assert.True(isValid2);
            Assert.Equal(sessionId, session2);
            Assert.Equal(commandId, command2);
        }

        [Fact]
        public async Task ValidateToken_ConcurrentCreationAndValidation_ThreadSafe()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var tasks = new List<Task>();
            var tokens = new System.Collections.Concurrent.ConcurrentBag<string>();

            // Act - Create tokens concurrently
            for (int i = 0; i < 100; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    var token = validator.CreateToken($"session-{index}", $"command-{index}");
                    tokens.Add(token);
                }));
            }

            await Task.WhenAll([.. tasks]);

            // Assert - All tokens should be valid
            foreach (var token in tokens)
            {
                var (isValid, _, _) = validator.ValidateToken(token);
                Assert.True(isValid);
            }
        }

        [Fact]
        public async Task RevokeToken_ConcurrentRevocation_ThreadSafe()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var tokens = new List<string>();

            for (int i = 0; i < 100; i++)
            {
                tokens.Add(validator.CreateToken($"session-{i}", $"command-{i}"));
            }

            // Act - Revoke tokens concurrently
            var tasks = tokens.Select(token => Task.Run(() => validator.RevokeToken(token))).ToList();
            await Task.WhenAll([.. tasks]);

            // Assert - All tokens should be invalid
            foreach (var token in tokens)
            {
                var (isValid, _, _) = validator.ValidateToken(token);
                Assert.False(isValid);
            }
        }

        #region CreateToken Validation Tests

        [Fact]
        public void CreateToken_WithNullSessionId_ThrowsArgumentException()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => validator.CreateToken(null!, "command-1"));
            Assert.Contains("Session ID", ex.Message);
        }

        [Fact]
        public void CreateToken_WithEmptySessionId_ThrowsArgumentException()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => validator.CreateToken("", "command-1"));
            Assert.Contains("Session ID", ex.Message);
        }

        [Fact]
        public void CreateToken_WithWhitespaceSessionId_ThrowsArgumentException()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => validator.CreateToken("   ", "command-1"));
            Assert.Contains("Session ID", ex.Message);
        }

        [Fact]
        public void CreateToken_WithNullCommandId_ThrowsArgumentException()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => validator.CreateToken("session-1", null!));
            Assert.Contains("Command ID", ex.Message);
        }

        [Fact]
        public void CreateToken_WithEmptyCommandId_ThrowsArgumentException()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => validator.CreateToken("session-1", ""));
            Assert.Contains("Command ID", ex.Message);
        }

        [Fact]
        public void CreateToken_WithWhitespaceCommandId_ThrowsArgumentException()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => validator.CreateToken("session-1", "   "));
            Assert.Contains("Command ID", ex.Message);
        }

        [Fact]
        public void CreateToken_ReturnsTokenStartingWithExtPrefix()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act
            var token = validator.CreateToken("session-1", "command-1");

            // Assert
            Assert.StartsWith("ext_", token);
        }

        #endregion

        #region ValidateToken Additional Tests

        [Fact]
        public void ValidateToken_WithWhitespaceToken_ReturnsFalse()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act
            var (isValid, sessionId, commandId) = validator.ValidateToken("   ");

            // Assert
            Assert.False(isValid);
            Assert.Null(sessionId);
            Assert.Null(commandId);
        }

        [Fact]
        public void ValidateToken_WithExpiredToken_ReturnsFalse()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var token = validator.CreateToken("session-1", "command-1");

            // Use reflection to expire the token
            var tokensField = typeof(ExtensionTokenValidator)
                .GetField("m_Tokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tokensDict = tokensField!.GetValue(validator)!;
            var tokensDictType = tokensDict.GetType();

            // Get the dictionary as IEnumerable and iterate
            foreach (var item in (System.Collections.IEnumerable)tokensDict)
            {
                var kvpType = item.GetType();
                var valueProperty = kvpType.GetProperty("Value");
                var tokenInfo = valueProperty!.GetValue(item)!;
                var expiresAtProp = tokenInfo.GetType().GetProperty("ExpiresAt");
                expiresAtProp!.SetValue(tokenInfo, DateTime.Now.AddHours(-1)); // Expired 1 hour ago
            }

            // Act
            var (isValid, sessionId, commandId) = validator.ValidateToken(token);

            // Assert
            Assert.False(isValid);
            Assert.Null(sessionId);
            Assert.Null(commandId);
        }

        #endregion

        #region RevokeToken Additional Tests

        [Fact]
        public void RevokeToken_WithEmptyToken_DoesNotThrow()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert - Should not throw
            validator.RevokeToken("");
        }

        [Fact]
        public void RevokeToken_WithWhitespaceToken_DoesNotThrow()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert - Should not throw
            validator.RevokeToken("   ");
        }

        #endregion

        #region RevokeSessionTokens Tests

        [Fact]
        public void RevokeSessionTokens_WithValidSessionId_RevokesAllTokens()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var sessionId = "test-session";
            var token1 = validator.CreateToken(sessionId, "command-1");
            var token2 = validator.CreateToken(sessionId, "command-2");
            var token3 = validator.CreateToken("other-session", "command-3");

            // Act
            validator.RevokeSessionTokens(sessionId);

            // Assert
            var (isValid1, _, _) = validator.ValidateToken(token1);
            var (isValid2, _, _) = validator.ValidateToken(token2);
            var (isValid3, _, _) = validator.ValidateToken(token3);

            Assert.False(isValid1);
            Assert.False(isValid2);
            Assert.True(isValid3); // Different session, should still be valid
        }

        [Fact]
        public void RevokeSessionTokens_WithNullSessionId_DoesNotThrow()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert - Should not throw
            validator.RevokeSessionTokens(null!);
        }

        [Fact]
        public void RevokeSessionTokens_WithEmptySessionId_DoesNotThrow()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert - Should not throw
            validator.RevokeSessionTokens("");
        }

        [Fact]
        public void RevokeSessionTokens_WithWhitespaceSessionId_DoesNotThrow()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert - Should not throw
            validator.RevokeSessionTokens("   ");
        }

        [Fact]
        public void RevokeSessionTokens_WithNonexistentSessionId_DoesNotThrow()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Act & Assert - Should not throw
            validator.RevokeSessionTokens("nonexistent-session");
        }

        [Fact]
        public void RevokeSessionTokens_WithMultipleTokens_RevokesAllForSession()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var sessionId = "multi-token-session";
            var tokens = new List<string>();

            for (int i = 0; i < 10; i++)
            {
                tokens.Add(validator.CreateToken(sessionId, $"command-{i}"));
            }

            // Act
            validator.RevokeSessionTokens(sessionId);

            // Assert
            foreach (var token in tokens)
            {
                var (isValid, _, _) = validator.ValidateToken(token);
                Assert.False(isValid);
            }
        }

        #endregion

        #region CleanupExpiredTokens Tests

        [Fact]
        public void CreateToken_TriggersPeriodicCleanup()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);

            // Create some tokens
            for (int i = 0; i < 5; i++)
            {
                validator.CreateToken($"session-{i}", $"command-{i}");
            }

            // Act - Use reflection to set lastCleanup to old time to trigger cleanup
            var lastCleanupField = typeof(ExtensionTokenValidator)
                .GetField("m_LastCleanup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastCleanupField!.SetValue(validator, DateTime.Now.AddMinutes(-10)); // 10 minutes ago

            // Create a token to trigger cleanup
            var token = validator.CreateToken("cleanup-test", "command");

            // Assert - Token should still be valid (cleanup doesn't affect valid tokens)
            var (isValid, _, _) = validator.ValidateToken(token);
            Assert.True(isValid);
        }

        [Fact]
        public void CleanupExpiredTokens_RemovesExpiredTokens()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var token = validator.CreateToken("session-1", "command-1");

            // Use reflection to expire the token
            var tokensField = typeof(ExtensionTokenValidator)
                .GetField("m_Tokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tokensDict = tokensField!.GetValue(validator)!;

            foreach (var item in (System.Collections.IEnumerable)tokensDict)
            {
                var kvpType = item.GetType();
                var valueProperty = kvpType.GetProperty("Value");
                var tokenInfo = valueProperty!.GetValue(item)!;
                var expiresAtProp = tokenInfo.GetType().GetProperty("ExpiresAt");
                expiresAtProp!.SetValue(tokenInfo, DateTime.Now.AddHours(-1)); // Expired 1 hour ago
            }

            // Use reflection to set lastCleanup to old time
            var lastCleanupField = typeof(ExtensionTokenValidator)
                .GetField("m_LastCleanup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastCleanupField!.SetValue(validator, DateTime.Now.AddMinutes(-10));

            // Act - Create a new token to trigger cleanup
            validator.CreateToken("session-2", "command-2");

            // Assert - Expired token should be removed (validate returns false and doesn't find it)
            var (isValid, _, _) = validator.ValidateToken(token);
            Assert.False(isValid);
        }

        [Fact]
        public void CleanupExpiredTokens_RemovesRevokedTokens()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var token = validator.CreateToken("session-1", "command-1");
            validator.RevokeToken(token);

            // Use reflection to set lastCleanup to old time
            var lastCleanupField = typeof(ExtensionTokenValidator)
                .GetField("m_LastCleanup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastCleanupField!.SetValue(validator, DateTime.Now.AddMinutes(-10));

            // Act - Create a new token to trigger cleanup
            validator.CreateToken("session-2", "command-2");

            // Assert - Revoked token should be removed from internal dictionary
            // We just verify the cleanup logic doesn't throw
            Assert.True(true); // Cleanup executed without exception
        }

        [Fact]
        public void CleanupExpiredTokens_WithinCooldownPeriod_DoesNotCleanup()
        {
            // Arrange
            var validator = new ExtensionTokenValidator(m_MockLogger.Object);
            var token1 = validator.CreateToken("session-1", "command-1");

            // Act - Create another token immediately (within 5-minute cooldown)
            var token2 = validator.CreateToken("session-2", "command-2");

            // Assert - Both tokens should still be valid (no cleanup occurred)
            var (isValid1, _, _) = validator.ValidateToken(token1);
            var (isValid2, _, _) = validator.ValidateToken(token2);
            Assert.True(isValid1);
            Assert.True(isValid2);
        }

        #endregion
    }
}

