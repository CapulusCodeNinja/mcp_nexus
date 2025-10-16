using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Extensions
{
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

    /// <summary>
    /// Implementation of extension token validator.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ExtensionTokenValidator"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public class ExtensionTokenValidator(ILogger<ExtensionTokenValidator> logger) : IExtensionTokenValidator
    {
        private readonly ILogger<ExtensionTokenValidator> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentDictionary<string, ExtensionTokenInfo> m_Tokens = new();
        private readonly object m_CleanupLock = new();
        private DateTime m_LastCleanup = DateTime.Now;

        /// <summary>
        /// Creates and registers a new extension token.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="commandId">The command ID.</param>
        /// <returns>A secure token string.</returns>
        /// <exception cref="ArgumentException">Thrown when session ID or command ID is invalid.</exception>
        public string CreateToken(string sessionId, string commandId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

            // Generate secure random token
            var tokenBytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(tokenBytes);
            var randomPart = Convert.ToBase64String(tokenBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "");

            var token = $"ext_{randomPart}";

            var tokenInfo = new ExtensionTokenInfo
            {
                Token = token,
                SessionId = sessionId,
                CommandId = commandId,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(2), // 2 hour expiration
                IsRevoked = false
            };

            m_Tokens[token] = tokenInfo;

            m_Logger.LogDebug("Created extension token for session {SessionId}, command {CommandId}",
                sessionId, commandId);

            // Periodic cleanup of expired tokens
            CleanupExpiredTokens();

            return token;
        }

        /// <summary>
        /// Validates an extension token and returns associated session and command IDs.
        /// </summary>
        /// <param name="token">The token to validate.</param>
        /// <returns>A tuple containing validation result, session ID, and command ID.</returns>
        public (bool isValid, string? sessionId, string? commandId) ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                m_Logger.LogWarning("Token validation failed: Token is null or empty");
                return (false, null, null);
            }

            if (!m_Tokens.TryGetValue(token, out var tokenInfo))
            {
                m_Logger.LogWarning("Token validation failed: Token not found");
                return (false, null, null);
            }

            if (tokenInfo.IsRevoked)
            {
                m_Logger.LogWarning("Token validation failed: Token is revoked for session {SessionId}",
                    tokenInfo.SessionId);
                return (false, null, null);
            }

            if (tokenInfo.ExpiresAt < DateTime.Now)
            {
                m_Logger.LogWarning("Token validation failed: Token expired for session {SessionId}",
                    tokenInfo.SessionId);
                m_Tokens.TryRemove(token, out _);
                return (false, null, null);
            }

            m_Logger.LogDebug("Token validated successfully for session {SessionId}, command {CommandId}",
                tokenInfo.SessionId, tokenInfo.CommandId);

            return (true, tokenInfo.SessionId, tokenInfo.CommandId);
        }

        /// <summary>
        /// Revokes a token, making it invalid for future use.
        /// </summary>
        /// <param name="token">The token to revoke.</param>
        public void RevokeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            if (m_Tokens.TryGetValue(token, out var tokenInfo))
            {
                tokenInfo.IsRevoked = true;
                m_Logger.LogInformation("Revoked token for session {SessionId}, command {CommandId}",
                    tokenInfo.SessionId, tokenInfo.CommandId);
            }
        }

        /// <summary>
        /// Revokes all tokens for a given session.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        public void RevokeSessionTokens(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            var count = 0;
            foreach (var kvp in m_Tokens)
            {
                if (kvp.Value.SessionId == sessionId)
                {
                    kvp.Value.IsRevoked = true;
                    count++;
                }
            }

            if (count > 0)
            {
                m_Logger.LogInformation("Revoked {Count} tokens for session {SessionId}", count, sessionId);
            }
        }

        /// <summary>
        /// Cleans up expired tokens periodically.
        /// </summary>
        private void CleanupExpiredTokens()
        {
            lock (m_CleanupLock)
            {
                // Only cleanup every 5 minutes
                if ((DateTime.Now - m_LastCleanup).TotalMinutes < 5)
                    return;

                var removed = 0;
                foreach (var kvp in m_Tokens)
                {
                    var info = kvp.Value;
                    if (info.ExpiresAt < DateTime.Now || info.IsRevoked)
                    {
                        if (m_Tokens.TryRemove(kvp.Key, out _))
                        {
                            removed++;
                        }
                    }
                }

                if (removed > 0)
                {
                    m_Logger.LogDebug("Cleaned up {Count} expired/revoked tokens", removed);
                }

                m_LastCleanup = DateTime.Now;
            }
        }
    }

    /// <summary>
    /// Information about an extension token.
    /// </summary>
    internal class ExtensionTokenInfo
    {
        /// <summary>
        /// The token string.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Session ID associated with this token.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Command ID associated with this token.
        /// </summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// When the token was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the token expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Whether the token has been revoked.
        /// </summary>
        public bool IsRevoked { get; set; }
    }
}

