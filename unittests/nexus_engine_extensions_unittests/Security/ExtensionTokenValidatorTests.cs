using FluentAssertions;

using Nexus.Engine.Extensions.Security;

namespace Nexus.Engine.Extensions.Tests.Security;

/// <summary>
/// Unit tests for ExtensionTokenValidator class.
/// Tests token creation, validation, expiration, and revocation logic.
/// </summary>
public class ExtensionTokenValidatorTests
{
    /// <summary>
    /// Verifies that CreateToken generates a valid token with ext_ prefix.
    /// </summary>
    [Fact]
    public void CreateToken_ShouldGenerateTokenWithPrefix()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var token = validator.CreateToken("session-1", "cmd-1");

        // Assert
        _ = token.Should().NotBeNullOrEmpty();
        _ = token.Should().StartWith("ext_");
    }

    /// <summary>
    /// Verifies that CreateToken throws ArgumentException when sessionId is null.
    /// </summary>
    [Fact]
    public void CreateToken_WithNullSessionId_ThrowsArgumentException()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => validator.CreateToken(null!, "cmd-1"));
    }

    /// <summary>
    /// Verifies that CreateToken throws ArgumentException when sessionId is empty.
    /// </summary>
    [Fact]
    public void CreateToken_WithEmptySessionId_ThrowsArgumentException()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => validator.CreateToken(string.Empty, "cmd-1"));
    }

    /// <summary>
    /// Verifies that CreateToken throws ArgumentException when commandId is null.
    /// </summary>
    [Fact]
    public void CreateToken_WithNullCommandId_ThrowsArgumentException()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => validator.CreateToken("session-1", null!));
    }

    /// <summary>
    /// Verifies that CreateToken throws ArgumentException when commandId is empty.
    /// </summary>
    [Fact]
    public void CreateToken_WithEmptyCommandId_ThrowsArgumentException()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => validator.CreateToken("session-1", string.Empty));
    }

    /// <summary>
    /// Verifies that CreateToken generates unique tokens for each call.
    /// </summary>
    [Fact]
    public void CreateToken_GeneratesUniqueTokens()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var token1 = validator.CreateToken("session-1", "cmd-1");
        var token2 = validator.CreateToken("session-1", "cmd-2");
        var token3 = validator.CreateToken("session-2", "cmd-1");

        // Assert
        _ = token1.Should().NotBe(token2);
        _ = token1.Should().NotBe(token3);
        _ = token2.Should().NotBe(token3);
    }

    /// <summary>
    /// Verifies that ValidateToken returns true for a newly created token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token = validator.CreateToken("session-1", "cmd-1");

        // Act
        var (isValid, sessionId, commandId) = validator.ValidateToken(token);

        // Assert
        _ = isValid.Should().BeTrue();
        _ = sessionId.Should().Be("session-1");
        _ = commandId.Should().Be("cmd-1");
    }

    /// <summary>
    /// Verifies that ValidateToken returns false for a null token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithNullToken_ReturnsFalse()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var (isValid, sessionId, commandId) = validator.ValidateToken(null!);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = sessionId.Should().BeNull();
        _ = commandId.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ValidateToken returns false for an empty token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var (isValid, sessionId, commandId) = validator.ValidateToken(string.Empty);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = sessionId.Should().BeNull();
        _ = commandId.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ValidateToken returns false for a non-existent token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithNonExistentToken_ReturnsFalse()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var (isValid, sessionId, commandId) = validator.ValidateToken("ext_nonexistent");

        // Assert
        _ = isValid.Should().BeFalse();
        _ = sessionId.Should().BeNull();
        _ = commandId.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ValidateToken returns correct sessionId and commandId.
    /// </summary>
    [Fact]
    public void ValidateToken_ReturnsCorrectSessionAndCommandIds()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token = validator.CreateToken("test-session", "test-command");

        // Act
        var (isValid, sessionId, commandId) = validator.ValidateToken(token);

        // Assert
        _ = isValid.Should().BeTrue();
        _ = sessionId.Should().Be("test-session");
        _ = commandId.Should().Be("test-command");
    }

    /// <summary>
    /// Verifies that RevokeToken makes a token invalid.
    /// </summary>
    [Fact]
    public void RevokeToken_MakesTokenInvalid()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token = validator.CreateToken("session-1", "cmd-1");

        // Act
        validator.RevokeToken(token);
        var (isValid, _, _) = validator.ValidateToken(token);

        // Assert
        _ = isValid.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RevokeToken handles null token gracefully.
    /// </summary>
    [Fact]
    public void RevokeToken_WithNullToken_DoesNotThrow()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert - Should not throw
        validator.RevokeToken(null!);
    }

    /// <summary>
    /// Verifies that RevokeToken handles empty token gracefully.
    /// </summary>
    [Fact]
    public void RevokeToken_WithEmptyToken_DoesNotThrow()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert - Should not throw
        validator.RevokeToken(string.Empty);
    }

    /// <summary>
    /// Verifies that RevokeToken handles non-existent token gracefully.
    /// </summary>
    [Fact]
    public void RevokeToken_WithNonExistentToken_DoesNotThrow()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert - Should not throw
        validator.RevokeToken("ext_nonexistent");
    }

    /// <summary>
    /// Verifies that RevokeSessionTokens revokes all tokens for a session.
    /// </summary>
    [Fact]
    public void RevokeSessionTokens_RevokesAllTokensForSession()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token1 = validator.CreateToken("session-1", "cmd-1");
        var token2 = validator.CreateToken("session-1", "cmd-2");
        var token3 = validator.CreateToken("session-2", "cmd-1");

        // Act
        validator.RevokeSessionTokens("session-1");

        // Assert
        var (isValid1, _, _) = validator.ValidateToken(token1);
        var (isValid2, _, _) = validator.ValidateToken(token2);
        var (isValid3, _, _) = validator.ValidateToken(token3);

        _ = isValid1.Should().BeFalse();
        _ = isValid2.Should().BeFalse();
        _ = isValid3.Should().BeTrue(); // Different session, should still be valid
    }

    /// <summary>
    /// Verifies that RevokeSessionTokens handles null sessionId gracefully.
    /// </summary>
    [Fact]
    public void RevokeSessionTokens_WithNullSessionId_DoesNotThrow()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert - Should not throw
        validator.RevokeSessionTokens(null!);
    }

    /// <summary>
    /// Verifies that RevokeSessionTokens handles empty sessionId gracefully.
    /// </summary>
    [Fact]
    public void RevokeSessionTokens_WithEmptySessionId_DoesNotThrow()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert - Should not throw
        validator.RevokeSessionTokens(string.Empty);
    }

    /// <summary>
    /// Verifies that RevokeSessionTokens handles non-existent session gracefully.
    /// </summary>
    [Fact]
    public void RevokeSessionTokens_WithNonExistentSession_DoesNotThrow()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert - Should not throw
        validator.RevokeSessionTokens("nonexistent-session");
    }

    /// <summary>
    /// Verifies that tokens from different validators are independent.
    /// </summary>
    [Fact]
    public void Tokens_FromDifferentValidators_AreIndependent()
    {
        // Arrange
        var validator1 = new ExtensionTokenValidator();
        var validator2 = new ExtensionTokenValidator();

        var token1 = validator1.CreateToken("session-1", "cmd-1");

        // Act
        var (isValid, _, _) = validator2.ValidateToken(token1);

        // Assert - Token from validator1 should not be valid in validator2
        _ = isValid.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CreateToken generates tokens without special URL characters.
    /// </summary>
    [Fact]
    public void CreateToken_GeneratesUrlSafeTokens()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var token = validator.CreateToken("session-1", "cmd-1");

        // Assert - Should not contain URL-unsafe characters
        _ = token.Should().NotContain("+");
        _ = token.Should().NotContain("/");
        _ = token.Should().NotContain("=");
    }

    /// <summary>
    /// Verifies that ValidateToken correctly handles multiple tokens for same session.
    /// </summary>
    [Fact]
    public void ValidateToken_HandlesMultipleTokensForSameSession()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token1 = validator.CreateToken("session-1", "cmd-1");
        var token2 = validator.CreateToken("session-1", "cmd-2");

        // Act
        var (isValid1, sessionId1, commandId1) = validator.ValidateToken(token1);
        var (isValid2, sessionId2, commandId2) = validator.ValidateToken(token2);

        // Assert
        _ = isValid1.Should().BeTrue();
        _ = sessionId1.Should().Be("session-1");
        _ = commandId1.Should().Be("cmd-1");

        _ = isValid2.Should().BeTrue();
        _ = sessionId2.Should().Be("session-1");
        _ = commandId2.Should().Be("cmd-2");
    }

    /// <summary>
    /// Verifies that RevokeToken only revokes the specific token, not others for same session.
    /// </summary>
    [Fact]
    public void RevokeToken_OnlyRevokesSpecificToken()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token1 = validator.CreateToken("session-1", "cmd-1");
        var token2 = validator.CreateToken("session-1", "cmd-2");

        // Act
        validator.RevokeToken(token1);

        // Assert
        var (isValid1, _, _) = validator.ValidateToken(token1);
        var (isValid2, _, _) = validator.ValidateToken(token2);

        _ = isValid1.Should().BeFalse();
        _ = isValid2.Should().BeTrue(); // Should still be valid
    }

    /// <summary>
    /// Verifies that CreateToken handles special characters in sessionId and commandId.
    /// </summary>
    [Fact]
    public void CreateToken_HandlesSpecialCharactersInIds()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var token = validator.CreateToken("session-with-dashes", "cmd_with_underscores");

        // Assert
        _ = token.Should().NotBeNullOrEmpty();
        var (isValid, sessionId, commandId) = validator.ValidateToken(token);
        _ = isValid.Should().BeTrue();
        _ = sessionId.Should().Be("session-with-dashes");
        _ = commandId.Should().Be("cmd_with_underscores");
    }
}
