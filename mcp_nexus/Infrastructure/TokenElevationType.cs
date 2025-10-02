using System;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Represents the elevation type of a token
    /// </summary>
    public enum TokenElevationType
    {
        /// <summary>
        /// Token elevation type is unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Token is not elevated
        /// </summary>
        Limited = 1,

        /// <summary>
        /// Token is elevated
        /// </summary>
        Full = 2
    }
}
