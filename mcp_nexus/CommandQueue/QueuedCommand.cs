namespace mcp_nexus.CommandQueue
{
    public record QueuedCommand(
        string Id,
        string Command,
        DateTime QueueTime,
        TaskCompletionSource<string> CompletionSource,
        CancellationTokenSource CancellationTokenSource,
        CommandState State = CommandState.Queued
    );
}
