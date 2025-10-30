using Nexus.Config;
using Nexus.Engine.Batch;
using Nexus.Engine.Internal;
using Nexus.Engine.Preprocessing;
using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

namespace Nexus.Engine.Unittests.Internal;

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
    /// <param name="processManager">The process manager abstraction.</param>
    /// <param name="batchProcessor">The batch processing engine.</param>
    public DebugSessionTestAccessor(
        string sessionId,
        string dumpFilePath,
        string? symbolPath,
        ISettings settings,
        IFileSystem fileSystem,
        IProcessManager processManager,
        IBatchProcessor batchProcessor)
        : base(sessionId, dumpFilePath, symbolPath, settings, fileSystem, processManager, batchProcessor, new CommandPreprocessor(fileSystem, settings))
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
