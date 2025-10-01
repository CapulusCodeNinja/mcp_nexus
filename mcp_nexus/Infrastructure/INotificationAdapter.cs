namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Infrastructure adapter for notifications - maintains compatibility with existing code
    /// </summary>
    public interface INotificationAdapter
    {
        /// <summary>
        /// Gets the underlying notification implementation type
        /// </summary>
        string ImplementationType { get; }
    }
}
