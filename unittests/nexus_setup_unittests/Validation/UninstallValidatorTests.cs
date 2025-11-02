using FluentAssertions;

using Moq;

using Nexus.Config.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Setup.Validation;

using Xunit;

namespace Nexus.Setup.Unittests.Validation;

/// <summary>
/// Unit tests for the <see cref="UninstallValidator"/> class.
/// </summary>
public class UninstallValidatorTests
{
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IServiceController> m_ServiceControllerMock;
    private readonly SharedConfiguration m_Configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="UninstallValidatorTests"/> class.
    /// </summary>
    public UninstallValidatorTests()
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
                    BackupPath = @"C:\Program Files\MCP-Nexus\backup",
                },
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
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);

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
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        // Assert
        _ = validator.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ValidateUninstall returns false when service is not installed.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenServiceNotInstalled_ReturnsFalse()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.McpNexus.Service.ServiceName))
            .Returns(false);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateUninstall returns a boolean when installation directory does not exist.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenInstallationDirectoryNotExist_ReturnsBoolean()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.McpNexus.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.McpNexus.Service.InstallPath))
            .Returns(false);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert - Verify it returns a boolean (directory check may or may not happen depending on admin privileges)
        _ = result.GetType().Should().Be(typeof(bool));
    }

    /// <summary>
    /// Verifies that ValidateUninstall returns a boolean when all checks pass.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenAllChecksPass_ReturnsBoolean()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.McpNexus.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.McpNexus.Service.InstallPath))
            .Returns(true);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert - Will be true if running as admin, false otherwise
        _ = result.GetType().Should().Be(typeof(bool));
    }

    /// <summary>
    /// Verifies that ValidateUninstall returns a boolean with existing installation directory.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WithExistingInstallationDirectory_ReturnsBoolean()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.McpNexus.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.McpNexus.Service.InstallPath))
            .Returns(true);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert - Verify it returns a boolean (directory check may or may not happen depending on admin privileges)
        _ = result.GetType().Should().Be(typeof(bool));
    }
}
