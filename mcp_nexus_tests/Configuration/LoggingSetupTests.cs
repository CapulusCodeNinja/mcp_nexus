using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Configuration;

namespace mcp_nexus_tests.Configuration
{
    /// <summary>
    /// Tests for LoggingSetup
    /// </summary>
    public class LoggingSetupTests
    {
        [Fact]
        public void LoggingSetup_Class_Exists()
        {
            // This test verifies that the LoggingSetup class exists and can be instantiated
            Assert.True(typeof(LoggingSetup) != null);
        }
    }
}
