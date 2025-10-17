namespace mcp_nexus.Utilities.PathHandling
{
    /// <summary>
    /// Interface for WSL path conversion operations.
    /// This allows for mocking in tests.
    /// </summary>
    public interface IWslPathConverter
    {
        /// <summary>
        /// Attempts to convert a WSL path to Windows format using wsl.exe.
        /// </summary>
        /// <param name="wslPath">The WSL path to convert</param>
        /// <param name="windowsPath">The converted Windows path if successful</param>
        /// <returns>True if conversion succeeded, false otherwise</returns>
        bool TryConvertToWindowsPath(string wslPath, out string windowsPath);

        /// <summary>
        /// Attempts to load WSL mount mappings from /etc/fstab.
        /// </summary>
        /// <returns>Dictionary of mount point to Windows path mappings</returns>
        Dictionary<string, string> LoadFstabMappings();
    }
}

