using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Engine;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for closing a debugging session and releasing resources.
/// </summary>
[McpServerToolType]
internal static class CloseDumpAnalyzeSessionTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Closes a debugging session and releases all associated resources.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
    /// <returns>Session closure result.</returns>
    [McpServerTool, Description("Closes a debugging session and releases resources.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static async Task<object> nexus_close_dump_analyze_session(
        IServiceProvider serviceProvider,
        [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CloseDumpAnalyzeSessionTool");

        logger.LogInformation("Closing debugging session: {SessionId}", sessionId);

        try
        {
            await DebugEngine.Instance.CloseSessionAsync(sessionId);

            logger.LogInformation("Successfully closed session: {SessionId}", sessionId);

            return new
            {
                sessionId,
                status = "Success",
                operation = "nexus_close_dump_analyze_session",
                message = $"Session {sessionId} closed successfully",
                usage = UsageField
            };
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid session ID: {Message}", ex.Message);
            return new
            {
                sessionId,
                status = "Failed",
                operation = "nexus_close_dump_analyze_session",
                message = ex.Message,
                usage = UsageField
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error closing session");
            return new
            {
                sessionId,
                status = "Failed",
                operation = "nexus_close_dump_analyze_session",
                message = $"Unexpected error: {ex.Message}",
                usage = UsageField
            };
        }
    }
}

