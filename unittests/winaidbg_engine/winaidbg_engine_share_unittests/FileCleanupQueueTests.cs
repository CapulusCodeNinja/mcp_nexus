// Copyright (c) 2025 David Roller.
// Use of this source code is governed by a MIT-style license that can be
// found in the LICENSE file in the root of this repository.

using FluentAssertions;

using Moq;

using WinAiDbg.External.Apis.FileSystem;

using Xunit;

namespace WinAiDbg.Engine.Share.Tests;

/// <summary>
/// Unit tests for the <see cref="FileCleanupQueue"/> class.
/// Tests file cleanup operations, retry logic, and disposal behavior.
/// </summary>
public class FileCleanupQueueTests : IDisposable
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private FileCleanupQueue? m_Queue;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCleanupQueueTests"/> class.
    /// </summary>
    public FileCleanupQueueTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
    }

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        m_Queue?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that constructor throws when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new FileCleanupQueue(null!));
    }

    /// <summary>
    /// Verifies that constructor creates instance successfully with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidFileSystem_Succeeds()
    {
        // Act
        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Assert
        _ = m_Queue.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Enqueue with null path does not throw.
    /// </summary>
    [Fact]
    public void Enqueue_WithNullPath_DoesNotThrow()
    {
        // Arrange
        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        var exception = Record.Exception(() => m_Queue.Enqueue(null!));

        // Assert
        _ = exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Enqueue with empty path does not throw.
    /// </summary>
    [Fact]
    public void Enqueue_WithEmptyPath_DoesNotThrow()
    {
        // Arrange
        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        var exception = Record.Exception(() => m_Queue.Enqueue(string.Empty));

        // Assert
        _ = exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Enqueue with whitespace path does not throw.
    /// </summary>
    [Fact]
    public void Enqueue_WithWhitespacePath_DoesNotThrow()
    {
        // Arrange
        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        var exception = Record.Exception(() => m_Queue.Enqueue("   "));

        // Assert
        _ = exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a file is deleted when it exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enqueue_WithExistingFile_DeletesFile()
    {
        // Arrange
        const string filePath = @"C:\test\file.dmp";
        var deleteCalled = new TaskCompletionSource<bool>();

        _ = m_MockFileSystem.Setup(fs => fs.FileExists(filePath)).Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DeleteFile(filePath))
            .Callback(() => deleteCalled.TrySetResult(true));

        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        m_Queue.Enqueue(filePath);

        // Wait for processing (with timeout)
        var completed = await Task.WhenAny(deleteCalled.Task, Task.Delay(5000));

        // Assert
        _ = completed.Should().Be(deleteCalled.Task, "DeleteFile should have been called");
        m_MockFileSystem.Verify(fs => fs.DeleteFile(filePath), Times.Once);
    }

    /// <summary>
    /// Verifies that a file is skipped when it doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enqueue_WithNonExistingFile_SkipsDelete()
    {
        // Arrange
        const string filePath = @"C:\test\missing.dmp";
        var fileExistsCalled = new TaskCompletionSource<bool>();

        _ = m_MockFileSystem.Setup(fs => fs.FileExists(filePath))
            .Callback(() => fileExistsCalled.TrySetResult(true))
            .Returns(false);

        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        m_Queue.Enqueue(filePath);

        // Wait for processing (with timeout)
        var completed = await Task.WhenAny(fileExistsCalled.Task, Task.Delay(5000));

        // Assert
        _ = completed.Should().Be(fileExistsCalled.Task, "FileExists should have been called");
        m_MockFileSystem.Verify(fs => fs.FileExists(filePath), Times.Once);
        m_MockFileSystem.Verify(fs => fs.DeleteFile(filePath), Times.Never);
    }

    /// <summary>
    /// Verifies that multiple files can be enqueued and processed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enqueue_MultipleFiles_DeletesAll()
    {
        // Arrange
        var filePaths = new[] { @"C:\test\file1.dmp", @"C:\test\file2.dmp", @"C:\test\file3.dmp" };
        var deletedFiles = new List<string>();
        var allDeleted = new TaskCompletionSource<bool>();

        foreach (var path in filePaths)
        {
            _ = m_MockFileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
            _ = m_MockFileSystem.Setup(fs => fs.DeleteFile(path))
                .Callback<string>(p =>
                {
                    lock (deletedFiles)
                    {
                        deletedFiles.Add(p);
                        if (deletedFiles.Count == filePaths.Length)
                        {
                            _ = allDeleted.TrySetResult(true);
                        }
                    }
                });
        }

        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        foreach (var path in filePaths)
        {
            m_Queue.Enqueue(path);
        }

        // Wait for processing (with timeout)
        var completed = await Task.WhenAny(allDeleted.Task, Task.Delay(10000));

        // Assert
        _ = completed.Should().Be(allDeleted.Task, "All files should have been deleted");
        _ = deletedFiles.Should().BeEquivalentTo(filePaths);
    }

    /// <summary>
    /// Verifies that Dispose can be called multiple times without throwing.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            m_Queue.Dispose();
            m_Queue.Dispose();
            m_Queue.Dispose();
        });

        _ = exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Dispose stops processing pending items gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_WithPendingItems_StopsGracefully()
    {
        // Arrange
        const string filePath = @"C:\test\pending.dmp";

        // Set up file system to block on FileExists to simulate slow processing
        _ = m_MockFileSystem.Setup(fs => fs.FileExists(filePath))
            .Returns(() =>
            {
                Thread.Sleep(50); // Brief block
                return true;
            });

        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);
        m_Queue.Enqueue(filePath);

        // Act - Dispose should not hang
        var disposeTask = Task.Run(() => m_Queue.Dispose());

        // Assert - if we get here without timeout, dispose completed successfully
        var exception = await Record.ExceptionAsync(async () =>
            await disposeTask.WaitAsync(TimeSpan.FromSeconds(10)));

        _ = exception.Should().BeNull("Dispose should complete within timeout");
    }

    /// <summary>
    /// Verifies that IOException triggers retry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enqueue_WithIOException_RetriesDelete()
    {
        // Arrange
        const string filePath = @"C:\test\locked.dmp";
        var deleteAttempts = 0;
        var secondAttemptStarted = new TaskCompletionSource<bool>();

        _ = m_MockFileSystem.Setup(fs => fs.FileExists(filePath)).Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DeleteFile(filePath))
            .Callback(() =>
            {
                deleteAttempts++;
                if (deleteAttempts == 1)
                {
                    throw new IOException("File in use");
                }

                // Second attempt - signal completion
                _ = secondAttemptStarted.TrySetResult(true);
            });

        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        m_Queue.Enqueue(filePath);

        // Wait for retry (with timeout - note: retry delay is 5 seconds)
        var completed = await Task.WhenAny(secondAttemptStarted.Task, Task.Delay(15000));

        // Assert
        _ = completed.Should().Be(secondAttemptStarted.Task, "Retry should have occurred");
        _ = deleteAttempts.Should().BeGreaterThanOrEqualTo(2);
    }

    /// <summary>
    /// Verifies that UnauthorizedAccessException triggers retry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enqueue_WithUnauthorizedAccessException_RetriesDelete()
    {
        // Arrange
        const string filePath = @"C:\test\noaccess.dmp";
        var deleteAttempts = 0;
        var secondAttemptStarted = new TaskCompletionSource<bool>();

        _ = m_MockFileSystem.Setup(fs => fs.FileExists(filePath)).Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DeleteFile(filePath))
            .Callback(() =>
            {
                deleteAttempts++;
                if (deleteAttempts == 1)
                {
                    throw new UnauthorizedAccessException("Access denied");
                }

                // Second attempt - signal completion
                _ = secondAttemptStarted.TrySetResult(true);
            });

        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        m_Queue.Enqueue(filePath);

        // Wait for retry (with timeout - note: retry delay is 5 seconds)
        var completed = await Task.WhenAny(secondAttemptStarted.Task, Task.Delay(15000));

        // Assert
        _ = completed.Should().Be(secondAttemptStarted.Task, "Retry should have occurred");
        _ = deleteAttempts.Should().BeGreaterThanOrEqualTo(2);
    }

    /// <summary>
    /// Verifies that unexpected exceptions do not crash the queue.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enqueue_WithUnexpectedException_ContinuesProcessing()
    {
        // Arrange
        const string filePath1 = @"C:\test\error.dmp";
        const string filePath2 = @"C:\test\success.dmp";
        var file2Deleted = new TaskCompletionSource<bool>();

        _ = m_MockFileSystem.Setup(fs => fs.FileExists(filePath1)).Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DeleteFile(filePath1))
            .Throws(new InvalidOperationException("Unexpected error"));

        _ = m_MockFileSystem.Setup(fs => fs.FileExists(filePath2)).Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DeleteFile(filePath2))
            .Callback(() => file2Deleted.TrySetResult(true));

        m_Queue = new FileCleanupQueue(m_MockFileSystem.Object);

        // Act
        m_Queue.Enqueue(filePath1);
        m_Queue.Enqueue(filePath2);

        // Wait for second file to be processed
        var completed = await Task.WhenAny(file2Deleted.Task, Task.Delay(5000));

        // Assert
        _ = completed.Should().Be(file2Deleted.Task, "Second file should still be processed after first fails");
        m_MockFileSystem.Verify(fs => fs.DeleteFile(filePath2), Times.Once);
    }
}
