using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Resources;

/// <summary>
/// MCP resource for listing all active debugging sessions.
/// </summary>
[McpServerResourceType]
internal static class SessionsResource
{
    /// <summary>
    /// Lists all active debugging sessions with status information.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <returns>Markdown containing session information.</returns>
    [McpServerResource]
    [Description("Lists all active debugging sessions with basic status information.")]
    public static Task<string> Sessions(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        try
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SessionsResource");
            var engine = EngineService.Get();

            logger.LogDebug("Sessions resource accessed");

            var sessionIds = engine.GetActiveSessions();
            var sessions = sessionIds.Select(sessionId => new
            {
                sessionId,
                state = engine.GetSessionState(sessionId)?.ToString() ?? "Unknown",
                isActive = engine.IsSessionActive(sessionId),
            }).ToArray();

            var md = new StringBuilder();
            _ = md.AppendLine("## Sessions");
            _ = md.AppendLine();
            _ = md.AppendLine($"**Count:** {sessions.Length}");
            _ = md.AppendLine($"**Timestamp:** {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
            _ = md.AppendLine();

            if (sessions.Length == 0)
            {
                _ = md.AppendLine("No active sessions.");
                return Task.FromResult(md.ToString());
            }

            var headers = new[] { "Session ID", "State", "Active" };
            var rows = sessions.Select(s => new[]
            {
                s.sessionId,
                s.state,
                s.isActive ? "Yes" : "No",
            }).ToArray();

            _ = md.Append(MarkdownFormatter.CreateTable(headers, rows));
            return Task.FromResult(md.ToString());
        }
        catch (Exception ex)
        {
            var md = new StringBuilder();
            _ = md.AppendLine("## Sessions");
            _ = md.AppendLine();
            _ = md.AppendLine("**Status:** Error");
            _ = md.AppendLine();
            _ = md.AppendLine("```");
            _ = md.AppendLine(ex.Message);
            _ = md.AppendLine("```");
            return Task.FromResult(md.ToString());
        }
    }
}
