using nexus.engine.Configuration;
using nexus.engine.Events;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;
using Microsoft.Extensions.Logging;

namespace nexus.engine.unittests;

/// <summary>
/// Test accessor for DebugEngine that provides access to protected methods for testing.
/// </summary>
internal class DebugEngineTestAccessor : DebugEngine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebugEngineTestAccessor"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="fileSystem">The file system interface.</param>
    /// <param name="processManager">The process manager interface.</param>
    internal DebugEngineTestAccessor(
        ILoggerFactory loggerFactory,
        DebugEngineConfiguration configuration,
        IFileSystem fileSystem,
        IProcessManager processManager)
        : base(loggerFactory, configuration, fileSystem, processManager)
    {
    }

    /// <summary>
    /// Calls the protected OnSessionCommandStateChanged method.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    internal void TestOnSessionCommandStateChanged(object? sender, CommandStateChangedEventArgs e)
    {
        OnSessionCommandStateChanged(sender, e);
    }

    /// <summary>
    /// Calls the protected OnSessionStateChanged method.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    internal void TestOnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        OnSessionStateChanged(sender, e);
    }

    /// <summary>
    /// Calls the protected ThrowIfDisposed method.
    /// </summary>
    internal void TestThrowIfDisposed()
    {
        ThrowIfDisposed();
    }
}
