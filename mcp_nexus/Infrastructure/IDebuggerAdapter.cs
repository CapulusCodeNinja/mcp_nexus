using mcp_nexus.Core.Application;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Infrastructure adapter for debugger - implements IDebuggerService
    /// </summary>
    public interface IDebuggerAdapter : IDebuggerService
    {
        /// <summary>
        /// Gets the underlying debugger implementation type
        /// </summary>
        string ImplementationType { get; }
    }
}
