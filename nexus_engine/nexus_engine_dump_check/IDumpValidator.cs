namespace Nexus.Engine.DumpCheck;

/// <summary>
/// Provides an abstraction for validating crash dump files before analysis.
/// </summary>
public interface IDumpValidator
{
    /// <summary>
    /// Runs dumpchk for the specified dump file path when dumpchk integration is enabled.
    /// </summary>
    /// <param name="dumpFilePath">The full path to the dump file to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the combined
    /// dumpchk standard output and error streams as a single string.
    /// </returns>
    Task<string> RunDumpChkAsync(string dumpFilePath, CancellationToken cancellationToken = default);
}


