using System.Text;
using System.Threading.Channels;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles command execution and output reading for CDB debugger sessions.
    /// Provides thread-safe, asynchronous command execution with proper timeout handling and output parsing.
    /// </summary>
    public class CdbCommandExecutor : IDisposable
    {
        private readonly ILogger<CdbCommandExecutor> m_logger;
        private readonly CdbSessionConfiguration m_config;
        private readonly CdbOutputParser m_outputParser;
        private readonly object m_cancellationLock = new();
        private readonly SemaphoreSlim m_commandExecutionSemaphore = new(1, 1); // Ensure only one command executes at a time
        private readonly SemaphoreSlim m_streamAccessSemaphore = new(1, 1); // Add stream access synchronization for async operations
        private CancellationTokenSource? m_currentOperationCts;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbCommandExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording command execution and errors.</param>
        /// <param name="config">The CDB session configuration containing timeout and other settings.</param>
        /// <param name="outputParser">The output parser for analyzing CDB command responses.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public CdbCommandExecutor(
            ILogger<CdbCommandExecutor> logger,
            CdbSessionConfiguration config,
            CdbOutputParser outputParser)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_outputParser = outputParser ?? throw new ArgumentNullException(nameof(outputParser));
        }

        /// <summary>
        /// Executes a command in the CDB session and returns the output asynchronously.
        /// This method ensures thread-safe execution by using semaphores and proper timeout handling.
        /// </summary>
        /// <param name="command">The CDB command to execute. Cannot be null or empty.</param>
        /// <param name="processManager">The CDB process manager providing access to the debugger process streams.</param>
        /// <param name="externalCancellationToken">Optional cancellation token for external cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the command output as a string, or an error message if execution fails.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active debugging session is available.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
        public async Task<string> ExecuteCommandAsync(
            string command,
            CdbProcessManager processManager,
            CancellationToken externalCancellationToken = default)
        {
            // Ensure only one command executes at a time to prevent stream concurrency issues
            m_logger.LogDebug("🔒 Waiting for command execution semaphore for command: {Command}", command);
            await m_commandExecutionSemaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);
            m_logger.LogDebug("🔓 Acquired command execution semaphore for command: {Command}", command);
            try
            {
                if (string.IsNullOrWhiteSpace(command))
                    throw new ArgumentException("Command cannot be null or empty", nameof(command));

                if (!processManager.IsActive)
                    throw new InvalidOperationException("No active debugging session");

                m_logger.LogInformation("🎯 CDB ExecuteCommand START (TRUE ASYNC): {Command}", command);

                // BULLETPROOF: Set command context for stateful parsing
                m_outputParser.SetCurrentCommand(command);

                // Set up cancellation
                CancellationTokenSource operationCts;
                CancellationTokenSource? timeoutCts = null;
                lock (m_cancellationLock)
                {
                    try
                    {
                        timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(m_config.CommandTimeoutMs));
                        operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                            externalCancellationToken,
                            timeoutCts.Token);
                        m_currentOperationCts = operationCts;
                    }
                    catch
                    {
                        timeoutCts?.Dispose();
                        throw;
                    }
                }

                try
                {
                    // TRUE ASYNC: No blocking, proper async/await all the way through
                    return await ExecuteCommandInternalAsync(command, processManager, operationCts.Token).ConfigureAwait(false);
                }
                finally
                {
                    lock (m_cancellationLock)
                    {
                        if (m_currentOperationCts == operationCts)
                            m_currentOperationCts = null;
                    }
                    operationCts.Dispose();
                    timeoutCts?.Dispose();
                }
            }
            finally
            {
                m_logger.LogDebug("🔓 Releasing command execution semaphore for command: {Command}", command);
                m_commandExecutionSemaphore.Release();
            }
        }

        /// <summary>
        /// Cancels the currently executing command operation.
        /// This method is thread-safe and can be called from any thread.
        /// </summary>
        public void CancelCurrentOperation()
        {
            lock (m_cancellationLock)
            {
                if (m_currentOperationCts != null && !m_currentOperationCts.Token.IsCancellationRequested)
                {
                    m_logger.LogWarning("Cancelling current CDB command operation");
                    m_currentOperationCts.Cancel();
                }
                else
                {
                    m_logger.LogDebug("No active operation to cancel or already cancelled");
                }
            }
        }

        private async Task<string> ExecuteCommandInternalAsync(
            string command,
            CdbProcessManager processManager,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            m_logger.LogDebug("🔓 ExecuteCommand - no session lock needed (queue serializes) - TRUE ASYNC");

            // Validate process state
            var debuggerProcess = processManager.DebuggerProcess;
            var debuggerInput = processManager.DebuggerInput;

            if (debuggerProcess?.HasExited == true)
            {
                m_logger.LogError("CDB process has exited unexpectedly");
                return "CDB process has exited unexpectedly. Please restart the session.";
            }

            if (debuggerInput == null)
            {
                m_logger.LogError("No input stream available for CDB process");
                return "No input stream available for CDB process.";
            }

            try
            {
                // Chain start and end markers around the original command. If the echo is suppressed, we fall back to prompt/timeout
                var chainedCommand = string.IsNullOrWhiteSpace(command)
                    ? $".echo {CdbSentinels.StartMarker}; .echo {CdbSentinels.EndMarker}"
                    : $".echo {CdbSentinels.StartMarker}; {command}; .echo {CdbSentinels.EndMarker}";

                m_logger.LogInformation("🔧 Executing command with sentinels: '{Command}' -> '{ChainedCommand}'", 
                    command, chainedCommand);

                // Send command (TRUE ASYNC)
                await SendCommandAsync(chainedCommand, debuggerInput, cancellationToken).ConfigureAwait(false);

                // Read response (TRUE ASYNC) with sentinel short-circuit
                var output = await ReadCommandOutputAsync(processManager, cancellationToken).ConfigureAwait(false);

                m_logger.LogInformation("✅ CDB ExecuteCommand COMPLETED: {Command} (Output length: {Length})", 
                    command, output?.Length ?? 0);
                    
                return output ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                m_logger.LogWarning("Command execution cancelled: {Command}", command);
                return "Command execution was cancelled.";
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error executing command: {Command}", command);
                return $"Error executing command: {ex.Message}";
            }
        }

        private async Task SendCommandAsync(string command, StreamWriter debuggerInput, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            m_logger.LogDebug("Sending command to CDB: {Command}", command);
            m_logger.LogDebug("Command length: {Length}, Contains newlines: {HasNewlines}", 
                command.Length, command.Contains('\n') || command.Contains('\r'));

            // TRUE ASYNC: Use WriteLineAsync instead of blocking WriteLine
            await debuggerInput.WriteLineAsync(command).ConfigureAwait(false);
            await debuggerInput.FlushAsync().ConfigureAwait(false);

            m_logger.LogDebug("Command sent successfully to CDB");
        }

        private async Task<string> ReadCommandOutputAsync(CdbProcessManager processManager, CancellationToken cancellationToken)
        {
            var debuggerOutput = processManager.DebuggerOutput;
            var debuggerError = processManager.DebuggerError;

            if (debuggerOutput == null)
            {
                m_logger.LogError("No output stream available for reading");
                return "No output stream available";
            }

            // Use Channel to merge stdout and stderr without blocking
            // This prevents stderr from blocking command completion
            var channel = Channel.CreateUnbounded<(string Line, bool IsStderr)>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false // Both stdout and stderr write to it
            });

            // Shared completion signal for stdout and stderr coordination
            var completionSignal = new CancellationTokenSource();

            // Thread-safe: These collections are only written by consumerTask
            // and only read after awaiting consumerTask (happens-before relationship)
            var stderrLines = new List<string>();
            var stdoutOutput = new StringBuilder();

            // Start readers concurrently but with proper stream synchronization
            // The semaphore in each reader method will prevent concurrent access to the same stream
            var stdoutTask = ReadStdoutToChannelAsync(processManager, debuggerOutput, channel.Writer, cancellationToken, completionSignal);
            var stderrTask = debuggerError != null
                ? ReadStderrToChannelAsync(processManager, debuggerError, channel.Writer, cancellationToken, completionSignal)
                : Task.CompletedTask;

            // Consumer: read from channel and build result
            // CRITICAL: Use CancellationToken.None for consumer to prevent race with writers
            // We control consumer lifecycle by completing the channel explicitly
            var consumerTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var (line, isStderr) in channel.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
                    {
                        if (isStderr)
                            stderrLines.Add(line);
                        else
                            stdoutOutput.AppendLine(line);
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error in channel consumer");
                    throw;
                }
            });

            try
            {
                // Wait for both readers to complete concurrently
                // The semaphore in each reader method prevents stream concurrency issues
                // Add timeout to prevent infinite waiting - use enhanced timeout configuration
                var outputReadingTimeoutMs = m_config.OutputReadingTimeoutMs > 0 ? m_config.OutputReadingTimeoutMs : 60000; // Default 60 seconds
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(outputReadingTimeoutMs));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                
                try
                {
                    await Task.WhenAll(stdoutTask, stderrTask).WaitAsync(linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    m_logger.LogWarning("Command output reading timed out after {TimeoutMs}ms - forcing completion", outputReadingTimeoutMs);
                    // Force completion of any remaining tasks
                    completionSignal.Cancel();
                }
            }
            finally
            {
                // CRITICAL: Always complete channel to unblock consumer
                // Even if stdout/stderr tasks threw exceptions
                channel.Writer.TryComplete();
            }

            // Wait for consumer to finish draining channel
            // If consumer threw, this will propagate the exception
            await consumerTask.ConfigureAwait(false);

            // Merge stderr into result if present
            if (stderrLines.Count > 0)
            {
                m_logger.LogWarning("CDB produced stderr output: {LineCount} lines", stderrLines.Count);
                stdoutOutput.AppendLine();
                stdoutOutput.AppendLine("[Note: CDB also produced the following diagnostics/warnings on its error stream:]");
                foreach (var line in stderrLines)
                    stdoutOutput.AppendLine(line);
            }

            return stdoutOutput.ToString();
        }

        /// <summary>
        /// Reads stdout and writes lines to channel until command completion marker found
        /// </summary>
        private async Task ReadStdoutToChannelAsync(
            CdbProcessManager processManager,
            StreamReader debuggerOutput,
            ChannelWriter<(string Line, bool IsStderr)> writer,
            CancellationToken cancellationToken,
            CancellationTokenSource completionSignal)
        {
            var startTime = DateTime.Now;
            var lastOutputTime = startTime;
            var linesRead = 0;
            var idleTimeoutMs = CalculateIdleTimeout();

            try
            {

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Check if CDB process has exited unexpectedly
                    var debuggerProcess = processManager.DebuggerProcess;
                    if (debuggerProcess?.HasExited == true)
                    {
                        m_logger.LogError("CDB process has exited unexpectedly during output reading. Exit code: {ExitCode}", 
                            debuggerProcess.ExitCode);
                        await writer.WriteAsync(("CDB process has exited unexpectedly. Please restart the session.", false), cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    // Check completion signal from stderr
                    if (completionSignal.Token.IsCancellationRequested)
                    {
                        m_logger.LogInformation("✅ Completion signal received from stderr - ending stdout read");
                        break;
                    }
                    
                    // Check timeouts FIRST
                    if (CheckAbsoluteTimeout(startTime))
                    {
                        await writer.WriteAsync(("Command execution timed out; output may be incomplete.", false), cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    if (CheckIdleTimeout(lastOutputTime, idleTimeoutMs))
                    {
                        await writer.WriteAsync(("Command idle timed out; output may be incomplete.", false), cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    // Try to read a line (with 500ms polling)
                    var readResult = await TryReadLineWithTimeoutAsync(debuggerOutput, cancellationToken).ConfigureAwait(false);

                    if (!readResult.ShouldContinue)
                        break;

                    if (readResult.Line == null)
                    {
                        await HandleNullLineAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }


                    // Drop start marker from output, if present (handle both standalone and prompt+marker cases)
                    if (string.Equals(readResult.Line, CdbSentinels.StartMarker, StringComparison.Ordinal) ||
                        readResult.Line.Contains(CdbSentinels.StartMarker))
                    {
                        // Skip writing start marker to output
                        lastOutputTime = DateTime.Now;
                        continue;
                    }

                    // Check end sentinel for short-circuit completion and do not emit it
                    if (string.Equals(readResult.Line, CdbSentinels.EndMarker, StringComparison.Ordinal) ||
                        readResult.Line.Contains(CdbSentinels.EndMarker))
                    {
                        m_logger.LogInformation("✅ End sentinel detected - completing stdout read early: '{Line}'", readResult.Line);
                        break; // Do NOT write sentinel to output
                    }

                    // Write line to channel
                    await writer.WriteAsync((readResult.Line, false), cancellationToken).ConfigureAwait(false);
                    linesRead++;
                    lastOutputTime = DateTime.Now;

                    // Check if command is complete
                    if (IsCommandComplete(readResult.Line))
                    {
                        m_logger.LogInformation("✅ Command completion detected via IsCommandComplete. Total lines: {LinesRead}, Total time: {TotalTime}ms, Line: '{Line}'",
                            linesRead, (DateTime.Now - startTime).TotalMilliseconds, readResult.Line);
                        break;
                    }

                    // Special case: If we get a CDB prompt without any sentinels, the command completed silently
                    // This handles commands like .srcpath, .lines, etc. that don't produce output
                    if (CdbCompletionPatterns.IsCdbPrompt(readResult.Line) && linesRead == 0)
                    {
                        m_logger.LogInformation("✅ Silent command completion detected (no output produced). CDB prompt: '{Line}'", readResult.Line);
                        // Don't write the prompt to output since it's not part of the command result
                        break;
                    }
                }

                m_logger.LogTrace("Finished reading stdout");
            }
            catch (OperationCanceledException)
            {
                m_logger.LogWarning("Stdout reading cancelled");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error reading stdout");
            }
        }

        /// <summary>
        /// Reads stderr and writes lines to channel
        /// Uses short timeout to detect when no more data is available
        /// </summary>
        private async Task ReadStderrToChannelAsync(
            CdbProcessManager processManager,
            StreamReader debuggerError,
            ChannelWriter<(string Line, bool IsStderr)> writer,
            CancellationToken cancellationToken,
            CancellationTokenSource completionSignal)
        {
            try
            {
                m_logger.LogTrace("Started reading stderr (channel-based)...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Check if CDB process has exited unexpectedly
                    var debuggerProcess = processManager.DebuggerProcess;
                    if (debuggerProcess?.HasExited == true)
                    {
                        m_logger.LogError("CDB process has exited unexpectedly during stderr reading. Exit code: {ExitCode}", 
                            debuggerProcess.ExitCode);
                        break;
                    }

                    try
                    {
                        // Synchronize stream access to prevent concurrent read operations
                        string? line;
                        await m_streamAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            // Use natural async reading without artificial timeouts
                            line = await debuggerError.ReadLineAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            m_streamAccessSemaphore.Release();
                        }

                        if (line == null)
                            break; // End of stream

                        // Drop markers if they appear on stderr for any reason
                        if (string.Equals(line, CdbSentinels.StartMarker, StringComparison.Ordinal) ||
                            string.Equals(line, CdbSentinels.EndMarker, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        try
                        {
                            await writer.WriteAsync((line, true), cancellationToken).ConfigureAwait(false);
                            
                            // Signal completion when stderr has output (indicates command finished with error)
                            m_logger.LogInformation("✅ Stderr output detected - signaling command completion: '{Line}'", line);
                            
                            // Signal completion immediately - no artificial delay needed
                            completionSignal.Cancel();
                        }
                        catch (ChannelClosedException)
                        {
                            // Channel closed after stdout completion; stop quietly
                            break;
                        }
                        m_logger.LogTrace("stderr: {Line}", line);
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation requested - exit gracefully
                        break;
                    }
                }

                m_logger.LogTrace("Finished reading stderr");
            }
            catch (OperationCanceledException)
            {
                m_logger.LogTrace("Stderr reading cancelled");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error reading stderr: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Calculates the idle timeout based on configuration.
        /// </summary>
        /// <returns>The idle timeout in milliseconds.</returns>
        private int CalculateIdleTimeout()
        {
            // Use configured idle timeout to allow symbol server downloads to complete
            // Symbol servers can be slow, especially on first download or with proxies
            // Default: 180000ms (3 minutes)
            return m_config.IdleTimeoutMs;
        }

        /// <summary>
        /// Checks if the absolute timeout has been exceeded.
        /// </summary>
        /// <param name="startTime">The time when the command execution started.</param>
        /// <returns><c>true</c> if the absolute timeout has been exceeded; otherwise, <c>false</c>.</returns>
        private bool CheckAbsoluteTimeout(DateTime startTime)
        {
            var currentElapsed = DateTime.Now - startTime;
            if (currentElapsed.TotalMilliseconds >= m_config.CommandTimeoutMs)
            {
                m_logger.LogWarning("⏰ Command execution timed out after {ElapsedMs}ms (timeout: {TimeoutMs}ms), forcing completion",
                    currentElapsed.TotalMilliseconds, m_config.CommandTimeoutMs);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the idle timeout has been exceeded.
        /// </summary>
        /// <param name="lastOutputTime">The time when the last output was received.</param>
        /// <param name="idleTimeoutMs">The idle timeout in milliseconds.</param>
        /// <returns><c>true</c> if the idle timeout has been exceeded; otherwise, <c>false</c>.</returns>
        private bool CheckIdleTimeout(DateTime lastOutputTime, int idleTimeoutMs)
        {
            var idleElapsed = (DateTime.Now - lastOutputTime).TotalMilliseconds;
            if (idleElapsed >= idleTimeoutMs)
            {
                m_logger.LogWarning("⏳ Command idle timed out after {IdleElapsedMs}ms (idle timeout: {IdleTimeoutMs}ms), forcing completion",
                    idleElapsed, idleTimeoutMs);
                return true;
            }
            return false;
        }

        private async Task<(string? Line, bool ShouldContinue)> TryReadLineWithTimeoutAsync(StreamReader debuggerOutput, CancellationToken cancellationToken)
        {
            try
            {
                // TRUE ASYNC: Let StreamReader.ReadLineAsync() work naturally without artificial polling
                // The method will block until data is available or cancellation is requested
                var result = await debuggerOutput.ReadLineAsync().ConfigureAwait(false);
                return (result, true);
            }
            catch (OperationCanceledException)
            {
                // External cancellation - continue looping to check which timeout triggered
                return (null, true);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("currently in use"))
            {
                // Concurrent access to stream - this is expected when timeout occurs
                // Let the idle timeout handle this naturally
                return (null, true);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "⚠️ Error reading from StreamReader: {ExType}: {ExMsg}",
                    ex.GetType().Name, ex.Message);
                return (null, false); // Stop reading on unexpected errors
            }
        }

        private Task HandleNullLineAsync(CancellationToken cancellationToken)
        {
            // CRITICAL: null from ReadLine() means no data YET, not end of stream!
            // StreamReader.ReadLine() returns null when:
            // 1. Stream is closed/disposed (this is bad - should throw)
            // 2. No complete line is available yet (this is normal - wait for more data)
            // We rely on idle timeout to detect when CDB has actually stopped outputting
            m_logger.LogTrace("ReadLine() returned null - no data available, waiting for more...");
            // No artificial delay needed - let the natural async flow handle timing
            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines if a command line indicates command completion.
        /// </summary>
        /// <param name="line">The line to check for completion indicators.</param>
        /// <returns><c>true</c> if the line indicates command completion; otherwise, <c>false</c>.</returns>
        private bool IsCommandComplete(string line)
        {
            if (m_outputParser.IsCommandComplete(line))
            {
                m_logger.LogTrace("Command completion detected on line: '{Line}'", line);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Disposes of the command executor resources.
        /// This method releases semaphores and other unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            m_commandExecutionSemaphore?.Dispose();
            m_streamAccessSemaphore?.Dispose();
        }
    }
}
