namespace mcp_nexus.Infrastructure.Adapters
{
    /// <summary>
    /// Infrastructure adapter for command queue - maintains compatibility with existing code
    /// </summary>
    public interface ICommandQueueAdapter
    {
        /// <summary>
        /// Gets the underlying queue implementation type
        /// </summary>
        string ImplementationType { get; }
    }
}
