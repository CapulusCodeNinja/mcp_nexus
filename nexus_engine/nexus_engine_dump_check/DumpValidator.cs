using Nexus.External.Apis.FileSystem;

using NLog;

namespace Nexus.Engine.DumpCheck
{
    /// <summary>
    /// Default implementation of <see cref="IDumpValidator"/> that validates crash dump files
    /// using the configured file system abstraction.
    /// </summary>
    public class DumpValidator : IDumpValidator
    {
        /// <summary>
        /// Logger for dump validation operations.
        /// </summary>
        private readonly Logger m_Logger;

        /// <summary>
        /// File system abstraction for file operations.
        /// </summary>
        private readonly IFileSystem m_FileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DumpValidator"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system abstraction used to probe dump files.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileSystem"/> is <c>null</c>.</exception>
        public DumpValidator(IFileSystem fileSystem)
        {
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_Logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Validates the specified dump file path by checking for existence and basic readability.
        /// </summary>
        /// <param name="dumpFilePath">The full path to the dump file to validate.</param>
        /// <exception cref="FileNotFoundException">Thrown when the specified dump file does not exist.</exception>
        /// <exception cref="IOException">Thrown when the dump file cannot be read.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access to the dump file is denied.</exception>
        public void Validate(string dumpFilePath)
        {
            m_Logger.Debug("Check for dump file exists {DumpFilePath}", dumpFilePath);
            if (!m_FileSystem.FileExists(dumpFilePath))
            {
                throw new FileNotFoundException($"Dump file not found: {dumpFilePath}", dumpFilePath);
            }

            m_Logger.Debug("Probing readability for dump file {DumpFilePath}", dumpFilePath);
            m_FileSystem.ProbeRead(dumpFilePath);
        }
    }
}
