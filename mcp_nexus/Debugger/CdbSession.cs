using System.Diagnostics;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Refactored CDB session that orchestrates focused components for debugger operations.
    /// Provides a high-level interface for managing CDB debugging sessions with thread-safe command execution.
    /// </summary>
    public class CdbSession : IDisposable, ICdbSession
    {
        private readonly ILogger<CdbSession> m_logger;
        private readonly CdbSessionConfiguration m_config;
        private readonly CdbProcessManager m_processManager;
        private readonly CdbCommandExecutor m_commandExecutor;
        private readonly CdbOutputParser m_outputParser;

        // CRITICAL: Semaphore to ensure only ONE command executes at a time in CDB
        // CDB is single-threaded and cannot handle concurrent commands
        private readonly SemaphoreSlim m_commandSemaphore = new SemaphoreSlim(1, 1);

        private bool m_disposed;

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
        /// <param name="sessionId">Optional session ID for creating session-specific log files. If null, uses default naming.</param>
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
            string? sessionId = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create configuration with validation
            m_config = new CdbSessionConfiguration(
                commandTimeoutMs: commandTimeoutMs,
                idleTimeoutMs: idleTimeoutMs,
                customCdbPath: customCdbPath,
                symbolServerMaxRetries: symbolServerMaxRetries,
                symbolSearchPath: symbolSearchPath,
                startupDelayMs: startupDelayMs,
                outputReadingTimeoutMs: outputReadingTimeoutMs);

            // CRITICAL FIX: Logger casting was failing, causing NullLogger to be used!
            // This resulted in zero visibility into CdbProcessManager init consumer
            // Instead of casting (which fails), create simple wrapper loggers
            var processLogger = new LoggerWrapper<CdbProcessManager>(logger);
            var parserLogger = new LoggerWrapper<CdbOutputParser>(logger);
            var executorLogger = new LoggerWrapper<CdbCommandExecutor>(logger);

            m_processManager = new CdbProcessManager(processLogger, m_config);
            m_outputParser = new CdbOutputParser(parserLogger);
            m_commandExecutor = new CdbCommandExecutor(executorLogger, m_config, m_outputParser);

            // Set session ID for session-specific log files
            if (!string.IsNullOrEmpty(sessionId))
            {
                m_processManager.SetSessionId(sessionId);
            }

            m_logger.LogDebug("CdbSession initialized with focused components");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbSession"/> class with a pre-configured <see cref="CdbSessionConfiguration"/>.
        /// </summary>
        /// <param name="logger">The logger instance for recording session operations and errors.</param>
        /// <param name="config">The CDB session configuration object.</param>
        /// <param name="sessionId">Optional session ID for creating session-specific log files. If null, uses default naming.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="config"/> is null.</exception>
        public CdbSession(
            ILogger<CdbSession> logger,
            CdbSessionConfiguration config,
            string? sessionId = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));

            // CRITICAL FIX: Logger casting was failing, causing NullLogger to be used!
            // This resulted in zero visibility into CdbProcessManager init consumer
            // Instead of casting (which fails), create simple wrapper loggers
            var processLogger = new LoggerWrapper<CdbProcessManager>(logger);
            var parserLogger = new LoggerWrapper<CdbOutputParser>(logger);
            var executorLogger = new LoggerWrapper<CdbCommandExecutor>(logger);

            m_processManager = new CdbProcessManager(processLogger, m_config);
            m_outputParser = new CdbOutputParser(parserLogger);
            m_commandExecutor = new CdbCommandExecutor(executorLogger, m_config, m_outputParser);

            // Set session ID for session-specific log files
            if (!string.IsNullOrEmpty(sessionId))
            {
                m_processManager.SetSessionId(sessionId);
            }

            m_logger.LogDebug("CdbSession initialized with focused components and configuration object");
        }

        /// <summary>
        /// Simple logger wrapper that implements ILogger&lt;T&gt; by forwarding to an untyped logger
        /// This fixes the NullLogger issue when logger casting fails
        /// </summary>
        private class LoggerWrapper<T> : ILogger<T>
        {
            private readonly ILogger m_InnerLogger;

            public LoggerWrapper(ILogger logger)
            {
                m_InnerLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

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
                if (m_disposed)
                    return false;

                return m_processManager.IsActive;
            }
        }

        /// <summary>
        /// Gets the process ID of the CDB debugger process.
        /// </summary>
        public int? ProcessId
        {
            get
            {
                if (m_disposed)
                    return null;

                try
                {
                    return m_processManager.DebuggerProcess?.Id;
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Could not retrieve CDB process ID");
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

            m_logger.LogWarning("Cancelling current operation");
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

            m_logger.LogInformation("🔧 StartSession called with target: {Target}, arguments: {Arguments}", target, arguments);
            m_logger.LogInformation("🔧 ProcessManager is null: {IsNull}", m_processManager == null);

            try
            {
                m_logger.LogInformation("🔧 About to call StartProcess directly");
                var result = m_processManager?.StartProcess(target, m_config.CustomCdbPath) ?? throw new InvalidOperationException("Process manager not initialized");
                m_logger.LogInformation("🔧 StartProcess returned: {Result}", result);

                if (result)
                {
                    // Initialize the session-scoped producer-consumer architecture
                    m_logger.LogInformation("🔧 Initializing session-scoped architecture");
                    await m_commandExecutor.InitializeSessionAsync(m_processManager).ConfigureAwait(false);
                    m_logger.LogInformation("🔧 Session-scoped architecture initialized successfully");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                m_logger.LogError("❌ StartSession timed out after {TimeoutMs}ms for target: {Target}", m_config.CommandTimeoutMs, target);
                return false;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Failed to start CDB session with target: {Target}", target);
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
            await m_commandSemaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);

            try
            {
                if (!IsActive)
                {
                    m_logger.LogError("Cannot execute command '{Command}' - CDB session is not active (IsActive={IsActive})", command, IsActive);
                    throw new InvalidOperationException("No active debugging session");
                }

                m_logger.LogInformation("🔒 SEMAPHORE: About to execute command '{Command}' (TRUE ASYNC)", command);

                // Preprocess the command to fix common issues (e.g., .srcpath path conversion and directory creation)
                // Only if preprocessing is enabled in configuration
                var preprocessedCommand = command;
                if (m_config.EnableCommandPreprocessing)
                {
                    preprocessedCommand = mcp_nexus.Utilities.CommandPreprocessor.PreprocessCommand(command);
                    if (preprocessedCommand != command)
                    {
                        m_logger.LogInformation("🔧 Command preprocessed: '{Original}' -> '{Preprocessed}'", command, preprocessedCommand);
                    }
                }
                else
                {
                    m_logger.LogDebug("⚠️ Command preprocessing is DISABLED - sending command as-is: '{Command}'", command);
                }

                // TRUE ASYNC: Direct call without Task.Run - proper async all the way through
                // Semaphore ensures serialization, no need for thread pool wrapper
                var result = await m_commandExecutor.ExecuteCommandAsync(preprocessedCommand, commandId, m_processManager, externalCancellationToken).ConfigureAwait(false);

                m_logger.LogInformation("🔒 SEMAPHORE: Command '{Command}' completed, result length: {Length}", command, result?.Length ?? 0);

                return result ?? string.Empty;
            }
            finally
            {
                m_commandSemaphore.Release();
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

            m_logger.LogDebug("StopSession called");

            return Task.Run(() =>
            {
                try
                {
                    var result = m_processManager.StopProcess();
                    m_logger.LogInformation("StopSession completed with result: {Result}", result);
                    return result;
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error stopping session");
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
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbSession));
        }

        // Backward compatibility methods for tests
        /// <summary>
        /// Gets the current system architecture for backward compatibility with tests.
        /// </summary>
        /// <returns>A string representing the current architecture.</returns>
        private string GetCurrentArchitecture()
        {
            return m_config.GetCurrentArchitecture();
        }

        private string? FindCdbPath()
        {
            try
            {
                return m_config.FindCdbPath();
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
            return m_outputParser.IsCommandComplete(line);
        }

        /// <summary>
        /// Disposes the session and all associated resources.
        /// This method stops the CDB process, releases the command semaphore, and cleans up all resources.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_logger.LogDebug("Disposing CdbSession");

            // Dispose semaphore first
            try
            {
                m_commandSemaphore?.Dispose();
            }
            catch { }

            try
            {
                m_processManager?.Dispose();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error disposing process manager");
            }

            m_disposed = true;
            m_logger.LogDebug("CdbSession disposed successfully");
        }
    }
}
