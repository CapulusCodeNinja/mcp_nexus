namespace mcp_nexus.Utilities
{
    /// <summary>
    /// Interface for path conversion and handling operations.
    /// Abstracts WSL path conversion, Windows path validation, and path normalization.
    /// </summary>
    public interface IPathHandler
    {
        /// <summary>
        /// Converts a WSL path to a Windows path.
        /// </summary>
        /// <param name="path">The WSL path to convert.</param>
        /// <returns>The converted Windows path.</returns>
        string ConvertToWindowsPath(string path);

        /// <summary>
        /// Converts a Windows path to a WSL path.
        /// </summary>
        /// <param name="path">The Windows path to convert.</param>
        /// <returns>The converted WSL path.</returns>
        string ConvertToWslPath(string path);

        /// <summary>
        /// Normalizes a path for Windows by converting WSL paths if detected.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized Windows path.</returns>
        string NormalizeForWindows(string path);

        /// <summary>
        /// Normalizes multiple paths for Windows by converting WSL paths if detected.
        /// </summary>
        /// <param name="paths">The paths to normalize.</param>
        /// <returns>The normalized Windows paths.</returns>
        string[] NormalizeForWindows(string[] paths);

        /// <summary>
        /// Determines if a path is a Windows path.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path is a Windows path; otherwise, false.</returns>
        bool IsWindowsPath(string path);

        /// <summary>
        /// Clears the internal path conversion cache.
        /// </summary>
        void ClearCache();
    }
}

