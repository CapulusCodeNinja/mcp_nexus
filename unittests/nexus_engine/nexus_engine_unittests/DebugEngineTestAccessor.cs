using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

namespace Nexus.Engine.Unittests;

/// <summary>
/// Test accessor class for DebugEngine that exposes protected methods for testing.
/// </summary>
internal class DebugEngineTestAccessor : DebugEngine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebugEngineTestAccessor"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    public DebugEngineTestAccessor(IFileSystem fileSystem, IProcessManager processManager)
        : base(fileSystem, processManager)
    {
    }

    /// <summary>
    /// Exposes ValidateSessionId for testing.
    /// </summary>
    public static new void ValidateSessionId(string sessionId, string paramName)
    {
        DebugEngine.ValidateSessionId(sessionId, paramName);
    }

    /// <summary>
    /// Exposes ValidateCommandId for testing.
    /// </summary>
    public static new void ValidateCommandId(string commandId, string paramName)
    {
        DebugEngine.ValidateCommandId(commandId, paramName);
    }

    /// <summary>
    /// Exposes ValidateCommand for testing.
    /// </summary>
    public static new void ValidateCommand(string command, string paramName)
    {
        DebugEngine.ValidateCommand(command, paramName);
    }

    /// <summary>
    /// Exposes ValidateExtensionName for testing.
    /// </summary>
    public static new void ValidateExtensionName(string extensionName, string paramName)
    {
        DebugEngine.ValidateExtensionName(extensionName, paramName);
    }

    /// <summary>
    /// Exposes ThrowIfDisposed for testing.
    /// </summary>
    public new void ThrowIfDisposed()
    {
        base.ThrowIfDisposed();
    }
}

