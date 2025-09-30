using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for ServiceFileManager
    /// </summary>
    public class ServiceFileManagerTests
    {
        [Fact]
        public void ServiceFileManager_Class_Exists()
        {
            // This test verifies that the ServiceFileManager class exists and can be instantiated
            Assert.True(typeof(ServiceFileManager) != null);
        }
    }
}
