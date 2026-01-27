using NLog;

using WinAiDbg.Engine.Share.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Tools;

/// <summary>
/// MCP tool for opening a new debugging session for crash dump analysis.
/// </summary>
internal class OpenDumpAnalyzeSessionTool
{
    /// <summary>
    /// Opens a new debugging session for analyzing a crash dump file.
    /// </summary>
    /// <param name="dumpPath">Full path to the crash dump file (.dmp).</param>
    /// <param name="symbolsPath">Optional path to symbol files directory.</param>
    /// <returns>Session creation result with sessionId.</returns>
    public async Task<object> Execute(
        string dumpPath,
        string? symbolsPath = null)
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

            var createResult = await EngineService.Get().CreateSessionAsync(dumpPath, symbolsPath);

            logger.Info("Successfully created session: {SessionId}", createResult.SessionId);

            var markdown = MarkdownFormatter.CreateSessionResult(
                createResult.SessionId,
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Success",
                createResult.DumpCheck,
                symbolsPath,
                $"Session {createResult.SessionId} created successfully");

            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (FileNotFoundException ex)
        {
            logger.Error(ex, "File not found: {Message}", ex.Message);
            var markdown = MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                new DumpCheckResult
                {
                    IsEnabled = false,
                    WasExecuted = false,
                    ExitCode = -1,
                    Message = string.Empty,
                },
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
                new DumpCheckResult
                {
                    IsEnabled = false,
                    WasExecuted = false,
                    ExitCode = -1,
                    Message = string.Empty,
                },
                null,
                ex.Message);
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.Error(ex, "Access denied: {Message}", ex.Message);
            var errorMarkdown = MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                new DumpCheckResult
                {
                    IsEnabled = false,
                    WasExecuted = false,
                    ExitCode = -1,
                    Message = string.Empty,
                },
                null,
                $"Cannot open file (access denied): {ex.Message}");
            errorMarkdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return errorMarkdown;
        }
        catch (IOException ex)
        {
            logger.Error(ex, "I/O error when opening dump: {DumpPath}", dumpPath);
            var errorMarkdown = MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                new DumpCheckResult
                {
                    IsEnabled = false,
                    WasExecuted = false,
                    ExitCode = -1,
                    Message = string.Empty,
                },
                null,
                $"Cannot open file: {ex.Message}");
            errorMarkdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return errorMarkdown;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error creating session");
            var markdown = MarkdownFormatter.CreateSessionResult(
                "N/A",
                fileSystem.GetFileName(dumpPath) ?? "Unknown",
                "Failed",
                new DumpCheckResult
                {
                    IsEnabled = false,
                    WasExecuted = false,
                    ExitCode = -1,
                    Message = string.Empty,
                },
                null,
                $"Unexpected error: {ex.Message}");
            markdown += MarkdownFormatter.GetUsageGuideMarkdown();
            return markdown;
        }
    }
}
