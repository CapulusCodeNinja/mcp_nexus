using System.Text;

using WinAiDbg.Config;
using WinAiDbg.Engine.Internal;
using WinAiDbg.Engine.Preprocessing;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;

namespace WinAiDbg.Engine.Unittests.Internal;

/// <summary>
/// Test accessor for CdbSession that exposes protected methods for unit testing.
/// </summary>
internal class CdbSessionTestAccessor : CdbSession
{
    private bool? m_IsProcessExitedOverrideForTesting;
    private TimeSpan? m_DefaultCommandTimeoutOverrideForTesting;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdbSessionTestAccessor"/> class.
    /// </summary>
    /// <param name="settings">The product settings.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    public CdbSessionTestAccessor(ISettings settings, IFileSystem fileSystem, IProcessManager processManager)
        : base(settings, fileSystem, processManager, new CommandPreprocessor(fileSystem, processManager, settings))
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
    protected override Task WriteQuitCommandAsync()
    {
        WasWriteQuitCommandCalledForTesting = true;
        return Task.CompletedTask;
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
    public bool GetEffectiveProcessExitedForTesting()
    {
        return IsProcessExited();
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
    /// Exposes the CleanupOldCdbLogs method for testing.
    /// </summary>
    /// <param name="sessionsDirectory">The directory containing the CDB log files.</param>
    /// <param name="retentionDays">The retention period in days.</param>
    public void CleanupOldCdbLogsForTesting(string sessionsDirectory, int retentionDays)
    {
        CleanupOldCdbLogs(sessionsDirectory, retentionDays);
    }

    /// <summary>
    /// Sets the process exited state for testing purposes.
    /// </summary>
    /// <param name="exited">If set to true, simulates that the CDB process has exited.</param>
    public void SetProcessExitedForTesting(bool exited)
    {
        m_IsProcessExitedOverrideForTesting = exited;
    }

    /// <summary>
    /// Gets a value indicating whether the quit command was written during testing.
    /// </summary>
    public bool WasWriteQuitCommandCalledForTesting
    {
        get; private set;
    }

    /// <summary>
    /// Sets the default command timeout for testing purposes.
    /// </summary>
    /// <param name="timeout">The timeout to use.</param>
    public void SetDefaultCommandTimeoutForTesting(TimeSpan timeout)
    {
        m_DefaultCommandTimeoutOverrideForTesting = timeout;
    }

    /// <summary>
    /// Overrides the IsProcessExited method to use the test-specific state when provided.
    /// </summary>
    /// <returns>True if the process has exited, false otherwise.</returns>
    protected override bool IsProcessExited()
    {
        return m_IsProcessExitedOverrideForTesting ?? base.IsProcessExited();
    }

    /// <summary>
    /// Overrides the default command timeout for test determinism when provided.
    /// </summary>
    /// <returns>The command execution timeout.</returns>
    protected override TimeSpan GetDefaultCommandTimeout()
    {
        return m_DefaultCommandTimeoutOverrideForTesting ?? base.GetDefaultCommandTimeout();
    }

    /// <summary>
    /// Overrides output draining for tests to avoid arbitrary delays.
    /// </summary>
    /// <param name="maxDuration">Maximum time to drain.</param>
    /// <param name="cancellationToken">External cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task DrainUntilEndMarkerAsync(TimeSpan maxDuration, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the initialized state directly for testing purposes.
    /// </summary>
    /// <param name="initialized">The initialized state to set.</param>
    public new void SetInitializedForTesting(bool initialized)
    {
        base.SetInitializedForTesting(initialized);
    }
}
