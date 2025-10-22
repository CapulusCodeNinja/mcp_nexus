using FluentAssertions;
using Xunit;

namespace nexus.engine.unittests.Internal;

/// <summary>
/// Unit tests for the CdbSentinels class.
/// </summary>
public class CdbSentinelsTests
{
    [Fact]
    public void StartMarker_ShouldHaveCorrectValue()
    {
        // Act & Assert
        nexus.engine.Internal.CdbSentinels.StartMarker.Should().Be("MCP_NEXUS_SENTINEL_COMMAND_START");
    }

    [Fact]
    public void EndMarker_ShouldHaveCorrectValue()
    {
        // Act & Assert
        nexus.engine.Internal.CdbSentinels.EndMarker.Should().Be("MCP_NEXUS_SENTINEL_COMMAND_END");
    }

    [Fact]
    public void CommandSeparator_ShouldHaveCorrectValue()
    {
        // Act & Assert
        nexus.engine.Internal.CdbSentinels.CommandSeparator.Should().Be("MCP_NEXUS_COMMAND_SEPERATOR");
    }

    [Fact]
    public void AllSentinels_ShouldBeUnique()
    {
        // Act
        var sentinels = new[]
        {
            nexus.engine.Internal.CdbSentinels.StartMarker,
            nexus.engine.Internal.CdbSentinels.EndMarker,
            nexus.engine.Internal.CdbSentinels.CommandSeparator
        };

        // Assert
        sentinels.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllSentinels_ShouldNotBeEmpty()
    {
        // Act & Assert
        nexus.engine.Internal.CdbSentinels.StartMarker.Should().NotBeNullOrEmpty();
        nexus.engine.Internal.CdbSentinels.EndMarker.Should().NotBeNullOrEmpty();
        nexus.engine.Internal.CdbSentinels.CommandSeparator.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AllSentinels_ShouldContainMCPNexusPrefix()
    {
        // Act & Assert
        nexus.engine.Internal.CdbSentinels.StartMarker.Should().StartWith("MCP_NEXUS_");
        nexus.engine.Internal.CdbSentinels.EndMarker.Should().StartWith("MCP_NEXUS_");
        nexus.engine.Internal.CdbSentinels.CommandSeparator.Should().StartWith("MCP_NEXUS_");
    }

    [Fact]
    public void StartMarker_ShouldContainStartKeyword()
    {
        // Act & Assert
        nexus.engine.Internal.CdbSentinels.StartMarker.Should().Contain("START");
    }

    [Fact]
    public void EndMarker_ShouldContainEndKeyword()
    {
        // Act & Assert
        nexus.engine.Internal.CdbSentinels.EndMarker.Should().Contain("END");
    }

    [Fact]
    public void CommandSeparator_ShouldContainSeparatorKeyword()
    {
        // Act & Assert
        nexus.engine.Internal.CdbSentinels.CommandSeparator.Should().Contain("SEPERATOR");
    }
}
