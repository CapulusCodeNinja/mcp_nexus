using FluentAssertions;

using Moq;

using Nexus.External.Apis.FileSystem;
using Nexus.Setup.Utilities;

using Xunit;

namespace Nexus.Setup.Unittests.Utilities;

/// <summary>
/// Unit tests for the <see cref="DirectoryCopyUtility"/> class.
/// </summary>
public class DirectoryCopyUtilityTests
{
    private readonly Mock<IFileSystem> m_FileSystemMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryCopyUtilityTests"/> class.
    /// </summary>
    public DirectoryCopyUtilityTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that parameterless constructor creates utility successfully.
    /// </summary>
    [Fact]
    public void Constructor_Parameterless_Succeeds()
    {
        // Act
        var utility = new DirectoryCopyUtility();

        // Assert
        _ = utility.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that constructor with parameter creates utility successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithParameter_Succeeds()
    {
        // Act
        var utility = new DirectoryCopyUtility(m_FileSystemMock.Object);

        // Assert
        _ = utility.Should().NotBeNull();
    }

    #endregion

    #region CopyDirectoryAsync Tests

    /// <summary>
    /// Verifies that CopyDirectoryAsync prevents infinite loops when destination is inside source.
    /// </summary>
    [Fact]
    public async Task CopyDirectoryAsync_WhenDestinationInsideSource_SkipsCopy()
    {
        // Arrange
        var utility = new DirectoryCopyUtility(m_FileSystemMock.Object);
        var sourceDir = @"C:\Source";
        var destDir = @"C:\Source\SubDir";

        // Act
        await utility.CopyDirectoryAsync(sourceDir, destDir);

        // Assert - Should not create directory or copy any files
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
        m_FileSystemMock.Verify(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }


    /// <summary>
    /// Verifies that CopyDirectoryAsync handles paths with trailing slashes correctly.
    /// </summary>
    [Fact]
    public async Task CopyDirectoryAsync_WithTrailingSlashes_PreventInfiniteLoop()
    {
        // Arrange
        var utility = new DirectoryCopyUtility(m_FileSystemMock.Object);
        var sourceDir = @"C:\Source\";
        var destDir = @"C:\Source\SubDir\";

        // Act
        await utility.CopyDirectoryAsync(sourceDir, destDir);

        // Assert - Should not create directory or copy any files
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
        m_FileSystemMock.Verify(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// Verifies that CopyDirectoryAsync handles paths with alternate directory separators.
    /// </summary>
    [Fact]
    public async Task CopyDirectoryAsync_WithAltDirectorySeparator_PreventInfiniteLoop()
    {
        // Arrange
        var utility = new DirectoryCopyUtility(m_FileSystemMock.Object);
        var sourceDir = @"C:/Source";
        var destDir = @"C:/Source/SubDir";

        // Act
        await utility.CopyDirectoryAsync(sourceDir, destDir);

        // Assert - Should not create directory or copy any files
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
        m_FileSystemMock.Verify(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    #endregion
}

