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
        /// Retrieves the debug engine instance in a thread-safe manner.
        /// </summary>
        /// <returns>The initialized debug engine instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the debug engine has not been initialized via <see cref="Initialize(IFileSystem, IProcessManager, ISettings)"/>.</exception>
        public static IDebugEngine Get()
        {
            m_DebugEngineLock.EnterReadLock();
            try
            {
                return m_DebugEngine!;
            }
            finally
            {
                m_DebugEngineLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Initializes the debug engine instance with the specified settings.
        /// This method must be called before using <see cref="Get()"/> to retrieve the engine.
        /// </summary>
        /// <param name="fileSystem">The file system abstraction.</param>
        /// <param name="processManager">The process manager abstraction.</param>
        /// <param name="settings">The product settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
        public static void Initialize(IFileSystem fileSystem, IProcessManager processManager, ISettings settings)
        {
            m_DebugEngineLock.EnterWriteLock();
            try
            {
                m_DebugEngine = new DebugEngine(fileSystem, processManager, settings);
            }
            finally
            {
                m_DebugEngineLock.ExitWriteLock();
            }
        }
    }
}
