using System.Text;
using System.Threading.Channels;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles command execution and output reading for CDB debugger sessions
    /// </summary>
    public class CdbCommandExecutor
    {
        private readonly ILogger<CdbCommandExecutor> m_logger;
        private readonly CdbSessionConfiguration m_config;
        private readonly CdbOutputParser m_outputParser;
        private readonly object m_cancellationLock = new();
        private CancellationTokenSource? m_currentOperationCts;

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
        /// Executes a command in the CDB session and returns the output (TRUE ASYNC)
        /// </summary>
        public async Task<string> ExecuteCommandAsync(
            string command,
            CdbProcessManager processManager,
            CancellationToken externalCancellationToken = default)
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

        /// <summary>
        /// Cancels the currently executing command
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
                // Send command (TRUE ASYNC)
                await SendCommandAsync(command, debuggerInput, cancellationToken).ConfigureAwait(false);

                // Read response (TRUE ASYNC)
                var output = await ReadCommandOutputAsync(processManager, cancellationToken).ConfigureAwait(false);

                m_logger.LogInformation("✅ CDB ExecuteCommand COMPLETED: {Command}", command);
                return output;
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

            // TRUE ASYNC: Use WriteLineAsync instead of blocking WriteLine
            await debuggerInput.WriteLineAsync(command).ConfigureAwait(false);
            await debuggerInput.FlushAsync().ConfigureAwait(false);

            m_logger.LogTrace("Command sent successfully");
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

            // Thread-safe: These collections are only written by consumerTask
            // and only read after awaiting consumerTask (happens-before relationship)
            var stderrLines = new List<string>();
            var stdoutOutput = new StringBuilder();

            // Start readers - both write to channel
            var stdoutTask = ReadStdoutToChannelAsync(debuggerOutput, channel.Writer, cancellationToken);
            var stderrTask = debuggerError != null
                ? ReadStderrToChannelAsync(debuggerError, channel.Writer, cancellationToken)
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
                // Wait for stdout to complete (it signals when it finds completion marker)
                await stdoutTask.ConfigureAwait(false);

                // Give stderr a brief grace period (500ms) to finish writing any pending output
                // This prevents blocking if stderr has no data or is slow
                using var gracePeriodCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gracePeriodCts.Token);
                
                try
                {
                    await stderrTask.WaitAsync(linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Grace period expired or cancelled - continue anyway
                    m_logger.LogTrace("Stderr reading stopped after grace period");
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
            StreamReader debuggerOutput, 
            ChannelWriter<(string Line, bool IsStderr)> writer,
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            var lastOutputTime = startTime;
            var linesRead = 0;
            var idleTimeoutMs = CalculateIdleTimeout();

            try
            {
                m_logger.LogTrace("Started reading stdout (channel-based)...");

                while (!cancellationToken.IsCancellationRequested)
                {
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

                    // Write line to channel
                    await writer.WriteAsync((readResult.Line, false), cancellationToken).ConfigureAwait(false);
                    linesRead++;
                    lastOutputTime = DateTime.Now;

                    // Check if command is complete
                    if (IsCommandComplete(readResult.Line))
                    {
                        m_logger.LogDebug("Command completion detected. Total lines: {LinesRead}, Total time: {TotalTime}ms",
                            linesRead, (DateTime.Now - startTime).TotalMilliseconds);
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
            StreamReader debuggerError, 
            ChannelWriter<(string Line, bool IsStderr)> writer, 
            CancellationToken cancellationToken)
        {
            try
            {
                m_logger.LogTrace("Started reading stderr (channel-based)...");

                int emptySpins = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Short timeout (100ms) per line - if no data, assume done
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                            cancellationToken, timeoutCts.Token);

                        var line = await debuggerError.ReadLineAsync()
                            .WaitAsync(TimeSpan.FromMilliseconds(100), linkedCts.Token)
                            .ConfigureAwait(false);
                        
                        if (line == null)
                            break; // End of stream
                        
                        emptySpins = 0;
                        try
                        {
                            await writer.WriteAsync((line, true), cancellationToken).ConfigureAwait(false);
                        }
                        catch (ChannelClosedException)
                        {
                            // Channel closed after stdout completion; stop quietly
                            break;
                        }
                        m_logger.LogTrace("stderr: {Line}", line);
                    }
                    catch (TimeoutException)
                    {
                        // No data for 100ms - require two consecutive timeouts to exit
                        if (++emptySpins >= 2)
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
                m_logger.LogWarning(ex, "Error reading stderr: {Message}", ex.Message);
            }
        }

        private int CalculateIdleTimeout()
        {
            // Use configured idle timeout to allow symbol server downloads to complete
            // Symbol servers can be slow, especially on first download or with proxies
            // Default: 180000ms (3 minutes)
            return m_config.IdleTimeoutMs;
        }

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
                // Use 500ms polling to reduce CPU usage (was 100ms)
                // This is still responsive but much more efficient during idle waits
                // TRUE ASYNC: No thread pool blocking, proper async/await
                using var idleCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, idleCts.Token);

                var readTask = debuggerOutput.ReadLineAsync();
                
                try
                {
                    // TRUE ASYNC: Use WaitAsync instead of blocking Wait()
                    var result = await readTask.WaitAsync(TimeSpan.FromMilliseconds(500), linkedCts.Token).ConfigureAwait(false);
                    return (result, true);
                }
                catch (TimeoutException)
                {
                    // No data within 500ms - return null but continue looping
                    return (null, true);
                }
            }
            catch (OperationCanceledException)
            {
                // External cancellation - continue looping to check which timeout triggered
                return (null, true);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("currently in use"))
            {
                // Concurrent access to stream - this is expected when timeout occurs
                // Just wait a bit and continue - the idle timeout will eventually trigger
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                return (null, true);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "⚠️ Error reading from StreamReader: {ExType}: {ExMsg}",
                    ex.GetType().Name, ex.Message);
                return (null, false); // Stop reading on unexpected errors
            }
        }

        private async Task HandleNullLineAsync(CancellationToken cancellationToken)
        {
            // CRITICAL: null from ReadLine() means no data YET, not end of stream!
            // StreamReader.ReadLine() returns null when:
            // 1. Stream is closed/disposed (this is bad - should throw)
            // 2. No complete line is available yet (this is normal - wait for more data)
            // We rely on idle timeout to detect when CDB has actually stopped outputting
            m_logger.LogTrace("ReadLine() returned null - no data available, waiting for more...");
            // TRUE ASYNC: Brief delay before next attempt (was blocking Wait())
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }

        private bool IsCommandComplete(string line)
        {
            if (m_outputParser.IsCommandComplete(line))
            {
                m_logger.LogTrace("Command completion detected on line: '{Line}'", line);
                return true;
            }
            return false;
        }
    }
}
