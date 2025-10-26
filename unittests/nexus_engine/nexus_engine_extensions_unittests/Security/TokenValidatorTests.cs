using FluentAssertions;

using Nexus.Engine.Extensions.Security;

namespace Nexus.Engine.Extensions.Tests.Security;

/// <summary>
/// Unit tests for the <see cref="TokenValidator"/> class.
/// </summary>
public class TokenValidatorTests : IDisposable
{
    private readonly TokenValidator m_Validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenValidatorTests"/> class.
    /// </summary>
    public TokenValidatorTests()
    {
        m_Validator = new TokenValidator();
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        m_Validator.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GenerateToken Tests

    /// <summary>
    /// Verifies that GenerateToken returns a non-empty token.
    /// </summary>
    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        // Act
        var token = m_Validator.GenerateToken("session-1", "cmd-1");

        // Assert
        _ = token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that GenerateToken generates unique tokens.
    /// </summary>
    [Fact]
    public void GenerateToken_GeneratesUniqueTokens()
    {
        // Act
        var token1 = m_Validator.GenerateToken("session-1", "cmd-1");
        var token2 = m_Validator.GenerateToken("session-1", "cmd-2");

        // Assert
        _ = token1.Should().NotBe(token2);
    }

    /// <summary>
    /// Verifies that GenerateToken creates a URL-safe token.
    /// </summary>
    [Fact]
    public void GenerateToken_CreatesUrlSafeToken()
    {
        // Act
        var token = m_Validator.GenerateToken("session-1", "cmd-1");

        // Assert
        _ = token.Should().NotContain("+");
        _ = token.Should().NotContain("/");
        _ = token.Should().NotContain("=");
    }

    /// <summary>
    /// Verifies that GenerateToken with custom validity minutes is accepted.
    /// </summary>
    [Fact]
    public void GenerateToken_WithCustomValidityMinutes_Succeeds()
    {
        // Act
        var token = m_Validator.GenerateToken("session-1", "cmd-1", validityMinutes: 30);

        // Assert
        _ = token.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ValidateToken Tests

    /// <summary>
    /// Verifies that ValidateToken returns valid for freshly generated token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithFreshToken_ReturnsValid()
    {
        // Arrange
        var token = m_Validator.GenerateToken("session-1", "cmd-1");

        // Act
        var (isValid, sessionId, commandId) = m_Validator.ValidateToken(token);

        // Assert
        _ = isValid.Should().BeTrue();
        _ = sessionId.Should().Be("session-1");
        _ = commandId.Should().Be("cmd-1");
    }

    /// <summary>
    /// Verifies that ValidateToken returns invalid for null token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithNullToken_ReturnsInvalid()
    {
        // Act
        var (isValid, sessionId, commandId) = m_Validator.ValidateToken(null!);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = sessionId.Should().BeNull();
        _ = commandId.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ValidateToken returns invalid for empty token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithEmptyToken_ReturnsInvalid()
    {
        // Act
        var (isValid, sessionId, commandId) = m_Validator.ValidateToken(string.Empty);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = sessionId.Should().BeNull();
        _ = commandId.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ValidateToken returns invalid for unknown token.
    /// </summary>
    [Fact]
    public void ValidateToken_WithUnknownToken_ReturnsInvalid()
    {
        // Act
        var (isValid, sessionId, commandId) = m_Validator.ValidateToken("unknown-token");

        // Assert
        _ = isValid.Should().BeFalse();
        _ = sessionId.Should().BeNull();
        _ = commandId.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ValidateToken can validate multiple different tokens.
    /// </summary>
    [Fact]
    public void ValidateToken_WithMultipleTokens_ValidatesCorrectly()
    {
        // Arrange
        var token1 = m_Validator.GenerateToken("session-1", "cmd-1");
        var token2 = m_Validator.GenerateToken("session-2", "cmd-2");

        // Act
        var (isValid1, sessionId1, commandId1) = m_Validator.ValidateToken(token1);
        var (isValid2, sessionId2, commandId2) = m_Validator.ValidateToken(token2);

        // Assert
        _ = isValid1.Should().BeTrue();
        _ = sessionId1.Should().Be("session-1");
        _ = commandId1.Should().Be("cmd-1");

        _ = isValid2.Should().BeTrue();
        _ = sessionId2.Should().Be("session-2");
        _ = commandId2.Should().Be("cmd-2");
    }

    #endregion

    #region RevokeToken Tests

    /// <summary>
    /// Verifies that RevokeToken invalidates a valid token.
    /// </summary>
    [Fact]
    public void RevokeToken_InvalidatesValidToken()
    {
        // Arrange
        var token = m_Validator.GenerateToken("session-1", "cmd-1");

        // Act
        m_Validator.RevokeToken(token);
        var (isValid, _, _) = m_Validator.ValidateToken(token);

        // Assert
        _ = isValid.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RevokeToken handles unknown token gracefully.
    /// </summary>
    [Fact]
    public void RevokeToken_WithUnknownToken_DoesNotThrow()
    {
        // Act
        var act = () => m_Validator.RevokeToken("unknown-token");

        // Assert
        _ = act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that RevokeToken handles null token gracefully.
    /// </summary>
    [Fact]
    public void RevokeToken_WithNullToken_DoesNotThrow()
    {
        // Act
        var act = () => m_Validator.RevokeToken(null!);

        // Assert
        _ = act.Should().NotThrow();
    }

    #endregion

    #region CleanupExpiredTokens Tests

    /// <summary>
    /// Verifies that CleanupExpiredTokens removes expired tokens.
    /// </summary>
    [Fact]
    public void CleanupExpiredTokens_RemovesExpiredTokens()
    {
        // Arrange
        var token = m_Validator.GenerateToken("session-1", "cmd-1", validityMinutes: 0);
        Thread.Sleep(10); // Wait for token to expire

        // Act
        m_Validator.CleanupExpiredTokens();
        var (isValid, _, _) = m_Validator.ValidateToken(token);

        // Assert
        _ = isValid.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CleanupExpiredTokens does not affect valid tokens.
    /// </summary>
    [Fact]
    public void CleanupExpiredTokens_DoesNotAffectValidTokens()
    {
        // Arrange
        var token = m_Validator.GenerateToken("session-1", "cmd-1", validityMinutes: 60);

        // Act
        m_Validator.CleanupExpiredTokens();
        var (isValid, _, _) = m_Validator.ValidateToken(token);

        // Assert
        _ = isValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CleanupExpiredTokens can be called multiple times.
    /// </summary>
    [Fact]
    public void CleanupExpiredTokens_CanBeCalledMultipleTimes()
    {
        // Arrange
        _ = m_Validator.GenerateToken("session-1", "cmd-1", validityMinutes: 60);

        // Act
        var act = () =>
        {
            m_Validator.CleanupExpiredTokens();
            m_Validator.CleanupExpiredTokens();
            m_Validator.CleanupExpiredTokens();
        };

        // Assert
        _ = act.Should().NotThrow();
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies that Dispose can be called multiple times.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var validator = new TokenValidator();

        // Act
        var act = () =>
        {
            validator.Dispose();
            validator.Dispose();
        };

        // Assert
        _ = act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that operations after Dispose do not throw.
    /// </summary>
    [Fact]
    public void Operations_AfterDispose_DoNotThrow()
    {
        // Arrange
        var validator = new TokenValidator();
        var token = validator.GenerateToken("session-1", "cmd-1");

        // Act
        validator.Dispose();
        var actValidate = () => validator.ValidateToken(token);
        var actRevoke = () => validator.RevokeToken(token);
        var actCleanup = () => validator.CleanupExpiredTokens();

        // Assert
        _ = actValidate.Should().NotThrow();
        _ = actRevoke.Should().NotThrow();
        _ = actCleanup.Should().NotThrow();
    }

    #endregion

    #region Thread Safety Tests

    /// <summary>
    /// Verifies that concurrent token generation is thread-safe.
    /// </summary>
    [Fact]
    public async Task GenerateToken_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int tokensPerThread = 10;

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(i =>
            Task.Run(() =>
            {
                var tokens = new List<string>();
                for (var j = 0; j < tokensPerThread; j++)
                {
                    tokens.Add(m_Validator.GenerateToken($"session-{i}", $"cmd-{j}"));
                }
                return tokens;
            })
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        var allTokens = results.SelectMany(t => t).ToList();
        _ = allTokens.Should().HaveCount(threadCount * tokensPerThread);
        _ = allTokens.Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Verifies that concurrent validation is thread-safe.
    /// </summary>
    [Fact]
    public async Task ValidateToken_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var token = m_Validator.GenerateToken("session-1", "cmd-1");

        // Act
        var tasks = Enumerable.Range(0, 20).Select(_ =>
            Task.Run(() => m_Validator.ValidateToken(token))
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        _ = results.Should().AllSatisfy(r =>
        {
            _ = r.IsValid.Should().BeTrue();
            _ = r.SessionId.Should().Be("session-1");
            _ = r.CommandId.Should().Be("cmd-1");
        });
    }

    /// <summary>
    /// Verifies that concurrent cleanup is thread-safe.
    /// </summary>
    [Fact]
    public async Task CleanupExpiredTokens_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            _ = m_Validator.GenerateToken($"session-{i}", $"cmd-{i}");
        }

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            Task.Run(() => m_Validator.CleanupExpiredTokens())
        ).ToArray();

        var act = async () => await Task.WhenAll(tasks);

        // Assert
        _ = await act.Should().NotThrowAsync();
    }

    #endregion
}

