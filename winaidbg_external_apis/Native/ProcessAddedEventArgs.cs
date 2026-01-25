namespace WinAiDbg.External.Apis.Native;

/// <summary>
/// Event arguments for the <see cref="ProcessTracker.ProcessAdded"/> event.
/// </summary>
public sealed class ProcessAddedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessAddedEventArgs"/> class.
    /// </summary>
    /// <param name="processSnapshotList">The snapshot for the process that was added.</param>
    public ProcessAddedEventArgs(IReadOnlyList<TrackedProcessSnapshot> processSnapshotList)
    {
        ArgumentNullException.ThrowIfNull(processSnapshotList);

        ProcessSnapshotList = processSnapshotList;
    }

    /// <summary>
    /// Gets the snapshot for the process that was added.
    /// </summary>
    public IReadOnlyList<TrackedProcessSnapshot> ProcessSnapshotList
    {
        get;
    }
}

