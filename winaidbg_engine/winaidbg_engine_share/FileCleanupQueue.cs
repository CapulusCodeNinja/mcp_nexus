using System.Collections.Concurrent;

using NLog;

using WinAiDbg.External.Apis.FileSystem;

namespace WinAiDbg.Engine.Share;

/// <summary>
/// Provides an asynchronous queue for file cleanup operations with retry logic.
/// Files are deleted in the background, and failed deletions are re-queued.
/// </summary>
public class FileCleanupQueue : IFileCleanupQueue
{
    /// <summary>
    /// Maximum number of retry attempts before giving up on a file.
    /// </summary>
    private const int MaxRetries = 10;

    /// <summary>
    /// Delay between retry attempts in milliseconds.
    /// </summary>
    private const int RetryDelayMs = 5000;

    private readonly Logger m_Logger = LogManager.GetCurrentClassLogger();
    private readonly IFileSystem m_FileSystem;
    private readonly TimeSpan m_RetryDelay;
    private readonly ConcurrentQueue<CleanupItem> m_Queue = new();
    private readonly CancellationTokenSource m_Cts = new();
    private readonly Task m_ProcessingTask;
    private readonly SemaphoreSlim m_QueueSignal = new(0);
    private bool m_Disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCleanupQueue"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction for file operations.</param>
    /// <param name="startBackgroundProcessing">True to start background processing immediately.</param>
    /// <param name="retryDelay">Optional retry delay override (defaults to 5 seconds).</param>
    public FileCleanupQueue(IFileSystem fileSystem, bool startBackgroundProcessing = true, TimeSpan? retryDelay = null)
    {
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_RetryDelay = retryDelay ?? TimeSpan.FromMilliseconds(RetryDelayMs);
        m_ProcessingTask = startBackgroundProcessing ? Task.Run(ProcessQueueAsync) : Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Enqueue(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        m_Logger.Debug("Enqueuing file for cleanup: {FilePath}", filePath);
        m_Queue.Enqueue(new CleanupItem(filePath, 0));
        _ = m_QueueSignal.Release();
    }

    /// <summary>
    /// Disposes the cleanup queue, waiting for pending operations to complete.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the cleanup queue resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (m_Disposed)
        {
            return;
        }

        if (disposing)
        {
            m_Cts.Cancel();

            try
            {
                // Wait briefly for the processing task to complete
                _ = m_ProcessingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Task was cancelled, expected
            }

            m_QueueSignal.Dispose();
            m_Cts.Dispose();
        }

        m_Disposed = true;
    }

    /// <summary>
    /// Background task that continuously processes the cleanup queue.
    /// </summary>
    /// <returns>A task that represents the asynchronous processing operation.</returns>
    private async Task ProcessQueueAsync()
    {
        m_Logger.Info("File cleanup queue started");

        while (!m_Cts.IsCancellationRequested)
        {
            try
            {
                if (!m_Queue.TryDequeue(out var item))
                {
                    await m_QueueSignal.WaitAsync(m_Cts.Token);
                    continue;
                }

                await ProcessItemAsync(item);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Unexpected error in file cleanup queue processing");
            }
        }

        m_Logger.Info("File cleanup queue stopped");
    }

    /// <summary>
    /// Processes a single cleanup item, attempting to delete the file.
    /// </summary>
    /// <param name="item">The cleanup item to process.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    private async Task ProcessItemAsync(CleanupItem item)
    {
        try
        {
            if (!m_FileSystem.FileExists(item.FilePath))
            {
                m_Logger.Debug("File no longer exists, skipping: {FilePath}", item.FilePath);
                return;
            }

            m_FileSystem.DeleteFile(item.FilePath);
            m_Logger.Info("Successfully deleted file: {FilePath}", item.FilePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleRetryAsync(item, ex, "access denied");
        }
        catch (IOException ex)
        {
            await HandleRetryAsync(item, ex, "file in use");
        }
        catch (Exception ex)
        {
            m_Logger.Warn(ex, "Failed to delete file (unexpected error): {FilePath}", item.FilePath);
        }
    }

    /// <summary>
    /// Handles retry logic for failed deletion attempts.
    /// </summary>
    /// <param name="item">The cleanup item that failed.</param>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="reason">A human-readable reason for the failure.</param>
    /// <returns>A task that represents the asynchronous retry operation.</returns>
    private async Task HandleRetryAsync(CleanupItem item, Exception ex, string reason)
    {
        var newRetryCount = item.RetryCount + 1;

        if (newRetryCount >= MaxRetries)
        {
            m_Logger.Warn(ex, "Giving up on file deletion after {MaxRetries} attempts ({Reason}): {FilePath}", MaxRetries, reason, item.FilePath);
            return;
        }

        m_Logger.Debug("File deletion failed ({Reason}), re-queuing for retry {RetryCount}/{MaxRetries}: {FilePath}", reason, newRetryCount, MaxRetries, item.FilePath);

        // Wait before re-queuing to give the system time to release file handles
        await Task.Delay(m_RetryDelay, m_Cts.Token);

        // Re-queue at the end with incremented retry count
        m_Queue.Enqueue(new CleanupItem(item.FilePath, newRetryCount));
        _ = m_QueueSignal.Release();
    }

    /// <summary>
    /// Processes the next queued item synchronously for unit testing.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous processing operation, with a value indicating whether an item was processed.
    /// </returns>
    internal async Task<bool> ProcessNextItemForTestingAsync()
    {
        if (m_Queue.TryDequeue(out var item))
        {
            await ProcessItemAsync(item);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Represents an item in the cleanup queue.
    /// </summary>
    /// <param name="FilePath">The path to the file to delete.</param>
    /// <param name="RetryCount">The number of times deletion has been attempted.</param>
    private sealed record CleanupItem(string FilePath, int RetryCount);
}
