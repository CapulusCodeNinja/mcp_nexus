using mcp_nexus.Core.Domain;

namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Command use case implementation
    /// </summary>
    public class CommandUseCase : ICommandUseCase
    {
        #region Private Fields

        private readonly IServiceLocator m_serviceLocator;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new command use case
        /// </summary>
        /// <param name="serviceLocator">Service locator</param>
        public CommandUseCase(IServiceLocator serviceLocator)
        {
            m_serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="commandText">Command to execute</param>
        /// <returns>Command result</returns>
        public async Task<ICommandResult> ExecuteCommandAsync(string sessionId, string commandText)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            if (string.IsNullOrEmpty(commandText))
                throw new ArgumentException("Command text cannot be null or empty", nameof(commandText));

            // Get required services
            var debuggerService = m_serviceLocator.GetService<IDebuggerService>();
            var commandQueueService = m_serviceLocator.GetService<ICommandQueueService>();
            var notificationService = m_serviceLocator.GetService<INotificationService>();

            // Create command
            var commandId = Guid.NewGuid().ToString();
            var command = new Command(commandId, commandText);

            try
            {
                // Enqueue command
                await commandQueueService.EnqueueCommandAsync(command);

                // Execute command through debugger
                var output = await debuggerService.ExecuteCommandAsync(sessionId, commandText);

                // Mark command as completed
                command.MarkCompleted();

                // Notify command completed
                await notificationService.PublishEventAsync("CommandCompleted", new { CommandId = commandId, SessionId = sessionId });

                return CommandResult.Success(output);
            }
            catch (Exception ex)
            {
                // Mark command as failed
                command.UpdateState(CommandState.Failed);

                // Notify command failed
                await notificationService.PublishEventAsync("CommandFailed", new { CommandId = commandId, SessionId = sessionId, Error = ex.Message });

                return CommandResult.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Gets command status
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>Command status</returns>
        public async Task<CommandState> GetCommandStatusAsync(string commandId)
        {
            if (string.IsNullOrEmpty(commandId))
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

            var commandQueueService = m_serviceLocator.GetService<ICommandQueueService>();
            return await commandQueueService.GetCommandStatusAsync(commandId);
        }

        /// <summary>
        /// Cancels a command
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>True if cancelled successfully</returns>
        public async Task<bool> CancelCommandAsync(string commandId)
        {
            if (string.IsNullOrEmpty(commandId))
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));

            var commandQueueService = m_serviceLocator.GetService<ICommandQueueService>();
            var notificationService = m_serviceLocator.GetService<INotificationService>();

            var success = await commandQueueService.CancelCommandAsync(commandId);

            if (success)
            {
                // Notify command cancelled
                await notificationService.PublishEventAsync("CommandCancelled", new { CommandId = commandId });
            }

            return success;
        }

        #endregion
    }
}
