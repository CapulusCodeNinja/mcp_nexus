using NLog;

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
            ToolInputValidator.EnsureNonEmpty(dumpPath, "dumpPath");
            ToolInputValidator.EnsureDumpFileExists(dumpPath, fileSystem);

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
            throw new McpToolUserInputException($"Invalid `dumpPath`: {ex.Message}", ex);
        }
        catch (InvalidOperationException ex)
        {
            logger.Error(ex, "Cannot create session: {Message}", ex.Message);
            throw new McpToolUserInputException(ex.Message, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.Error(ex, "Access denied: {Message}", ex.Message);
            throw new McpToolUserInputException($"Cannot open file (access denied): {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            logger.Error(ex, "I/O error when opening dump: {DumpPath}", dumpPath);
            throw new McpToolUserInputException($"Cannot open file: {ex.Message}", ex);
        }
        catch (McpToolUserInputException ex)
        {
            logger.Warn(ex, "Invalid inputs for session creation");
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error creating session");
            throw;
        }
    }
}
