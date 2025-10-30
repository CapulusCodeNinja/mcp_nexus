using FluentAssertions;

using Moq;

using Nexus.Config;
using Nexus.Engine.Extensions.Core;
using Nexus.External.Apis.FileSystem;

using Xunit;

namespace Nexus.Engine.Extensions.Tests.Core;

/// <summary>
/// Unit tests for the <see cref="Manager"/> class.
/// </summary>
public class ManagerTests : IDisposable
{
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private Manager? m_Manager;
    private readonly Mock<ISettings> m_Settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagerTests"/> class.
    /// </summary>
    public ManagerTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
        m_Settings = new Mock<ISettings>();

        // Setup default file system mock behavior
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Returns(Array.Empty<string>());
    }

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        m_Manager?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new Manager(null!));
    }

    /// <summary>
    /// Verifies that constructor creates extensions directory if it doesn't exist.
    /// </summary>
    [Fact]
    public void Constructor_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

        // Act
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Assert
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that GetExtension returns null for null extension name.
    /// </summary>
    [Fact]
    public void GetExtension_WithNullName_ReturnsNull()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.GetExtension(null!);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetExtension returns null for empty extension name.
    /// </summary>
    [Fact]
    public void GetExtension_WithEmptyName_ReturnsNull()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.GetExtension(string.Empty);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetExtension returns null for whitespace extension name.
    /// </summary>
    [Fact]
    public void GetExtension_WithWhitespaceName_ReturnsNull()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.GetExtension("   ");

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetExtension returns null for unknown extension.
    /// </summary>
    [Fact]
    public void GetExtension_WithUnknownName_ReturnsNull()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.GetExtension("NonExistentExtension");

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetAllExtensions returns empty collection when no extensions loaded.
    /// </summary>
    [Fact]
    public void GetAllExtensions_WhenNoExtensions_ReturnsEmptyCollection()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.GetAllExtensions();

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ExtensionExists returns false for null extension name.
    /// </summary>
    [Fact]
    public void ExtensionExists_WithNullName_ReturnsFalse()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.ExtensionExists(null!);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ExtensionExists returns false for empty extension name.
    /// </summary>
    [Fact]
    public void ExtensionExists_WithEmptyName_ReturnsFalse()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.ExtensionExists(string.Empty);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ExtensionExists returns false for whitespace extension name.
    /// </summary>
    [Fact]
    public void ExtensionExists_WithWhitespaceName_ReturnsFalse()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.ExtensionExists("   ");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ExtensionExists returns false for unknown extension.
    /// </summary>
    [Fact]
    public void ExtensionExists_WithUnknownName_ReturnsFalse()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var result = m_Manager.ExtensionExists("NonExistentExtension");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateExtension returns invalid for unknown extension.
    /// </summary>
    [Fact]
    public void ValidateExtension_WithUnknownExtension_ReturnsInvalid()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var (isValid, errorMessage) = m_Manager.ValidateExtension("NonExistentExtension");

        // Assert
        _ = isValid.Should().BeFalse();
        _ = errorMessage.Should().Contain("not found");
    }

    /// <summary>
    /// Verifies that ValidateExtension returns invalid for null extension name.
    /// </summary>
    [Fact]
    public void ValidateExtension_WithNullName_ReturnsInvalid()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var (isValid, errorMessage) = m_Manager.ValidateExtension(null!);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = errorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that GetExtensionsVersion returns non-negative value.
    /// </summary>
    [Fact]
    public void GetExtensionsVersion_ReturnsNonNegativeValue()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act
        var version = m_Manager.GetExtensionsVersion();

        // Assert
        _ = version.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Verifies that Dispose can be called multiple times without throwing.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Act & Assert
        m_Manager.Dispose();
        m_Manager.Dispose(); // Should not throw
    }

    /// <summary>
    /// Verifies that operations after Dispose do not throw.
    /// </summary>
    [Fact]
    public void Operations_AfterDispose_DoNotThrow()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);
        m_Manager.Dispose();

        // Act & Assert - should not throw
        _ = m_Manager.GetAllExtensions();
        _ = m_Manager.GetExtension("test");
        _ = m_Manager.ExtensionExists("test");
        _ = m_Manager.GetExtensionsVersion();
    }
}
