using WinAiDbg.Config;
using WinAiDbg.Engine.Batch;
using WinAiDbg.Engine.Internal;
using WinAiDbg.Engine.Preprocessing;
using WinAiDbg.Engine.Share;
using WinAiDbg.Engine.Share.Events;
using WinAiDbg.Engine.Share.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;

namespace WinAiDbg.Engine.Unittests.Internal;

/// <summary>
/// Test accessor class for DebugSession that exposes protected methods for testing.
/// </summary>
internal class DebugSessionTestAccessor : DebugSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebugSessionTestAccessor"/> class.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="dumpFilePath">The dump file path.</param>
    /// <param name="symbolPath">The symbol path.</param>
    /// <param name="settings">The product settings.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="fileCleanupQueue">The file cleanup queue.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    /// <param name="batchProcessor">The batch processing engine.</param>
    public DebugSessionTestAccessor(
        string sessionId,
        string dumpFilePath,
        string? symbolPath,
        ISettings settings,
        IFileSystem fileSystem,
        IFileCleanupQueue fileCleanupQueue,
        IProcessManager processManager,
        IBatchProcessor batchProcessor)
        : base(sessionId, dumpFilePath, symbolPath, settings, fileSystem, fileCleanupQueue, processManager, batchProcessor, new CommandPreprocessor(fileSystem, processManager, settings))
    {
    }

    /// <summary>
    /// Exposes OnCommandStateChanged for testing.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event args.</param>
    public new void OnCommandStateChanged(object? sender, CommandStateChangedEventArgs e)
    {
        base.OnCommandStateChanged(sender, e);
    }

    /// <summary>
    /// Exposes SetState for testing.
    /// </summary>
    /// <param name="newState">New state to set.</param>
    public new void SetState(SessionState newState)
    {
        base.SetState(newState);
    }

    /// <summary>
    /// Exposes ThrowIfDisposed for testing.
    /// </summary>
    public new void ThrowIfDisposed()
    {
        base.ThrowIfDisposed();
    }

    /// <summary>
    /// Exposes ThrowIfNotActive for testing.
    /// </summary>
    public new void ThrowIfNotActive()
    {
        base.ThrowIfNotActive();
    }
}
