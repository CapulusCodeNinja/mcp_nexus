using System.ComponentModel;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

namespace Nexus.Protocol.Resources;

/// <summary>
/// MCP resource for listing commands from all active sessions.
/// </summary>
/// <remarks>
/// Current Limitations:
/// - IDebugEngine lacks GetActiveSessions() method
/// - Cannot list commands without session enumeration
/// 
/// Future Enhancement:
/// - Add IEnumerable&lt;string&gt; GetActiveSessions() to IDebugEngine
/// - Add Dictionary&lt;string, CommandInfo&gt; GetAllCommands() to IDebugEngine (optional, for efficiency)
/// </remarks>
[McpServerResourceType]
internal static class CommandsResource
{
    /// <summary>
    /// Lists commands from all active sessions with status information.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <returns>JSON string containing command information.</returns>
    /// <remarks>
    /// LIMITATION: Currently returns empty list because IDebugEngine interface
    /// does not expose methods to enumerate sessions or query commands globally.
    /// 
    /// To implement full functionality, IDebugEngine needs:
    /// 1. IEnumerable&lt;string&gt; GetActiveSessions()
    /// 2. Dictionary&lt;string, CommandInfo&gt; GetAllCommands() (optional, for efficiency)
    /// </remarks>
    [McpServerResource, Description("Lists commands from all sessions. Note: Requires IDebugEngine enhancement for full functionality.")]
    public static Task<string> Commands(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        try
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CommandsResource");

            logger.LogDebug("Commands resource accessed - returning limited data due to IDebugEngine interface constraints");

            var result = new
            {
                commands = Array.Empty<object>(),
                count = 0,
                timestamp = DateTimeOffset.Now,
                note = "Command listing requires IDebugEngine.GetActiveSessions() method (pending interface enhancement)"
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
                commands = Array.Empty<object>(),
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

