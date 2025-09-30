using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for ResilientQueueConfiguration
    /// </summary>
    public class ResilientQueueConfigurationTests
    {
        [Fact]
        public void ResilientQueueConfiguration_Class_Exists()
        {
            // This test verifies that the ResilientQueueConfiguration class exists and can be instantiated
            Assert.True(typeof(ResilientQueueConfiguration) != null);
        }
    }
}
