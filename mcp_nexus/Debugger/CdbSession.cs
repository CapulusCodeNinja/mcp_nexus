using System.Diagnostics;
using mcp_nexus.Utilities.Validation;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Refactored CDB session that orchestrates focused components for debugger operations.
    /// Provides a high-level interface for managing CDB debugging sessions with thread-safe command execution.
    /// </summary>
    public class CdbSession : IDisposable, ICdbSession
    {
        private readonly ILogger<CdbSession> m_Logger;
        private readonly CdbSessionConfiguration m_Config;
        private readonly CdbProcessManager m_ProcessManager;
        private readonly CdbCommandExecutor m_CommandExecutor;
        private readonly CdbOutputParser m_OutputParser;
        private readonly ICommandPreprocessor? m_CommandPreprocessor;

        // CRITICAL: Semaphore to ensure only ONE command executes at a time in CDB
        // CDB is single-threaded and cannot handle concurrent commands
        private readonly SemaphoreSlim m_CommandSemaphore = new(1, 1);

        private bool m_Disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbSession"/> class.
        /// Maintains backward compatibility with the original constructor signature.
        /// </summary>
        /// <param name="logger">The logger instance for recording session operations and errors.</param>
        /// <param name="commandTimeoutMs">The timeout in milliseconds for command execution. Default is 30000ms (30 seconds).</param>
        /// <param name="idleTimeoutMs">The timeout in milliseconds for idle operations. Default is 180000ms (3 minutes).</param>
        /// <param name="customCdbPath">Optional custom path to the CDB executable. If null, uses the default path.</param>
        /// <param name="symbolServerMaxRetries">The maximum number of retries for symbol server operations. Default is 1.</param>
        /// <param name="symbolSearchPath">Optional symbol search path for CDB. If null, uses the default path.</param>
        /// <param name="startupDelayMs">The delay in milliseconds before starting the session. Default is 1000ms (1 second).</param>
        /// <param name="outputReadingTimeoutMs">The output reading timeout in milliseconds. Default is 300000ms (5 minutes).</param>
        /// <param name="enableCommandPreprocessing">Whether to enable command preprocessing (path conversion and directory creation). Default is true.</param>
        /// <param name="sessionId">Optional session ID for creating session-specific log files. If null, uses default naming.</param>
        /// <param name="commandPreprocessor">Optional command preprocessor for path conversion. If null and preprocessing is enabled, preprocessing will be skipped.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public CdbSession(
            ILogger<CdbSession> logger,
            int commandTimeoutMs = 30000,
            int idleTimeoutMs = 180000,
            string? customCdbPath = null,
            int symbolServerMaxRetries = 1,
            string? symbolSearchPath = null,
            int startupDelayMs = 1000,
            int outputReadingTimeoutMs = 300000,
            bool enableCommandPreprocessing = true,
            string? sessionId = null,
            ICommandPreprocessor? commandPreprocessor = null)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_CommandPreprocessor = commandPreprocessor;

            // Create configuration with validation
            m_Config = new CdbSessionConfiguration(
                commandTimeoutMs: commandTimeoutMs,
                idleTimeoutMs: idleTimeoutMs,
                customCdbPath: customCdbPath,
                symbolServerMaxRetries: symbolServerMaxRetries,
                symbolSearchPath: symbolSearchPath,
                startupDelayMs: startupDelayMs,
                outputReadingTimeoutMs: outputReadingTimeoutMs,
                enableCommandPreprocessing: enableCommandPreprocessing);

            // CRITICAL FIX: Logger casting was failing, causing NullLogger to be used!
            // This resulted in zero visibility into CdbProcessManager init consumer
            // Instead of casting (which fails), create simple wrapper loggers
            var processLogger = new LoggerWrapper<CdbProcessManager>(logger);
            var parserLogger = new LoggerWrapper<CdbOutputParser>(logger);
            var executorLogger = new LoggerWrapper<CdbCommandExecutor>(logger);

            m_ProcessManager = new CdbProcessManager(processLogger, m_Config);
            m_OutputParser = new CdbOutputParser(parserLogger);
            m_CommandExecutor = new CdbCommandExecutor(executorLogger, m_Config, m_OutputParser);

            // Set session ID for session-specific log files
            if (!string.IsNullOrEmpty(sessionId))
            {
                m_ProcessManager.SetSessionId(sessionId);
            }

            m_Logger.LogDebug("CdbSession initialized with focused components");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbSession"/> class with a pre-configured <see cref="CdbSessionConfiguration"/>.
        /// </summary>
        /// <param name="logger">The logger instance for recording session operations and errors.</param>
        /// <param name="config">The CDB session configuration object.</param>
        /// <param name="sessionId">Optional session ID for creating session-specific log files. If null, uses default naming.</param>
        /// <param name="commandPreprocessor">Optional command preprocessor for path conversion. If null and preprocessing is enabled, preprocessing will be skipped.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="config"/> is null.</exception>
        public CdbSession(
            ILogger<CdbSession> logger,
            CdbSessionConfiguration config,
            string? sessionId = null,
            ICommandPreprocessor? commandPreprocessor = null)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_CommandPreprocessor = commandPreprocessor;

            // CRITICAL FIX: Logger casting was failing, causing NullLogger to be used!
            // This resulted in zero visibility into CdbProcessManager init consumer
            // Instead of casting (which fails), create simple wrapper loggers
            var processLogger = new LoggerWrapper<CdbProcessManager>(logger);
            var parserLogger = new LoggerWrapper<CdbOutputParser>(logger);
            var executorLogger = new LoggerWrapper<CdbCommandExecutor>(logger);

            m_ProcessManager = new CdbProcessManager(processLogger, m_Config);
            m_OutputParser = new CdbOutputParser(parserLogger);
            m_CommandExecutor = new CdbCommandExecutor(executorLogger, m_Config, m_OutputParser);

            // Set session ID for session-specific log files
            if (!string.IsNullOrEmpty(sessionId))
            {
                m_ProcessManager.SetSessionId(sessionId);
            }

            m_Logger.LogDebug("CdbSession initialized with focused components and configuration object");
        }

        /// <summary>
        /// Simple logger wrapper that implements ILogger&lt;T&gt; by forwarding to an untyped logger
        /// This fixes the NullLogger issue when logger casting fails
        /// </summary>
        private class LoggerWrapper<T>(ILogger logger) : ILogger<T>
        {
            private readonly ILogger m_InnerLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
                => m_InnerLogger.BeginScope(state);

            /// <summary>
            /// Determines if logging is enabled for the specified log level.
            /// </summary>
            /// <param name="logLevel">The log level to check.</param>
            /// <returns><c>true</c> if logging is enabled for the specified level; otherwise, <c>false</c>.</returns>
            public bool IsEnabled(LogLevel logLevel)
                => m_InnerLogger.IsEnabled(logLevel);

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => m_InnerLogger.Log(logLevel, eventId, state, exception, formatter);
        }

        /// <summary>
        /// Gets a value indicating whether the CDB session is currently active and ready for commands.
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (m_Disposed)
                    return false;

                return m_ProcessManager.IsActive;
            }
        }

        /// <summary>
        /// Gets the process ID of the CDB debugger process.
        /// </summary>
        public int? ProcessId
        {
            get
            {
                if (m_Disposed)
                    return null;

                try
                {
                    return m_ProcessManager.DebuggerProcess?.Id;
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Could not retrieve CDB process ID");
                    return null;
                }
            }
        }

        /// <summary>
        /// Cancels the currently executing command operation.
        /// This method is thread-safe and can be called from any thread.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public void CancelCurrentOperation()
        {
            ThrowIfDisposed();

            m_Logger.LogWarning("Cancelling current operation");
            // Note: With the new architecture, cancellation is handled at the session level
            // Individual command cancellation is managed through CancellationToken
        }

        /// <summary>
        /// Starts a debugging session with the specified target.
        /// This method initializes the CDB process and prepares it for command execution.
        /// </summary>
        /// <param name="target">The target to debug (dump file path or process ID).</param>
        /// <param name="arguments">Optional additional arguments for the CDB process. Currently not used.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the session was started successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="target"/> is null or empty.</exception>
        public async Task<bool> StartSession(string target, string? arguments = null)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target cannot be null or empty", nameof(target));

            m_Logger.LogInformation("🔧 StartSession called");
            m_Logger.LogDebug("StartSession target: {Target}, arguments: {Arguments}, ProcessManager null: {IsNull}", target, arguments, m_ProcessManager == null);

            try
            {
                m_Logger.LogDebug("About to call StartProcess directly");
                var result = m_ProcessManager?.StartProcess(target, m_Config.CustomCdbPath) ?? throw new InvalidOperationException("Process manager not initialized");
                m_Logger.LogDebug("🔧 StartProcess returned: {Result}", result);

                if (result)
                {
                    // Initialize the session-scoped producer-consumer architecture
                    m_Logger.LogDebug("🔧 Initializing session-scoped architecture");
                    await m_CommandExecutor.InitializeSessionAsync(m_ProcessManager).ConfigureAwait(false);
                    m_Logger.LogDebug("🔧 Session-scoped architecture initialized successfully");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("❌ StartSession timed out after {TimeoutMs}ms for target: {Target}", m_Config.CommandTimeoutMs, target);
                return false;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "❌ Failed to start CDB session with target: {Target}", target);
                return false;
            }
        }

        /// <summary>
        /// Executes a command in the debugging session without cancellation support.
        /// This is a convenience method that calls <see cref="ExecuteCommand(string, CancellationToken)"/> with <see cref="CancellationToken.None"/>.
        /// </summary>
        /// <param name="command">The CDB command to execute.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the command output as a string, or an error message if execution fails.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active debugging session is available.</exception>
        public Task<string> ExecuteCommand(string command)
        {
            return ExecuteCommand(command, CancellationToken.None);
        }

        /// <summary>
        /// Executes a command in the debugging session with cancellation support.
        /// This method ensures thread-safe execution by using a semaphore to serialize command execution.
        /// </summary>
        /// <param name="command">The CDB command to execute.</param>
        /// <param name="externalCancellationToken">The cancellation token for external cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the command output as a string, or an error message if execution fails.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active debugging session is available.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
        public async Task<string> ExecuteCommand(string command, CancellationToken externalCancellationToken)
        {
            return await ExecuteCommand(command, Guid.NewGuid().ToString(), externalCancellationToken);
        }

        /// <summary>
        /// Executes a command in the debugging session with cancellation support and command ID.
        /// This method ensures thread-safe execution by using a semaphore to serialize command execution.
        /// </summary>
        /// <param name="command">The CDB command to execute.</param>
        /// <param name="commandId">The unique command ID from the command queue.</param>
        /// <param name="externalCancellationToken">The cancellation token for external cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the command output as a string, or an error message if execution fails.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active debugging session is available.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
        public async Task<string> ExecuteCommand(string command, string commandId, CancellationToken externalCancellationToken)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            // CRITICAL: Use semaphore to ensure ONLY ONE command executes at a time
            // CDB is single-threaded and crashes/hangs if multiple commands run concurrently
            await m_CommandSemaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);

            try
            {
                if (!IsActive)
                {
                    m_Logger.LogError("Cannot execute command '{Command}' - CDB session is not active (IsActive={IsActive})", command, IsActive);
                    throw new InvalidOperationException("No active debugging session");
                }

                m_Logger.LogDebug("🔒 SEMAPHORE: About to execute command '{Command}'", command);

                // Preprocess the command to fix common issues (e.g., .srcpath path conversion and directory creation)
                // Only if preprocessing is enabled in configuration and preprocessor is available
                var preprocessedCommand = command;
                if (m_Config.EnableCommandPreprocessing && m_CommandPreprocessor != null)
                {
                    preprocessedCommand = m_CommandPreprocessor.PreprocessCommand(command);
                    if (preprocessedCommand != command)
                    {
                        m_Logger.LogDebug("🔧 Command preprocessed: '{Original}' -> '{Preprocessed}'", command, preprocessedCommand);
                    }
                }
                else if (m_Config.EnableCommandPreprocessing && m_CommandPreprocessor == null)
                {
                    m_Logger.LogWarning("⚠️ Command preprocessing is ENABLED but no preprocessor provided - sending command as-is: '{Command}'", command);
                }
                else
                {
                    m_Logger.LogDebug("⚠️ Command preprocessing is DISABLED - sending command as-is: '{Command}'", command);
                }

                // TRUE ASYNC: Direct call without Task.Run - proper async all the way through
                // Semaphore ensures serialization, no need for thread pool wrapper
                var result = await m_CommandExecutor.ExecuteCommandAsync(preprocessedCommand, commandId, m_ProcessManager, externalCancellationToken).ConfigureAwait(false);

                m_Logger.LogDebug("🔒 SEMAPHORE: Command '{Command}' completed, result length: {Length}", command, result?.Length ?? 0);

                return result ?? string.Empty;
            }
            finally
            {
                m_CommandSemaphore.Release();
            }
        }

        /// <summary>
        /// Executes a batch command in the debugging session without single-command sentinel wrapping.
        /// This method is specifically for batch commands that have their own sentinel system.
        /// </summary>
        /// <param name="batchCommand">The batch command to execute (with semicolon-separated commands).</param>
        /// <param name="externalCancellationToken">The cancellation token for external cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the batch command output as a string, or an error message if execution fails.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="batchCommand"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active debugging session is available.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
        public async Task<string> ExecuteBatchCommand(string batchCommand, CancellationToken externalCancellationToken)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(batchCommand))
                throw new ArgumentException("Batch command cannot be null or empty", nameof(batchCommand));

            // CRITICAL: Use semaphore to ensure ONLY ONE command executes at a time
            // CDB is single-threaded and crashes/hangs if multiple commands run concurrently
            await m_CommandSemaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);

            try
            {
                if (!IsActive)
                {
                    m_Logger.LogError("Cannot execute batch command - CDB session is not active (IsActive={IsActive})", IsActive);
                    throw new InvalidOperationException("No active debugging session");
                }

                m_Logger.LogDebug("🔒 SEMAPHORE: About to execute BATCH command (bypassing single command sentinels)");

                // Call ExecuteBatchCommandAsync directly without sentinel wrapping
                var result = await m_CommandExecutor.ExecuteBatchCommandAsync(batchCommand, m_ProcessManager, externalCancellationToken).ConfigureAwait(false);

                m_Logger.LogDebug("🔒 SEMAPHORE: Batch command completed, result length: {Length}", result?.Length ?? 0);

                return result ?? string.Empty;
            }
            finally
            {
                m_CommandSemaphore.Release();
            }
        }

        /// <summary>
        /// Stops the debugging session gracefully.
        /// This method stops the CDB process and cleans up associated resources.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the session was stopped successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public Task<bool> StopSession()
        {
            ThrowIfDisposed();

            m_Logger.LogDebug("StopSession called");

            return Task.Run(() =>
            {
                try
                {
                    var result = m_ProcessManager.StopProcess();
                    m_Logger.LogDebug("StopSession completed with result: {Result}", result);
                    return result;
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error stopping session");
                    return false;
                }
            });
        }

        /// <summary>
        /// Throws an ObjectDisposedException if this instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CdbSession));
        }

        // Backward compatibility methods for tests
        /// <summary>
        /// Gets the current system architecture for backward compatibility with tests.
        /// </summary>
        /// <returns>A string representing the current architecture.</returns>
        private string GetCurrentArchitecture()
        {
            return m_Config.GetCurrentArchitecture();
        }

        private string? FindCdbPath()
        {
            try
            {
                return m_Config.FindCdbPath();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if a command line indicates command completion.
        /// </summary>
        /// <param name="line">The line to check for completion indicators.</param>
        /// <returns><c>true</c> if the line indicates command completion; otherwise, <c>false</c>.</returns>
        private bool IsCommandComplete(string line)
        {
            return m_OutputParser.IsCommandComplete(line);
        }

        /// <summary>
        /// Disposes the session and all associated resources.
        /// This method stops the CDB process, releases the command semaphore, and cleans up all resources.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Logger.LogDebug("Disposing CdbSession");

            // Dispose semaphore first
            try
            {
                m_CommandSemaphore?.Dispose();
            }
            catch { }

            try
            {
                m_ProcessManager?.Dispose();
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error disposing process manager");
            }

            m_Disposed = true;
            m_Logger.LogDebug("CdbSession disposed successfully");
        }
    }
}
