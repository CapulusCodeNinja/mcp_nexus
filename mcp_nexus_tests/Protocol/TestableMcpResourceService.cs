using mcp_nexus.Protocol;
using mcp_nexus.Session;
using Microsoft.Extensions.Logging;

namespace mcp_nexus_tests.Protocol
{
    /// <summary>
    /// Testable wrapper for McpResourceService that exposes protected methods for testing
    /// </summary>
    public class TestableMcpResourceService : McpResourceService
    {
        public TestableMcpResourceService(ISessionManager sessionManager, ILogger<McpResourceService> logger) 
            : base(sessionManager, logger)
        {
        }

        /// <summary>
        /// Exposes the protected ParseCommandFilters method for testing
        /// </summary>
        public static CommandFilters TestParseCommandFilters(string uri)
        {
            return ParseCommandFilters(uri);
        }

        /// <summary>
        /// Exposes the protected ParseSessionFilters method for testing
        /// </summary>
        public static SessionFilters TestParseSessionFilters(string uri)
        {
            return ParseSessionFilters(uri);
        }
    }
}
