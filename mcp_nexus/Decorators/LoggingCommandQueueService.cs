using mcp_nexus.CommandQueue;

namespace mcp_nexus.Decorators
{
    /// <summary>
    /// Decorator for adding logging functionality to command queue service using Decorator Pattern
    /// </summary>
    public class LoggingCommandQueueService : ILoggingCommandQueueService
    {
        #region Private Fields

        private readonly ICommandQueueService m_underlyingService;
        private readonly ILogger<LoggingCommandQueueService> m_logger;

        #endregion

        #region Public Properties

        /// <summary>Gets the underlying command queue service</summary>
        public ICommandQueueService UnderlyingService => m_underlyingService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new logging command queue service decorator
        /// </summary>
        /// <param name="underlyingService">Underlying command queue service</param>
        /// <param name="logger">Logger instance</param>
        public LoggingCommandQueueService(ICommandQueueService underlyingService, ILogger<LoggingCommandQueueService> logger)
        {
            m_underlyingService = underlyingService ?? throw new ArgumentNullException(nameof(underlyingService));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region ICommandQueueService Implementation

        /// <summary>
        /// Enqueues a command with logging
        /// </summary>
        /// <param name="command">Command to enqueue</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Command result</returns>
        public async Task<string> EnqueueCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            m_logger.LogInformation("Enqueuing command: {Command}", command);
            
            try
            {
                var result = await m_underlyingService.EnqueueCommandAsync(command, cancellationToken);
                m_logger.LogInformation("Command enqueued successfully: {CommandId}", result);
                return result;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to enqueue command: {Command}", command);
                throw;
            }
        }

        /// <summary>
        /// Gets command status with logging
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>Command status</returns>
        public CommandState GetCommandStatus(string commandId)
        {
            m_logger.LogDebug("Getting command status: {CommandId}", commandId);
            
            try
            {
                var status = m_underlyingService.GetCommandStatus(commandId);
                m_logger.LogDebug("Command status retrieved: {CommandId} = {Status}", commandId, status);
                return status;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to get command status: {CommandId}", commandId);
                throw;
            }
        }

        /// <summary>
        /// Gets command result with logging
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Command result</returns>
        public async Task<string> GetCommandResultAsync(string commandId, CancellationToken cancellationToken = default)
        {
            m_logger.LogInformation("Getting command result: {CommandId}", commandId);
            
            try
            {
                var result = await m_underlyingService.GetCommandResultAsync(commandId, cancellationToken);
                m_logger.LogInformation("Command result retrieved: {CommandId}", commandId);
                return result;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to get command result: {CommandId}", commandId);
                throw;
            }
        }

        /// <summary>
        /// Cancels a command with logging
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>True if cancelled successfully</returns>
        public bool CancelCommand(string commandId)
        {
            m_logger.LogInformation("Cancelling command: {CommandId}", commandId);
            
            try
            {
                var result = m_underlyingService.CancelCommand(commandId);
                m_logger.LogInformation("Command cancellation result: {CommandId} = {Result}", commandId, result);
                return result;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to cancel command: {CommandId}", commandId);
                throw;
            }
        }

        /// <summary>
        /// Gets queue statistics with logging
        /// </summary>
        /// <returns>Queue statistics</returns>
        public QueueStatistics GetQueueStatistics()
        {
            m_logger.LogDebug("Getting queue statistics");
            
            try
            {
                var stats = m_underlyingService.GetQueueStatistics();
                m_logger.LogDebug("Queue statistics retrieved: {Stats}", stats);
                return stats;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to get queue statistics");
                throw;
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the logging command queue service
        /// </summary>
        public void Dispose()
        {
            m_logger.LogInformation("Disposing logging command queue service");
            m_underlyingService?.Dispose();
        }

        #endregion
    }
}
