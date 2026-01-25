using WinAiDbg.Engine.Internal;

using Xunit;

namespace WinAiDbg.Engine.Tests.Internal;

/// <summary>
/// Tests for <see cref="ProcessOutputLine"/>.
/// </summary>
public sealed class ProcessOutputLineTests
{
    /// <summary>
    /// Tests that constructor initializes properties correctly with non-error output.
    /// </summary>
    [Fact]
    public void Constructor_WithValidText_SetsPropertiesCorrectly()
    {
        // Arrange
        const string text = "Sample output line";
        const bool isError = false;

        // Act
        var line = new ProcessOutputLine(text, isError);

        // Assert
        Assert.Equal(text, line.Text);
        Assert.False(line.IsError);
    }

    /// <summary>
    /// Tests that constructor initializes properties correctly with error output.
    /// </summary>
    [Fact]
    public void Constructor_WithErrorFlag_SetsIsErrorToTrue()
    {
        // Arrange
        const string text = "Error message";
        const bool isError = true;

        // Act
        var line = new ProcessOutputLine(text, isError);

        // Assert
        Assert.Equal(text, line.Text);
        Assert.True(line.IsError);
    }

    /// <summary>
    /// Tests that constructor handles null text by converting to empty string.
    /// </summary>
    [Fact]
    public void Constructor_WithNullText_SetsTextToEmptyString()
    {
        // Arrange
        string? text = null;
        const bool isError = false;

        // Act
        var line = new ProcessOutputLine(text!, isError);

        // Assert
        Assert.Equal(string.Empty, line.Text);
        Assert.False(line.IsError);
    }

    /// <summary>
    /// Tests that constructor handles empty text correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyText_SetsTextToEmptyString()
    {
        // Arrange
        const string text = "";
        const bool isError = false;

        // Act
        var line = new ProcessOutputLine(text, isError);

        // Assert
        Assert.Equal(string.Empty, line.Text);
        Assert.False(line.IsError);
    }

    /// <summary>
    /// Tests that properties are immutable (get-only).
    /// </summary>
    [Fact]
    public void Properties_AreGetOnly_CannotBeModified()
    {
        // Arrange
        const string text = "Original text";
        const bool isError = true;
        _ = new ProcessOutputLine(text, isError);

        // Assert - verify properties don't have setters via reflection
        var textProperty = typeof(ProcessOutputLine).GetProperty(nameof(ProcessOutputLine.Text));
        var isErrorProperty = typeof(ProcessOutputLine).GetProperty(nameof(ProcessOutputLine.IsError));

        Assert.NotNull(textProperty);
        Assert.NotNull(isErrorProperty);
        Assert.Null(textProperty.SetMethod);
        Assert.Null(isErrorProperty.SetMethod);
    }

    /// <summary>
    /// Tests that multiple instances can be created with different values.
    /// </summary>
    [Fact]
    public void MultipleInstances_WithDifferentValues_AreIndependent()
    {
        // Arrange & Act
        var line1 = new ProcessOutputLine("First line", false);
        var line2 = new ProcessOutputLine("Second line", true);
        var line3 = new ProcessOutputLine("Third line", false);

        // Assert
        Assert.Equal("First line", line1.Text);
        Assert.False(line1.IsError);

        Assert.Equal("Second line", line2.Text);
        Assert.True(line2.IsError);

        Assert.Equal("Third line", line3.Text);
        Assert.False(line3.IsError);
    }

    /// <summary>
    /// Tests that constructor handles whitespace-only text correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithWhitespaceText_PreservesWhitespace()
    {
        // Arrange
        const string text = "   ";
        const bool isError = false;

        // Act
        var line = new ProcessOutputLine(text, isError);

        // Assert
        Assert.Equal(text, line.Text);
        Assert.False(line.IsError);
    }

    /// <summary>
    /// Tests that constructor handles special characters correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithSpecialCharacters_PreservesText()
    {
        // Arrange
        const string text = "Line with\ttabs\nand\rnewlines";
        const bool isError = true;

        // Act
        var line = new ProcessOutputLine(text, isError);

        // Assert
        Assert.Equal(text, line.Text);
        Assert.True(line.IsError);
    }

    /// <summary>
    /// Tests that constructor handles very long text correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithLongText_HandlesCorrectly()
    {
        // Arrange
        var text = new string('x', 10000);
        const bool isError = false;

        // Act
        var line = new ProcessOutputLine(text, isError);

        // Assert
        Assert.Equal(text, line.Text);
        Assert.Equal(10000, line.Text.Length);
        Assert.False(line.IsError);
    }
}

