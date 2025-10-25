using System.Security.Cryptography;

using NLog;

namespace Nexus.Engine.Extensions.Security;

/// <summary>
/// Validates security tokens for extension script callbacks.
/// </summary>
public class ExtensionTokenValidator
{
    private readonly Logger m_Logger;
    private readonly Dictionary<string, TokenInfo> m_ValidTokens = new();
    private readonly object m_Lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionTokenValidator"/> class.
    /// </summary>
    public ExtensionTokenValidator()
    {
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Generates a new security token for an extension script.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="validityMinutes">The validity period in minutes.</param>
    /// <returns>A new security token.</returns>
    public string GenerateToken(string sessionId, string commandId, int validityMinutes = 60)
    {
        var token = GenerateSecureToken();
        var expiresAt = DateTime.Now.AddMinutes(validityMinutes);

        lock (m_Lock)
        {
            m_ValidTokens[token] = new TokenInfo
            {
                SessionId = sessionId,
                CommandId = commandId,
                ExpiresAt = expiresAt
            };
        }

        m_Logger.Debug("Generated token for session {SessionId}, command {CommandId}, expires at {ExpiresAt}",
            sessionId, commandId, expiresAt);

        return token;
    }

    /// <summary>
    /// Validates a security token and extracts session information.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>A tuple containing validation result, session ID, and command ID.</returns>
    public (bool IsValid, string? SessionId, string? CommandId) ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return (false, null, null);
        }

        lock (m_Lock)
        {
            if (!m_ValidTokens.TryGetValue(token, out var tokenInfo))
            {
                m_Logger.Warn("Invalid token provided: {Token}", token);
                return (false, null, null);
            }

            if (DateTime.Now > tokenInfo.ExpiresAt)
            {
                m_Logger.Warn("Expired token provided: {Token}, expired at {ExpiresAt}", token, tokenInfo.ExpiresAt);
                _ = m_ValidTokens.Remove(token);
                return (false, null, null);
            }

            m_Logger.Debug("Valid token for session {SessionId}, command {CommandId}",
                tokenInfo.SessionId, tokenInfo.CommandId);

            return (true, tokenInfo.SessionId, tokenInfo.CommandId);
        }
    }

    /// <summary>
    /// Revokes a security token.
    /// </summary>
    /// <param name="token">The token to revoke.</param>
    public void RevokeToken(string token)
    {
        lock (m_Lock)
        {
            if (m_ValidTokens.Remove(token))
            {
                m_Logger.Debug("Revoked token: {Token}", token);
            }
        }
    }

    /// <summary>
    /// Cleans up expired tokens.
    /// </summary>
    public void CleanupExpiredTokens()
    {
        var now = DateTime.Now;
        var expiredTokens = new List<string>();

        lock (m_Lock)
        {
            foreach (var kvp in m_ValidTokens)
            {
                if (now > kvp.Value.ExpiresAt)
                {
                    expiredTokens.Add(kvp.Key);
                }
            }

            foreach (var token in expiredTokens)
            {
                _ = m_ValidTokens.Remove(token);
            }
        }

        if (expiredTokens.Count > 0)
        {
            m_Logger.Debug("Cleaned up {Count} expired tokens", expiredTokens.Count);
        }
    }

    /// <summary>
    /// Generates a secure random token.
    /// </summary>
    /// <returns>A secure random token string.</returns>
    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    /// <summary>
    /// Information about a token.
    /// </summary>
    private class TokenInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public string CommandId { get; set; } = string.Empty;
        public DateTime ExpiresAt
        {
            get; set;
        }
    }
}


