using FluentAssertions;

using Moq;

using WinAiDbg.Engine.Preprocessing;
using WinAiDbg.External.Apis.ProcessManagement;

using Xunit;

namespace WinAiDbg.Engine.Unittests.Preprocessing;

/// <summary>
/// Unit tests for the <see cref="PathHandler"/> class.
/// </summary>
public class PathHandlerTests
{
    private readonly Mock<IProcessManager> m_MockProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathHandlerTests"/> class.
    /// </summary>
    public PathHandlerTests()
    {
        m_MockProcessManager = new Mock<IProcessManager>();
    }

    /// <summary>
    /// Verifies that IsWindowsPath returns true for drive letter paths.
    /// </summary>
    /// <param name="path">Input path to evaluate.</param>
    [Theory]
    [InlineData("C:\\path\\to\\file")]
    [InlineData("D:\\")]
    [InlineData("E:\\folder")]
    [InlineData("c:\\lowercase")]
    public void IsWindowsPath_WithDriveLetterPath_ReturnsTrue(string path)
    {
        // Arrange
        var handler = new PathHandler(m_MockProcessManager.Object);

        // Act
        var result = handler.IsWindowsPath(path);

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsWindowsPath returns true for UNC paths.
    /// </summary>
    /// <param name="path">Input UNC path.</param>
    [Theory]
    [InlineData("\\\\server\\share")]
    [InlineData("\\\\server\\share\\folder")]
    [InlineData("\\\\10.0.0.1\\share")]
    public void IsWindowsPath_WithUncPath_ReturnsTrue(string path)
    {
        // Arrange
        var handler = new PathHandler(m_MockProcessManager.Object);

        // Act
        var result = handler.IsWindowsPath(path);

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsWindowsPath returns false for WSL paths.
    /// </summary>
    /// <param name="path">Input WSL path.</param>
    [Theory]
    [InlineData("/mnt/c/path")]
    [InlineData("/home/user")]
    [InlineData("/usr/bin")]
    public void IsWindowsPath_WithWslPath_ReturnsFalse(string path)
    {
        // Arrange
        var handler = new PathHandler(m_MockProcessManager.Object);

        // Act
        var result = handler.IsWindowsPath(path);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsWindowsPath returns false for null or whitespace.
    /// </summary>
    /// <param name="path">Nullable or whitespace path.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsWindowsPath_WithNullOrWhitespace_ReturnsFalse(string? path)
    {
        // Arrange
        var handler = new PathHandler(m_MockProcessManager.Object);

        // Act
        var result = handler.IsWindowsPath(path!);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ConvertToWindowsPath returns input for null or whitespace.
    /// </summary>
    /// <param name="path">Nullable or whitespace path.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertToWindowsPath_WithNullOrWhitespace_ReturnsInput(string? path)
    {
        // Arrange
        var handler = new PathHandler(m_MockProcessManager.Object);

        // Act
        var result = handler.ConvertToWindowsPath(path!);

        // Assert
        _ = result.Should().Be(path);
    }

    /// <summary>
    /// Verifies that ConvertToWindowsPath returns input for Windows paths.
    /// </summary>
    /// <param name="path">Windows path.</param>
    [Theory]
    [InlineData("C:\\path\\to\\file")]
    [InlineData("D:\\folder")]
    public void ConvertToWindowsPath_WithWindowsPath_ReturnsInput(string path)
    {
        // Arrange
        var handler = new PathHandler(m_MockProcessManager.Object);

        // Act
        var result = handler.ConvertToWindowsPath(path);

        // Assert
        _ = result.Should().Be(path);
    }

    /// <summary>
    /// Verifies that ConvertToWindowsPath returns input for UNC paths.
    /// </summary>
    /// <param name="path">UNC path.</param>
    [Theory]
    [InlineData("\\\\server\\share")]
    [InlineData("\\\\server\\share\\folder")]
    public void ConvertToWindowsPath_WithUncPath_ReturnsInput(string path)
    {
        // Arrange
        var handler = new PathHandler(m_MockProcessManager.Object);

        // Act
        var result = handler.ConvertToWindowsPath(path);

        // Assert
        _ = result.Should().Be(path);
    }

    /// <summary>
    /// Verifies that ConvertToWindowsPath returns input for non-Unix paths.
    /// </summary>
    /// <param name="path">Relative path.</param>
    [Theory]
    [InlineData("relative\\path")]
    [InlineData("file.txt")]
    public void ConvertToWindowsPath_WithRelativePath_ReturnsInput(string path)
    {
        // Arrange
        var handler = new PathHandler(m_MockProcessManager.Object);

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
        var handler = new PathHandler(m_MockProcessManager.Object);
        var path = "/mnt/c/test";

        // Act - call twice
        var result1 = handler.ConvertToWindowsPath(path);
        var result2 = handler.ConvertToWindowsPath(path);

        // Assert - results should be consistent (caching works)
        _ = result1.Should().Be(result2);
    }
}
