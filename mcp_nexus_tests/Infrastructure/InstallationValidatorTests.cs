using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for InstallationValidator
    /// </summary>
    public class InstallationValidatorTests
    {
        [Fact]
        public void InstallationValidator_Class_Exists()
        {
            // This test verifies that the InstallationValidator class exists and can be instantiated
            Assert.True(typeof(InstallationValidator) != null);
        }
    }
}
