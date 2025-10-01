namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Configuration class for setting up the clean architecture
    /// </summary>
    public static class ArchitectureConfiguration
    {
        /// <summary>
        /// Configures the service locator with all required services
        /// </summary>
        /// <param name="serviceLocator">Service locator to configure</param>
        /// <param name="debuggerAdapter">Debugger adapter implementation</param>
        /// <param name="commandQueueAdapter">Command queue adapter implementation</param>
        /// <param name="notificationAdapter">Notification adapter implementation</param>
        /// <param name="sessionRepository">Session repository implementation</param>
        public static void ConfigureServices(
            IServiceLocator serviceLocator,
            IDebuggerService debuggerAdapter,
            ICommandQueueService commandQueueAdapter,
            INotificationService notificationAdapter,
            ISessionRepository sessionRepository)
        {
            if (serviceLocator == null)
                throw new ArgumentNullException(nameof(serviceLocator));

            if (debuggerAdapter == null)
                throw new ArgumentNullException(nameof(debuggerAdapter));

            if (commandQueueAdapter == null)
                throw new ArgumentNullException(nameof(commandQueueAdapter));

            if (notificationAdapter == null)
                throw new ArgumentNullException(nameof(notificationAdapter));

            if (sessionRepository == null)
                throw new ArgumentNullException(nameof(sessionRepository));

            // Register core services
            serviceLocator.RegisterService(debuggerAdapter);
            serviceLocator.RegisterService(commandQueueAdapter);
            serviceLocator.RegisterService(notificationAdapter);
            serviceLocator.RegisterService(sessionRepository);
        }

        /// <summary>
        /// Creates a new MCP Nexus application with configured services
        /// </summary>
        /// <param name="debuggerAdapter">Debugger adapter implementation</param>
        /// <param name="commandQueueAdapter">Command queue adapter implementation</param>
        /// <param name="notificationAdapter">Notification adapter implementation</param>
        /// <param name="sessionRepository">Session repository implementation</param>
        /// <returns>Configured MCP Nexus application</returns>
        public static McpNexusApplication CreateApplication(
            IDebuggerService debuggerAdapter,
            ICommandQueueService commandQueueAdapter,
            INotificationService notificationAdapter,
            ISessionRepository sessionRepository)
        {
            var serviceLocator = new ServiceLocator();
            ConfigureServices(serviceLocator, debuggerAdapter, commandQueueAdapter, notificationAdapter, sessionRepository);
            return new McpNexusApplication(serviceLocator);
        }
    }
}
