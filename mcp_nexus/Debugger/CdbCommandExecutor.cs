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

                // Use a shorter idle timeout to detect hangs faster
                var idleTimeoutMs = Math.Min(10000, m_config.CommandTimeoutMs / 3); // 10s or 1/3 of total, whichever smaller

                while (true)
                {
                    try
                    {
                        // Check for absolute timeout
                        var currentElapsed = DateTime.Now - startTime;
                        if (currentElapsed.TotalMilliseconds >= m_config.CommandTimeoutMs)
                        {
                            m_logger.LogWarning("⏰ Command execution timed out after {ElapsedMs}ms (timeout: {TimeoutMs}ms), forcing completion",
                                currentElapsed.TotalMilliseconds, m_config.CommandTimeoutMs);
                            output.AppendLine($"Command execution timed out after {currentElapsed.TotalMilliseconds:F0}ms");
                            break;
                        }

                        // Check for idle timeout
                        if ((DateTime.Now - lastOutputTime).TotalMilliseconds >= idleTimeoutMs)
                        {
                            m_logger.LogWarning("⏳ Command idle timed out after {IdleElapsedMs}ms (idle timeout: {IdleTimeoutMs}ms), forcing completion",
                                (DateTime.Now - lastOutputTime).TotalMilliseconds, idleTimeoutMs);
                            output.AppendLine($"Command idle timed out after {(DateTime.Now - lastOutputTime).TotalMilliseconds:F0}ms");
                            break;
                        }

                        // Check for cancellation
                        cancellationToken.ThrowIfCancellationRequested();

                        // CRITICAL FIX: Use async read with timeout to avoid blocking forever
                        // ReadLine() can block if stream has no complete line yet
                        string? line = null;
                        var readTask = Task.Run(() => debuggerOutput.ReadLine());

                        // Wait with timeout - wrap in try-catch because Wait() can throw if task is faulted
                        bool readCompleted = false;
                        try
                        {
                            readCompleted = readTask.Wait(50); // 50ms timeout for read attempt
                        }
                        catch (AggregateException)
                        {
                            // Task faulted (e.g., stream closed) - treat as end of stream
                            m_logger.LogDebug("Read task faulted, treating as end of stream");
                            break;
                        }
                        
                        if (readCompleted)
                        {
                            line = readTask.Result;
                        }
                        else
                        {
                            // Check for cancellation manually
                            cancellationToken.ThrowIfCancellationRequested();

                            // No complete line available within timeout, wait and retry
                            Task.Delay(10, cancellationToken).GetAwaiter().GetResult();
                            continue;
                        }

                        if (line != null)
                        {
                            output.AppendLine(line);
                            linesRead++;
                            lastOutputTime = DateTime.Now;

                            if (m_outputParser.IsCommandComplete(line))
                            {
                                m_logger.LogTrace("Command completion detected on line: '{Line}'", line);
                                break;
                            }
                        }
                        else
                        {
                            // End of stream
                            m_logger.LogDebug("Debugger output stream ended or process exited.");
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Re-throw cancellation to preserve cancellation semantics
                        throw;
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Error reading debugger output: {Message}", ex.Message);
                        output.AppendLine($"Error reading debugger output: {ex.Message}");
                        
                        // Break on critical errors to avoid infinite loop
                        if (ex is OutOfMemoryException or StackOverflowException)
                        {
                            throw;
                        }
                        
                        // Check for cancellation first - fail fast if already cancelled
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        // Wait before retrying to avoid tight loop
                        Task.Delay(100).Wait();
                    }
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
    }
}
