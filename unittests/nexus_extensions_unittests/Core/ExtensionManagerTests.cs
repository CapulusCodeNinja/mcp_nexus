using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using nexus.extensions.Core;
using nexus.extensions.Models;
using System.Text.Json;

namespace nexus.extensions_unittests.Core;

/// <summary>
/// Unit tests for ExtensionManager.
/// </summary>
public class ExtensionManagerTests
{
    private readonly ILogger<ExtensionManager> m_Logger;
    private readonly string m_TestExtensionsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionManagerTests"/> class.
    /// </summary>
    public ExtensionManagerTests()
    {
        m_Logger = NullLogger<ExtensionManager>.Instance;
        m_TestExtensionsPath = Path.Combine(Path.GetTempPath(), "test_extensions_" + Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ExtensionManager(null!, "path"));
    }

    /// <summary>
    /// Verifies constructor throws when extensions path is null or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsArgumentException_WhenExtensionsPathIsNullOrEmpty(string? path)
    {
        Assert.Throws<ArgumentException>(() => new ExtensionManager(m_Logger, path!));
    }

    /// <summary>
    /// Verifies LoadExtensionsAsync creates directory if it doesn't exist.
    /// </summary>
    [Fact]
    public async Task LoadExtensionsAsync_CreatesDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var manager = new ExtensionManager(m_Logger, m_TestExtensionsPath);

        try
        {
            // Act
            await manager.LoadExtensionsAsync();

            // Assert
            Assert.True(Directory.Exists(m_TestExtensionsPath));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(m_TestExtensionsPath))
            {
                Directory.Delete(m_TestExtensionsPath, true);
            }
        }
    }

    /// <summary>
    /// Verifies GetExtension returns null for non-existent extension.
    /// </summary>
    [Fact]
    public void GetExtension_ReturnsNull_WhenExtensionDoesNotExist()
    {
        // Arrange
        var manager = new ExtensionManager(m_Logger, m_TestExtensionsPath);

        // Act
        var result = manager.GetExtension("non_existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies ExtensionExists returns false for non-existent extension.
    /// </summary>
    [Fact]
    public void ExtensionExists_ReturnsFalse_WhenExtensionDoesNotExist()
    {
        // Arrange
        var manager = new ExtensionManager(m_Logger, m_TestExtensionsPath);

        // Act
        var result = manager.ExtensionExists("non_existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies GetAllExtensions returns empty collection when no extensions loaded.
    /// </summary>
    [Fact]
    public void GetAllExtensions_ReturnsEmptyCollection_WhenNoExtensionsLoaded()
    {
        // Arrange
        var manager = new ExtensionManager(m_Logger, m_TestExtensionsPath);

        // Act
        var result = manager.GetAllExtensions();

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies ValidateExtension returns false for non-existent extension.
    /// </summary>
    [Fact]
    public void ValidateExtension_ReturnsFalse_WhenExtensionDoesNotExist()
    {
        // Arrange
        var manager = new ExtensionManager(m_Logger, m_TestExtensionsPath);

        // Act
        var (isValid, errorMessage) = manager.ValidateExtension("non_existent");

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("not found", errorMessage);
    }

    /// <summary>
    /// Verifies LoadExtensionsAsync loads valid extension.
    /// </summary>
    [Fact]
    public async Task LoadExtensionsAsync_LoadsValidExtension()
    {
        // Arrange
        Directory.CreateDirectory(m_TestExtensionsPath);
        var extensionPath = Path.Combine(m_TestExtensionsPath, "test_extension");
        Directory.CreateDirectory(extensionPath);

        var metadata = new ExtensionMetadata
        {
            Name = "test_extension",
            Description = "Test extension",
            ScriptType = "powershell",
            ScriptFile = "test.ps1",
            Version = "1.0.0"
        };

        var metadataJson = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(Path.Combine(extensionPath, "metadata.json"), metadataJson);
        await File.WriteAllTextAsync(Path.Combine(extensionPath, "test.ps1"), "# Test script");

        var manager = new ExtensionManager(m_Logger, m_TestExtensionsPath);

        try
        {
            // Act
            await manager.LoadExtensionsAsync();

            // Assert
            Assert.True(manager.ExtensionExists("test_extension"));
            var loadedExtension = manager.GetExtension("test_extension");
            Assert.NotNull(loadedExtension);
            Assert.Equal("test_extension", loadedExtension.Name);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(m_TestExtensionsPath))
            {
                TryDeleteDirectory(m_TestExtensionsPath);
            }
        }
    }

    /// <summary>
    /// Verifies GetExtensionsVersion increments after reload.
    /// </summary>
    [Fact]
    public async Task GetExtensionsVersion_Increments_AfterReload()
    {
        // Arrange
        Directory.CreateDirectory(m_TestExtensionsPath);
        var manager = new ExtensionManager(m_Logger, m_TestExtensionsPath);

        try
        {
            // Act
            var version1 = manager.GetExtensionsVersion();
            await manager.LoadExtensionsAsync();
            var version2 = manager.GetExtensionsVersion();
            await manager.LoadExtensionsAsync();
            var version3 = manager.GetExtensionsVersion();

            // Assert
            Assert.True(version2 > version1);
            Assert.True(version3 > version2);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(m_TestExtensionsPath))
            {
                TryDeleteDirectory(m_TestExtensionsPath);
            }
        }
    }

    /// <summary>
    /// Attempts to delete a directory with retry logic to handle file locking issues.
    /// </summary>
    /// <param name="path">The path to the directory to delete.</param>
    private static void TryDeleteDirectory(string path)
    {
        const int maxRetries = 3;
        const int delayMs = 50;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(delayMs);
            }
            catch (UnauthorizedAccessException) when (i < maxRetries - 1)
            {
                Thread.Sleep(delayMs);
            }
        }
    }
}

