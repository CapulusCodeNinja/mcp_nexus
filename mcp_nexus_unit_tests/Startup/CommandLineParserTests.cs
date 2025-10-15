using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for CommandLineParser class.
    /// </summary>
    public class CommandLineParserTests
    {
        [Fact]
        public void Parse_WithEmptyArgs_ReturnsDefaultValues()
        {
            var result = CommandLineParser.Parse(Array.Empty<string>());
            Assert.NotNull(result);
            Assert.False(result.UseHttp);
            Assert.False(result.ServiceMode);
            Assert.False(result.Install);
        }

        [Fact]
        public void Parse_WithHttpFlag_SetsUseHttpTrue()
        {
            var result = CommandLineParser.Parse(new[] { "--http" });
            Assert.True(result.UseHttp);
        }

        [Fact]
        public void Parse_WithServiceFlag_SetsServiceModeTrue()
        {
            var result = CommandLineParser.Parse(new[] { "--service" });
            Assert.True(result.ServiceMode);
        }

        [Fact]
        public void Parse_WithPortOption_SetsPort()
        {
            var result = CommandLineParser.Parse(new[] { "--port", "8080" });
            Assert.Equal(8080, result.Port);
            Assert.True(result.PortFromCommandLine);
        }

        [Fact]
        public void IsHelpRequest_WithHelpFlag_ReturnsTrue()
        {
            Assert.True(CommandLineParser.IsHelpRequest(new[] { "--help" }));
            Assert.True(CommandLineParser.IsHelpRequest(new[] { "-h" }));
            Assert.True(CommandLineParser.IsHelpRequest(new[] { "help" }));
        }

        [Fact]
        public void IsHelpRequest_WithEmptyArgs_ReturnsFalse()
        {
            Assert.False(CommandLineParser.IsHelpRequest(Array.Empty<string>()));
        }
    }
}

