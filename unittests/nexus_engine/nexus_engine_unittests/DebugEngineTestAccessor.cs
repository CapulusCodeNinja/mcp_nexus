using Nexus.Config;
using Nexus.Engine.Batch;
using Nexus.Engine.Share;
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
    /// <param name="fileCleanupQueue">The file cleanup queue.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    /// <param name="batchProcessor">The batch processing engine.</param>
    /// <param name="settings">The product settings.</param>
    public DebugEngineTestAccessor(IFileSystem fileSystem, IFileCleanupQueue fileCleanupQueue, IProcessManager processManager, IBatchProcessor batchProcessor, ISettings settings)
        : base(fileSystem, fileCleanupQueue, processManager, batchProcessor, settings)
    {
    }

    /// <summary>
    /// Exposes ValidateSessionId for testing.
    /// </summary>
    /// <param name="sessionId">Session identifier to validate.</param>
    /// <param name="paramName">Parameter name for exception context.</param>
    public static new void ValidateSessionId(string sessionId, string paramName)
    {
        DebugEngine.ValidateSessionId(sessionId, paramName);
    }

    /// <summary>
    /// Exposes ValidateCommandId for testing.
    /// </summary>
    /// <param name="commandId">Command identifier to validate.</param>
    /// <param name="paramName">Parameter name for exception context.</param>
    public static new void ValidateCommandId(string commandId, string paramName)
    {
        DebugEngine.ValidateCommandId(commandId, paramName);
    }

    /// <summary>
    /// Exposes ValidateCommand for testing.
    /// </summary>
    /// <param name="command">Command text to validate.</param>
    /// <param name="paramName">Parameter name for exception context.</param>
    public static new void ValidateCommand(string command, string paramName)
    {
        DebugEngine.ValidateCommand(command, paramName);
    }

    /// <summary>
    /// Exposes ValidateExtensionName for testing.
    /// </summary>
    /// <param name="extensionName">Extension name to validate.</param>
    /// <param name="paramName">Parameter name for exception context.</param>
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

    /// <summary>
    /// Invokes the protected idle session cleanup for testing.
    /// </summary>
    public void InvokeCleanupIdleSessions()
    {
        CleanupIdleSessions();
    }
}
