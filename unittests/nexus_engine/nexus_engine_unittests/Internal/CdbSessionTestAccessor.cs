using System.Text;

using Nexus.Engine.Internal;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

namespace Nexus.Engine.Tests.Internal;

/// <summary>
/// Test accessor for CdbSession that exposes protected methods for unit testing.
/// </summary>
internal class CdbSessionTestAccessor : CdbSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CdbSessionTestAccessor"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    public CdbSessionTestAccessor(IFileSystem fileSystem, IProcessManager processManager)
        : base(fileSystem, processManager, new Nexus.Engine.Preprocessing.CommandPreprocessor(fileSystem))
    {
    }

    /// <summary>
    /// Exposes the protected WaitForCdbInitializationAsync method.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous wait operation.</returns>
    public new Task WaitForCdbInitializationAsync(CancellationToken cancellationToken)
    {
        return base.WaitForCdbInitializationAsync(cancellationToken);
    }

    /// <summary>
    /// Exposes the protected SendQuitCommandAsync method.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task SendQuitCommandAsync()
    {
        return base.SendQuitCommandAsync();
    }

    /// <summary>
    /// Exposes the protected WriteQuitCommandAsync method.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task WriteQuitCommandAsync()
    {
        return base.WriteQuitCommandAsync();
    }

    /// <summary>
    /// Exposes the protected FlushInputAsync method.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task FlushInputAsync()
    {
        return base.FlushInputAsync();
    }

    /// <summary>
    /// Exposes the protected WaitForProcessExitAsync method.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task WaitForProcessExitAsync()
    {
        return base.WaitForProcessExitAsync();
    }

    /// <summary>
    /// Exposes the protected KillProcess method.
    /// </summary>
    public new void KillProcess()
    {
        base.KillProcess();
    }

    /// <summary>
    /// Exposes the protected DisposeResources method.
    /// </summary>
    public new void DisposeResources()
    {
        base.DisposeResources();
    }

    /// <summary>
    /// Exposes the protected SetDisposedState method.
    /// </summary>
    public new void SetDisposedState()
    {
        base.SetDisposedState();
    }

    /// <summary>
    /// Exposes the protected IsProcessExited method.
    /// </summary>
    /// <returns>True if the process has exited, false otherwise.</returns>
    public new bool IsProcessExited()
    {
        return base.IsProcessExited();
    }

    /// <summary>
    /// Exposes the protected WaitForInitializationDelay method.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public new Task WaitForInitializationDelay(CancellationToken cancellationToken)
    {
        return base.WaitForInitializationDelay(cancellationToken);
    }

    /// <summary>
    /// Exposes the protected SendCommandToCdbAsync method.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task SendCommandToCdbAsync(string command)
    {
        return base.SendCommandToCdbAsync(command);
    }

    /// <summary>
    /// Exposes the protected ReadCommandOutputAsync method.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command output as a string.</returns>
    public new Task<string> ReadCommandOutputAsync(CancellationToken cancellationToken)
    {
        return base.ReadCommandOutputAsync(cancellationToken);
    }

    /// <summary>
    /// Exposes the protected ReadLineFromOutputAsync method.
    /// </summary>
    /// <returns>The line read from the output stream, or null if no more lines.</returns>
    public new Task<string?> ReadLineFromOutputAsync()
    {
        return base.ReadLineFromOutputAsync();
    }

    /// <summary>
    /// Exposes the protected ProcessOutputLine method.
    /// </summary>
    /// <param name="line">The line to process.</param>
    /// <param name="startMarkerFound">Reference to the start marker found flag.</param>
    /// <param name="output">The output string builder to append to.</param>
    /// <returns>A tuple indicating whether to continue processing and whether to break the loop.</returns>
    public new (bool ShouldContinue, bool ShouldBreak) ProcessOutputLine(string line, ref bool startMarkerFound, StringBuilder output)
    {
        return base.ProcessOutputLine(line, ref startMarkerFound, output);
    }

    /// <summary>
    /// Exposes the protected ThrowIfDisposed method.
    /// </summary>
    public new void ThrowIfDisposed()
    {
        base.ThrowIfDisposed();
    }

    /// <summary>
    /// Exposes the protected ThrowIfNotInitialized method.
    /// </summary>
    public new void ThrowIfNotInitialized()
    {
        base.ThrowIfNotInitialized();
    }

    /// <summary>
    /// Exposes the static CreateCommandWithSentinels method.
    /// </summary>
    /// <param name="command">The CDB command to wrap.</param>
    /// <returns>The command string with sentinels.</returns>
    public static new string CreateCommandWithSentinels(string command)
    {
        return CdbSession.CreateCommandWithSentinels(command);
    }
}

