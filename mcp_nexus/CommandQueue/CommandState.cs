namespace mcp_nexus.CommandQueue
{
    public enum CommandState
    {
        Queued,
        Executing,
        Completed,
        Cancelled,
        Failed
    }
}
