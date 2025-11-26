using System.ComponentModel;

using ModelContextProtocol.Server;

using Nexus.External.Apis.FileSystem;
using Nexus.Protocol.Services;
using Nexus.Protocol.Utilities;

using NLog;

namespace Nexus.Protocol.Tools;

/// <summary>
/// MCP tool for opening a new debugging session for crash dump analysis.
/// </summary>
[McpServerToolType]
internal static class OpenDumpAnalyzeSessionTool
{
    /// <summary>
    /// Opens a new debugging session for analyzing a crash dump file.
    /// </summary>
    /// <param name="dumpPath">Full path to the crash dump file (.dmp).</param>
    /// <param name="symbolsPath">Optional path to symbol files directory.</param>
    /// <returns>Session creation result with sessionId.</returns>
    [McpServerTool]
    [Description("Opens a new debugging session for crash dump analysis. Returns sessionId for subsequent operations.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Required for interoperability with external system")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Required for interoperability with external system")]
    public static async Task<object> nexus_open_dump_analyze_session(
        [Description("Full path to the crash dump file (.dmp)")] string dumpPath,
        [Description("Optional path to symbol files directory")] string? symbolsPath = null)
    {
        var logger = LogManager.GetCurrentClassLogger();

        IFileSystem fileSystem = new FileSystem();

        logger.Info("Opening debugging session for dump: {DumpPath}", dumpPath);

        try
        {
            if (symbolsPath == "null" || string.IsNullOrWhiteSpace(symbolsPath))
            {
                symbolsPath = null;
            }

            if (!fileSystem.FileExists(dumpPath))
            {
                logger.Error("Dump file not found: {DumpPath}", dumpPath);
                var notFoundResult = MarkdownFormatter.CreateSessionResult(
                    "N/A",
                    fileSystem.GetFileName(dumpPath) ?? "Unknown",
                    "Failed",
                    null,
                    $"Dump file not found: {dumpPath}");
                notFoundResult += MarkdownFormatter.GetUsageGuideMarkdown();
                return notFoundResult;
            }

            try
            {
                using var stream = fileSystem.OpenRead(dumpPath);
                Span<byte> buffer = stackalloc byte[1];
                _ = stream.Read(buffer);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, "Access denied when opening dump: {DumpPath}", dumpPath);
                var markdown = MarkdownFormatter.CreateSessionResult(
                    "N/A",
                    fileSystem.GetFileName(dumpPath) ?? "Unknown",
                    "Failed",
                    null,
                    $"Cannot open dump file for read (access denied): {dumpPath}");
                markdown += MarkdownFormatter.GetUsageGuideMarkdown();
                return markdown;
            }
            catch (IOException ex)
            {
                logger.Error(ex, "I/O error when opening dump: {DumpPath}", dumpPath);
                var markdown = MarkdownFormatter.CreateSessionResult(
                    "N/A",
                    fileSystem.GetFileName(dumpPath) ?? "Unknown",
                    "Failed",
                    null,
                    $"Cannot open dump file for read: {dumpPath}. {ex.Message}");
                markdown += MarkdownFormatter.GetUsageGuideMarkdown();
                return markdown;
            }

            var sessionId = await EngineService.Get().CreateSessionAsync(dumpPath, symbolsPath);

            logger.Info("Successfully created session: {SessionId}", sessionId);

            var markdown = MarkdownFormatter.CreateSessionResult(
                sessionId,
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Success",
                symbolsPath,
                $"Session {sessionId} created successfully");

            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (FileNotFoundException ex)
        {
            logger.Error(ex, "Dump file not found: {DumpPath}", dumpPath);
            var markdown = MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                null,
                ex.Message);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (InvalidOperationException ex)
        {
            logger.Error(ex, "Cannot create session: {Message}", ex.Message);
            var markdown = MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                null,
                ex.Message);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error creating session");
            var markdown = MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                null,
                $"Unexpected error: {ex.Message}");
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
    }
}
