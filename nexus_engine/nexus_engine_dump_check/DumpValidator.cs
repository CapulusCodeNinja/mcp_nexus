using Nexus.External.Apis.FileSystem;

using NLog;

namespace Nexus.Engine.DumpCheck
{
    public class DumpValidator : IDumpValidator
    {
        /// <summary>
        /// Logger for debug engine operations.
        /// </summary>
        private readonly Logger m_Logger;

        /// <summary>
        /// File system abstraction for file operations.
        /// </summary>
        private readonly IFileSystem m_FileSystem;

        public DumpValidator(IFileSystem fileSystem)
        {
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_Logger = LogManager.GetCurrentClassLogger();
        }

        public void Validate(string dumpFilePath)
        {
            if (!m_FileSystem.FileExists(dumpFilePath))
            {
                throw new FileNotFoundException($"Dump file not found: {dumpFilePath}", dumpFilePath);
            }

            m_FileSystem.ProbeRead(dumpFilePath);
        }
    }
}
