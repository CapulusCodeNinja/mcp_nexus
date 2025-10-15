using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        private readonly IProcessWrapper m_ProcessWrapper;
        private readonly IExtensionTokenValidator m_TokenValidator;
        private readonly ConcurrentDictionary<string, ExtensionProcessInfo> m_RunningExtensions = new();
        private readonly ConcurrentDictionary<string, IProcessHandle> m_Processes = new();

        // Compiled regex to strip ANSI escape sequences from process output
        private static readonly Regex s_AnsiRegex = new("\x1B\\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording execution operations.</param>
        /// <param name="extensionManager">The extension manager for retrieving extension metadata.</param>
        /// <param name="callbackUrl">The base URL for extension callbacks.</param>
        /// <param name="processWrapper">Optional process wrapper for testing (defaults to real Process).</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or extensionManager is null.</exception>
        /// <exception cref="ArgumentException">Thrown when callbackUrl is null or empty.</exception>
        public ExtensionExecutor(
            ILogger<ExtensionExecutor> logger,
            IExtensionManager extensionManager,
            string callbackUrl,
            IProcessWrapper? processWrapper = null,
            IExtensionTokenValidator? tokenValidator = null)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_ExtensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));

            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            m_CallbackUrl = callbackUrl;
            m_ProcessWrapper = processWrapper ?? new ProcessWrapper();
            m_TokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
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
            var metadata = m_ExtensionManager.GetExtension(extensionName) ?? throw new InvalidOperationException($"Extension '{extensionName}' not found");

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

            string? callbackToken = null;
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

                // Generate callback token using validator (single source of truth)
                callbackToken = m_TokenValidator.CreateToken(sessionId, commandId);

                // Get process info for error messages (before creating process)
                string processDescription = $"{metadata.ScriptType} extension: {extensionName}";

                // Create process
                var process = CreateProcess(metadata, sessionId, commandId, callbackToken, parameters);
                m_Processes[commandId] = process;

                // Set up output handlers
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        var sanitized = StripAnsi(e.Data);
                        outputBuilder.AppendLine(sanitized);

                        // Check for progress messages
                        if (e.Data.StartsWith("[PROGRESS]"))
                        {
                            var progressMessage = e.Data[10..].Trim();
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
                        var sanitized = StripAnsi(e.Data);
                        errorBuilder.AppendLine(sanitized);
                        m_Logger.LogWarning("Extension {Extension} stderr: {Message}", extensionName, sanitized);
                    }
                };

                // Start process
                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Set process ID after successful start
                    processInfo.ProcessId = process.Id;

                    m_Logger.LogInformation("Extension {Extension} started with PID {ProcessId}",
                        extensionName, process.Id);
                }
                catch (Exception startEx)
                {
                    m_Logger.LogError(startEx, "Failed to start extension process for {ProcessDescription}",
                        processDescription);
                    throw new InvalidOperationException(
                        $"Failed to start extension process for '{processDescription}': {startEx.Message}", startEx);
                }

                // Wait for completion with timeout
                var timeout = metadata.Timeout > 0 ? metadata.Timeout : Timeout.Infinite;
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (timeout != Timeout.Infinite)
                {
                    cts.CancelAfter(timeout);
                }

                try
                {
                    m_Logger.LogDebug("Waiting for extension {Extension} to exit...", extensionName);
                    await process.WaitForExitAsync(cts.Token);
                    m_Logger.LogDebug("Extension {Extension} WaitForExitAsync completed", extensionName);
                }
                catch (OperationCanceledException)
                {
                    // Kill process if cancelled or timed out
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(entireProcessTree: true);
                            m_Logger.LogWarning("🔪 Killed extension script '{Extension}' (command {CommandId}) due to timeout after {Elapsed:F1} seconds (timeout: {Timeout}ms)",
                                extensionName, commandId, stopwatch.Elapsed.TotalSeconds, metadata.Timeout);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex, "❌ Failed to kill extension {Extension} (command {CommandId})", extensionName, commandId);
                    }

                    throw new OperationCanceledException(
                        $"Extension '{extensionName}' was cancelled or timed out after {stopwatch.Elapsed.TotalSeconds:F1} seconds");
                }

                // CRITICAL: Call synchronous WaitForExit() to ensure all output is flushed
                // This is required after WaitForExitAsync() when using redirected streams
                try
                {
                    m_Logger.LogDebug("Calling WaitForExit() for extension {Extension}...", extensionName);
                    process.WaitForExit();
                    m_Logger.LogDebug("WaitForExit() completed for extension {Extension}", extensionName);
                }
                catch (Exception waitEx)
                {
                    m_Logger.LogError(waitEx, "WaitForExit() failed for extension {Extension}", extensionName);
                    throw new InvalidOperationException($"Failed to wait for extension to complete: {waitEx.Message}", waitEx);
                }

                stopwatch.Stop();
                processInfo.IsRunning = false;

                int exitCode;
                try
                {
                    m_Logger.LogDebug("Getting exit code for extension {Extension}...", extensionName);
                    exitCode = process.ExitCode;
                    m_Logger.LogDebug("Exit code for extension {Extension}: {ExitCode}", extensionName, exitCode);
                }
                catch (Exception exCodeEx)
                {
                    m_Logger.LogError(exCodeEx, "Failed to get ExitCode for extension {Extension}", extensionName);
                    throw new InvalidOperationException($"Failed to get exit code: {exCodeEx.Message}", exCodeEx);
                }

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
                // Revoke token when execution ends
                if (!string.IsNullOrWhiteSpace(callbackToken))
                {
                    try { m_TokenValidator.RevokeToken(callbackToken); } catch { }
                }
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
        private IProcessHandle CreateProcess(
            ExtensionMetadata metadata,
            string sessionId,
            string commandId,
            string callbackToken,
            object? parameters)
        {
            // Build environment variables dictionary (for secrets and system info only)
            var environmentVariables = new Dictionary<string, string>
            {
                ["MCP_NEXUS_SESSION_ID"] = sessionId,
                ["MCP_NEXUS_COMMAND_ID"] = commandId,
                ["MCP_NEXUS_CALLBACK_URL"] = m_CallbackUrl,
                ["MCP_NEXUS_CALLBACK_TOKEN"] = callbackToken
            };

            // Configure based on script type (only PowerShell supported)
            var scriptType = metadata.ScriptType.ToLowerInvariant();
            string fileName;
            string arguments;

            if (scriptType == "powershell")
            {
                // Try to find PowerShell - prefer pwsh (PowerShell 7+), fall back to powershell (5.1)
                string? powershellPath = FindPowerShell() ?? throw new InvalidOperationException("PowerShell not found. Please ensure pwsh.exe or powershell.exe is in PATH or installed.");
                fileName = powershellPath;
                
                // Build PowerShell arguments with parameters
                var argumentsBuilder = new StringBuilder();
                argumentsBuilder.Append($"-NoProfile -ExecutionPolicy Bypass -File \"{metadata.FullScriptPath}\"");
                
                // Add parameters as PowerShell command-line arguments
                if (parameters != null)
                {
                    var paramArgs = BuildPowerShellParameterArguments(parameters);
                    if (!string.IsNullOrWhiteSpace(paramArgs))
                    {
                        argumentsBuilder.Append(' ');
                        argumentsBuilder.Append(paramArgs);
                    }
                }
                
                arguments = argumentsBuilder.ToString();
            }
            else
            {
                throw new InvalidOperationException($"Unsupported script type: {metadata.ScriptType}. Only 'powershell' is supported at the moment.");
            }

            m_Logger.LogDebug("Extension process: {FileName} {Arguments}", fileName, arguments);

            return m_ProcessWrapper.CreateProcess(fileName, arguments, environmentVariables);
        }


        /// <summary>
        /// Builds PowerShell command-line parameter arguments from a JSON object.
        /// Converts parameter names to PowerShell naming convention (camelCase to PascalCase).
        /// </summary>
        /// <param name="parameters">The parameters object to convert.</param>
        /// <returns>PowerShell parameter string (e.g., "-ThreadId '5' -Verbose $true").</returns>
        private string BuildPowerShellParameterArguments(object parameters)
        {
            if (parameters == null)
                return string.Empty;

            var argumentsBuilder = new StringBuilder();

            // Serialize to JSON and deserialize to JsonElement for easy property access
            var json = JsonSerializer.Serialize(parameters);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var property in root.EnumerateObject())
            {
                // Convert camelCase to PascalCase for PowerShell convention
                var paramName = char.ToUpper(property.Name[0]) + property.Name[1..];
                
                if (argumentsBuilder.Length > 0)
                    argumentsBuilder.Append(' ');

                argumentsBuilder.Append($"-{paramName}");

                // Handle different value types
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        // Escape single quotes in string values
                        var stringValue = property.Value.GetString() ?? string.Empty;
                        stringValue = stringValue.Replace("'", "''");
                        argumentsBuilder.Append($" '{stringValue}'");
                        break;

                    case JsonValueKind.Number:
                        argumentsBuilder.Append($" {property.Value.GetRawText()}");
                        break;

                    case JsonValueKind.True:
                        argumentsBuilder.Append(" $true");
                        break;

                    case JsonValueKind.False:
                        argumentsBuilder.Append(" $false");
                        break;

                    case JsonValueKind.Null:
                        argumentsBuilder.Append(" $null");
                        break;

                    default:
                        // For complex types (objects, arrays), pass as JSON string
                        var jsonValue = property.Value.GetRawText();
                        jsonValue = jsonValue.Replace("'", "''");
                        argumentsBuilder.Append($" '{jsonValue}'");
                        break;
                }
            }

            return argumentsBuilder.ToString();
        }

        /// <summary>
        /// Finds PowerShell executable on the system.
        /// Prefers pwsh.exe (PowerShell 7+), falls back to powershell.exe (Windows PowerShell 5.1).
        /// </summary>
        /// <returns>Path to PowerShell executable, or null if not found.</returns>
        private string? FindPowerShell()
        {
            // Try pwsh first (PowerShell 7+), then fall back to Windows PowerShell 5.1
            var paths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "6", "pwsh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
                "pwsh.exe", // Try simple names last (in PATH)
                "powershell.exe"
            };

            foreach (var path in paths)
            {
                try
                {
                    // Check if full path exists
                    if (Path.IsPathRooted(path) && File.Exists(path))
                    {
                        m_Logger.LogDebug("Found PowerShell at: {Path}", path);
                        return path;
                    }

                    // For simple names, just return them and let Process.Start handle PATH lookup
                    if (!Path.IsPathRooted(path))
                    {
                        m_Logger.LogDebug("Trying PowerShell from PATH: {Path}", path);
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.LogDebug(ex, "Error checking PowerShell path: {Path}", path);
                }
            }

            m_Logger.LogError("PowerShell not found. Tried: {Paths}", string.Join(", ", paths));
            return null;
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

        /// <summary>
        /// Removes ANSI escape sequences for clean logging/output.
        /// </summary>
        private static string StripAnsi(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            try
            {
                return s_AnsiRegex.Replace(input, string.Empty);
            }
            catch
            {
                return input;
            }
        }
    }
}

