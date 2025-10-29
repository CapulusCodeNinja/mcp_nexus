using System.ComponentModel;

using ModelContextProtocol.Server;

using Nexus.Engine;
using Nexus.Protocol.Utilities;

using NLog;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for enqueuing debugging commands for asynchronous execution.
/// </summary>
[McpServerToolType]
internal static class EnqueueAsyncExtensionCommandTool
{
    /// <summary>
    /// Enqueues an extension script for asynchronous execution in the specified session.
    /// </summary>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="extensionName">Name of the extension script to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the extension script.</param>
    /// <returns>Command enqueue result with commandId.</returns>
    [McpServerTool, Description("Enqueues an extension script for asynchronous execution. Returns commandId for tracking.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static async Task<object> nexus_enqueue_async_extension_command(
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("Name of the extension script to execute")] string extensionName,
        [Description("Optional parameters to pass to the extension script")] object? parameters = null)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info("Enqueuing extension script '{ExtensionName}' in session {SessionId}", extensionName, sessionId);

        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException("sessionId cannot be empty", nameof(sessionId));
            }

            if (string.IsNullOrWhiteSpace(extensionName))
            {
                throw new ArgumentException("extensionName cannot be empty", nameof(extensionName));
            }

            var commandId = await DebugEngine.Instance.EnqueueExtensionScriptAsync(sessionId, extensionName, parameters);

            logger.Info("Extension Script enqueued: {CommandId} in session {SessionId}", commandId, sessionId);

            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters?.ToString() ?? "None" },
                { "Status", "Queued" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueued",
                keyValues,
                $"Extension Script '{extensionName}' with command ID {commandId} queued successfully",
                true);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (ArgumentException ex)
        {
            logger.Error(ex, "Invalid argument: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters?.ToString() ?? "None" },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueue Failed",
                keyValues,
                ex.Message,
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (InvalidOperationException ex)
        {
            logger.Error(ex, "Cannot enqueue  extension script: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters?.ToString() ?? "None" },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueue Failed",
                keyValues,
                ex.Message,
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error enqueuing extension script ");
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters?.ToString() ?? "None" },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Script Enqueue Failed",
                keyValues,
                $"Unexpected error: {ex.Message}",
                false);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
    }
}

