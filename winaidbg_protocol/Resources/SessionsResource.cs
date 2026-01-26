using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using WinAiDbg.Engine.Share;

namespace WinAiDbg.Protocol.Resources;

/// <summary>
/// MCP resource for listing all active debugging sessions.
/// </summary>
[McpServerResourceType]
internal static class SessionsResource
{
    /// <summary>
    /// Creates a Markdown table for session listing.
    /// </summary>
    /// <param name="headers">The table headers.</param>
    /// <param name="rows">The table rows.</param>
    /// <returns>Markdown table.</returns>
    private static string CreateTable(string[] headers, string[][] rows)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine("| " + string.Join(" | ", headers) + " |");
        _ = sb.AppendLine("| " + string.Join(" | ", headers.Select(_ => "---")) + " |");

        foreach (var row in rows)
        {
            var padded = new string[headers.Length];
            for (var i = 0; i < headers.Length; i++)
            {
                padded[i] = i < row.Length ? row[i] : string.Empty;
            }

            _ = sb.AppendLine("| " + string.Join(" | ", padded) + " |");
        }

        return sb.ToString();
    }

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
            var engine = serviceProvider.GetRequiredService<IDebugEngine>();

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

            _ = md.Append(CreateTable(headers, rows));
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
