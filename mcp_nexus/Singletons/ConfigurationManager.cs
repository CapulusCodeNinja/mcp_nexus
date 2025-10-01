namespace mcp_nexus.Singletons
{
    /// <summary>
    /// Singleton implementation for configuration management using Singleton Pattern
    /// </summary>
    public sealed class ConfigurationManager : IConfigurationManager
    {
        #region Private Fields

        private static volatile ConfigurationManager? s_instance;
        private static readonly object s_lock = new();
        private readonly Dictionary<string, string> m_configuration = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the singleton instance of ConfigurationManager
        /// </summary>
        public static IConfigurationManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new ConfigurationManager();
                        }
                    }
                }
                return s_instance;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Private constructor to prevent direct instantiation
        /// </summary>
        private ConfigurationManager()
        {
            // Initialize with default configuration
            InitializeDefaultConfiguration();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>Configuration value or null if not found</returns>
        public string? GetValue(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            lock (s_lock)
            {
                m_configuration.TryGetValue(key, out var value);
                return value;
            }
        }

        /// <summary>
        /// Gets a configuration value by key with default
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Configuration value or default</returns>
        public string GetValue(string key, string defaultValue)
        {
            return GetValue(key) ?? defaultValue;
        }

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        public void SetValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            lock (s_lock)
            {
                m_configuration[key] = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Checks if a configuration key exists
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>True if key exists, false otherwise</returns>
        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            lock (s_lock)
            {
                return m_configuration.ContainsKey(key);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the configuration with default values
        /// </summary>
        private void InitializeDefaultConfiguration()
        {
            m_configuration["MaxConcurrentSessions"] = "1000";
            m_configuration["SessionTimeoutMinutes"] = "30";
            m_configuration["CommandTimeoutMinutes"] = "10";
            m_configuration["MemoryThresholdMB"] = "1024";
            m_configuration["CpuThresholdPercent"] = "80";
            m_configuration["LogLevel"] = "Information";
        }

        #endregion
    }
}
