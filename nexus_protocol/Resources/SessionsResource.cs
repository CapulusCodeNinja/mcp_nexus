using System.ComponentModel;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

namespace Nexus.Protocol.Resources;

/// <summary>
/// MCP resource for listing all active debugging sessions.
/// </summary>
/// <remarks>
/// Current Limitations:
/// - IDebugEngine lacks GetActiveSessions() method
/// 
/// Future Enhancement:
/// - Add IEnumerable&lt;string&gt; GetActiveSessions() to IDebugEngine
/// </remarks>
[McpServerResourceType]
internal static class SessionsResource
{
    /// <summary>
    /// Lists all active debugging sessions with status information.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <returns>JSON string containing session information.</returns>
    /// <remarks>
    /// LIMITATION: Currently returns empty list because IDebugEngine interface
    /// does not expose a method to enumerate active sessions.
    /// 
    /// To implement full functionality, IDebugEngine needs:
    /// IEnumerable&lt;string&gt; GetActiveSessions()
    /// </remarks>
    [McpServerResource, Description("Lists all active debugging sessions. Note: Requires IDebugEngine enhancement for full functionality.")]
    public static Task<string> Sessions(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        try
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SessionsResource");

            logger.LogDebug("Sessions resource accessed - returning limited data due to IDebugEngine interface constraints");

            var result = new
            {
                sessions = Array.Empty<object>(),
                count = 0,
                timestamp = DateTimeOffset.Now,
                note = "Session listing requires IDebugEngine.GetActiveSessions() method (pending interface enhancement)"
            };

            return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            // Logger might not be available if service resolution failed
            var errorResult = new
            {
                sessions = Array.Empty<object>(),
                count = 0,
                timestamp = DateTimeOffset.Now,
                error = ex.Message
            };

            return Task.FromResult(JsonSerializer.Serialize(errorResult, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    }
}

