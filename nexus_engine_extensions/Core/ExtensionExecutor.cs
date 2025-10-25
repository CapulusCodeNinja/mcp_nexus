using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Nexus.Config;
using Nexus.Engine.Extensions.Models;
using Nexus.Engine.Extensions.Security;
using Nexus.External.Apis.ProcessManagement;

using NLog;

namespace Nexus.Engine.Extensions.Core;

/// <summary>
/// Executes extension scripts and manages their lifecycle.
/// </summary>
internal class ExtensionExecutor
{
    private readonly Logger m_Logger;
    private readonly ExtensionManager m_ExtensionManager;
    private readonly string m_CallbackUrl;
    private readonly IProcessManager m_ProcessManager;
    private readonly ExtensionTokenValidator m_TokenValidator;

    // Compiled regex to strip ANSI escape sequences from process output
    private static readonly Regex AnsiRegex = new("\x1B\\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionExecutor"/> class with default dependencies.
    /// </summary>
    /// <param name="extensionManager">The extension manager.</param>
    public ExtensionExecutor(ExtensionManager extensionManager) : this(
        extensionManager,
        new ExtensionTokenValidator(),
        new ProcessManager())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionExecutor"/> class with injected dependencies.
    /// </summary>
    /// <param name="extensionManager">The extension manager.</param>
    /// <param name="tokenValidator">The token validator.</param>
    /// <param name="processManager">The process manager.</param>
    internal ExtensionExecutor(
        ExtensionManager extensionManager,
        ExtensionTokenValidator tokenValidator,
        IProcessManager processManager)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_ExtensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
        m_TokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));

        // Get callback URL from configuration
        var config = Settings.GetInstance().Get();
        var callbackPort = config.McpNexus.Extensions.CallbackPort;
        if (callbackPort == 0)
        {
            callbackPort = config.McpNexus.Server.Port;
        }
        m_CallbackUrl = $"http://127.0.0.1:{callbackPort}/extension-callback";
    }

    /// <summary>
    /// Executes an extension script asynchronously.
    /// </summary>
    /// <param name="extensionName">The name of the extension to execute.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="parameters">Optional parameters to pass to the extension.</param>
    /// <param name="commandId">The command ID for tracking this execution.</param>
    /// <param name="progressCallback">Optional progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the extension execution.</returns>
    public async Task<ExtensionResult> ExecuteAsync(
        string extensionName,
        string sessionId,
        object? parameters,
        string commandId,
        Action<string>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        m_Logger.Info("Executing extension {ExtensionName} with command ID {CommandId} in session {SessionId}",
            extensionName, commandId, sessionId);

        var startTime = DateTime.Now;

        try
        {
            // Get extension metadata
            var metadata = m_ExtensionManager.GetExtension(extensionName);
            if (metadata == null)
            {
                return new ExtensionResult
                {
                    Success = false,
                    Error = $"Extension '{extensionName}' not found",
                    ExitCode = -1,
                    StartTime = startTime,
                    EndTime = DateTime.Now
                };
            }

            // Generate security token
            var token = m_TokenValidator.GenerateToken(sessionId, commandId);

            // Create process
            var process = await CreateProcessAsync(metadata, parameters, token);
            if (process == null)
            {
                return new ExtensionResult
                {
                    Success = false,
                    Error = "Failed to create process",
                    ExitCode = -1,
                    StartTime = startTime,
                    EndTime = DateTime.Now
                };
            }

            // Execute the script
            var result = await ExecuteScriptAsync(process, metadata, progressCallback, cancellationToken);
            result.StartTime = startTime;
            result.ProcessId = process.Id;

            return result;
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error executing extension {ExtensionName} with command ID {CommandId}",
                extensionName, commandId);

            return new ExtensionResult
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1,
                StartTime = startTime,
                EndTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Creates a process for the extension script.
    /// </summary>
    private Task<Process?> CreateProcessAsync(
        ExtensionMetadata metadata,
        object? parameters,
        string token)
    {
        try
        {
            var scriptPath = metadata.FullScriptPath;
            var workingDirectory = Path.GetDirectoryName(scriptPath);

            // Prepare environment variables
            var environmentVariables = new Dictionary<string, string>
            {
                ["MCP_NEXUS_SESSION_ID"] = metadata.Name, // This will be updated with actual session ID
                ["MCP_NEXUS_COMMAND_ID"] = metadata.Name, // This will be updated with actual command ID
                ["MCP_NEXUS_CALLBACK_URL"] = m_CallbackUrl,
                ["MCP_NEXUS_TOKEN"] = token
            };

            // Serialize parameters if provided
            if (parameters != null)
            {
                var parametersJson = JsonSerializer.Serialize(parameters);
                environmentVariables["MCP_NEXUS_PARAMETERS"] = parametersJson;
            }

            // Determine the command to run based on script type
            string fileName;
            string arguments;

            if (metadata.ScriptType.Equals("PowerShell", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "powershell.exe";
                arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
            }
            else
            {
                // Default to PowerShell
                fileName = "powershell.exe";
                arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
            }

            m_Logger.Debug("Creating process: {FileName} {Arguments}", fileName, arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            if (environmentVariables != null)
            {
                foreach (var kvp in environmentVariables)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            var process = m_ProcessManager.StartProcess(startInfo);
            return Task.FromResult<Process?>(process);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error creating process for extension {ExtensionName}", metadata.Name);
            return Task.FromResult<Process?>(null);
        }
    }

    /// <summary>
    /// Executes the script and waits for completion.
    /// </summary>
    private async Task<ExtensionResult> ExecuteScriptAsync(
        Process process,
        ExtensionMetadata metadata,
        Action<string>? progressCallback,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;
        var output = new StringBuilder();
        var error = new StringBuilder();

        try
        {
            // Set up output and error handling
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var cleanOutput = AnsiRegex.Replace(e.Data, string.Empty);
                    _ = output.AppendLine(cleanOutput);
                    progressCallback?.Invoke(cleanOutput);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var cleanError = AnsiRegex.Replace(e.Data, string.Empty);
                    _ = error.AppendLine(cleanError);
                    m_Logger.Warn("Extension {ExtensionName} error: {Error}", metadata.Name, cleanError);
                }
            };

            // Start the process
            _ = process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout
            var timeoutMs = metadata.TimeoutMs;
            var completed = await Task.Run(() => process.WaitForExit(timeoutMs), cancellationToken);

            if (!completed)
            {
                m_Logger.Warn("Extension {ExtensionName} exceeded timeout of {TimeoutMs}ms - terminating process",
                    metadata.Name, timeoutMs);

                process.Kill();
                return new ExtensionResult
                {
                    Success = false,
                    Error = $"Extension exceeded timeout of {timeoutMs}ms",
                    Output = output.ToString(),
                    ExitCode = -1,
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                    ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds
                };
            }

            var endTime = DateTime.Now;
            var executionTime = (long)(endTime - startTime).TotalMilliseconds;
            var success = process.ExitCode == 0;

            m_Logger.Info("Extension {ExtensionName} completed with exit code {ExitCode} in {ExecutionTime}ms",
                metadata.Name, process.ExitCode, executionTime);

            return new ExtensionResult
            {
                Success = success,
                Output = output.ToString(),
                Error = error.ToString(),
                ExitCode = process.ExitCode,
                StartTime = startTime,
                EndTime = endTime,
                ExecutionTimeMs = executionTime
            };
        }
        catch (OperationCanceledException)
        {
            m_Logger.Info("Extension {ExtensionName} was cancelled",
                metadata.Name);

            if (!process.HasExited)
            {
                process.Kill();
            }
            return new ExtensionResult
            {
                Success = false,
                Error = "Extension execution was cancelled",
                Output = output.ToString(),
                ExitCode = -1,
                StartTime = startTime,
                EndTime = DateTime.Now,
                ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error executing extension {ExtensionName}", metadata.Name);
            return new ExtensionResult
            {
                Success = false,
                Error = ex.Message,
                Output = output.ToString(),
                ExitCode = -1,
                StartTime = startTime,
                EndTime = DateTime.Now,
                ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds
            };
        }
    }
}
