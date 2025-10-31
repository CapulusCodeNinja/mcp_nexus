using System.ComponentModel;

using ModelContextProtocol.Server;

using Nexus.Engine;
using Nexus.Protocol.Utilities;

using NLog;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for closing a debugging session and releasing resources.
/// </summary>
[McpServerToolType]
internal static class CloseDumpAnalyzeSessionTool
{
    /// <summary>
    /// Closes a debugging session and releases all associated resources.
    /// </summary>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <returns>Session closure result.</returns>
    [McpServerTool]
    [Description("Closes a debugging session and releases resources.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Required for interoperability with external system")]
    public static async Task<object> nexus_close_dump_analyze_session(
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Closing debugging session: {SessionId}", sessionId);

        try
        {
            await DebugEngine.Instance.CloseSessionAsync(sessionId);

            logger.Info("Successfully closed session: {SessionId}", sessionId);

            var keyValues = new Dictionary<string, object?>
            {
                { "Session ID", sessionId },
                { "Status", "Success" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Session Closed",
                keyValues,
                $"Session {sessionId} closed successfully",
                true);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (ArgumentException ex)
        {
            logger.Error(ex, "Invalid session ID: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Session ID", sessionId },
                { "Status", "Failed" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Session Close Failed",
                keyValues,
                ex.Message,
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error closing session");
            var keyValues = new Dictionary<string, object?>
            {
                { "Session ID", sessionId },
                { "Status", "Failed" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Session Close Failed",
                keyValues,
                $"Unexpected error: {ex.Message}",
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
    }
}
