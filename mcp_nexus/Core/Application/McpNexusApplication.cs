using mcp_nexus.Core.Domain;

namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Main application service that orchestrates all use cases
    /// </summary>
    public class McpNexusApplication
    {
        #region Private Fields

        private readonly IServiceLocator m_serviceLocator;

        #endregion

        #region Public Properties

        /// <summary>Gets the session use case</summary>
        public ISessionUseCase Sessions { get; }

        /// <summary>Gets the command use case</summary>
        public ICommandUseCase Commands { get; }

        /// <summary>Gets the notification service</summary>
        public INotificationService Notifications { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new MCP Nexus application
        /// </summary>
        /// <param name="serviceLocator">Service locator for dependency injection</param>
        public McpNexusApplication(IServiceLocator serviceLocator)
        {
            m_serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            
            // Initialize use cases
            Sessions = new SessionUseCase(m_serviceLocator);
            Commands = new CommandUseCase(m_serviceLocator);
            Notifications = m_serviceLocator.GetService<INotificationService>();
        }

        #endregion
    }
}
