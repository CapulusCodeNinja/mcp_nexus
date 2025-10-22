namespace nexus.extensions;

/// <summary>
/// Validates extension callback tokens for secure authentication.
/// </summary>
public interface IExtensionTokenValidator
{
    /// <summary>
    /// Creates and registers a new extension token.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <returns>A secure token string.</returns>
    string CreateToken(string sessionId, string commandId);

    /// <summary>
    /// Validates an extension token and returns associated session and command IDs.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>A tuple containing validation result, session ID, and command ID.</returns>
    (bool isValid, string? sessionId, string? commandId) ValidateToken(string token);

    /// <summary>
    /// Revokes a token, making it invalid for future use.
    /// </summary>
    /// <param name="token">The token to revoke.</param>
    void RevokeToken(string token);

    /// <summary>
    /// Revokes all tokens for a given session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    void RevokeSessionTokens(string sessionId);
}

