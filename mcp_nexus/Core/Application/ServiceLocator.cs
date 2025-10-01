namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Service locator implementation for dependency management
    /// </summary>
    public class ServiceLocator : IServiceLocator
    {
        #region Private Fields

        private readonly Dictionary<Type, object> m_services = new();
        private readonly Dictionary<Type, Func<object>> m_factories = new();
        private readonly object m_lock = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a service instance by type
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>Service instance</returns>
        public T GetService<T>() where T : class
        {
            return (T)GetService(typeof(T));
        }

        /// <summary>
        /// Gets a service instance by type
        /// </summary>
        /// <param name="serviceType">Service type</param>
        /// <returns>Service instance</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            lock (m_lock)
            {
                // Check for registered instance
                if (m_services.TryGetValue(serviceType, out var instance))
                {
                    return instance;
                }

                // Check for registered factory
                if (m_factories.TryGetValue(serviceType, out var factory))
                {
                    var newInstance = factory();
                    m_services[serviceType] = newInstance;
                    return newInstance;
                }

                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered");
            }
        }

        /// <summary>
        /// Registers a service instance
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="instance">Service instance</param>
        public void RegisterService<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            lock (m_lock)
            {
                m_services[typeof(T)] = instance;
            }
        }

        /// <summary>
        /// Registers a service factory
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="factory">Service factory</param>
        public void RegisterFactory<T>(Func<T> factory) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (m_lock)
            {
                m_factories[typeof(T)] = () => factory();
            }
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>True if service is registered</returns>
        public bool IsRegistered<T>() where T : class
        {
            lock (m_lock)
            {
                return m_services.ContainsKey(typeof(T)) || m_factories.ContainsKey(typeof(T));
            }
        }

        #endregion
    }
}
