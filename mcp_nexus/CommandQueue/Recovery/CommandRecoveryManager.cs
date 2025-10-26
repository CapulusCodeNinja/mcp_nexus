using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Recovery;
using mcp_nexus.Notifications;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus.CommandQueue.Recovery
{
    /// <summary>
    /// Manages command recovery, timeout handling, and session recovery for resilient operations
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CommandRecoveryManager"/> class.
    /// </remarks>
    /// <param name="cdbSession">The CDB session to monitor and recover.</param>
    /// <param name="logger">The logger instance for recording recovery operations.</param>
    /// <param name="timeoutService">The timeout service for managing command timeouts.</param>
    /// <param name="recoveryService">The recovery service for session recovery operations.</param>
    /// <param name="config">The resilient queue configuration settings.</param>
    /// <param name="notificationService">Optional notification service for publishing recovery events.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
    public class CommandRecoveryManager(
        ICdbSession cdbSession,
        ILogger logger,
        ICommandTimeoutService timeoutService,
        ICdbSessionRecoveryService recoveryService,
        ResilientQueueConfiguration config,
        IMcpNotificationService? notificationService = null)
    {
        private readonly ICdbSession m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
        private readonly ILogger m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICommandTimeoutService m_TimeoutService = timeoutService ?? throw new ArgumentNullException(nameof(timeoutService));
        private readonly ICdbSessionRecoveryService m_RecoveryService = recoveryService ?? throw new ArgumentNullException(nameof(recoveryService));
        private readonly IMcpNotificationService? m_NotificationService = notificationService;
        private readonly ResilientQueueConfiguration m_Config = config ?? throw new ArgumentNullException(nameof(config));

        /// <summary>
        /// Executes a command with comprehensive recovery and timeout handling
        /// </summary>
        /// <param name="queuedCommand">The command to execute</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The command result</returns>
        public async Task<string> ExecuteCommandWithRecoveryAsync(QueuedCommand queuedCommand, CancellationToken cancellationToken)
        {
            var commandTimeout = m_Config.DetermineCommandTimeout(queuedCommand.Command ?? string.Empty);
            var startTime = DateTime.Now;

            m_Logger.LogInformation("🔄 Executing resilient command {CommandId}: {Command} (timeout: {Timeout})",
                queuedCommand.Id, queuedCommand.Command, commandTimeout);

            try
            {
                // Start heartbeat for long-running commands
                var heartbeatTask = StartHeartbeatAsync(queuedCommand, startTime, cancellationToken);

                // Execute command with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(commandTimeout);

                var result = await ExecuteCommandWithTimeoutAsync(queuedCommand, timeoutCts.Token);

                // Stop heartbeat (task will be cancelled by the cancellation token)

                var elapsed = DateTime.Now - startTime;
                m_Logger.LogInformation("✅ Command {CommandId} completed successfully in {Elapsed}ms",
                    queuedCommand.Id, elapsed.TotalMilliseconds);

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                m_Logger.LogWarning("🚫 Command {CommandId} was cancelled", queuedCommand.Id);
                throw;
            }
            catch (TimeoutException ex)
            {
                var elapsed = DateTime.Now - startTime;
                m_Logger.LogError("⏰ Command {CommandId} timed out after {Elapsed}ms: {Error}",
                    queuedCommand.Id, elapsed.TotalMilliseconds, ex.Message);

                // Attempt recovery
                await AttemptCommandRecoveryAsync(queuedCommand, ex);
                throw;
            }
            catch (Exception ex)
            {
                var elapsed = DateTime.Now - startTime;
                m_Logger.LogError(ex, "❌ Command {CommandId} failed after {Elapsed}ms: {Error}",
                    queuedCommand.Id, elapsed.TotalMilliseconds, ex.Message);

                // Attempt recovery for certain types of failures
                if (ShouldAttemptRecovery(ex))
                {
                    await AttemptCommandRecoveryAsync(queuedCommand, ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Executes a command with timeout monitoring
        /// </summary>
        /// <param name="queuedCommand">The command to execute</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The command result</returns>
        private async Task<string> ExecuteCommandWithTimeoutAsync(QueuedCommand queuedCommand, CancellationToken cancellationToken)
        {
            try
            {
                // Start command timeout
                var commandTimeout = m_Config.DetermineCommandTimeout(queuedCommand.Command ?? string.Empty);
                m_TimeoutService.StartCommandTimeout(queuedCommand.Id ?? string.Empty, commandTimeout, () =>
                {
                    m_Logger.LogError("Command {CommandId} timed out", queuedCommand.Id);
                    // The timeout will be handled by the calling method
                    return Task.CompletedTask;
                });

                // Execute the command
                var result = await m_CdbSession.ExecuteCommand(queuedCommand.Command ?? string.Empty, cancellationToken);

                // Cancel timeout since command completed
                m_TimeoutService.CancelCommandTimeout(queuedCommand.Id ?? string.Empty);

                return result;
            }
            catch (Exception)
            {
                // Ensure cleanup even on failure
                m_TimeoutService.CancelCommandTimeout(queuedCommand.Id ?? string.Empty);
                throw;
            }
        }

        /// <summary>
        /// Starts a heartbeat task for long-running commands
        /// </summary>
        /// <param name="queuedCommand">The command to monitor</param>
        /// <param name="startTime">The time when the command started</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A task representing the heartbeat operation, or null if not needed</returns>
        private Task? StartHeartbeatAsync(QueuedCommand queuedCommand, DateTime startTime, CancellationToken cancellationToken)
        {
            var commandTimeout = m_Config.DetermineCommandTimeout(queuedCommand.Command ?? string.Empty);

            // Only start heartbeat for commands that might take longer than 30 seconds
            if (commandTimeout <= TimeSpan.FromSeconds(30))
                return null;

            return Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(m_Config.HeartbeatInterval, cancellationToken);

                        var elapsed = DateTime.Now - startTime;
                        var heartbeatDetails = ResilientQueueConfiguration.GenerateHeartbeatDetails(queuedCommand.Command ?? string.Empty, elapsed);

                        m_Logger.LogDebug("💓 Heartbeat for command {CommandId}: {Details} (elapsed: {Elapsed})",
                            queuedCommand.Id, heartbeatDetails, elapsed);

                        // Send notification if service is available
                        if (m_NotificationService != null)
                        {
                            await m_NotificationService.NotifyCommandHeartbeatAsync(queuedCommand.Id ?? string.Empty, queuedCommand.Command ?? string.Empty, elapsed, heartbeatDetails);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when command completes or is cancelled
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Heartbeat task failed for command {CommandId}", queuedCommand.Id);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Determines if recovery should be attempted for a given exception.
        /// </summary>
        /// <param name="ex">The exception to evaluate for recovery eligibility.</param>
        /// <returns><c>true</c> if recovery should be attempted; otherwise, <c>false</c>.</returns>
        private static bool ShouldAttemptRecovery(Exception ex)
        {
            return ex is InvalidOperationException ||
                   ex is TimeoutException ||
                   ex.Message.Contains("debugger", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("session", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Attempts to recover from a command failure
        /// </summary>
        /// <param name="queuedCommand">The command that failed</param>
        /// <param name="originalException">The exception that caused the failure</param>
        /// <returns>A task representing the recovery operation</returns>
        private async Task AttemptCommandRecoveryAsync(QueuedCommand queuedCommand, Exception originalException)
        {
            try
            {
                m_Logger.LogWarning("🔧 Attempting recovery for command {CommandId} after {ExceptionType}: {Message}",
                    queuedCommand.Id, originalException.GetType().Name, originalException.Message);

                // Use the recovery service to attempt session recovery
                var recoveryResult = await m_RecoveryService.RecoverStuckSession($"Command {queuedCommand.Id} failed: {originalException.Message}");

                if (recoveryResult)
                {
                    m_Logger.LogInformation("✅ Recovery successful for command {CommandId}", queuedCommand.Id);
                }
                else
                {
                    m_Logger.LogError("❌ Recovery failed for command {CommandId}", queuedCommand.Id);
                }
            }
            catch (Exception recoveryEx)
            {
                m_Logger.LogError(recoveryEx, "💥 Recovery attempt failed for command {CommandId}", queuedCommand.Id);
            }
        }

        /// <summary>
        /// Cancels a command and performs cleanup
        /// </summary>
        /// <param name="commandId">The ID of the command to cancel</param>
        /// <param name="reason">The reason for cancellation</param>
        /// <returns>True if the command was successfully cancelled</returns>
        public bool CancelCommand(string commandId, string? reason = null)
        {
            try
            {
                m_Logger.LogInformation("🚫 Cancelling command {CommandId}: {Reason}", commandId, reason ?? "User requested");

                // Cancel timeout for this command
                m_TimeoutService.CancelCommandTimeout(commandId);

                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error cancelling command {CommandId}", commandId);
                return false;
            }
        }

        /// <summary>
        /// Performs cleanup operations for the recovery manager.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                m_Logger.LogDebug("🧹 Performing recovery manager cleanup");
                // Any cleanup operations needed
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during recovery manager cleanup");
            }
        }
    }
}
