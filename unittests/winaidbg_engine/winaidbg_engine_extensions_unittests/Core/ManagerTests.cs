using FluentAssertions;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.Engine.Extensions.Core;
using WinAiDbg.External.Apis.FileSystem;

using Xunit;

namespace WinAiDbg.Engine.Extensions.Unittests.Core;

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
        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Extensions = new ExtensionsSettings
                {
                    ExtensionsPath = "extensions",
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);

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
    /// Verifies that constructor throws NullReferenceException when settings is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullSettings_ThrowsNullReferenceException()
    {
        // Act & Assert
        _ = Assert.Throws<NullReferenceException>(() => new Manager(m_FileSystemMock.Object, null!));
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

    /// <summary>
    /// Verifies that constructor handles directory creation failure gracefully.
    /// </summary>
    [Fact]
    public void Constructor_WhenDirectoryCreationFails_HandlesGracefully()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        _ = m_FileSystemMock.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        _ = m_FileSystemMock.SetupSequence(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false) // First check - doesn't exist
            .Returns(false); // After creation - still doesn't exist (creation failed)

        // Act - should not throw
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Assert
        _ = m_Manager.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ValidateExtension returns invalid when script file does not exist.
    /// </summary>
    [Fact]
    public void ValidateExtension_WhenScriptFileDoesNotExist_ReturnsInvalid()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);
        var extensionName = "TestExtension";
        var metadata = new WinAiDbg.Engine.Extensions.Models.ExtensionMetadata
        {
            Name = extensionName,
            ScriptFile = "script.ps1",
            ScriptType = "powershell",
            FullScriptPath = "C:\\extensions\\TestExtension\\script.ps1",
        };

        // Manually add extension to manager's internal dictionary via reflection
        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (extensionsField?.GetValue(m_Manager) is System.Collections.Generic.Dictionary<string, WinAiDbg.Engine.Extensions.Models.ExtensionMetadata> extensions)
        {
            extensions[extensionName] = metadata;
        }

        // Setup file system to return false for file exists check
        _ = m_FileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        var (isValid, errorMessage) = m_Manager.ValidateExtension(extensionName);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = errorMessage.Should().Contain("not found");
    }

    /// <summary>
    /// Verifies that ValidateExtension returns invalid when script file is null or empty.
    /// </summary>
    [Fact]
    public void ValidateExtension_WhenScriptFileIsEmpty_ReturnsInvalid()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);
        var extensionName = "TestExtension";
        var metadata = new WinAiDbg.Engine.Extensions.Models.ExtensionMetadata
        {
            Name = extensionName,
            ScriptFile = string.Empty,
            ScriptType = "powershell",
        };

        // Manually add extension to manager's internal dictionary via reflection
        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (extensionsField?.GetValue(m_Manager) is System.Collections.Generic.Dictionary<string, WinAiDbg.Engine.Extensions.Models.ExtensionMetadata> extensions)
        {
            extensions[extensionName] = metadata;
        }

        // Act
        var (isValid, errorMessage) = m_Manager.ValidateExtension(extensionName);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = errorMessage.Should().Contain("no script file");
    }

    /// <summary>
    /// Verifies that ValidateExtension returns invalid when script type is unsupported.
    /// </summary>
    [Fact]
    public void ValidateExtension_WhenScriptTypeIsUnsupported_ReturnsInvalid()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);
        var extensionName = "TestExtension";
        var metadata = new WinAiDbg.Engine.Extensions.Models.ExtensionMetadata
        {
            Name = extensionName,
            ScriptFile = "script.sh",
            ScriptType = "bash",
            FullScriptPath = "C:\\extensions\\TestExtension\\script.sh",
        };

        // Manually add extension to manager's internal dictionary via reflection
        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (extensionsField?.GetValue(m_Manager) is System.Collections.Generic.Dictionary<string, WinAiDbg.Engine.Extensions.Models.ExtensionMetadata> extensions)
        {
            extensions[extensionName] = metadata;
        }

        // Setup file system to return true for file exists check
        _ = m_FileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);

        // Act
        var (isValid, errorMessage) = m_Manager.ValidateExtension(extensionName);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = errorMessage.Should().Contain("unsupported script type");
    }

    /// <summary>
    /// Verifies that ValidateExtension returns invalid when script type is null or empty.
    /// </summary>
    [Fact]
    public void ValidateExtension_WhenScriptTypeIsEmpty_ReturnsInvalid()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);
        var extensionName = "TestExtension";
        var metadata = new WinAiDbg.Engine.Extensions.Models.ExtensionMetadata
        {
            Name = extensionName,
            ScriptFile = "script.ps1",
            ScriptType = string.Empty,
            FullScriptPath = "C:\\extensions\\TestExtension\\script.ps1",
        };

        // Manually add extension to manager's internal dictionary via reflection
        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (extensionsField?.GetValue(m_Manager) is System.Collections.Generic.Dictionary<string, WinAiDbg.Engine.Extensions.Models.ExtensionMetadata> extensions)
        {
            extensions[extensionName] = metadata;
        }

        // Setup file system to return true for file exists check
        _ = m_FileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);

        // Act
        var (isValid, errorMessage) = m_Manager.ValidateExtension(extensionName);

        // Assert
        _ = isValid.Should().BeFalse();
        _ = errorMessage.Should().Contain("no script type");
    }

    /// <summary>
    /// Verifies that constructor handles absolute path configuration correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithAbsolutePathConfiguration_UsesPathAsIs()
    {
        // Arrange
        var absolutePath = "C:\\Absolute\\Extensions\\Path";
        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Extensions = new ExtensionsSettings
                {
                    ExtensionsPath = absolutePath,
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.Is<string>(p => p == absolutePath))).Returns(true);

        // Act
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Assert
        _ = m_Manager.Should().NotBeNull();
        m_FileSystemMock.Verify(fs => fs.DirectoryExists(It.Is<string>(p => p == absolutePath)), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that LoadExtensionsAsync handles directory that doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadExtensionsAsync_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

        // Act
        await m_Manager.LoadExtensionsAsync();

        // Assert
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that LoadExtensionsAsync propagates file system errors from GetFiles.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadExtensionsAsync_WhenGetFilesThrowsException_PropagatesException()
    {
        // Arrange
        m_Manager = new Manager(m_FileSystemMock.Object, m_Settings.Object);

        // Reset mock after constructor setup
        m_FileSystemMock.Reset();
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act & Assert - exception should propagate since GetFiles is not wrapped in try-catch
        _ = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await m_Manager.LoadExtensionsAsync());
    }
}
