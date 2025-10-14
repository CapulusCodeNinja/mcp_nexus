namespace mcp_nexus.Utilities
{
    /// <summary>
    /// Interface for preprocessing WinDBG commands before execution.
    /// Handles path conversion and directory creation.
    /// </summary>
    public interface ICommandPreprocessor
    {
        /// <summary>
        /// Preprocesses a WinDBG command to convert WSL paths to Windows paths and ensure directories exist.
        /// ONLY does path conversion - NO syntax changes.
        /// </summary>
        /// <param name="command">The original command.</param>
        /// <returns>The command with paths converted.</returns>
        string PreprocessCommand(string command);
    }
}

