using FluentAssertions;
using Xunit;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Unit tests for the CdbSentinels class.
/// </summary>
public class CdbSentinelsTests
{
    [Fact]
    public void StartMarker_ShouldHaveCorrectValue()
    {
        // Act & Assert
        mcp_nexus.Engine.Internal.CdbSentinels.StartMarker.Should().Be("MCP_NEXUS_SENTINEL_COMMAND_START");
    }

    [Fact]
    public void EndMarker_ShouldHaveCorrectValue()
    {
        // Act & Assert
        mcp_nexus.Engine.Internal.CdbSentinels.EndMarker.Should().Be("MCP_NEXUS_SENTINEL_COMMAND_END");
    }

    [Fact]
    public void CommandSeparator_ShouldHaveCorrectValue()
    {
        // Act & Assert
        mcp_nexus.Engine.Internal.CdbSentinels.CommandSeparator.Should().Be("MCP_NEXUS_COMMAND_SEPERATOR");
    }

    [Fact]
    public void AllSentinels_ShouldBeUnique()
    {
        // Act
        var sentinels = new[]
        {
            mcp_nexus.Engine.Internal.CdbSentinels.StartMarker,
            mcp_nexus.Engine.Internal.CdbSentinels.EndMarker,
            mcp_nexus.Engine.Internal.CdbSentinels.CommandSeparator
        };

        // Assert
        sentinels.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllSentinels_ShouldNotBeEmpty()
    {
        // Act & Assert
        mcp_nexus.Engine.Internal.CdbSentinels.StartMarker.Should().NotBeNullOrEmpty();
        mcp_nexus.Engine.Internal.CdbSentinels.EndMarker.Should().NotBeNullOrEmpty();
        mcp_nexus.Engine.Internal.CdbSentinels.CommandSeparator.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AllSentinels_ShouldContainMCPNexusPrefix()
    {
        // Act & Assert
        mcp_nexus.Engine.Internal.CdbSentinels.StartMarker.Should().StartWith("MCP_NEXUS_");
        mcp_nexus.Engine.Internal.CdbSentinels.EndMarker.Should().StartWith("MCP_NEXUS_");
        mcp_nexus.Engine.Internal.CdbSentinels.CommandSeparator.Should().StartWith("MCP_NEXUS_");
    }

    [Fact]
    public void StartMarker_ShouldContainStartKeyword()
    {
        // Act & Assert
        mcp_nexus.Engine.Internal.CdbSentinels.StartMarker.Should().Contain("START");
    }

    [Fact]
    public void EndMarker_ShouldContainEndKeyword()
    {
        // Act & Assert
        mcp_nexus.Engine.Internal.CdbSentinels.EndMarker.Should().Contain("END");
    }

    [Fact]
    public void CommandSeparator_ShouldContainSeparatorKeyword()
    {
        // Act & Assert
        mcp_nexus.Engine.Internal.CdbSentinels.CommandSeparator.Should().Contain("SEPERATOR");
    }
}
