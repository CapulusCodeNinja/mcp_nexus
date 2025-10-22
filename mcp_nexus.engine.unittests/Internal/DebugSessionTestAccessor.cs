using mcp_nexus.Engine.Configuration;
using mcp_nexus.Engine.Events;
using mcp_nexus.Engine.Internal;
using mcp_nexus.Engine.Models;
using mcp_nexus.Utilities.FileSystem;
using mcp_nexus.Utilities.ProcessManagement;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Test accessor for DebugSession that provides access to protected methods for testing.
/// </summary>
internal class DebugSessionTestAccessor : DebugSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebugSessionTestAccessor"/> class.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="dumpFilePath">The dump file path.</param>
    /// <param name="symbolPath">The symbol path.</param>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="fileSystem">The file system interface.</param>
    /// <param name="processManager">The process manager interface.</param>
    internal DebugSessionTestAccessor(
        string sessionId,
        string dumpFilePath,
        string? symbolPath,
        DebugEngineConfiguration configuration,
        ILoggerFactory loggerFactory,
        IFileSystem fileSystem,
        IProcessManager processManager)
        : base(sessionId, dumpFilePath, symbolPath, configuration, loggerFactory, fileSystem, processManager)
    {
    }

    /// <summary>
    /// Calls the protected OnCommandStateChanged method.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    internal void TestOnCommandStateChanged(object? sender, CommandStateChangedEventArgs e)
    {
        OnCommandStateChanged(sender, e);
    }

    /// <summary>
    /// Calls the protected SetState method.
    /// </summary>
    /// <param name="newState">The new state.</param>
    internal void TestSetState(SessionState newState)
    {
        SetState(newState);
    }

    /// <summary>
    /// Calls the protected ThrowIfDisposed method.
    /// </summary>
    internal void TestThrowIfDisposed()
    {
        ThrowIfDisposed();
    }

    /// <summary>
    /// Calls the protected ThrowIfNotActive method.
    /// </summary>
    internal void TestThrowIfNotActive()
    {
        ThrowIfNotActive();
    }
}
