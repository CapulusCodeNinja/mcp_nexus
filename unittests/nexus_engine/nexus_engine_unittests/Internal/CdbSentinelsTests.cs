using FluentAssertions;

using Xunit;

namespace Nexus.Engine.Unittests.Internal;

/// <summary>
/// Unit tests for the CdbSentinels class.
/// </summary>
public class CdbSentinelsTests
{
    /// <summary>
    /// Verifies that the StartMarker sentinel has the correct expected value.
    /// </summary>
    [Fact]
    public void StartMarker_ShouldHaveCorrectValue()
    {
        // Act & Assert
        _ = Nexus.Engine.Internal.CdbSentinels.StartMarker.Should().Be("MCP_NEXUS_SENTINEL_COMMAND_START");
    }

    /// <summary>
    /// Verifies that the EndMarker sentinel has the correct expected value.
    /// </summary>
    [Fact]
    public void EndMarker_ShouldHaveCorrectValue()
    {
        // Act & Assert
        _ = Nexus.Engine.Internal.CdbSentinels.EndMarker.Should().Be("MCP_NEXUS_SENTINEL_COMMAND_END");
    }

    /// <summary>
    /// Verifies that the CommandSeparator sentinel has the correct expected value.
    /// </summary>
    [Fact]
    public void CommandSeparator_ShouldHaveCorrectValue()
    {
        // Act & Assert
        _ = Nexus.Engine.Internal.CdbSentinels.CommandSeparator.Should().Be("MCP_NEXUS_COMMAND_SEPERATOR");
    }

    /// <summary>
    /// Verifies that all sentinel values are unique and do not overlap.
    /// </summary>
    [Fact]
    public void AllSentinels_ShouldBeUnique()
    {
        // Act
        var sentinels = new[]
        {
            Nexus.Engine.Internal.CdbSentinels.StartMarker,
            Nexus.Engine.Internal.CdbSentinels.EndMarker,
            Nexus.Engine.Internal.CdbSentinels.CommandSeparator,
        };

        // Assert
        _ = sentinels.Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Verifies that all sentinel values are not null or empty strings.
    /// </summary>
    [Fact]
    public void AllSentinels_ShouldNotBeEmpty()
    {
        // Act & Assert
        _ = Nexus.Engine.Internal.CdbSentinels.StartMarker.Should().NotBeNullOrEmpty();
        _ = Nexus.Engine.Internal.CdbSentinels.EndMarker.Should().NotBeNullOrEmpty();
        _ = Nexus.Engine.Internal.CdbSentinels.CommandSeparator.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that all sentinel values start with the MCP_NEXUS_ prefix.
    /// </summary>
    [Fact]
    public void AllSentinels_ShouldContainMCPNexusPrefix()
    {
        // Act & Assert
        _ = Nexus.Engine.Internal.CdbSentinels.StartMarker.Should().StartWith("MCP_NEXUS_");
        _ = Nexus.Engine.Internal.CdbSentinels.EndMarker.Should().StartWith("MCP_NEXUS_");
        _ = Nexus.Engine.Internal.CdbSentinels.CommandSeparator.Should().StartWith("MCP_NEXUS_");
    }

    /// <summary>
    /// Verifies that the StartMarker contains the START keyword.
    /// </summary>
    [Fact]
    public void StartMarker_ShouldContainStartKeyword()
    {
        // Act & Assert
        _ = Nexus.Engine.Internal.CdbSentinels.StartMarker.Should().Contain("START");
    }

    /// <summary>
    /// Verifies that the EndMarker contains the END keyword.
    /// </summary>
    [Fact]
    public void EndMarker_ShouldContainEndKeyword()
    {
        // Act & Assert
        _ = Nexus.Engine.Internal.CdbSentinels.EndMarker.Should().Contain("END");
    }

    /// <summary>
    /// Verifies that the CommandSeparator contains the SEPERATOR keyword.
    /// </summary>
    [Fact]
    public void CommandSeparator_ShouldContainSeparatorKeyword()
    {
        // Act & Assert
        _ = Nexus.Engine.Internal.CdbSentinels.CommandSeparator.Should().Contain("SEPERATOR");
    }
}
