namespace WinAiDbg.Engine.Share;

/// <summary>
/// Provides an asynchronous queue for file cleanup operations.
/// Files added to the queue will be deleted in the background with retry logic.
/// </summary>
public interface IFileCleanupQueue : IDisposable
{
    /// <summary>
    /// Enqueues a file for deletion. The deletion will be attempted asynchronously.
    /// If the deletion fails, the file will be re-queued for retry.
    /// </summary>
    /// <param name="filePath">The full path to the file to delete.</param>
    void Enqueue(string filePath);
}
