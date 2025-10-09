using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Executes extension scripts and manages their lifecycle.
    /// </summary>
    public class ExtensionExecutor : IExtensionExecutor
    {
        private readonly ILogger<ExtensionExecutor> m_Logger;
        private readonly IExtensionManager m_ExtensionManager;
        private readonly string m_CallbackUrl;
        private readonly ConcurrentDictionary<string, ExtensionProcessInfo> m_RunningExtensions = new();
        private readonly ConcurrentDictionary<string, Process> m_Processes = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording execution operations.</param>
        /// <param name="extensionManager">The extension manager for retrieving extension metadata.</param>
        /// <param name="callbackUrl">The base URL for extension callbacks.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or extensionManager is null.</exception>
        /// <exception cref="ArgumentException">Thrown when callbackUrl is null or empty.</exception>
        public ExtensionExecutor(
            ILogger<ExtensionExecutor> logger,
            IExtensionManager extensionManager,
            string callbackUrl)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_ExtensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
            
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            m_CallbackUrl = callbackUrl;
        }

        /// <summary>
        /// Executes an extension script asynchronously.
        /// </summary>
        /// <param name="extensionName">The name of the extension to execute.</param>
        /// <param name="sessionId">The session ID this extension is running for.</param>
        /// <param name="parameters">Parameters to pass to the extension (will be serialized as JSON).</param>
        /// <param name="commandId">The command ID for tracking this extension execution.</param>
        /// <param name="progressCallback">Optional callback for progress updates.</param>
        /// <param name="cancellationToken">Cancellation token to stop execution.</param>
        /// <returns>The extension execution result.</returns>
        /// <exception cref="ArgumentException">Thrown when extension name, session ID, or command ID is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when extension validation fails or script type is unsupported.</exception>
        public async Task<ExtensionResult> ExecuteAsync(
            string extensionName,
            string sessionId,
            object? parameters,
            string commandId,
            Action<string>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(extensionName))
                throw new ArgumentException("Extension name cannot be null or empty", nameof(extensionName));

            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

            m_Logger.LogInformation("Starting extension: {Extension} for session {SessionId} with command ID {CommandId}",
                extensionName, sessionId, commandId);

            // Get extension metadata
            var metadata = m_ExtensionManager.GetExtension(extensionName);
            if (metadata == null)
            {
                throw new InvalidOperationException($"Extension '{extensionName}' not found");
            }

            // Validate extension
            var (isValid, errorMessage) = m_ExtensionManager.ValidateExtension(extensionName);
            if (!isValid)
            {
                throw new InvalidOperationException($"Extension validation failed: {errorMessage}");
            }

            var stopwatch = Stopwatch.StartNew();
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var callbackCount = 0;

            try
            {
                // Create process info
                var processInfo = new ExtensionProcessInfo
                {
                    CommandId = commandId,
                    ExtensionName = extensionName,
                    SessionId = sessionId,
                    StartedAt = DateTime.UtcNow,
                    IsRunning = true,
                    CallbackCount = 0
                };

                m_RunningExtensions[commandId] = processInfo;

                // Generate callback token
                var callbackToken = GenerateCallbackToken(sessionId, commandId);

                // Create process
                var process = CreateProcess(metadata, sessionId, commandId, callbackToken, parameters);
                m_Processes[commandId] = process;
                processInfo.ProcessId = process.Id;

                // Set up output handlers
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);

                        // Check for progress messages
                        if (e.Data.StartsWith("[PROGRESS]"))
                        {
                            var progressMessage = e.Data.Substring(10).Trim();
                            progressCallback?.Invoke(progressMessage);
                        }

                        // Check for callback count updates
                        if (e.Data.StartsWith("[CALLBACK]"))
                        {
                            callbackCount++;
                            processInfo.CallbackCount = callbackCount;
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorBuilder.AppendLine(e.Data);
                        m_Logger.LogWarning("Extension {Extension} stderr: {Message}", extensionName, e.Data);
                    }
                };

                // Start process
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                m_Logger.LogInformation("Extension {Extension} started with PID {ProcessId}",
                    extensionName, process.Id);

                // Wait for completion with timeout
                var timeout = metadata.Timeout > 0 ? metadata.Timeout : Timeout.Infinite;
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (timeout != Timeout.Infinite)
                {
                    cts.CancelAfter(timeout);
                }

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Kill process if cancelled or timed out
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(entireProcessTree: true);
                            m_Logger.LogWarning("Extension {Extension} killed due to cancellation/timeout",
                                extensionName);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex, "Failed to kill extension {Extension}", extensionName);
                    }

                    throw new OperationCanceledException(
                        $"Extension '{extensionName}' was cancelled or timed out after {stopwatch.Elapsed.TotalSeconds:F1} seconds");
                }

                stopwatch.Stop();
                processInfo.IsRunning = false;

                var exitCode = process.ExitCode;
                var output = outputBuilder.ToString();
                var standardError = errorBuilder.ToString();

                m_Logger.LogInformation(
                    "Extension {Extension} completed with exit code {ExitCode} in {Elapsed}ms",
                    extensionName, exitCode, stopwatch.ElapsedMilliseconds);

                return new ExtensionResult
                {
                    Success = exitCode == 0,
                    Output = output,
                    Error = exitCode != 0 ? $"Extension exited with code {exitCode}" : null,
                    ExitCode = exitCode,
                    ExecutionTime = stopwatch.Elapsed,
                    CallbackCount = callbackCount,
                    StandardError = string.IsNullOrWhiteSpace(standardError) ? null : standardError
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                m_Logger.LogError(ex, "Extension {Extension} failed after {Elapsed}ms",
                    extensionName, stopwatch.ElapsedMilliseconds);

                return new ExtensionResult
                {
                    Success = false,
                    Output = outputBuilder.ToString(),
                    Error = ex.Message,
                    ExitCode = -1,
                    ExecutionTime = stopwatch.Elapsed,
                    CallbackCount = callbackCount,
                    StandardError = errorBuilder.ToString()
                };
            }
            finally
            {
                // Cleanup
                m_RunningExtensions.TryRemove(commandId, out _);
                if (m_Processes.TryRemove(commandId, out var process))
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Creates a process for executing an extension script.
        /// </summary>
        /// <param name="metadata">The extension metadata.</param>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="commandId">The command ID.</param>
        /// <param name="callbackToken">The callback authentication token.</param>
        /// <param name="parameters">Parameters to pass to the extension.</param>
        /// <returns>A configured process ready to start.</returns>
        /// <exception cref="InvalidOperationException">Thrown when script type is unsupported.</exception>
        private Process CreateProcess(
            ExtensionMetadata metadata,
            string sessionId,
            string commandId,
            string callbackToken,
            object? parameters)
        {
            var process = new Process();
            var startInfo = process.StartInfo;

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            startInfo.CreateNoWindow = true;

            // Set environment variables
            startInfo.Environment["MCP_NEXUS_SESSION_ID"] = sessionId;
            startInfo.Environment["MCP_NEXUS_COMMAND_ID"] = commandId;
            startInfo.Environment["MCP_NEXUS_CALLBACK_URL"] = m_CallbackUrl;
            startInfo.Environment["MCP_NEXUS_CALLBACK_TOKEN"] = callbackToken;

            // Add extension parameters as JSON
            if (parameters != null)
            {
                var parametersJson = JsonSerializer.Serialize(parameters);
                startInfo.Environment["MCP_NEXUS_PARAMETERS"] = parametersJson;
            }

            // Configure based on script type (only PowerShell supported)
            var scriptType = metadata.ScriptType.ToLowerInvariant();
            if (scriptType == "powershell")
            {
                startInfo.FileName = "pwsh.exe";
                startInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{metadata.FullScriptPath}\"";
            }
            else
            {
                throw new InvalidOperationException($"Unsupported script type: {metadata.ScriptType}. Only 'powershell' is supported at the moment.");
            }

            m_Logger.LogDebug("Extension process: {FileName} {Arguments}",
                startInfo.FileName, startInfo.Arguments);

            return process;
        }

        /// <summary>
        /// Generates a secure callback token for an extension.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="commandId">The command ID.</param>
        /// <returns>A secure callback token.</returns>
        private string GenerateCallbackToken(string sessionId, string commandId)
        {
            // Generate a secure random token
            var tokenBytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(tokenBytes);
            var token = Convert.ToBase64String(tokenBytes);

            // Store token for validation (in a real implementation, this would be in a secure token store)
            return $"ext_{sessionId}_{commandId}_{token}";
        }

        /// <summary>
        /// Kills a running extension by command ID.
        /// </summary>
        /// <param name="commandId">The command ID of the extension to kill.</param>
        /// <returns>True if the extension was killed, false if not found or already completed.</returns>
        public bool KillExtension(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return false;

            if (!m_Processes.TryGetValue(commandId, out var process))
                return false;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    m_Logger.LogInformation("Killed extension with command ID: {CommandId}", commandId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to kill extension with command ID: {CommandId}", commandId);
            }

            return false;
        }

        /// <summary>
        /// Gets information about a running extension.
        /// </summary>
        /// <param name="commandId">The command ID of the extension.</param>
        /// <returns>Extension process information, or null if not found.</returns>
        public ExtensionProcessInfo? GetExtensionInfo(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            return m_RunningExtensions.TryGetValue(commandId, out var info) ? info : null;
        }
    }
}

