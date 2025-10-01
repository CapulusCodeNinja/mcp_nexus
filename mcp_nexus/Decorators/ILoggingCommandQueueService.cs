using mcp_nexus.CommandQueue;

namespace mcp_nexus.Decorators
{
    /// <summary>
    /// Decorator interface for adding logging functionality to command queue service
    /// </summary>
    public interface ILoggingCommandQueueService : ICommandQueueService
    {
        /// <summary>
        /// Gets the underlying command queue service
        /// </summary>
        ICommandQueueService UnderlyingService { get; }
    }
}
