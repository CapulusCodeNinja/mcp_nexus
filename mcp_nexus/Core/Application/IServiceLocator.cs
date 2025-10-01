namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Service locator interface for dependency management - major connection interface
    /// </summary>
    public interface IServiceLocator
    {
        /// <summary>
        /// Gets a service instance by type
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>Service instance</returns>
        T GetService<T>() where T : class;

        /// <summary>
        /// Gets a service instance by type
        /// </summary>
        /// <param name="serviceType">Service type</param>
        /// <returns>Service instance</returns>
        object GetService(Type serviceType);

        /// <summary>
        /// Registers a service instance
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="instance">Service instance</param>
        void RegisterService<T>(T instance) where T : class;

        /// <summary>
        /// Registers a service factory
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="factory">Service factory</param>
        void RegisterFactory<T>(Func<T> factory) where T : class;

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>True if service is registered</returns>
        bool IsRegistered<T>() where T : class;
    }
}
