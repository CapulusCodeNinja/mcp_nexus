using mcp_nexus.Core.Application;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Infrastructure adapter for notifications - implements INotificationService
    /// </summary>
    public interface INotificationAdapter : INotificationService
    {
        /// <summary>
        /// Gets the underlying notification implementation type
        /// </summary>
        string ImplementationType { get; }
    }
}
