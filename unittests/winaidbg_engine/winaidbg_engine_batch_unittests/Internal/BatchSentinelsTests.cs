using WinAiDbg.Engine.Batch.Internal;

using Xunit;

namespace WinAiDbg.Engine.Batch.Tests;

/// <summary>
/// Tests for <see cref="BatchSentinels"/>.
/// </summary>
public class BatchSentinelsTests
{
    /// <summary>
    /// Tests that GetStartMarker returns the correct format for a given command ID.
    /// </summary>
    [Fact]
    public void GetStartMarker_ReturnsCorrectFormat()
    {
        // Arrange
        var commandId = "cmd-123";

        // Act
        var marker = BatchSentinels.GetStartMarker(commandId);

        // Assert
        Assert.Equal("winaidbg_COMMAND_SEPARATOR_cmd-123_START", marker);
    }

    /// <summary>
    /// Tests that GetEndMarker returns the correct format for a given command ID.
    /// </summary>
    [Fact]
    public void GetEndMarker_ReturnsCorrectFormat()
    {
        // Arrange
        var commandId = "cmd-123";

        // Act
        var marker = BatchSentinels.GetEndMarker(commandId);

        // Assert
        Assert.Equal("winaidbg_COMMAND_SEPARATOR_cmd-123_END", marker);
    }

    /// <summary>
    /// Tests that GetStartMarker returns the correct format for a different command ID.
    /// </summary>
    [Fact]
    public void GetStartMarker_WithDifferentId_ReturnsCorrectFormat()
    {
        // Arrange
        var commandId = "test-456";

        // Act
        var marker = BatchSentinels.GetStartMarker(commandId);

        // Assert
        Assert.Equal("winaidbg_COMMAND_SEPARATOR_test-456_START", marker);
    }

    /// <summary>
    /// Tests that GetEndMarker returns the correct format for a different command ID.
    /// </summary>
    [Fact]
    public void GetEndMarker_WithDifferentId_ReturnsCorrectFormat()
    {
        // Arrange
        var commandId = "test-456";

        // Act
        var marker = BatchSentinels.GetEndMarker(commandId);

        // Assert
        Assert.Equal("winaidbg_COMMAND_SEPARATOR_test-456_END", marker);
    }
}
