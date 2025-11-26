namespace Nexus.Engine.DumpCheck;

/// <summary>
/// Provides an abstraction for validating crash dump files before analysis.
/// </summary>
public interface IDumpValidator
{
    /// <summary>
    /// Validates the specified dump file path and returns a value indicating whether it is considered valid.
    /// </summary>
    /// <param name="dumpFilePath">The full path to the dump file to validate.</param>
    /// <returns><c>true</c> if the dump is considered valid; otherwise, <c>false</c>.</returns>
    bool Validate(string dumpFilePath);
}


