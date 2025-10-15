namespace mcp_nexus.Infrastructure.Adapters
{
    /// <summary>
    /// Infrastructure adapter for debugger - maintains compatibility with existing code
    /// </summary>
    public interface IDebuggerAdapter
    {
        /// <summary>
        /// Gets the underlying debugger implementation type
        /// </summary>
        string ImplementationType { get; }
    }
}
