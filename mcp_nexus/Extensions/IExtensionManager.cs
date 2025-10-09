namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Interface for managing and discovering extension scripts.
    /// </summary>
    public interface IExtensionManager
    {
        /// <summary>
        /// Discovers and loads all available extensions from the extensions directory.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task LoadExtensionsAsync();

        /// <summary>
        /// Gets metadata for a specific extension by name.
        /// </summary>
        /// <param name="extensionName">The name of the extension to retrieve.</param>
        /// <returns>The extension metadata, or null if not found.</returns>
        ExtensionMetadata? GetExtension(string extensionName);

        /// <summary>
        /// Gets all available extensions.
        /// </summary>
        /// <returns>A collection of all extension metadata.</returns>
        IEnumerable<ExtensionMetadata> GetAllExtensions();

        /// <summary>
        /// Checks if an extension with the given name exists.
        /// </summary>
        /// <param name="extensionName">The name of the extension to check.</param>
        /// <returns>True if the extension exists, false otherwise.</returns>
        bool ExtensionExists(string extensionName);

        /// <summary>
        /// Validates an extension's metadata and script file.
        /// </summary>
        /// <param name="extensionName">The name of the extension to validate.</param>
        /// <returns>A tuple containing validation result and error message if validation fails.</returns>
        (bool isValid, string? errorMessage) ValidateExtension(string extensionName);
    }
}

