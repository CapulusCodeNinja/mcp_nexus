using FluentAssertions;

using Moq;

using Nexus.Config.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Setup.Validation;

using NLog;

using Xunit;

namespace Nexus.Setup.Unittests.Validation;

/// <summary>
/// Unit tests for InstallationValidator class.
/// </summary>
public class InstallationValidatorTests
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IServiceController> m_MockServiceController;
    private readonly Logger m_Logger;
    private readonly InstallationValidator m_Validator;

    /// <summary>
    /// Initializes a new instance of the InstallationValidatorTests class.
    /// </summary>
    public InstallationValidatorTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockServiceController = new Mock<IServiceController>();
        m_Logger = LogManager.GetCurrentClassLogger();
        m_Validator = new InstallationValidator(m_MockFileSystem.Object, m_MockServiceController.Object);
    }

    /// <summary>
    /// Verifies that constructor with null file system throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InstallationValidator(null!, m_MockServiceController.Object);

        // Assert
        _ = action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystem");
    }

    /// <summary>
    /// Verifies that constructor with null service controller throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServiceController_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InstallationValidator(m_MockFileSystem.Object, null!);

        // Assert
        _ = action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceController");
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns false when service is already installed.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WithExistingService_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        _ = m_MockServiceController.Setup(sc => sc.IsServiceInstalled("TestService"))
            .Returns(true);

        // Act
        var result = m_Validator.ValidateInstallation(config, "C:\\source");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns false when source directory doesn't exist.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WithNonExistentSourceDirectory_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        _ = m_MockServiceController.Setup(sc => sc.IsServiceInstalled(It.IsAny<string>()))
            .Returns(false);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\source"))
            .Returns(false);

        // Act
        var result = m_Validator.ValidateInstallation(config, "C:\\source");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns false when source executable doesn't exist.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WithMissingExecutable_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        _ = m_MockServiceController.Setup(sc => sc.IsServiceInstalled(It.IsAny<string>()))
            .Returns(false);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\source"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.FileExists("C:\\source\\nexus.exe"))
            .Returns(false);

        // Act
        var result = m_Validator.ValidateInstallation(config, "C:\\source");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns false when install parent directory doesn't exist.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WithNonExistentInstallParent_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        _ = m_MockServiceController.Setup(sc => sc.IsServiceInstalled(It.IsAny<string>()))
            .Returns(false);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\source"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.FileExists("C:\\source\\nexus.exe"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\Program Files"))
            .Returns(false);

        // Act
        var result = m_Validator.ValidateInstallation(config, "C:\\source");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns false when backup parent directory doesn't exist.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WithNonExistentBackupParent_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        _ = m_MockServiceController.Setup(sc => sc.IsServiceInstalled(It.IsAny<string>()))
            .Returns(false);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\source"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.FileExists("C:\\source\\nexus.exe"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\Program Files"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\Backups"))
            .Returns(false);

        // Act
        var result = m_Validator.ValidateInstallation(config, "C:\\source");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Creates a test configuration.
    /// </summary>
    /// <returns>A SharedConfiguration instance for testing.</returns>
    private static SharedConfiguration CreateTestConfiguration()
    {
        return new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Service = new ServiceSettings
                {
                    ServiceName = "TestService",
                    DisplayName = "Test Service",
                    InstallPath = "C:\\Program Files\\MCP-Nexus",
                    BackupPath = "C:\\Backups\\MCP-Nexus"
                }
            }
        };
    }
}

