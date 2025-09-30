namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Detailed command information for type-safe status checking
    /// </summary>
    public class CommandInfo
    {
        public string CommandId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public CommandState State { get; set; }
        public DateTime QueueTime { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan Remaining { get; set; }
        public int QueuePosition { get; set; }
        public bool IsCompleted { get; set; }
    }
}
