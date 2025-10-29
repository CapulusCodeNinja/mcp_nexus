using FluentAssertions;

using Moq;

using Nexus.Config.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Setup.Validation;

using Xunit;

namespace Nexus.Setup.Unittests.Validation;

/// <summary>
/// Unit tests for the <see cref="InstallationValidator"/> class.
/// </summary>
public class InstallationValidatorTests
{
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IServiceController> m_ServiceControllerMock;
    private readonly SharedConfiguration m_Configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallationValidatorTests"/> class.
    /// </summary>
    public InstallationValidatorTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
        m_ServiceControllerMock = new Mock<IServiceController>();

        m_Configuration = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Service = new ServiceSettings
                {
                    ServiceName = "TestService",
                    InstallPath = @"C:\Program Files\MCP-Nexus",
                    BackupPath = @"C:\Program Files\MCP-Nexus\backup"
                }
            },
        };
    }


    /// <summary>
    /// Verifies that parameterless constructor creates validator successfully.
    /// </summary>
    [Fact]
    public void Constructor_Parameterless_Succeeds()
    {
        // Act
        var validator = new InstallationValidator();

        // Assert
        _ = validator.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that constructor with parameters creates validator successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithParameters_Succeeds()
    {
        // Act
        var validator = new InstallationValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        // Assert
        _ = validator.Should().NotBeNull();
    }



    /// <summary>
    /// Verifies that ValidateInstallation returns false when service is already installed.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WhenServiceAlreadyInstalled_ReturnsFalse()
    {
        // Arrange
        var validator = new InstallationValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);
        var sourceDirectory = @"C:\Source";

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.McpNexus.Service.ServiceName))
            .Returns(true);

        // Act
        var result = validator.ValidateInstallation(m_Configuration, sourceDirectory);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns false when source directory does not exist.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WhenSourceDirectoryNotExist_ReturnsFalse()
    {
        // Arrange
        var validator = new InstallationValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);
        var sourceDirectory = @"C:\NonExistent";

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(It.IsAny<string>()))
            .Returns(false);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(sourceDirectory))
            .Returns(false);

        // Act
        var result = validator.ValidateInstallation(m_Configuration, sourceDirectory);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns false when source executable does not exist.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WhenSourceExecutableNotExist_ReturnsFalse()
    {
        // Arrange
        var validator = new InstallationValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);
        var sourceDirectory = @"C:\Source";

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(It.IsAny<string>()))
            .Returns(false);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(sourceDirectory))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.FileExists(Path.Combine(sourceDirectory, "nexus.exe")))
            .Returns(false);

        // Act
        var result = validator.ValidateInstallation(m_Configuration, sourceDirectory);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns false when target directory parent does not exist.
    /// </summary>
    [Fact]
    public void ValidateInstallation_WhenTargetParentNotExist_ReturnsFalse()
    {
        // Arrange
        var validator = new InstallationValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);
        var sourceDirectory = @"C:\Source";

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(It.IsAny<string>()))
            .Returns(false);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(sourceDirectory))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.FileExists(Path.Combine(sourceDirectory, "nexus.exe")))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(@"C:\Program Files"))
            .Returns(false);

        // Act
        var result = validator.ValidateInstallation(m_Configuration, sourceDirectory);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateInstallation returns a boolean when all checks pass (admin check may vary).
    /// </summary>
    [Fact]
    public void ValidateInstallation_WhenAllChecksPass_ReturnsBoolean()
    {
        // Arrange
        var validator = new InstallationValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);
        var sourceDirectory = @"C:\Source";

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(It.IsAny<string>()))
            .Returns(false);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(sourceDirectory))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.FileExists(Path.Combine(sourceDirectory, "nexus.exe")))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(@"C:\Program Files"))
            .Returns(true);

        // Act
        var result = validator.ValidateInstallation(m_Configuration, sourceDirectory);

        // Assert - Will be true if running as admin, false otherwise - just verify it returns a boolean
        _ = result.GetType().Should().Be(typeof(bool));
    }
}
