using NLog;

using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for enqueuing debugging commands for asynchronous execution.
/// </summary>
internal class EnqueueAsyncExtensionCommandTool
{
    /// <summary>
    /// Enqueues an extension script for asynchronous execution in the specified session.
    /// </summary>
    /// <param name="sessionId">Session ID from winaidbg_open_dump_analyze_session.</param>
    /// <param name="extensionName">Name of the extension script to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the extension script.</param>
    /// <returns>Command enqueue result with commandId.</returns>
    public async Task<object> Execute(
        string sessionId,
        string extensionName,
        object? parameters = null)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Enqueuing extension script '{ExtensionName}' in session {SessionId}", extensionName, sessionId);

        try
        {
            ToolInputValidator.EnsureNonEmpty(extensionName, "extensionName");
            ToolInputValidator.EnsureSessionIsActive(sessionId);

            var commandId = await EngineService.Get().EnqueueExtensionScriptAsync(sessionId, extensionName, parameters);

            logger.Info("Extension Script enqueued: {CommandId} in session {SessionId}", commandId, sessionId);

            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters?.ToString() ?? "None" },
                { "Status", "Queued" },
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueued",
                keyValues,
                $"Extension Script '{extensionName}' with command ID {commandId} queued successfully",
                true);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (McpToolUserInputException ex)
        {
            logger.Warn(ex, "Invalid inputs for extension enqueue");
            throw;
        }
        catch (ArgumentException ex)
        {
            var message = $"Invalid argument: {ex.Message}";
            logger.Error(ex, message);
            throw new McpToolUserInputException(message, ex);
        }
        catch (InvalidOperationException ex)
        {
            var message = $"Cannot enqueue extension script: {ex.Message}";
            logger.Error(ex, message);
            throw new McpToolUserInputException(message, ex);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error enqueuing extension script ");
            throw;
        }
    }
}
