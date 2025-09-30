namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Represents the elevation type of a Windows access token
    /// </summary>
    public enum TokenElevationType
    {
        /// <summary>
        /// Token elevation type could not be determined
        /// </summary>
        Unknown,

        /// <summary>
        /// Token is a limited token (UAC filtered)
        /// </summary>
        Limited,

        /// <summary>
        /// Token has default privileges (standard user or admin without elevation)
        /// </summary>
        Default,

        /// <summary>
        /// Token is fully elevated with administrator privileges
        /// </summary>
        Full
    }
}
