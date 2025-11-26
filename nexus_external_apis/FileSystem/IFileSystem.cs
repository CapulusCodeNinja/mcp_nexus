namespace Nexus.External.Apis.FileSystem;

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

    /// <summary>
    /// Gets all files in a directory matching a pattern.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="searchPattern">The search pattern (e.g., "*.*").</param>
    /// <param name="searchOption">The search option (TopDirectoryOnly or AllDirectories).</param>
    /// <returns>An array of file paths.</returns>
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Copies a file from source to destination.
    /// </summary>
    /// <param name="sourceFileName">The source file path.</param>
    /// <param name="destFileName">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite an existing file.</param>
    void CopyFile(string sourceFileName, string destFileName, bool overwrite);

    /// <summary>
    /// Copies a file from source to destination asynchronously.
    /// </summary>
    /// <param name="sourceFileName">The source file path.</param>
    /// <param name="destFileName">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite an existing file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CopyFileAsync(string sourceFileName, string destFileName, bool overwrite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a DirectoryInfo object for the specified path.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>A DirectoryInfo object.</returns>
    DirectoryInfo GetDirectoryInfo(string path);

    /// <summary>
    /// Probes the specified file path by attempting to read from it to verify basic readability.
    /// </summary>
    /// <param name="path">The file path to probe.</param>
    void ProbeRead(string path);
}
