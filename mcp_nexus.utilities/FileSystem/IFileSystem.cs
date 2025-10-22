using System.IO;

namespace mcp_nexus.Utilities.FileSystem;

/// <summary>
/// Interface for file system operations to enable mocking in tests.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Checks if a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>True if the directory exists, false otherwise.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Gets the full path of the specified path.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The full path.</returns>
    string GetFullPath(string path);

    /// <summary>
    /// Combines multiple path strings into a single path.
    /// </summary>
    /// <param name="paths">The path components to combine.</param>
    /// <returns>The combined path.</returns>
    string CombinePaths(params string[] paths);

    /// <summary>
    /// Gets the directory name of the specified path.
    /// </summary>
    /// <param name="path">The path to get the directory name for.</param>
    /// <returns>The directory name, or null if the path is null or empty.</returns>
    string? GetDirectoryName(string path);

    /// <summary>
    /// Gets the file name of the specified path.
    /// </summary>
    /// <param name="path">The path to get the file name for.</param>
    /// <returns>The file name, or null if the path is null or empty.</returns>
    string? GetFileName(string path);

    /// <summary>
    /// Gets the file name without extension of the specified path.
    /// </summary>
    /// <param name="path">The path to get the file name without extension for.</param>
    /// <returns>The file name without extension, or null if the path is null or empty.</returns>
    string? GetFileNameWithoutExtension(string path);

    /// <summary>
    /// Gets the extension of the specified path.
    /// </summary>
    /// <param name="path">The path to get the extension for.</param>
    /// <returns>The extension, or null if the path is null or empty.</returns>
    string? GetExtension(string path);

    /// <summary>
    /// Creates a directory at the specified path.
    /// </summary>
    /// <param name="path">The directory path to create.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <param name="path">The file path to delete.</param>
    void DeleteFile(string path);

    /// <summary>
    /// Deletes a directory at the specified path.
    /// </summary>
    /// <param name="path">The directory path to delete.</param>
    /// <param name="recursive">Whether to delete subdirectories and files.</param>
    void DeleteDirectory(string path, bool recursive = false);

    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <returns>The file contents as a string.</returns>
    string ReadAllText(string path);

    /// <summary>
    /// Writes text to a file.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="contents">The text to write.</param>
    void WriteAllText(string path, string contents);

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    /// <returns>The current working directory path.</returns>
    string GetCurrentDirectory();

    /// <summary>
    /// Sets the current working directory.
    /// </summary>
    /// <param name="path">The directory path to set as current.</param>
    void SetCurrentDirectory(string path);
}
