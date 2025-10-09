using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Extensions;
using Xunit;

namespace mcp_nexus_tests.Extensions
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

            await Task.WhenAll(tasks.ToArray());

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
            await Task.WhenAll(tasks.ToArray());

            // Assert - All tokens should be invalid
            foreach (var token in tokens)
            {
                var (isValid, _, _) = validator.ValidateToken(token);
                Assert.False(isValid);
            }
        }
    }
}

