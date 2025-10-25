using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Config;
using Nexus.Extensions;
using Nexus.Extensions.Configuration;
using Nexus.Extensions.Core;
using Nexus.Extensions.Infrastructure;
using Nexus.Extensions.Security;
using Nexus.Protocol.Utilities;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for enqueuing extension commands for asynchronous execution.
/// </summary>
[McpServerToolType]
internal static class EnqueueAsyncExtensionCommandTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Enqueues an extension command for asynchronous execution in the specified session.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <param name="extensionName">Name of the extension to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the extension (JSON object).</param>
    /// <returns>Extension command enqueue result with commandId.</returns>
    [McpServerTool, Description("Enqueues an extension command for asynchronous execution. Returns commandId for tracking.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static async Task<object> nexus_enqueue_async_extension_command(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
        [Description("Name of the extension to execute")] string extensionName,
        [Description("Optional parameters to pass to the extension (JSON object)")] object? parameters = null)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EnqueueAsyncExtensionCommandTool");

        logger.LogInformation("Enqueuing extension command in session {SessionId}: {ExtensionName}", sessionId, extensionName);

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

            // Check if extensions are enabled
            var settings = Settings.GetInstance().Get();
            if (!settings.McpNexus.Extensions.Enabled)
            {
                throw new InvalidOperationException("Extensions are disabled in configuration");
            }

            // Create extension services manually
            var extensionsPath = settings.McpNexus.Extensions.ExtensionsPath ?? "extensions";
            var extensionManager = new ExtensionManager(extensionsPath);
            var callbackUrl = $"http://127.0.0.1:{settings.McpNexus.Extensions.CallbackPort}";
            var configuration = new ExtensionConfiguration();
            var tokenValidator = new ExtensionTokenValidator();
            var processWrapper = new ProcessWrapper();
            
            var extensionExecutor = new ExtensionExecutor(
                extensionManager,
                callbackUrl,
                configuration,
                tokenValidator,
                processWrapper);

            // Generate command ID with extension prefix
            var commandId = $"ext-{Guid.NewGuid():N}";

            // Execute extension asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await extensionExecutor.ExecuteAsync(
                        extensionName,
                        sessionId,
                        parameters,
                        commandId,
                        progressCallback: null,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Extension execution failed for {CommandId}: {Message}", commandId, ex.Message);
                }
                finally
                {
                    extensionExecutor.Dispose();
                }
            });

            logger.LogInformation("Extension command enqueued: {CommandId} in session {SessionId}", commandId, sessionId);

            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", commandId },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters },
                { "Status", "Queued" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Command Enqueued",
                keyValues,
                $"Extension command {commandId} queued successfully",
                true);

            return markdown;
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Command Enqueue Failed",
                keyValues,
                ex.Message,
                false);

            return markdown;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Cannot enqueue extension command: {Message}", ex.Message);
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Command Enqueue Failed",
                keyValues,
                ex.Message,
                false);

            return markdown;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error enqueuing extension command");
            var keyValues = new Dictionary<string, object?>
            {
                { "Command ID", "N/A" },
                { "Session ID", sessionId },
                { "Extension Name", extensionName },
                { "Parameters", parameters },
                { "Status", "Failed" }
            };

            var markdown = MarkdownFormatter.CreateOperationResult(
                "Extension Command Enqueue Failed",
                keyValues,
                $"Unexpected error: {ex.Message}",
                false);

            return markdown;
        }
    }
}
