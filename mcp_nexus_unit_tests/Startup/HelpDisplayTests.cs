using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for HelpDisplay class.
    /// </summary>
    public class HelpDisplayTests
    {
        [Fact]
        public async Task ShowHelpAsync_ExecutesWithoutException()
        {
            // Act & Assert - Should complete without throwing
            await HelpDisplay.ShowHelpAsync();
        }
    }
}

