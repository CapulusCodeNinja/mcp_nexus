using Nexus.Config;
using Nexus.Engine;
using Nexus.Engine.Share;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

namespace Nexus.Protocol.Services
{
    /// <summary>
    /// Provides thread-safe singleton access to the debug engine instance.
    /// Manages initialization and retrieval of the debug engine with proper synchronization.
    /// </summary>
    internal static class EngineService
    {
        /// <summary>
        /// The debug engine instance. Null until initialized via <see cref="Initialize(IFileSystem, IProcessManager, ISettings)"/>.
        /// </summary>
        private static IDebugEngine? m_DebugEngine;

        /// <summary>
        /// Reader-writer lock used to synchronize access to the debug engine instance.
        /// Ensures thread-safe initialization and retrieval operations.
        /// </summary>
        private static readonly ReaderWriterLockSlim m_DebugEngineLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The last file system instance used to initialize the debug engine.
        /// Stored to allow safe reinitialization after shutdown in concurrent scenarios.
        /// </summary>
        private static IFileSystem? m_FileSystem;

        /// <summary>
        /// The last process manager instance used to initialize the debug engine.
        /// Stored to allow safe reinitialization after shutdown in concurrent scenarios.
        /// </summary>
        private static IProcessManager? m_ProcessManager;

        /// <summary>
        /// The last settings instance used to initialize the debug engine.
        /// Stored to allow safe reinitialization after shutdown in concurrent scenarios.
        /// </summary>
        private static ISettings? m_Settings;

        /// <summary>
        /// Retrieves the debug engine instance in a thread-safe manner.
        /// If the engine has been shut down but initialization dependencies are known,
        /// the engine will be lazily re-created to avoid race conditions between
        /// initialization, shutdown, and retrieval.
        /// </summary>
        /// <returns>The initialized debug engine instance.</returns>
        /// <exception cref="NullReferenceException">Thrown when the debug engine has never been initialized and cannot be created.</exception>
        public static IDebugEngine Get()
        {
            m_DebugEngineLock.EnterUpgradeableReadLock();
            try
            {
                if (m_DebugEngine != null)
                {
                    return m_DebugEngine;
                }

                if (m_FileSystem == null || m_ProcessManager == null || m_Settings == null)
                {
                    throw new NullReferenceException("Debug engine has not been initialized.");
                }

                m_DebugEngineLock.EnterWriteLock();
                try
                {
                    m_DebugEngine ??= new DebugEngine(m_FileSystem, m_ProcessManager, m_Settings);
                    return m_DebugEngine;
                }
                finally
                {
                    m_DebugEngineLock.ExitWriteLock();
                }
            }
            finally
            {
                m_DebugEngineLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Initializes the debug engine instance with the specified settings.
        /// This method must be called before using <see cref="Get()"/> to retrieve the engine.
        /// If an existing engine instance is present, it will be disposed before creating a new one.
        /// </summary>
        /// <param name="fileSystem">The file system abstraction.</param>
        /// <param name="processManager">The process manager abstraction.</param>
        /// <param name="settings">The product settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
        public static void Initialize(IFileSystem fileSystem, IProcessManager processManager, ISettings settings)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(processManager);
            ArgumentNullException.ThrowIfNull(settings);

            m_DebugEngineLock.EnterWriteLock();
            try
            {
                m_FileSystem = fileSystem;
                m_ProcessManager = processManager;
                m_Settings = settings;

                m_DebugEngine?.Dispose();
                m_DebugEngine = new DebugEngine(m_FileSystem, m_ProcessManager, m_Settings);
            }
            finally
            {
                m_DebugEngineLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Shuts down the debug engine instance, disposing it if present and clearing the singleton reference.
        /// This method is safe to call multiple times and will only dispose an existing engine once.
        /// </summary>
        public static void Shutdown()
        {
            m_DebugEngineLock.EnterWriteLock();
            try
            {
                if (m_DebugEngine is { } debugEngine)
                {
                    debugEngine.Dispose();
                    m_DebugEngine = null;
                }
            }
            finally
            {
                m_DebugEngineLock.ExitWriteLock();
            }
        }
    }
}
