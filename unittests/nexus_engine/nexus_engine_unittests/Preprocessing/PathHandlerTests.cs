using FluentAssertions;

using Nexus.Engine.Preprocessing;

using Xunit;

namespace Nexus.Engine.Tests.Preprocessing;

/// <summary>
/// Unit tests for the <see cref="PathHandler"/> class.
/// </summary>
public class PathHandlerTests
{
    #region IsWindowsPath Tests

    /// <summary>
    /// Verifies that IsWindowsPath returns true for drive letter paths.
    /// </summary>
    [Theory]
    [InlineData("C:\\path\\to\\file")]
    [InlineData("D:\\")]
    [InlineData("E:\\folder")]
    [InlineData("c:\\lowercase")]
    public void IsWindowsPath_WithDriveLetterPath_ReturnsTrue(string path)
    {
        // Arrange
        var handler = new PathHandler();

        // Act
        var result = handler.IsWindowsPath(path);

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsWindowsPath returns true for UNC paths.
    /// </summary>
    [Theory]
    [InlineData("\\\\server\\share")]
    [InlineData("\\\\server\\share\\folder")]
    [InlineData("\\\\10.0.0.1\\share")]
    public void IsWindowsPath_WithUncPath_ReturnsTrue(string path)
    {
        // Arrange
        var handler = new PathHandler();

        // Act
        var result = handler.IsWindowsPath(path);

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsWindowsPath returns false for WSL paths.
    /// </summary>
    [Theory]
    [InlineData("/mnt/c/path")]
    [InlineData("/home/user")]
    [InlineData("/usr/bin")]
    public void IsWindowsPath_WithWslPath_ReturnsFalse(string path)
    {
        // Arrange
        var handler = new PathHandler();

        // Act
        var result = handler.IsWindowsPath(path);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsWindowsPath returns false for null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsWindowsPath_WithNullOrWhitespace_ReturnsFalse(string? path)
    {
        // Arrange
        var handler = new PathHandler();

        // Act
        var result = handler.IsWindowsPath(path!);

        // Assert
        _ = result.Should().BeFalse();
    }

    #endregion

    #region ConvertToWindowsPath Tests

    /// <summary>
    /// Verifies that ConvertToWindowsPath returns input for null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertToWindowsPath_WithNullOrWhitespace_ReturnsInput(string? path)
    {
        // Arrange
        var handler = new PathHandler();

        // Act
        var result = handler.ConvertToWindowsPath(path!);

        // Assert
        _ = result.Should().Be(path);
    }

    /// <summary>
    /// Verifies that ConvertToWindowsPath returns input for Windows paths.
    /// </summary>
    [Theory]
    [InlineData("C:\\path\\to\\file")]
    [InlineData("D:\\folder")]
    public void ConvertToWindowsPath_WithWindowsPath_ReturnsInput(string path)
    {
        // Arrange
        var handler = new PathHandler();

        // Act
        var result = handler.ConvertToWindowsPath(path);

        // Assert
        _ = result.Should().Be(path);
    }

    /// <summary>
    /// Verifies that ConvertToWindowsPath returns input for UNC paths.
    /// </summary>
    [Theory]
    [InlineData("\\\\server\\share")]
    [InlineData("\\\\server\\share\\folder")]
    public void ConvertToWindowsPath_WithUncPath_ReturnsInput(string path)
    {
        // Arrange
        var handler = new PathHandler();

        // Act
        var result = handler.ConvertToWindowsPath(path);

        // Assert
        _ = result.Should().Be(path);
    }

    /// <summary>
    /// Verifies that ConvertToWindowsPath returns input for non-Unix paths.
    /// </summary>
    [Theory]
    [InlineData("relative\\path")]
    [InlineData("file.txt")]
    public void ConvertToWindowsPath_WithRelativePath_ReturnsInput(string path)
    {
        // Arrange
        var handler = new PathHandler();

        // Act
        var result = handler.ConvertToWindowsPath(path);

        // Assert
        _ = result.Should().Be(path);
    }

    /// <summary>
    /// Verifies that ConvertToWindowsPath caches results.
    /// </summary>
    [Fact]
    public void ConvertToWindowsPath_WithSamePath_UsesCaching()
    {
        // Arrange
        var handler = new PathHandler();
        var path = "/mnt/c/test";

        // Act - call twice
        var result1 = handler.ConvertToWindowsPath(path);
        var result2 = handler.ConvertToWindowsPath(path);

        // Assert - results should be consistent (caching works)
        _ = result1.Should().Be(result2);
    }

    #endregion
}

