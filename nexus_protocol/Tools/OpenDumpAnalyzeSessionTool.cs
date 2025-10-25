using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using Nexus.Engine;
using Nexus.External.Apis.FileSystem;
using Nexus.Protocol.Utilities;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for opening a new debugging session for crash dump analysis.
/// </summary>
[McpServerToolType]
internal static class OpenDumpAnalyzeSessionTool
{
    private const string UsageField = "MCP Nexus Tool - See tool description for usage details";

    /// <summary>
    /// Opens a new debugging session for analyzing a crash dump file.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="dumpPath">Full path to the crash dump file (.dmp).</param>
    /// <param name="symbolsPath">Optional path to symbol files directory.</param>
    /// <returns>Session creation result with sessionId.</returns>
    [McpServerTool, Description("Opens a new debugging session for crash dump analysis. Returns sessionId for subsequent operations.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    public static async Task<object> nexus_open_dump_analyze_session(
        IServiceProvider serviceProvider,
        [Description("Full path to the crash dump file (.dmp)")] string dumpPath,
        [Description("Optional path to symbol files directory")] string? symbolsPath = null)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OpenDumpAnalyzeSessionTool");
        IFileSystem fileSystem = new FileSystem();

        logger.LogInformation("Opening debugging session for dump: {DumpPath}", dumpPath);

        try
        {
            if (symbolsPath == "null" || string.IsNullOrWhiteSpace(symbolsPath))
            {
                symbolsPath = null;
            }

            if (!fileSystem.FileExists(dumpPath))
            {
                logger.LogError("Dump file not found: {DumpPath}", dumpPath);
                return MarkdownFormatter.CreateSessionResult(
                    "N/A",
                    fileSystem.GetFileName(dumpPath) ?? "Unknown" ?? "Unknown",
                    "Failed",
                    null,
                    $"Dump file not found: {dumpPath}");
            }

            var sessionId = await DebugEngine.Instance.CreateSessionAsync(dumpPath, symbolsPath);

            logger.LogInformation("Successfully created session: {SessionId}", sessionId);

            return MarkdownFormatter.CreateSessionResult(
                sessionId,
                fileSystem.GetFileName(dumpPath) ?? "Unknown" ?? "Unknown",
                "Success",
                symbolsPath,
                $"Session {sessionId} created successfully");
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Dump file not found: {DumpPath}", dumpPath);
            return MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                null,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Cannot create session: {Message}", ex.Message);
            return MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                null,
                ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating session");
            return MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                null,
                $"Unexpected error: {ex.Message}");
        }
    }
}

