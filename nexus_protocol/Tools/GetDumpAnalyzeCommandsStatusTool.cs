using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Engine;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for getting the status of all commands in a session.
/// </summary>
[McpServerToolType]
internal static class GetDumpAnalyzeCommandsStatusTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Gets the status of all commands in a session. Efficient for monitoring multiple commands.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <returns>Array of command status information.</returns>
    [McpServerTool, Description("Gets status of all commands in a session. Use for efficient bulk monitoring.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static Task<object> nexus_get_dump_analyze_commands_status(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("GetDumpAnalyzeCommandsStatusTool");

        logger.LogInformation("Getting all command statuses for session: {SessionId}", sessionId);

        try
        {
            var allCommands = DebugEngine.Instance.GetAllCommandInfos(sessionId);

            var commandStatuses = allCommands.Values.Select(cmd => new
            {
                commandId = cmd.CommandId,
                command = cmd.Command,
                state = cmd.State.ToString(),
                queuedTime = cmd.QueuedTime,
                startTime = cmd.StartTime,
                endTime = cmd.EndTime,
                executionTime = cmd.ExecutionTime,
                totalTime = cmd.TotalTime,
                isSuccess = cmd.IsSuccess,
                hasOutput = !string.IsNullOrEmpty(cmd.Output)
            }).ToArray();

            logger.LogInformation("Retrieved status for {Count} commands in session {SessionId}", commandStatuses.Length, sessionId);

            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Status Summary");
            markdown.AppendLine();
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Total Commands:** {commandStatuses.Length}");
            markdown.AppendLine();

            if (commandStatuses.Length > 0)
            {
                markdown.AppendLine("### Commands");
                markdown.AppendLine();
                markdown.AppendLine("| Command ID | Command | State | Success | Execution Time |");
                markdown.AppendLine("|------------|---------|-------|---------|----------------|");
                
                foreach (var cmd in commandStatuses)
                {
                    var execTime = cmd.executionTime?.TotalSeconds.ToString("F2") ?? "N/A";
                    var success = cmd.isSuccess?.ToString() ?? "N/A";
                    var cmdText = cmd.command.Length > 50 ? cmd.command.Substring(0, 47) + "..." : cmd.command;
                    markdown.AppendLine($"| `{cmd.commandId}` | `{cmdText}` | {cmd.state} | {success} | {execTime}s |");
                }
            }
            else
            {
                markdown.AppendLine("No commands found.");
            }

            return Task.FromResult<object>(markdown.ToString());
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid session ID: {Message}", ex.Message);
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Status Summary");
            markdown.AppendLine();
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Total Commands:** 0");
            markdown.AppendLine();
            markdown.AppendLine("### Error");
            markdown.AppendLine("```");
            markdown.AppendLine(ex.Message);
            markdown.AppendLine("```");
            return Task.FromResult<object>(markdown.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error getting command statuses");
            var markdown = new StringBuilder();
            markdown.AppendLine("## Command Status Summary");
            markdown.AppendLine();
            markdown.AppendLine($"**Session ID:** `{sessionId}`");
            markdown.AppendLine($"**Total Commands:** 0");
            markdown.AppendLine();
            markdown.AppendLine("### Error");
            markdown.AppendLine("```");
            markdown.AppendLine($"Unexpected error: {ex.Message}");
            markdown.AppendLine("```");
            return Task.FromResult<object>(markdown.ToString());
        }
    }
}

