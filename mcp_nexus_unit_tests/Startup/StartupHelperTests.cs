using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for StartupHelper class.
    /// </summary>
    public class StartupHelperTests
    {
        [Fact]
        public void SetupConsoleEncoding_ExecutesWithoutException()
        {
            // Act & Assert - Should not throw
            StartupHelper.SetupConsoleEncoding();
        }

        [Fact]
        public void ValidateServiceModeOnWindows_WithServiceModeFalse_ReturnsTrue()
        {
            var result = StartupHelper.ValidateServiceModeOnWindows(false);
            Assert.True(result);
        }

        [Fact]
        public void SetEnvironmentForServiceMode_WithServiceFlag_SetsEnvironment()
        {
            var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            try
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
                StartupHelper.SetEnvironmentForServiceMode(new[] { "--service" });
                var newEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                Assert.Equal("Service", newEnv);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            }
        }
    }
}

