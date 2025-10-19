using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for ServerRunner class.
    /// Note: ServerRunner methods create WebApplication/Host instances which are difficult to unit test.
    /// These are smoke tests to verify basic instantiation and configuration.
    /// </summary>
    public class ServerRunnerTests
    {
        [Fact]
        public void ServerRunner_IsStaticClass()
        {
            var type = typeof(ServerRunner);
            Assert.True(type.IsAbstract && type.IsSealed, "ServerRunner should be a static class");
        }
    }
}

