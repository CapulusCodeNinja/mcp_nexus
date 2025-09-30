using System.Diagnostics;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Refactored CDB session that orchestrates focused components for debugger operations
    /// </summary>
    public class CdbSession : IDisposable, ICdbSession
    {
        private readonly ILogger<CdbSession> m_logger;
        private readonly CdbSessionConfiguration m_config;
        private readonly CdbProcessManager m_processManager;
        private readonly CdbCommandExecutor m_commandExecutor;
        private readonly CdbOutputParser m_outputParser;
        private bool m_disposed;

        // Maintain backward compatibility with original constructor signature
        public CdbSession(
            ILogger<CdbSession> logger,
            int commandTimeoutMs = 30000,
            string? customCdbPath = null,
            int symbolServerTimeoutMs = 30000,
            int symbolServerMaxRetries = 1,
            string? symbolSearchPath = null,
            int startupDelayMs = 2000)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create configuration with validation
            m_config = new CdbSessionConfiguration(
                commandTimeoutMs,
                customCdbPath,
                symbolServerTimeoutMs,
                symbolServerMaxRetries,
                symbolSearchPath,
                startupDelayMs);

            // Create focused components - use the same logger for now to maintain compatibility
            m_processManager = new CdbProcessManager(logger as ILogger<CdbProcessManager> ??
                Microsoft.Extensions.Logging.Abstractions.NullLogger<CdbProcessManager>.Instance, m_config);
            m_outputParser = new CdbOutputParser(logger as ILogger<CdbOutputParser> ??
                Microsoft.Extensions.Logging.Abstractions.NullLogger<CdbOutputParser>.Instance);
            m_commandExecutor = new CdbCommandExecutor(logger as ILogger<CdbCommandExecutor> ??
                Microsoft.Extensions.Logging.Abstractions.NullLogger<CdbCommandExecutor>.Instance, m_config, m_outputParser);

            m_logger.LogDebug("CdbSession initialized with focused components");
        }

        /// <summary>
        /// Gets whether the session is currently active
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
        /// Cancels the currently executing operation
        /// </summary>
        public void CancelCurrentOperation()
        {
            ThrowIfDisposed();

            m_logger.LogWarning("Cancelling current operation");
            m_commandExecutor.CancelCurrentOperation();
        }

        /// <summary>
        /// Starts a debugging session with the specified target
        /// </summary>
        public Task<bool> StartSession(string target, string? arguments = null)
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
                return Task.FromResult(result);
            }
            catch (OperationCanceledException)
            {
                m_logger.LogError("❌ StartSession timed out after {TimeoutMs}ms for target: {Target}", m_config.CommandTimeoutMs, target);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Failed to start CDB session with target: {Target}", target);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Executes a command in the debugging session
        /// </summary>
        public Task<string> ExecuteCommand(string command)
        {
            return ExecuteCommand(command, CancellationToken.None);
        }

        /// <summary>
        /// Executes a command in the debugging session with cancellation support
        /// </summary>
        public Task<string> ExecuteCommand(string command, CancellationToken externalCancellationToken)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            if (!IsActive)
                throw new InvalidOperationException("No active debugging session");

            return Task.Run(() =>
            {
                try
                {
                    return m_commandExecutor.ExecuteCommand(command, m_processManager, externalCancellationToken);
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error executing command: {Command}", command);
                    return $"Error executing command: {ex.Message}";
                }
            }, externalCancellationToken);
        }

        /// <summary>
        /// Stops the debugging session
        /// </summary>
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

        private void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbSession));
        }

        // Backward compatibility methods for tests
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

        private bool IsCommandComplete(string line)
        {
            return m_outputParser.IsCommandComplete(line);
        }

        /// <summary>
        /// Disposes the session and all associated resources
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_logger.LogDebug("Disposing CdbSession");

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
