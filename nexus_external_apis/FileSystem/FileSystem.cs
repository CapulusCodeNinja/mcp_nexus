namespace Nexus.External.Apis.FileSystem;

/// <summary>
/// Concrete implementation of IFileSystem that uses the real file system.
/// </summary>
public class FileSystem : IFileSystem
{
    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// Checks if a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>True if the directory exists, false otherwise.</returns>
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    /// <summary>
    /// Gets the full path of the specified path.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The full path.</returns>
    public string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Combines multiple path strings into a single path.
    /// </summary>
    /// <param name="paths">The path components to combine.</param>
    /// <returns>The combined path.</returns>
    public string CombinePaths(params string[] paths)
    {
        return Path.Combine(paths);
    }

    /// <summary>
    /// Gets the directory name of the specified path.
    /// </summary>
    /// <param name="path">The path to get the directory name for.</param>
    /// <returns>The directory name, or null if the path is null or empty.</returns>
    public string? GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    /// <summary>
    /// Gets the file name of the specified path.
    /// </summary>
    /// <param name="path">The path to get the file name for.</param>
    /// <returns>The file name, or null if the path is null or empty.</returns>
    public string? GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    /// <summary>
    /// Gets the file name without extension of the specified path.
    /// </summary>
    /// <param name="path">The path to get the file name without extension for.</param>
    /// <returns>The file name without extension, or null if the path is null or empty.</returns>
    public string? GetFileNameWithoutExtension(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>
    /// Gets the extension of the specified path.
    /// </summary>
    /// <param name="path">The path to get the extension for.</param>
    /// <returns>The extension, or null if the path is null or empty.</returns>
    public string? GetExtension(string path)
    {
        return Path.GetExtension(path);
    }

    /// <summary>
    /// Creates a directory at the specified path.
    /// </summary>
    /// <param name="path">The directory path to create.</param>
    public void CreateDirectory(string path)
    {
        _ = Directory.CreateDirectory(path);
    }

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <param name="path">The file path to delete.</param>
    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    /// <summary>
    /// Deletes a directory at the specified path.
    /// </summary>
    /// <param name="path">The directory path to delete.</param>
    /// <param name="recursive">Whether to delete subdirectories and files.</param>
    public void DeleteDirectory(string path, bool recursive = false)
    {
        Directory.Delete(path, recursive);
    }

    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <returns>The file contents as a string.</returns>
    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Writes text to a file.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="contents">The text to write.</param>
    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    /// <returns>The current working directory path.</returns>
    public string GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Sets the current working directory.
    /// </summary>
    /// <param name="path">The directory path to set as current.</param>
    public void SetCurrentDirectory(string path)
    {
        Directory.SetCurrentDirectory(path);
    }

    /// <summary>
    /// Gets all files in a directory matching a pattern.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="searchPattern">The search pattern (e.g., "*.*").</param>
    /// <param name="searchOption">The search option (TopDirectoryOnly or AllDirectories).</param>
    /// <returns>An array of file paths.</returns>
    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    /// <summary>
    /// Copies a file from source to destination.
    /// </summary>
    /// <param name="sourceFileName">The source file path.</param>
    /// <param name="destFileName">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite an existing file.</param>
    public void CopyFile(string sourceFileName, string destFileName, bool overwrite)
    {
        File.Copy(sourceFileName, destFileName, overwrite);
    }

    /// <summary>
    /// Copies a file from source to destination asynchronously.
    /// </summary>
    /// <param name="sourceFileName">The source file path.</param>
    /// <param name="destFileName">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite an existing file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CopyFileAsync(string sourceFileName, string destFileName, bool overwrite, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => File.Copy(sourceFileName, destFileName, overwrite), cancellationToken);
    }

    /// <summary>
    /// Gets a DirectoryInfo object for the specified path.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>A DirectoryInfo object.</returns>
    public DirectoryInfo GetDirectoryInfo(string path)
    {
        return new DirectoryInfo(path);
    }
}
