using mcp_nexus.Engine.Configuration;
using mcp_nexus.Engine.Internal;
using mcp_nexus.Utilities.FileSystem;
using mcp_nexus.Utilities.ProcessManagement;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Test accessor for CdbSession that provides access to protected methods for testing.
/// </summary>
internal class CdbSessionTestAccessor : CdbSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CdbSessionTestAccessor"/> class.
    /// </summary>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fileSystem">The file system interface.</param>
    /// <param name="processManager">The process manager interface.</param>
    internal CdbSessionTestAccessor(
        DebugEngineConfiguration configuration,
        ILogger<CdbSession> logger,
        IFileSystem fileSystem,
        IProcessManager processManager)
        : base(configuration, logger, fileSystem, processManager)
    {
    }

    /// <summary>
    /// Calls the protected WaitForCdbInitializationAsync method.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal Task TestWaitForCdbInitializationAsync(CancellationToken cancellationToken = default)
    {
        return WaitForCdbInitializationAsync(cancellationToken);
    }

    /// <summary>
    /// Calls the protected SendCommandToCdbAsync method.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal Task TestSendCommandToCdbAsync(string command, CancellationToken cancellationToken = default)
    {
        return SendCommandToCdbAsync(command, cancellationToken);
    }

    /// <summary>
    /// Calls the protected ReadCommandOutputAsync method.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command output.</returns>
    internal Task<string> TestReadCommandOutputAsync(CancellationToken cancellationToken = default)
    {
        return ReadCommandOutputAsync(cancellationToken);
    }

    /// <summary>
    /// Calls the protected ThrowIfDisposed method.
    /// </summary>
    internal void TestThrowIfDisposed()
    {
        ThrowIfDisposed();
    }

    /// <summary>
    /// Calls the protected ThrowIfNotInitialized method.
    /// </summary>
    internal void TestThrowIfNotInitialized()
    {
        ThrowIfNotInitialized();
    }
}
