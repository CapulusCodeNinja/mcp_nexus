using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using nexus.engine;
using nexus.external_apis.FileSystem;

namespace nexus.protocol.Tools;

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
    public static async Task<object> nexus_open_dump_analyze_session(
        IServiceProvider serviceProvider,
        [Description("Full path to the crash dump file (.dmp)")] string dumpPath,
        [Description("Optional path to symbol files directory")] string? symbolsPath = null)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OpenDumpAnalyzeSessionTool");
        var debugEngine = serviceProvider.GetRequiredService<IDebugEngine>();
        var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

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
                return new
                {
                    sessionId = (string?)null,
                    dumpFile = fileSystem.GetFileName(dumpPath),
                    status = "Failed",
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Dump file not found: {dumpPath}",
                    usage = UsageField
                };
            }

            var sessionId = await debugEngine.CreateSessionAsync(dumpPath, symbolsPath);

            logger.LogInformation("Successfully created session: {SessionId}", sessionId);

            return new
            {
                sessionId,
                dumpFile = fileSystem.GetFileName(dumpPath),
                status = "Success",
                operation = "nexus_open_dump_analyze_session",
                message = $"Session {sessionId} created successfully",
                usage = UsageField
            };
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Dump file not found: {DumpPath}", dumpPath);
            return new
            {
                sessionId = (string?)null,
                dumpFile = fileSystem.GetFileName(dumpPath),
                status = "Failed",
                operation = "nexus_open_dump_analyze_session",
                message = ex.Message,
                usage = UsageField
            };
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Cannot create session: {Message}", ex.Message);
            return new
            {
                sessionId = (string?)null,
                dumpFile = fileSystem.GetFileName(dumpPath),
                status = "Failed",
                operation = "nexus_open_dump_analyze_session",
                message = ex.Message,
                usage = UsageField
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating session");
            return new
            {
                sessionId = (string?)null,
                dumpFile = fileSystem.GetFileName(dumpPath),
                status = "Failed",
                operation = "nexus_open_dump_analyze_session",
                message = $"Unexpected error: {ex.Message}",
                usage = UsageField
            };
        }
    }
}

