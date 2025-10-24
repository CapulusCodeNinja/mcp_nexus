using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Nexus.Extensions.Security;

using NLog;

namespace Nexus.Extensions_unittests.Security;

/// <summary>
/// Unit tests for ExtensionTokenValidator.
/// </summary>
public class ExtensionTokenValidatorTests
{
    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ExtensionTokenValidator());
    }

    /// <summary>
    /// Verifies CreateToken throws when session ID is null or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateToken_ThrowsArgumentException_WhenSessionIdIsNullOrEmpty(string? sessionId)
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.CreateToken(sessionId!, "cmd-123"));
    }

    /// <summary>
    /// Verifies CreateToken throws when command ID is null or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateToken_ThrowsArgumentException_WhenCommandIdIsNullOrEmpty(string? commandId)
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.CreateToken("session-123", commandId!));
    }

    /// <summary>
    /// Verifies CreateToken generates valid token.
    /// </summary>
    [Fact]
    public void CreateToken_GeneratesValidToken()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var token = validator.CreateToken("session-123", "cmd-123");

        // Assert
        Assert.NotNull(token);
        Assert.StartsWith("ext_", token);
        Assert.True(token.Length > 10);
    }

    /// <summary>
    /// Verifies ValidateToken returns true for valid token.
    /// </summary>
    [Fact]
    public void ValidateToken_ReturnsTrue_ForValidToken()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token = validator.CreateToken("session-123", "cmd-123");

        // Act
        var (isValid, sessionId, commandId) = validator.ValidateToken(token);

        // Assert
        Assert.True(isValid);
        Assert.Equal("session-123", sessionId);
        Assert.Equal("cmd-123", commandId);
    }

    /// <summary>
    /// Verifies ValidateToken returns false for null or empty token.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateToken_ReturnsFalse_ForNullOrEmptyToken(string? token)
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var (isValid, _, _) = validator.ValidateToken(token!);

        // Assert
        Assert.False(isValid);
    }

    /// <summary>
    /// Verifies ValidateToken returns false for non-existent token.
    /// </summary>
    [Fact]
    public void ValidateToken_ReturnsFalse_ForNonExistentToken()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var (isValid, _, _) = validator.ValidateToken("ext_nonexistent");

        // Assert
        Assert.False(isValid);
    }

    /// <summary>
    /// Verifies ValidateToken returns false for revoked token.
    /// </summary>
    [Fact]
    public void ValidateToken_ReturnsFalse_ForRevokedToken()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token = validator.CreateToken("session-123", "cmd-123");
        validator.RevokeToken(token);

        // Act
        var (isValid, _, _) = validator.ValidateToken(token);

        // Assert
        Assert.False(isValid);
    }

    /// <summary>
    /// Verifies RevokeToken revokes token.
    /// </summary>
    [Fact]
    public void RevokeToken_RevokesToken()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token = validator.CreateToken("session-123", "cmd-123");

        // Act
        validator.RevokeToken(token);
        var (isValid, _, _) = validator.ValidateToken(token);

        // Assert
        Assert.False(isValid);
    }

    /// <summary>
    /// Verifies RevokeSessionTokens revokes all tokens for session.
    /// </summary>
    [Fact]
    public void RevokeSessionTokens_RevokesAllTokensForSession()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();
        var token1 = validator.CreateToken("session-123", "cmd-1");
        var token2 = validator.CreateToken("session-123", "cmd-2");
        var token3 = validator.CreateToken("session-456", "cmd-3");

        // Act
        validator.RevokeSessionTokens("session-123");

        // Assert
        Assert.False(validator.ValidateToken(token1).isValid);
        Assert.False(validator.ValidateToken(token2).isValid);
        Assert.True(validator.ValidateToken(token3).isValid);
    }

    /// <summary>
    /// Verifies CreateToken generates unique tokens.
    /// </summary>
    [Fact]
    public void CreateToken_GeneratesUniqueTokens()
    {
        // Arrange
        var validator = new ExtensionTokenValidator();

        // Act
        var token1 = validator.CreateToken("session-123", "cmd-1");
        var token2 = validator.CreateToken("session-123", "cmd-2");

        // Assert
        Assert.NotEqual(token1, token2);
    }
}

