using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for ServiceCommandHandler class.
    /// Note: Service command handler methods call Environment.Exit, making them difficult to unit test.
    /// These are smoke tests to verify basic instantiation and configuration.
    /// </summary>
    public class ServiceCommandHandlerTests
    {
        [Fact]
        public void ServiceCommandHandler_IsStaticClass()
        {
            var type = typeof(ServiceCommandHandler);
            Assert.True(type.IsAbstract && type.IsSealed, "ServiceCommandHandler should be a static class");
        }
    }
}

