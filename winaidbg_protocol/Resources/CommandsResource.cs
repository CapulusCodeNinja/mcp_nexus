using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using WinAiDbg.Engine.Share;

namespace WinAiDbg.Protocol.Resources;

/// <summary>
/// MCP resource for listing commands from all active sessions.
/// </summary>
[McpServerResourceType]
internal static class CommandsResource
{
    /// <summary>
    /// Creates a Markdown table.
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
    /// Truncates long values for table display.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>Truncated string.</returns>
    private static string Truncate(string value, int maxLength)
    {
        return string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Lists commands from all active sessions with status information.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <returns>Markdown containing command information.</returns>
    [McpServerResource]
    [Description("Lists commands from all active sessions with status information.")]
    public static Task<string> Commands(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        try
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CommandsResource");
            var engine = serviceProvider.GetRequiredService<IDebugEngine>();

            logger.LogDebug("Commands resource accessed");

            var sessionIds = engine.GetActiveSessions();
            var commandStatuses = sessionIds
                .SelectMany(sessionId => engine.GetAllCommandInfos(sessionId).Values.Select(cmd => new
                {
                    sessionId,
                    commandId = cmd.CommandId,
                    command = cmd.Command,
                    state = cmd.State.ToString(),
                    queuedTime = cmd.QueuedTime,
                    startTime = cmd.StartTime,
                    endTime = cmd.EndTime,
                    executionTime = cmd.ExecutionTime,
                    totalTime = cmd.TotalTime,
                    isSuccess = cmd.IsSuccess,
                    errorMessage = cmd.ErrorMessage,
                    readCount = cmd.ReadCount,
                }))
                .OrderBy(c => c.queuedTime)
                .ToArray();

            var md = new StringBuilder();
            _ = md.AppendLine("## Commands");
            _ = md.AppendLine();
            _ = md.AppendLine($"**Sessions:** {sessionIds.Count}");
            _ = md.AppendLine($"**Count:** {commandStatuses.Length}");
            _ = md.AppendLine($"**Timestamp:** {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
            _ = md.AppendLine();

            if (commandStatuses.Length == 0)
            {
                _ = md.AppendLine("No commands found.");
                return Task.FromResult(md.ToString());
            }

            var headers = new[] { "Session ID", "Command ID", "Command", "State", "Success", "Queued" };
            var rows = commandStatuses.Select(c => new[]
            {
                c.sessionId,
                c.commandId,
                Truncate(c.command ?? string.Empty, 60),
                c.state,
                c.isSuccess?.ToString() ?? "N/A",
                c.queuedTime.ToString("yyyy-MM-dd HH:mm:ss"),
            }).ToArray();

            _ = md.Append(CreateTable(headers, rows));
            return Task.FromResult(md.ToString());
        }
        catch (Exception ex)
        {
            var md = new StringBuilder();
            _ = md.AppendLine("## Commands");
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
