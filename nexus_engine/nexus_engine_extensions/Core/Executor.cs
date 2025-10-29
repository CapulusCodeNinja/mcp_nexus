using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Nexus.Config;
using Nexus.Engine.Extensions.Models;
using Nexus.Engine.Extensions.Security;
using Nexus.Engine.Share.Models;
using Nexus.External.Apis.ProcessManagement;

using NLog;

namespace Nexus.Engine.Extensions.Core;

/// <summary>
/// Executes extension scripts and manages their lifecycle.
/// </summary>
internal class Executor
{
    private readonly Logger m_Logger;
    private readonly Manager m_Manager;
    private string m_CallbackUrl;
    private readonly IProcessManager m_ProcessManager;
    private readonly TokenValidator m_TokenValidator;

    // Compiled regex to strip ANSI escape sequences from process output
    private static readonly Regex AnsiRegex = new("\x1B\\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="Executor"/> class with default dependencies.
    /// </summary>
    /// <param name="manager">The extension manager.</param>
    /// <param name="tokenValidator">The token validator.</param>
    public Executor(Manager manager, TokenValidator tokenValidator)
        : this(
        manager,
        tokenValidator,
        new ProcessManager())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Executor"/> class with injected dependencies.
    /// </summary>
    /// <param name="manager">The extension manager.</param>
    /// <param name="tokenValidator">The token validator.</param>
    /// <param name="processManager">The process manager.</param>
    internal Executor(
        Manager manager,
        TokenValidator tokenValidator,
        IProcessManager processManager)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        m_TokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));

        // Get callback URL from configuration
        var config = Settings.Instance.Get();
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
    /// <param name="onProcessStarted">Optional callback invoked with the process ID right after process start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the extension execution.</returns>
    public async Task<CommandInfo> ExecuteAsync(
        string extensionName,
        string sessionId,
        object? parameters,
        string commandId,
        Action<string>? progressCallback = null,
        Action<int>? onProcessStarted = null,
        CancellationToken cancellationToken = default)
    {
        m_Logger.Info("Executing extension {ExtensionName} with command ID {CommandId} in session {SessionId}", extensionName, commandId, sessionId);

        var startTime = DateTime.Now;

        try
        {
            // Get extension metadata
            var metadata = m_Manager.GetExtension(extensionName);
            if (metadata == null)
            {
                m_Logger.Error("Extension {ExtensionName} not found", extensionName);

                return CommandInfo.Failed(
                    sessionId,
                    commandId,
                    $"Extension: {extensionName}",
                    startTime,
                    startTime,
                    DateTime.Now,
                    string.Empty,
                    $"Extension '{extensionName}' not found",
                    null);
            }

            // Generate security token
            var token = m_TokenValidator.GenerateToken(sessionId, commandId);

            // Start the PowerShell process
            var process = await StartProcessAsync(metadata, parameters, token, sessionId);
            if (process == null)
            {
                m_Logger.Error("Failed to create process for extension {ExtensionName}", extensionName);

                return CommandInfo.Failed(
                    sessionId,
                    commandId,
                    $"Extension: {extensionName}",
                    startTime,
                    startTime,
                    DateTime.Now,
                    string.Empty,
                    "Failed to create process",
                    null);
            }

            // Notify PID immediately for external tracking/cancellation support
            onProcessStarted?.Invoke(process.Id);

            // Monitor script execution (attach handlers and wait for completion)
            return await MonitorScriptAsync(process, sessionId, commandId, extensionName, metadata, progressCallback, cancellationToken);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error executing extension {ExtensionName} with command ID {CommandId}", extensionName, commandId);

            return CommandInfo.Failed(
                sessionId,
                commandId,
                $"Extension: {extensionName}",
                startTime,
                startTime,
                DateTime.Now,
                string.Empty,
                ex.Message,
                null);
        }
    }

    /// <summary>
    /// Starts the PowerShell process for the extension script and returns the running process.
    /// </summary>
    /// <param name="metadata">The extension metadata used to build the process command line.</param>
    /// <param name="parameters">Optional parameters object to pass to the script (serialized to JSON).</param>
    /// <param name="token">Security token used by the script to authenticate callbacks.</param>
    /// <param name="sessionId">The debugging session identifier associated with this execution.</param>
    /// <returns>A task that resolves to the started <see cref="Process"/> instance or null on failure.</returns>
    private Task<Process?> StartProcessAsync(
        ExtensionMetadata metadata,
        object? parameters,
        string token,
        string sessionId)
    {
        try
        {
            var scriptPath = metadata.FullScriptPath;
            var workingDirectory = Path.GetDirectoryName(scriptPath);

            // Serialize parameters if provided
            string? parametersJson = null;
            if (parameters != null)
            {
                parametersJson = JsonSerializer.Serialize(parameters);
            }

            // Determine the command to run based on script type
            string fileName;
            string arguments;

            if (metadata.ScriptType.Equals("PowerShell", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "powershell.exe";
                arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -SessionId \"{sessionId}\" -Token \"{token}\" -CallbackUrl \"{m_CallbackUrl}\"";

                // Add parameters if provided
                if (!string.IsNullOrEmpty(parametersJson))
                {
                    arguments += $" -Parameters '{parametersJson}'";
                }
            }
            else
            {
                // Default to PowerShell
                fileName = "powershell.exe";
                arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -SessionId \"{sessionId}\" -Token \"{token}\" -CallbackUrl \"{m_CallbackUrl}\"";

                // Add parameters if provided
                if (!string.IsNullOrEmpty(parametersJson))
                {
                    arguments += $" -Parameters '{parametersJson}'";
                }
            }

            m_Logger.Debug("Creating process: {FileName} {Arguments} ({WorkingDirectory})", fileName, arguments, workingDirectory);

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            var process = m_ProcessManager.StartProcess(startInfo);
            if (process == null)
            {
                m_Logger.Error("Failed to start extensions script for extension {ExtensionName}", metadata.Name);
            }
            else
            {
                m_Logger.Debug("Successfully started extensions script {ProcessId} for extension {ExtensionName}", process.Id, metadata.Name);
            }

            return Task.FromResult<Process?>(process);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error creating process for extension {ExtensionName}", metadata.Name);
            return Task.FromResult<Process?>(null);
        }
    }

    /// <summary>
    /// Attaches output/error handlers to the running process and waits for completion.
    /// </summary>
    /// <param name="process">The running process to monitor.</param>
    /// <param name="sessionId">The debugging session identifier.</param>
    /// <param name="commandId">The command identifier associated with this execution.</param>
    /// <param name="extensionName">The name of the extension being executed.</param>
    /// <param name="metadata">The extension metadata.</param>
    /// <param name="progressCallback">Optional callback invoked for each output line.</param>
    /// <param name="cancellationToken">Cancellation token to abort monitoring and execution.</param>
    /// <returns>A task that resolves to the final <see cref="CommandInfo"/>.</returns>
    private async Task<CommandInfo> MonitorScriptAsync(
        Process process,
        string sessionId,
        string commandId,
        string extensionName,
        ExtensionMetadata metadata,
        Action<string>? progressCallback,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;
        var output = new StringBuilder();
        var error = new StringBuilder();
        DataReceivedEventHandler? outHandler = null;
        DataReceivedEventHandler? errHandler = null;

        try
        {
            // Set up output and error handling
            outHandler = (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var cleanOutput = AnsiRegex.Replace(e.Data, string.Empty);
                    _ = output.AppendLine(cleanOutput);
                    progressCallback?.Invoke(cleanOutput);
                }
            };

            errHandler = (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var cleanError = AnsiRegex.Replace(e.Data, string.Empty);
                    _ = error.AppendLine(cleanError);
                }
            };

            process.OutputDataReceived += outHandler;
            process.ErrorDataReceived += errHandler;

            // Process was already started; attach readers now
            m_Logger.Debug("Monitoring PowerShell process for extension {ExtensionName}, PID: {ProcessId}", metadata.Name, process.Id);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout
            var timeoutMs = metadata.TimeoutMs;
            m_Logger.Debug("Waiting for extensions script process completion for extension {ExtensionName}, timeout: {TimeoutMs}ms", metadata.Name, timeoutMs);
            var completed = await m_ProcessManager.WaitForProcessExitAsync(process, timeoutMs, cancellationToken);

            if (!completed)
            {
                m_Logger.Warn("Extension {ExtensionName} exceeded timeout of {TimeoutMs}ms - terminating process", metadata.Name, timeoutMs);

                EndScriptExecution(process, metadata);

                return CommandInfo.TimedOut(
                    sessionId,
                    commandId,
                    $"Extension: {extensionName}",
                    startTime,
                    startTime,
                    DateTime.Now,
                    string.Empty,
                    $"Extension exceeded timeout of {timeoutMs}ms",
                    process.Id);
            }

            var endTime = DateTime.Now;
            var executionTime = (long)(endTime - startTime).TotalMilliseconds;
            var success = process.ExitCode == 0;

            m_Logger.Info("Extension {ExtensionName} completed with exit code {ExitCode} in {ExecutionTime}ms", metadata.Name, process.ExitCode, executionTime);

            return CommandInfo.Completed(
                sessionId,
                commandId,
                $"Extension: {extensionName}",
                startTime,
                startTime,
                DateTime.Now,
                output.ToString(),
                error.ToString(),
                process.Id);
        }
        catch (OperationCanceledException)
        {
            m_Logger.Info("Extension {ExtensionName} was cancelled", metadata.Name);

            EndScriptExecution(process, metadata, true);
            return CommandInfo.Cancelled(
                sessionId,
                commandId,
                $"Extension: {extensionName}",
                startTime,
                startTime,
                DateTime.Now,
                string.Empty,
                "Extension execution was cancelled",
                process.Id);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error executing extension {ExtensionName}", metadata.Name);

            return CommandInfo.Failed(
                sessionId,
                commandId,
                $"Extension: {extensionName}",
                startTime,
                startTime,
                DateTime.Now,
                string.Empty,
                ex.Message,
                process.Id);
        }
        finally
        {
            try
            {
                // Detach handlers and dispose process
                try
                {
                    process.CancelOutputRead();
                }
                catch
                {
                }

                try
                {
                    process.CancelErrorRead();
                }
                catch
                {
                }

                try
                {
                    process.OutputDataReceived -= outHandler;
                }
                catch
                {
                }

                try
                {
                    process.ErrorDataReceived -= errHandler;
                }
                catch
                {
                }
            }
            catch
            {
                // ignore
            }
            finally
            {
                try
                {
                    process.Dispose();
                }
                catch
                {
                }
            }
        }
    }

    /// <summary>
    /// Ends the script execution by killing the process if it is not already exited.
    /// </summary>
    /// <param name="process">The process to end.</param>
    /// <param name="metadata">The metadata of the extension.</param>
    /// <param name="force">Whether to force the process to exit.</param>
    private void EndScriptExecution(Process process, ExtensionMetadata metadata, bool force = false)
    {
        if (process.HasExited)
        {
            return;
        }

        // Graceperiod for the script to exit gracefully before killing it
        // The scripts should be designed to handle e.g. timeouts gracefully.
        if (!force && !process.WaitForExit(2000))
        {
            m_Logger.Warn("Extension {ExtensionName} process did not exit gracefully after 1 second - terminating process", metadata.Name);
            process.Kill();
        }
    }

    /// <summary>
    /// Updates the callback URL used by extensions to communicate back to the server.
    /// </summary>
    /// <param name="callbackUrl">The new callback URL.</param>
    public void UpdateCallbackUrl(string callbackUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callbackUrl);
        m_CallbackUrl = callbackUrl;
        m_Logger.Info("Updated extension callback URL to: {CallbackUrl}", m_CallbackUrl);
    }
}
