using Nexus.Config;
using Nexus.Engine.DumpCheck.Internal;
using Nexus.Engine.Share.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using NLog;

namespace Nexus.Engine.DumpCheck
{
    /// <summary>
    /// Default implementation of <see cref="IDumpValidator"/> that validates crash dump files
    /// and optionally runs dumpchk using the configured abstractions.
    /// </summary>
    public class DumpValidator : IDumpValidator
    {
        /// <summary>
        /// Logger for dump validation and dumpchk operations.
        /// </summary>
        private readonly Logger m_Logger;

        /// <summary>
        /// File system abstraction for file operations.
        /// </summary>
        private readonly IFileSystem m_FileSystem;

        /// <summary>
        /// Shared application settings.
        /// </summary>
        private readonly ISettings m_Settings;

        /// <summary>
        /// Process manager abstraction for running dumpchk.
        /// </summary>
        private readonly IProcessManager m_ProcessManager;

        /// <summary>
        /// Helper responsible for locating the dumpchk executable.
        /// </summary>
        private readonly DumpChkLocator m_DumpChkLocator;

        /// <summary>
        /// Helper responsible for executing dumpchk and aggregating its output.
        /// </summary>
        private readonly DumpChkProcessRunner m_DumpChkProcessRunner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DumpValidator"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system abstraction used to probe dump files.</param>
        /// <param name="settings">The shared application settings.</param>
        /// <param name="processManager">The process manager abstraction used to run dumpchk.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="fileSystem"/>, <paramref name="settings"/> or <paramref name="processManager"/> is <c>null</c>.
        /// </exception>
        public DumpValidator(IFileSystem fileSystem, ISettings settings, IProcessManager processManager)
        {
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            m_Logger = LogManager.GetCurrentClassLogger();
            m_DumpChkLocator = new DumpChkLocator(m_Settings, m_FileSystem);
            m_DumpChkProcessRunner = new DumpChkProcessRunner(m_Settings, m_ProcessManager);
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

        /// <summary>
        /// Runs dumpchk for the specified dump file when dumpchk integration is enabled.
        /// </summary>
        /// <param name="dumpFilePath">The full path to the dump file to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the combined
        /// dumpchk standard output and error streams as a single string.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="dumpFilePath"/> is null or empty.</exception>
        public async Task<DumpCheckResult> RunDumpChkAsync(string dumpFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dumpFilePath))
            {
                throw new ArgumentException("Dump file path cannot be null or empty", nameof(dumpFilePath));
            }

            var validationSettings = m_Settings.Get().McpNexus.Validation;

            if (!validationSettings.DumpChkEnabled)
            {
                m_Logger.Info("Dumpchk integration is disabled in configuration. Skipping dumpchk for {DumpFilePath}", dumpFilePath);
                return new DumpCheckResult
                {
                    IsEnabled = false,
                    Message = "Dumpchk is disabled in configuration.",
                    WasExecuted = false,
                    ExitCode = -1,
                };
            }

            // Validate the dump before invoking dumpchk.
            Validate(dumpFilePath);

            try
            {
                var dumpChkPath = await m_DumpChkLocator.FindDumpChkExecutableAsync().ConfigureAwait(false);
                return await m_DumpChkProcessRunner.RunAsync(dumpChkPath, dumpFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Failed to run dumpchk for dump file {DumpFilePath}", dumpFilePath);
                throw;
            }
        }
    }
}
