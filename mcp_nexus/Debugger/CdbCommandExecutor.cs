using System.Text;

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
        /// Executes a command in the CDB session and returns the output
        /// </summary>
        public string ExecuteCommand(
            string command,
            CdbProcessManager processManager,
            CancellationToken externalCancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            if (!processManager.IsActive)
                throw new InvalidOperationException("No active debugging session");

            m_logger.LogInformation("🎯 [LOCKLESS] CDB ExecuteCommand START: {Command}", command);

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
                return ExecuteCommandInternal(command, processManager, operationCts.Token);
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

        private string ExecuteCommandInternal(
            string command,
            CdbProcessManager processManager,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            m_logger.LogDebug("🔓 [LOCKLESS] ExecuteCommand - no session lock needed (queue serializes)");

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
                // Send command
                SendCommand(command, debuggerInput, cancellationToken);

                // Read response
                var output = ReadCommandOutput(processManager, cancellationToken);

                m_logger.LogInformation("✅ [LOCKLESS] CDB ExecuteCommand COMPLETED: {Command}", command);
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

        private void SendCommand(string command, StreamWriter debuggerInput, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            m_logger.LogDebug("Sending command to CDB: {Command}", command);

            debuggerInput.WriteLine(command);
            debuggerInput.Flush();

            m_logger.LogTrace("Command sent successfully");
        }

        private string ReadCommandOutput(CdbProcessManager processManager, CancellationToken cancellationToken)
        {
            var debuggerOutput = processManager.DebuggerOutput;
            if (debuggerOutput == null)
            {
                m_logger.LogError("No output stream available for reading");
                return "No output stream available";
            }

            return ReadDebuggerOutputWithCancellation(debuggerOutput, cancellationToken);
        }

        /// <summary>
        /// Reads debugger output with timeout and cancellation support
        /// </summary>
        private string ReadDebuggerOutputWithCancellation(StreamReader debuggerOutput, CancellationToken cancellationToken)
        {
            m_logger.LogDebug("ReadDebuggerOutputWithCancellation called with timeout: {TimeoutMs}ms", m_config.CommandTimeoutMs);

            var output = new StringBuilder();
            var startTime = DateTime.Now;
            var lastOutputTime = startTime;
            var linesRead = 0;

            try
            {
                m_logger.LogDebug("Starting to read debugger output with cancellation support...");

                // CRITICAL: Use 30s idle timeout to allow symbol loading to complete
                // Symbol servers can be slow, and CDB goes silent while downloading symbols
                // If we timeout too early, we get truncated results (especially for !analyze -v)
                var idleTimeoutMs = CalculateIdleTimeout();

                while (true)
                {
                    // Check timeouts and cancellation FIRST - before any read attempts
                    if (CheckAbsoluteTimeout(startTime, output))
                        break;

                    if (CheckIdleTimeout(lastOutputTime, idleTimeoutMs, output))
                        break;

                    cancellationToken.ThrowIfCancellationRequested();

                    // Try to read a line from CDB output
                    var readResult = TryReadLineWithTimeout(debuggerOutput, cancellationToken);

                    if (!readResult.ShouldContinue)
                        break;

                    if (readResult.Line == null)
                    {
                        HandleNullLine();
                        continue;
                    }

                    // Process the line
                    output.AppendLine(readResult.Line);
                    linesRead++;
                    lastOutputTime = DateTime.Now; // Reset idle timer when we get data

                    if (IsCommandComplete(readResult.Line))
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                m_logger.LogWarning("Command execution cancelled by token.");
                output.AppendLine("Command execution cancelled.");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error reading debugger output.");
                output.AppendLine($"Error reading debugger output: {ex.Message}");
            }

            m_logger.LogDebug("Finished reading debugger output. Total lines: {LinesRead}, Total time: {TotalTime}ms",
                linesRead, (DateTime.Now - startTime).TotalMilliseconds);

            return output.ToString();
        }

        private int CalculateIdleTimeout()
        {
            // Use configured idle timeout to allow symbol server downloads to complete
            // Symbol servers can be slow, especially on first download or with proxies
            // Default: 180000ms (3 minutes)
            return m_config.IdleTimeoutMs;
        }

        private bool CheckAbsoluteTimeout(DateTime startTime, StringBuilder output)
        {
            var currentElapsed = DateTime.Now - startTime;
            if (currentElapsed.TotalMilliseconds >= m_config.CommandTimeoutMs)
            {
                m_logger.LogWarning("⏰ Command execution timed out after {ElapsedMs}ms (timeout: {TimeoutMs}ms), forcing completion",
                    currentElapsed.TotalMilliseconds, m_config.CommandTimeoutMs);
                output.AppendLine($"Command execution timed out after {currentElapsed.TotalMilliseconds:F0}ms");
                return true;
            }
            return false;
        }

        private bool CheckIdleTimeout(DateTime lastOutputTime, int idleTimeoutMs, StringBuilder output)
        {
            var idleElapsed = (DateTime.Now - lastOutputTime).TotalMilliseconds;
            if (idleElapsed >= idleTimeoutMs)
            {
                m_logger.LogWarning("⏳ Command idle timed out after {IdleElapsedMs}ms (idle timeout: {IdleTimeoutMs}ms), forcing completion",
                    idleElapsed, idleTimeoutMs);
                output.AppendLine($"Command idle timed out after {idleElapsed:F0}ms");
                return true;
            }
            return false;
        }

        private (string? Line, bool ShouldContinue) TryReadLineWithTimeout(StreamReader debuggerOutput, CancellationToken cancellationToken)
        {
            try
            {
                using var idleCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, idleCts.Token);

                var readTask = debuggerOutput.ReadLineAsync();
                if (readTask.Wait(100, linkedCts.Token))
                {
                    return (readTask.Result, true);
                }
                else
                {
                    // No data within 100ms - return null but continue looping
                    return (null, true);
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout or external cancellation - continue looping to check which
                return (null, true);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("currently in use"))
            {
                // Concurrent access to stream - this is expected when timeout occurs
                // Just wait a bit and continue - the idle timeout will eventually trigger
                Task.Delay(50).GetAwaiter().GetResult();
                return (null, true);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "⚠️ Error reading from StreamReader: {ExType}: {ExMsg}",
                    ex.GetType().Name, ex.Message);
                return (null, false); // Stop reading on unexpected errors
            }
        }

        private void HandleNullLine()
        {
            // CRITICAL: null from ReadLine() means no data YET, not end of stream!
            // StreamReader.ReadLine() returns null when:
            // 1. Stream is closed/disposed (this is bad - should throw)
            // 2. No complete line is available yet (this is normal - wait for more data)
            // We rely on idle timeout to detect when CDB has actually stopped outputting
            m_logger.LogTrace("ReadLine() returned null - no data available, waiting for more...");
            Task.Delay(10).Wait(); // Brief delay before next attempt
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
