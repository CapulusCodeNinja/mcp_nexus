using mcp_nexus.Core.Application;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Infrastructure adapter for command queue - implements ICommandQueueService
    /// </summary>
    public interface ICommandQueueAdapter : ICommandQueueService
    {
        /// <summary>
        /// Gets the underlying queue implementation type
        /// </summary>
        string ImplementationType { get; }
    }
}
