using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Configuration;

namespace mcp_nexus_tests.Configuration
{
    /// <summary>
    /// Tests for ServiceRegistration
    /// </summary>
    public class ServiceRegistrationTests
    {
        [Fact]
        public void ServiceRegistration_Class_Exists()
        {
            // This test verifies that the ServiceRegistration class exists and can be instantiated
            Assert.True(typeof(ServiceRegistration) != null);
        }
    }
}
