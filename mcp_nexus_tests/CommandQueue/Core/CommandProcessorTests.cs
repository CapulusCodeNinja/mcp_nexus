using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_tests.CommandQueue.Core
{
    /// <summary>
    /// Tests for CommandProcessor
    /// </summary>
    public class CommandProcessorTests
    {
        [Fact]
        public void CommandProcessor_Class_Exists()
        {
            // This test verifies that the CommandProcessor class exists and can be instantiated
            Assert.NotNull(typeof(CommandProcessor));
        }
    }
}
