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
        /// Token belongs to a standard user (not in Administrators group)
        /// </summary>
        Standard,

        /// <summary>
        /// Token is a limited admin token (UAC filtered - admin user but privileges stripped)
        /// </summary>
        Limited,

        /// <summary>
        /// Token has default admin privileges (admin user, no UAC filtering)
        /// </summary>
        Default,

        /// <summary>
        /// Token is fully elevated with all administrator privileges
        /// </summary>
        Full
    }
}
