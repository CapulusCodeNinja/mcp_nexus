using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles command execution and output reading for CDB debugger sessions.
    /// Uses event-based stream reading to avoid deadlocks.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CdbCommandExecutor"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="config">The CDB session configuration.</param>
    /// <param name="outputParser">The output parser for processing CDB responses.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public class CdbCommandExecutor(ILogger<CdbCommandExecutor> logger, CdbSessionConfiguration config, CdbOutputParser outputParser) : IDisposable
    {
        private readonly ILogger<CdbCommandExecutor> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly CdbSessionConfiguration m_Config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly CdbOutputParser m_OutputParser = outputParser ?? throw new ArgumentNullException(nameof(outputParser));
        private readonly object m_CancellationLock = new();

        private readonly SemaphoreSlim m_CommandExecutionSemaphore = new(1, 1);

        // Session-scoped architecture components
        private Channel<(string Line, bool IsStderr, DateTime Timestamp)>? m_SessionChannel;
        private Task? m_Consumer;
        private CancellationTokenSource? m_SessionCancellation;
        private readonly Dictionary<string, TaskCompletionSource<string>> m_pendingCommands = [];
        private readonly object m_pendingCommandsLock = new();
        private string? m_currentCommandId = null;

        /// <summary>
        /// Event handler for data received from CDB process streams.
        /// Acts as the producer in the producer-consumer pattern.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The data received event arguments.</param>
        /// <param name="isStderr">True if this is stderr data, false for stdout.</param>
        private void DataReceivedHandler(object sender, DataReceivedEventArgs e, bool isStderr)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            // Parse lines without allocations: scan for \r or \n and slice
            if (m_SessionChannel != null)
            {
                ReadOnlySpan<char> span = e.Data.AsSpan();
                int start = 0;
                for (int i = 0; i < span.Length; i++)
                {
                    char c = span[i];
                    if (c == '\r' || c == '\n')
                    {
                        if (i > start)
                        {
                            var line = new string(span.Slice(start, i - start));
                            var item = (line, isStderr, DateTime.Now);
                            if (!m_SessionChannel.Writer.TryWrite(item))
                            {
                                var vt = m_SessionChannel.Writer.WriteAsync(item);
                                if (!vt.IsCompletedSuccessfully)
                                {
                                    _ = vt.AsTask();
                                }
                            }
                        }
                        start = i + 1; // skip delimiter
                    }
                }

                // Trailing segment
                if (start < span.Length)
                {
                    var line = new string(span.Slice(start));
                    var item = (line, isStderr, DateTime.Now);
                    if (!m_SessionChannel.Writer.TryWrite(item))
                    {
                        var vt = m_SessionChannel.Writer.WriteAsync(item);
                        if (!vt.IsCompletedSuccessfully)
                        {
                            _ = vt.AsTask();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the session-scoped event-based stream reading and consumer thread.
        /// Must be called before executing any commands.
        /// </summary>
        /// <param name="processManager">The CDB process manager providing access to streams.</param>
        /// <param name="cancellationToken">Cancellation token for the session.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="processManager"/> is null.</exception>
        public Task InitializeSessionAsync(CdbProcessManager processManager, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(processManager);

            if (m_SessionChannel != null)
            {
                m_Logger.LogWarning("Session already initialized");
                return Task.CompletedTask;
            }

            m_Logger.LogDebug("🚀 Initializing session-scoped event-based stream reading");

            // Create session-scoped channel
            m_SessionChannel = Channel.CreateUnbounded<(string Line, bool IsStderr, DateTime Timestamp)>(new UnboundedChannelOptions
            {
                SingleReader = true,  // Only consumer reads
                SingleWriter = false  // Event handlers write
            });

            // Create session cancellation token
            m_SessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start consumer (handles all parsing and command completion logic)
            m_Consumer = StartConsumerAsync(m_SessionCancellation.Token);

            // Register event handlers for non-blocking stream reading
            var debuggerProcess = processManager.DebuggerProcess;
            if (debuggerProcess != null)
            {
                debuggerProcess.OutputDataReceived += (s, e) => DataReceivedHandler(s, e, false);
                debuggerProcess.ErrorDataReceived += (s, e) => DataReceivedHandler(s, e, true);

                // Start non-blocking reading from streams
                debuggerProcess.BeginOutputReadLine();
                debuggerProcess.BeginErrorReadLine();

                m_Logger.LogDebug("🚀 Event-based stream reading started");
            }


            m_Logger.LogDebug("✅ Session-scoped event-based architecture initialized successfully");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes a command in the CDB session and returns the output asynchronously.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="commandId">The unique command ID from the command queue.</param>
        /// <param name="processManager">The CDB process manager.</param>
        /// <param name="externalCancellationToken">External cancellation token.</param>
        /// <returns>The command output.</returns>
        /// <exception cref="ArgumentException">Thrown when command is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active session or session not initialized.</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled.</exception>
        /// <exception cref="TimeoutException">Thrown when command execution times out.</exception>
        public async Task<string> ExecuteCommandAsync(
            string command,
            string commandId,
            CdbProcessManager processManager,
            CancellationToken externalCancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            if (!processManager.IsActive)
                throw new InvalidOperationException("No active debugging session");

            if (m_SessionChannel == null)
                throw new InvalidOperationException("Session not initialized. Call InitializeSessionAsync first.");

            // Ensure only one command executes at a time
            await m_CommandExecutionSemaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);
            try
            {
                m_Logger.LogDebug("🎯 CDB ExecuteCommand START: {Command}", command);

                // Create command with sentinels
                var commandWithSentinels = CreateCommandWithSentinels(command);
                m_Logger.LogDebug("Executing command with sentinels: '{Original}' -> '{WithSentinels}'",
                    command, commandWithSentinels);

                // Create completion source for this command using the provided command ID
                var completionSource = new TaskCompletionSource<string>();

                lock (m_pendingCommandsLock)
                {
                    m_pendingCommands[commandId] = completionSource;
                    m_currentCommandId = commandId; // Set the current command ID for the consumer
                }

                m_Logger.LogDebug("🎯 About to call SendCommandToCdbAsync");
                // Send command to CDB
                await SendCommandToCdbAsync(processManager, commandWithSentinels, externalCancellationToken).ConfigureAwait(false);
                m_Logger.LogDebug("🎯 SendCommandToCdbAsync completed");

                // Wait for command completion with timeout
                // Use CommandTimeoutMs for command execution - must be configured
                if (m_Config.CommandTimeoutMs <= 0)
                {
                    throw new InvalidOperationException("CommandTimeoutMs must be configured and greater than 0. Current value: " + m_Config.CommandTimeoutMs);
                }
                var timeoutMs = m_Config.CommandTimeoutMs;
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, timeoutCts.Token);

                try
                {
                    var result = await completionSource.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
                    m_Logger.LogDebug("✅ CDB ExecuteCommand COMPLETED: {Command}", command);
                    return result;
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    m_Logger.LogError("⏰ CDB ExecuteCommand TIMEOUT: {Command} (timeout: {TimeoutMs}ms)", command, timeoutMs);
                    throw new TimeoutException($"Command execution timed out after {timeoutMs}ms");
                }
                catch (OperationCanceledException)
                {
                    m_Logger.LogWarning("🚫 CDB ExecuteCommand CANCELLED: {Command}", command);
                    throw;
                }
            }
            finally
            {
                m_CommandExecutionSemaphore.Release();
            }
        }

        /// <summary>
        /// Starts the consumer thread that handles all parsing, sentinel detection, and command completion logic.
        /// This is the smart component that processes all output and determines when commands are complete.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the consumer thread.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the consumer is cancelled via the cancellation token.</exception>
        private async Task StartConsumerAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogDebug("🧠 Starting consumer thread");

            try
            {
                if (m_SessionChannel == null)
                {
                    m_Logger.LogError("Session channel not initialized");
                    return;
                }

                var currentCommandOutput = new StringBuilder();
                var currentCommandStderr = new List<string>();
                var currentCommandId = string.Empty;
                var inCommand = false;

                await foreach (var (line, isStderr, timestamp) in m_SessionChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        // Handle start sentinel
                        if (string.Equals(line, CdbSentinels.StartMarker, StringComparison.Ordinal) ||
                            line.Contains(CdbSentinels.StartMarker))
                        {
                            m_Logger.LogDebug("🧠 Start sentinel detected: {Line}", line);

                            // Complete previous command if any
                            if (inCommand && !string.IsNullOrEmpty(currentCommandId))
                            {
                                await CompleteCurrentCommandAsync(currentCommandId, currentCommandOutput.ToString(), currentCommandStderr).ConfigureAwait(false);
                            }

                            // Start new command - get the current command ID from the executor
                            lock (m_pendingCommandsLock)
                            {
                                currentCommandId = m_currentCommandId ?? string.Empty;
                            }
                            currentCommandOutput.Clear();
                            currentCommandStderr.Clear();
                            inCommand = true;
                            continue;
                        }

                        // Handle end sentinel
                        if (string.Equals(line, CdbSentinels.EndMarker, StringComparison.Ordinal) ||
                            line.Contains(CdbSentinels.EndMarker))
                        {
                            m_Logger.LogDebug("🧠 End sentinel detected - completing command: {Line}", line);

                            if (inCommand && !string.IsNullOrEmpty(currentCommandId))
                            {
                                await CompleteCurrentCommandAsync(currentCommandId, currentCommandOutput.ToString(), currentCommandStderr).ConfigureAwait(false);
                            }

                            // Reset for next command
                            lock (m_pendingCommandsLock)
                            {
                                m_currentCommandId = null; // Clear the current command ID
                            }
                            currentCommandId = string.Empty;
                            currentCommandOutput.Clear();
                            currentCommandStderr.Clear();
                            inCommand = false;
                            continue;
                        }

                        // FALLBACK: Check for CDB prompt patterns (100% reliable)
                        if (CdbCompletionPatterns.IsCdbPrompt(line))
                        {
                            m_Logger.LogDebug("🧠 CDB prompt detected - completing command: {Line}", line);

                            if (inCommand && !string.IsNullOrEmpty(currentCommandId))
                            {
                                await CompleteCurrentCommandAsync(currentCommandId, currentCommandOutput.ToString(), currentCommandStderr).ConfigureAwait(false);
                            }

                            // Reset for next command
                            lock (m_pendingCommandsLock)
                            {
                                m_currentCommandId = null; // Clear the current command ID
                            }
                            currentCommandId = string.Empty;
                            currentCommandOutput.Clear();
                            currentCommandStderr.Clear();
                            inCommand = false;
                            continue;
                        }

                        // FALLBACK: Check for ultra-safe completion patterns
                        if (CdbCompletionPatterns.IsUltraSafeCompletion(line))
                        {
                            m_Logger.LogDebug("🧠 Ultra-safe completion pattern detected - completing command: {Line}", line);

                            if (inCommand && !string.IsNullOrEmpty(currentCommandId))
                            {
                                await CompleteCurrentCommandAsync(currentCommandId, currentCommandOutput.ToString(), currentCommandStderr).ConfigureAwait(false);
                            }

                            // Reset for next command
                            lock (m_pendingCommandsLock)
                            {
                                m_currentCommandId = null; // Clear the current command ID
                            }
                            currentCommandId = string.Empty;
                            currentCommandOutput.Clear();
                            currentCommandStderr.Clear();
                            inCommand = false;
                            continue;
                        }

                        // Handle regular output
                        if (inCommand)
                        {
                            if (isStderr)
                                currentCommandStderr.Add(line);
                            else
                                currentCommandOutput.AppendLine(line);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        m_Logger.LogDebug("🧠 Consumer processing cancelled for line: {Line}", line);
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex, "🧠 Error processing line in consumer: {Line}", line);
                        // Continue processing other lines
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("🧠 Consumer thread cancelled");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "🧠 Error in consumer thread");
            }
            finally
            {
                m_Logger.LogDebug("🧠 Consumer thread ended");
            }
        }

        /// <summary>
        /// Completes a command by setting the result in the pending commands dictionary.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command to complete.</param>
        /// <param name="output">The stdout output from the command.</param>
        /// <param name="stderr">The stderr output from the command.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandId"/>, <paramref name="output"/>, or <paramref name="stderr"/> is null.</exception>
        private Task CompleteCurrentCommandAsync(string commandId, string output, List<string> stderr)
        {
            try
            {
                // Merge stderr into result if present
                var result = output;
                if (stderr.Count > 0)
                {
                    result += "\n--- STDERR ---\n" + string.Join("\n", stderr);
                }

                // Find and complete the pending command by ID
                TaskCompletionSource<string>? completionSource = null;
                lock (m_pendingCommandsLock)
                {
                    if (m_pendingCommands.TryGetValue(commandId, out completionSource))
                    {
                        m_pendingCommands.Remove(commandId);
                    }
                }

                if (completionSource != null)
                {
                    m_Logger.LogDebug("🧠 Completing command {CommandId} with {OutputLength} chars", commandId, result.Length);
                    completionSource.SetResult(result);
                }
                else
                {
                    m_Logger.LogWarning("🧠 No pending command found for completion: {CommandId}", commandId);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "🧠 Error completing command: {CommandId}", commandId);
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// Creates a command with start and end sentinels for proper output parsing.
        /// </summary>
        /// <param name="command">The original command to wrap with sentinels.</param>
        /// <returns>The command wrapped with start and end sentinels.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
        private static string CreateCommandWithSentinels(string command)
        {
            ArgumentNullException.ThrowIfNull(command);

            return $".echo {CdbSentinels.StartMarker}; {command}; .echo {CdbSentinels.EndMarker}";
        }

        /// <summary>
        /// Executes a batch command in the CDB session without single-command sentinel wrapping.
        /// This method is specifically for batch commands that have their own sentinel system.
        /// </summary>
        /// <param name="batchCommand">The batch command to execute (with semicolon-separated commands).</param>
        /// <param name="processManager">The CDB process manager.</param>
        /// <param name="externalCancellationToken">External cancellation token.</param>
        /// <returns>The batch command output.</returns>
        /// <exception cref="ArgumentException">Thrown when batchCommand is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active session or session not initialized.</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled.</exception>
        /// <exception cref="TimeoutException">Thrown when command execution times out.</exception>
        public async Task<string> ExecuteBatchCommandAsync(
            string batchCommand,
            CdbProcessManager processManager,
            CancellationToken externalCancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(batchCommand))
                throw new ArgumentException("Batch command cannot be null or empty", nameof(batchCommand));

            if (!processManager.IsActive)
                throw new InvalidOperationException("No active debugging session");

            if (m_SessionChannel == null)
                throw new InvalidOperationException("Session not initialized. Call InitializeSessionAsync first.");

            await m_CommandExecutionSemaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);
            try
            {
                m_Logger.LogDebug("🎯 CDB ExecuteBatchCommand START (no sentinel wrapping)");

                var batchCommandId = $"BATCH-{Guid.NewGuid()}";
                var completionSource = new TaskCompletionSource<string>();

                lock (m_pendingCommandsLock)
                {
                    m_pendingCommands[batchCommandId] = completionSource;
                    m_currentCommandId = batchCommandId;
                }

                // Wrap entire batch in single-command sentinels so normal completion applies
                var wrappedBatch = CreateCommandWithSentinels(batchCommand);
                await SendCommandToCdbAsync(processManager, wrappedBatch, externalCancellationToken).ConfigureAwait(false);

                var timeoutMs = m_Config.CommandTimeoutMs;
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, timeoutCts.Token);

                try
                {
                    var result = await completionSource.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
                    m_Logger.LogDebug("✅ CDB ExecuteBatchCommand COMPLETED");
                    return result;
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    m_Logger.LogError("⏰ CDB ExecuteBatchCommand TIMEOUT (timeout: {TimeoutMs}ms)", timeoutMs);
                    throw new TimeoutException($"Batch command execution timed out after {timeoutMs}ms");
                }
                catch (OperationCanceledException)
                {
                    m_Logger.LogWarning("🚫 CDB ExecuteBatchCommand CANCELLED");
                    throw;
                }
            }
            finally
            {
                m_CommandExecutionSemaphore.Release();
            }
        }

        /// <summary>
        /// Sends a command to the CDB process input stream.
        /// </summary>
        /// <param name="processManager">The CDB process manager.</param>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no input stream is available for sending commands.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
        private async Task SendCommandToCdbAsync(CdbProcessManager processManager, string command, CancellationToken cancellationToken)
        {
            var debuggerInput = processManager.DebuggerInput ?? throw new InvalidOperationException("No input stream available for sending command");
            m_Logger.LogDebug("Sending command to CDB: {Command}", command);

            // TRUE ASYNC: Use WriteLineAsync instead of blocking WriteLine
            await debuggerInput.WriteLineAsync(command).ConfigureAwait(false);
            await debuggerInput.FlushAsync(cancellationToken).ConfigureAwait(false);

            m_Logger.LogDebug("Command sent successfully to CDB");
        }

        /// <summary>
        /// Disposes of the command executor and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the command executor and cleans up resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_Logger.LogDebug("🧹 Disposing CdbCommandExecutor");

                // Cancel session
                m_SessionCancellation?.Cancel();

                // Complete channel to stop consumer
                m_SessionChannel?.Writer.TryComplete();

                // Wait for consumer task to complete
                try
                {
                    if (m_Consumer != null)
                    {
                        Task.WaitAll([m_Consumer], TimeSpan.FromSeconds(5));
                    }
                }
                catch (Exception ex)
                {

                    m_Logger.LogWarning(ex, "Error waiting for tasks to complete during disposal");
                }

                // Complete any pending commands with error
                lock (m_pendingCommandsLock)
                {
                    foreach (var kvp in m_pendingCommands)
                    {
                        kvp.Value.TrySetCanceled();
                    }
                    m_pendingCommands.Clear();
                }

                // Dispose resources
                m_SessionCancellation?.Dispose();
                m_CommandExecutionSemaphore?.Dispose();

                m_Logger.LogDebug("🧹 CdbCommandExecutor disposed");
            }
        }
    }
}
