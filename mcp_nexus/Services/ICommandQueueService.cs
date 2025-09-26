namespace mcp_nexus.Services
{
    public interface ICommandQueueService : IDisposable
    {
        string QueueCommand(string command);
        Task<string> GetCommandResult(string commandId);
        bool CancelCommand(string commandId);
        int CancelAllCommands(string? reason = null);
        IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus();
        QueuedCommand? GetCurrentCommand();
    }
}

