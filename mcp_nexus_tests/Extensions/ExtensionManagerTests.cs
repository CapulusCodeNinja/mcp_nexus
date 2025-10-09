using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Extensions;
using Xunit;

namespace mcp_nexus_tests.Extensions
{
    /// <summary>
    /// Tests for the ExtensionManager class.
    /// </summary>
    public class ExtensionManagerTests : IDisposable
    {
        private readonly Mock<ILogger<ExtensionManager>> m_MockLogger;
        private readonly string m_TestExtensionsPath;
        private readonly string m_TestExtension1Path;
        private readonly string m_TestExtension2Path;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionManagerTests"/> class.
        /// </summary>
        public ExtensionManagerTests()
        {
            m_MockLogger = new Mock<ILogger<ExtensionManager>>();
            
            // Create temporary test directory
            m_TestExtensionsPath = Path.Combine(Path.GetTempPath(), $"mcp_nexus_test_extensions_{Guid.NewGuid()}");
            Directory.CreateDirectory(m_TestExtensionsPath);

            m_TestExtension1Path = Path.Combine(m_TestExtensionsPath, "test_extension1");
            m_TestExtension2Path = Path.Combine(m_TestExtensionsPath, "test_extension2");
            
            Directory.CreateDirectory(m_TestExtension1Path);
            Directory.CreateDirectory(m_TestExtension2Path);
        }

        /// <summary>
        /// Cleans up test resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(m_TestExtensionsPath))
                {
                    Directory.Delete(m_TestExtensionsPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Creates a test metadata file.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="metadata">The metadata object.</param>
        private void CreateMetadataFile(string path, object metadata)
        {
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(path, "metadata.json"), json);
        }

        /// <summary>
        /// Creates a test script file.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="scriptName">The script file name.</param>
        private void CreateScriptFile(string path, string scriptName)
        {
            File.WriteAllText(Path.Combine(path, scriptName), "# Test script");
        }

        [Fact]
        public void Constructor_WithValidPath_Succeeds()
        {
            // Act
            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ExtensionManager(null!, m_TestExtensionsPath));
        }

        [Fact]
        public void Constructor_WithNullPath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new ExtensionManager(m_MockLogger.Object, null!));
        }

        [Fact]
        public void Constructor_WithEmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new ExtensionManager(m_MockLogger.Object, string.Empty));
        }

        [Fact]
        public void LoadExtensions_WithValidExtension_LoadsSuccessfully()
        {
            // Arrange
            var metadata = new
            {
                name = "test_extension1",
                description = "Test extension",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test.ps1",
                timeout = 300000,
                requires = new[] { "McpNexusExtensions" },
                parameters = new object[] { }
            };

            CreateMetadataFile(m_TestExtension1Path, metadata);
            CreateScriptFile(m_TestExtension1Path, "test.ps1");

            // Act
            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);
            var extensions = manager.GetAllExtensions();

            // Assert
            var extensionList = extensions.ToList();
            Assert.Single(extensionList);
            Assert.Equal("test_extension1", extensionList[0].Name);
            Assert.Equal("Test extension", extensionList[0].Description);
            Assert.Equal("1.0.0", extensionList[0].Version);
        }

        [Fact]
        public void LoadExtensions_WithMultipleExtensions_LoadsAll()
        {
            // Arrange
            var metadata1 = new
            {
                name = "test_extension1",
                description = "Test extension 1",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test1.ps1",
                timeout = 300000
            };

            var metadata2 = new
            {
                name = "test_extension2",
                description = "Test extension 2",
                version = "2.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test2.ps1",
                timeout = 300000
            };

            CreateMetadataFile(m_TestExtension1Path, metadata1);
            CreateScriptFile(m_TestExtension1Path, "test1.ps1");
            CreateMetadataFile(m_TestExtension2Path, metadata2);
            CreateScriptFile(m_TestExtension2Path, "test2.ps1");

            // Act
            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);
            var extensions = manager.GetAllExtensions();

            // Assert
            Assert.Equal(2, extensions.Count());
        }

        [Fact]
        public void GetExtension_WithValidName_ReturnsExtension()
        {
            // Arrange
            var metadata = new
            {
                name = "test_extension1",
                description = "Test extension",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test.ps1",
                timeout = 300000
            };

            CreateMetadataFile(m_TestExtension1Path, metadata);
            CreateScriptFile(m_TestExtension1Path, "test.ps1");

            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var extension = manager.GetExtension("test_extension1");

            // Assert
            Assert.NotNull(extension);
            Assert.Equal("test_extension1", extension.Name);
        }

        [Fact]
        public void GetExtension_WithInvalidName_ReturnsNull()
        {
            // Arrange
            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var extension = manager.GetExtension("nonexistent_extension");

            // Assert
            Assert.Null(extension);
        }

        [Fact]
        public void ExtensionExists_WithValidName_ReturnsTrue()
        {
            // Arrange
            var metadata = new
            {
                name = "test_extension1",
                description = "Test extension",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test.ps1",
                timeout = 300000
            };

            CreateMetadataFile(m_TestExtension1Path, metadata);
            CreateScriptFile(m_TestExtension1Path, "test.ps1");

            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var exists = manager.ExtensionExists("test_extension1");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void ExtensionExists_WithInvalidName_ReturnsFalse()
        {
            // Arrange
            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var exists = manager.ExtensionExists("nonexistent_extension");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void ValidateExtension_WithValidExtension_ReturnsTrue()
        {
            // Arrange
            var metadata = new
            {
                name = "test_extension1",
                description = "Test extension",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test.ps1",
                timeout = 300000
            };

            CreateMetadataFile(m_TestExtension1Path, metadata);
            CreateScriptFile(m_TestExtension1Path, "test.ps1");

            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var (isValid, errorMessage) = manager.ValidateExtension("test_extension1");

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidateExtension_WithNonexistentExtension_ReturnsFalse()
        {
            // Arrange
            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var (isValid, errorMessage) = manager.ValidateExtension("nonexistent_extension");

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("not found", errorMessage);
        }

        [Fact]
        public void ValidateExtension_WithMissingScriptFile_ReturnsFalse()
        {
            // Arrange
            var metadata = new
            {
                name = "test_extension1",
                description = "Test extension",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "missing.ps1",
                timeout = 300000
            };

            CreateMetadataFile(m_TestExtension1Path, metadata);
            // Don't create the script file

            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var (isValid, errorMessage) = manager.ValidateExtension("test_extension1");

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("does not exist", errorMessage);
        }

        [Fact]
        public void LoadExtensions_WithInvalidJson_SkipsExtension()
        {
            // Arrange
            File.WriteAllText(Path.Combine(m_TestExtension1Path, "metadata.json"), "invalid json {{{");

            // Act
            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);
            var extensions = manager.GetAllExtensions();

            // Assert
            Assert.Empty(extensions);
        }

        [Fact]
        public void LoadExtensions_WithNonexistentPath_ReturnsEmptyList()
        {
            // Arrange
            var nonexistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");

            // Act
            var manager = new ExtensionManager(m_MockLogger.Object, nonexistentPath);
            var extensions = manager.GetAllExtensions();

            // Assert
            Assert.Empty(extensions);
        }

        [Fact]
        public void GetExtension_CaseInsensitive_ReturnsExtension()
        {
            // Arrange
            var metadata = new
            {
                name = "test_extension1",
                description = "Test extension",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test.ps1",
                timeout = 300000
            };

            CreateMetadataFile(m_TestExtension1Path, metadata);
            CreateScriptFile(m_TestExtension1Path, "test.ps1");

            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var extension = manager.GetExtension("TEST_EXTENSION1");

            // Assert
            Assert.NotNull(extension);
            Assert.Equal("test_extension1", extension.Name);
        }

        [Fact]
        public void LoadExtensions_WithParametersInMetadata_LoadsParameters()
        {
            // Arrange
            var metadata = new
            {
                name = "test_extension1",
                description = "Test extension",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test.ps1",
                timeout = 300000,
                parameters = new[]
                {
                    new
                    {
                        name = "param1",
                        type = "string",
                        description = "Test parameter",
                        required = true,
                        defaultValue = "test"
                    }
                }
            };

            CreateMetadataFile(m_TestExtension1Path, metadata);
            CreateScriptFile(m_TestExtension1Path, "test.ps1");

            // Act
            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);
            var extension = manager.GetExtension("test_extension1");

            // Assert
            Assert.NotNull(extension);
            Assert.NotNull(extension.Parameters);
            Assert.Single(extension.Parameters);
            Assert.Equal("param1", extension.Parameters[0].Name);
        }

        [Fact]
        public void FullScriptPath_ReturnsCorrectAbsolutePath()
        {
            // Arrange
            var metadata = new
            {
                name = "test_extension1",
                description = "Test extension",
                version = "1.0.0",
                author = "Test Author",
                scriptType = "powershell",
                scriptFile = "test.ps1",
                timeout = 300000
            };

            CreateMetadataFile(m_TestExtension1Path, metadata);
            CreateScriptFile(m_TestExtension1Path, "test.ps1");

            var manager = new ExtensionManager(m_MockLogger.Object, m_TestExtensionsPath);

            // Act
            var extension = manager.GetExtension("test_extension1");

            // Assert
            Assert.NotNull(extension);
            Assert.True(Path.IsPathRooted(extension.FullScriptPath));
            Assert.True(File.Exists(extension.FullScriptPath));
        }
    }
}

