using NLog;

using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for closing a debugging session and releasing resources.
/// </summary>
internal class CloseDumpAnalyzeSessionTool
{
    /// <summary>
    /// Closes a debugging session and releases all associated resources.
    /// </summary>
    /// <param name="sessionId">Session ID from winaidbg_open_dump_analyze_session.</param>
    /// <returns>Session closure result.</returns>
    public async Task<object> Execute(
        string sessionId)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Closing debugging session: {SessionId}", sessionId);

        try
        {
            await EngineService.Get().CloseSessionAsync(sessionId);

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
