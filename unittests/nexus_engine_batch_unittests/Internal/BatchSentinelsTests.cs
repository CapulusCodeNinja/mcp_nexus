using nexus.engine.batch.Internal;
using Xunit;

namespace nexus.engine.batch.tests;

/// <summary>
/// Tests for <see cref="BatchSentinels"/>.
/// </summary>
public class BatchSentinelsTests
{
    [Fact]
    public void GetStartMarker_ReturnsCorrectFormat()
    {
        // Arrange
        var commandId = "cmd-123";

        // Act
        var marker = BatchSentinels.GetStartMarker(commandId);

        // Assert
        Assert.Equal("MCP_NEXUS_COMMAND_SEPARATOR_cmd-123_START", marker);
    }

    [Fact]
    public void GetEndMarker_ReturnsCorrectFormat()
    {
        // Arrange
        var commandId = "cmd-123";

        // Act
        var marker = BatchSentinels.GetEndMarker(commandId);

        // Assert
        Assert.Equal("MCP_NEXUS_COMMAND_SEPARATOR_cmd-123_END", marker);
    }

    [Fact]
    public void GetStartMarker_WithDifferentId_ReturnsCorrectFormat()
    {
        // Arrange
        var commandId = "test-456";

        // Act
        var marker = BatchSentinels.GetStartMarker(commandId);

        // Assert
        Assert.Equal("MCP_NEXUS_COMMAND_SEPARATOR_test-456_START", marker);
    }

    [Fact]
    public void GetEndMarker_WithDifferentId_ReturnsCorrectFormat()
    {
        // Arrange
        var commandId = "test-456";

        // Act
        var marker = BatchSentinels.GetEndMarker(commandId);

        // Assert
        Assert.Equal("MCP_NEXUS_COMMAND_SEPARATOR_test-456_END", marker);
    }
}

