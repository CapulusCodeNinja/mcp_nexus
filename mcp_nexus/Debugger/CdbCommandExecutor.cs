using System.Text;
using System.Threading.Channels;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles command execution and output reading for CDB debugger sessions.
    /// Uses a producer-consumer architecture with session-scoped stream readers.
    /// </summary>
    public class CdbCommandExecutor : IDisposable
    {
        private readonly ILogger<CdbCommandExecutor> m_logger;
        private readonly CdbSessionConfiguration m_config;
        private readonly CdbOutputParser m_outputParser;
        private readonly object m_cancellationLock = new();
        private readonly SemaphoreSlim m_commandExecutionSemaphore = new(1, 1);
        private readonly SemaphoreSlim m_streamAccessSemaphore = new(1, 1);

        // Session-scoped architecture components
        private Channel<(string Line, bool IsStderr, DateTime Timestamp)>? m_sessionChannel;
        private Task? m_stdoutProducer;
        private Task? m_stderrProducer;
        private Task? m_consumer;
        private CancellationTokenSource? m_sessionCancellation;
        private readonly Dictionary<string, TaskCompletionSource<string>> m_pendingCommands = new();
        private readonly object m_pendingCommandsLock = new();

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
        /// Initializes the session-scoped producer-consumer architecture.
        /// Must be called before executing any commands.
        /// </summary>
        /// <param name="processManager">The CDB process manager providing access to streams.</param>
        /// <param name="cancellationToken">Cancellation token for the session.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="processManager"/> is null.</exception>
        public Task InitializeSessionAsync(CdbProcessManager processManager, CancellationToken cancellationToken = default)
        {
            if (m_sessionChannel != null)
            {
                m_logger.LogWarning("Session already initialized");
                return Task.CompletedTask;
            }

            m_logger.LogInformation("🚀 Initializing session-scoped producer-consumer architecture");

            // Create session-scoped channel
            m_sessionChannel = Channel.CreateUnbounded<(string Line, bool IsStderr, DateTime Timestamp)>(new UnboundedChannelOptions
            {
                SingleReader = true,  // Only consumer reads
                SingleWriter = false  // Both stdout and stderr producers write
            });

            // Create session cancellation token
            m_sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start session-scoped producers (they run for the entire session lifetime)
            m_stdoutProducer = StartStdoutProducerAsync(processManager, m_sessionCancellation.Token);
            m_stderrProducer = StartStderrProducerAsync(processManager, m_sessionCancellation.Token);

            // Start consumer (handles all parsing and command completion logic)
            m_consumer = StartConsumerAsync(m_sessionCancellation.Token);

            m_logger.LogInformation("✅ Session-scoped architecture initialized successfully");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes a command in the CDB session and returns the output asynchronously.
        /// </summary>
        /// <param name="command">The CDB command to execute.</param>
        /// <param name="processManager">The CDB process manager providing access to the debugger process streams.</param>
        /// <param name="externalCancellationToken">Optional cancellation token for external cancellation.</param>
        /// <returns>The command output as a string.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="processManager"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active debugging session is available or session is not initialized.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
        public async Task<string> ExecuteCommandAsync(
            string command,
            CdbProcessManager processManager,
            CancellationToken externalCancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            if (!processManager.IsActive)
                throw new InvalidOperationException("No active debugging session");

            if (m_sessionChannel == null)
                throw new InvalidOperationException("Session not initialized. Call InitializeSessionAsync first.");

            // Ensure only one command executes at a time
            await m_commandExecutionSemaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);
            try
            {
                m_logger.LogInformation("🎯 CDB ExecuteCommand START: {Command}", command);

                // Create command with sentinels
                var commandWithSentinels = CreateCommandWithSentinels(command);
                m_logger.LogDebug("Executing command with sentinels: '{Original}' -> '{WithSentinels}'", 
                    command, commandWithSentinels);

                // Send command to CDB
                await SendCommandToCdbAsync(processManager, commandWithSentinels, externalCancellationToken).ConfigureAwait(false);

                // Create completion source for this command
                var commandId = Guid.NewGuid().ToString();
                var completionSource = new TaskCompletionSource<string>();
                
                lock (m_pendingCommandsLock)
                {
                    m_pendingCommands[commandId] = completionSource;
                }

                // Wait for command completion with timeout
                var timeoutMs = m_config.OutputReadingTimeoutMs > 0 ? m_config.OutputReadingTimeoutMs : 300000; // 5 minutes default
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, timeoutCts.Token);

                try
                {
                    var result = await completionSource.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
                    m_logger.LogInformation("✅ CDB ExecuteCommand COMPLETED: {Command}", command);
                    return result;
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    m_logger.LogWarning("⏰ Command timed out after {TimeoutMs}ms: {Command}", timeoutMs, command);
                    
                    // Remove from pending commands
                    lock (m_pendingCommandsLock)
                    {
                        m_pendingCommands.Remove(commandId);
                    }
                    
                    return $"Command timed out after {timeoutMs}ms - output may be incomplete.";
                }
                finally
                {
                    // Clean up completion source
                    lock (m_pendingCommandsLock)
                    {
                        m_pendingCommands.Remove(commandId);
                    }
                }
            }
            finally
            {
                m_commandExecutionSemaphore.Release();
            }
        }

        /// <summary>
        /// Starts the stdout producer thread that reads from stdout for the entire session lifetime.
        /// This is a simple producer that only reads and sends to channel - no logic.
        /// </summary>
        /// <param name="processManager">The CDB process manager providing access to the stdout stream.</param>
        /// <param name="cancellationToken">Cancellation token for the producer thread.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="processManager"/> is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the producer is cancelled via the cancellation token.</exception>
        private async Task StartStdoutProducerAsync(CdbProcessManager processManager, CancellationToken cancellationToken)
        {
            m_logger.LogInformation("📤 Starting stdout producer thread");
            
            try
            {
                var debuggerOutput = processManager.DebuggerOutput;
                if (debuggerOutput == null)
                {
                    m_logger.LogError("No stdout stream available");
                    return;
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Check if CDB process has exited
                        var debuggerProcess = processManager.DebuggerProcess;
                        if (debuggerProcess?.HasExited == true)
                        {
                            m_logger.LogError("CDB process has exited unexpectedly. Exit code: {ExitCode}", 
                                debuggerProcess.ExitCode);
                            break;
                        }

                        // Read line from stdout
                        string? line;
                        await m_streamAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            line = await debuggerOutput.ReadLineAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            m_streamAccessSemaphore.Release();
                        }

                        if (line == null)
                        {
                            m_logger.LogInformation("📤 Stdout stream ended");
                            break; // End of stream
                        }

                        // Send to channel - NO LOGIC, just pure data transfer
                        if (m_sessionChannel != null)
                        {
                            await m_sessionChannel.Writer.WriteAsync((line, false, DateTime.Now), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        m_logger.LogDebug("📤 Stdout producer cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "📤 Error in stdout producer");
                        // Continue reading - don't let one error stop the producer
                    }
                }
            }
            finally
            {
                m_logger.LogInformation("📤 Stdout producer thread ended");
            }
        }

        /// <summary>
        /// Starts the stderr producer thread that reads from stderr for the entire session lifetime.
        /// This is a simple producer that only reads and sends to channel - no logic.
        /// </summary>
        /// <param name="processManager">The CDB process manager providing access to the stderr stream.</param>
        /// <param name="cancellationToken">Cancellation token for the producer thread.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="processManager"/> is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the producer is cancelled via the cancellation token.</exception>
        private async Task StartStderrProducerAsync(CdbProcessManager processManager, CancellationToken cancellationToken)
        {
            m_logger.LogInformation("📤 Starting stderr producer thread");
            
            try
            {
                var debuggerError = processManager.DebuggerError;
                if (debuggerError == null)
                {
                    m_logger.LogInformation("📤 No stderr stream available");
                    return;
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Check if CDB process has exited
                        var debuggerProcess = processManager.DebuggerProcess;
                        if (debuggerProcess?.HasExited == true)
                        {
                            m_logger.LogError("CDB process has exited unexpectedly. Exit code: {ExitCode}", 
                                debuggerProcess.ExitCode);
                            break;
                        }

                        // Read line from stderr
                        string? line;
                        await m_streamAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            line = await debuggerError.ReadLineAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            m_streamAccessSemaphore.Release();
                        }

                        if (line == null)
                        {
                            m_logger.LogInformation("📤 Stderr stream ended");
                            break; // End of stream
                        }

                        // Send to channel - NO LOGIC, just pure data transfer
                        if (m_sessionChannel != null)
                        {
                            await m_sessionChannel.Writer.WriteAsync((line, true, DateTime.Now), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        m_logger.LogDebug("📤 Stderr producer cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "📤 Error in stderr producer");
                        // Continue reading - don't let one error stop the producer
                    }
                }
            }
            finally
            {
                m_logger.LogInformation("📤 Stderr producer thread ended");
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
            m_logger.LogInformation("🧠 Starting consumer thread");
            
            try
            {
                if (m_sessionChannel == null)
                {
                    m_logger.LogError("Session channel not initialized");
                    return;
                }

                var currentCommandOutput = new StringBuilder();
                var currentCommandStderr = new List<string>();
                var currentCommandId = string.Empty;
                var inCommand = false;

                await foreach (var (line, isStderr, timestamp) in m_sessionChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        // Handle start sentinel
                        if (string.Equals(line, CdbSentinels.StartMarker, StringComparison.Ordinal) ||
                            line.Contains(CdbSentinels.StartMarker))
                        {
                            m_logger.LogDebug("🧠 Start sentinel detected: {Line}", line);
                            
                            // Complete previous command if any
                            if (inCommand && !string.IsNullOrEmpty(currentCommandId))
                            {
                                await CompleteCurrentCommandAsync(currentCommandId, currentCommandOutput.ToString(), currentCommandStderr).ConfigureAwait(false);
                            }
                            
                            // Start new command
                            currentCommandId = Guid.NewGuid().ToString();
                            currentCommandOutput.Clear();
                            currentCommandStderr.Clear();
                            inCommand = true;
                            continue;
                        }

                        // Handle end sentinel
                        if (string.Equals(line, CdbSentinels.EndMarker, StringComparison.Ordinal) ||
                            line.Contains(CdbSentinels.EndMarker))
                        {
                            m_logger.LogInformation("🧠 End sentinel detected - completing command: {Line}", line);
                            
                            if (inCommand && !string.IsNullOrEmpty(currentCommandId))
                            {
                                await CompleteCurrentCommandAsync(currentCommandId, currentCommandOutput.ToString(), currentCommandStderr).ConfigureAwait(false);
                            }
                            
                            // Reset for next command
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
                            {
                                currentCommandStderr.Add(line);
                            }
                            else
                            {
                                currentCommandOutput.AppendLine(line);
                            }
                        }
                        else
                        {
                            // Output outside of command context - could be CDB prompts, etc.
                            m_logger.LogDebug("🧠 Output outside command context: {Line}", line);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "🧠 Error processing line in consumer: {Line}", line);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_logger.LogDebug("🧠 Consumer cancelled");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🧠 Error in consumer thread");
            }
            finally
            {
                m_logger.LogInformation("🧠 Consumer thread ended");
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

                // Find and complete the pending command
                TaskCompletionSource<string>? completionSource = null;
                lock (m_pendingCommandsLock)
                {
                    // Find the oldest pending command (FIFO)
                    var oldestCommand = m_pendingCommands.FirstOrDefault();
                    if (oldestCommand.Key != null)
                    {
                        completionSource = oldestCommand.Value;
                        m_pendingCommands.Remove(oldestCommand.Key);
                    }
                }

                if (completionSource != null)
                {
                    m_logger.LogInformation("🧠 Completing command {CommandId} with {OutputLength} chars", commandId, result.Length);
                    completionSource.SetResult(result);
                }
                else
                {
                    m_logger.LogWarning("🧠 No pending command found for completion: {CommandId}", commandId);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "🧠 Error completing command: {CommandId}", commandId);
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a command with start and end sentinels for proper output parsing.
        /// </summary>
        /// <param name="command">The original command to wrap with sentinels.</param>
        /// <returns>A command string with start and end sentinels for output parsing.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
        private string CreateCommandWithSentinels(string command)
        {
            return $".echo {CdbSentinels.StartMarker}; {command}; .echo {CdbSentinels.EndMarker}";
        }

        /// <summary>
        /// Sends a command to the CDB process input stream.
        /// </summary>
        /// <param name="processManager">The CDB process manager providing access to the input stream.</param>
        /// <param name="command">The command to send to CDB.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="processManager"/> or <paramref name="command"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no input stream is available for sending commands.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
        private async Task SendCommandToCdbAsync(CdbProcessManager processManager, string command, CancellationToken cancellationToken)
        {
            var debuggerInput = processManager.DebuggerInput;
            if (debuggerInput == null)
            {
                throw new InvalidOperationException("No input stream available for sending command");
            }

            m_logger.LogDebug("Sending command to CDB: {Command}", command);

            // TRUE ASYNC: Use WriteLineAsync instead of blocking WriteLine
            await debuggerInput.WriteLineAsync(command).ConfigureAwait(false);
            await debuggerInput.FlushAsync().ConfigureAwait(false);

            m_logger.LogDebug("Command sent successfully to CDB");
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
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_logger.LogInformation("🧹 Disposing CdbCommandExecutor");

                // Cancel session
                m_sessionCancellation?.Cancel();

                // Complete channel to stop consumer
                m_sessionChannel?.Writer.TryComplete();

                // Wait for all tasks to complete
                try
                {
                    Task.WaitAll(
                        new[] { m_stdoutProducer, m_stderrProducer, m_consumer }
                            .Where(t => t != null)
                            .Cast<Task>()
                            .ToArray(),
                        TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Error waiting for tasks to complete during disposal");
                }

                // Dispose resources
                m_sessionCancellation?.Dispose();
                m_commandExecutionSemaphore?.Dispose();
                m_streamAccessSemaphore?.Dispose();

                m_logger.LogInformation("✅ CdbCommandExecutor disposed");
            }
        }
    }
}