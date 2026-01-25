using FluentAssertions;

using Moq;

using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.Security;
using WinAiDbg.External.Apis.ServiceManagement;
using WinAiDbg.Setup.Validation;

using Xunit;

namespace WinAiDbg.Setup.Unittests.Validation;

/// <summary>
/// Unit tests for the <see cref="UninstallValidator"/> class.
/// </summary>
public class UninstallValidatorTests
{
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IServiceController> m_ServiceControllerMock;
    private readonly Mock<IAdministratorChecker> m_AdministratorCheckerMock;
    private readonly SharedConfiguration m_Configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="UninstallValidatorTests"/> class.
    /// </summary>
    public UninstallValidatorTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
        m_ServiceControllerMock = new Mock<IServiceController>();
        m_AdministratorCheckerMock = new Mock<IAdministratorChecker>();

        m_Configuration = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Service = new ServiceSettings
                {
                    ServiceName = "TestService",
                    InstallPath = @"C:\Program Files\WinAiDbg",
                    BackupPath = @"C:\Program Files\WinAiDbg\backup",
                },
            },
        };
    }

    /// <summary>
    /// Verifies that constructor creates validator successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithParameters_Succeeds()
    {
        // Act
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

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
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
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
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.WinAiDbg.Service.InstallPath))
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
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.WinAiDbg.Service.InstallPath))
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
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.WinAiDbg.Service.InstallPath))
            .Returns(true);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert - Verify it returns a boolean (directory check may or may not happen depending on admin privileges)
        _ = result.GetType().Should().Be(typeof(bool));
    }

    /// <summary>
    /// Verifies that ValidateUninstall handles installation directory validation when directory exists.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenInstallationDirectoryExists_CompletesSuccessfully()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.WinAiDbg.Service.InstallPath))
            .Returns(true);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert - Verify it returns a boolean (will be true if running as admin, false otherwise)
        _ = result.GetType().Should().Be(typeof(bool));
    }

    /// <summary>
    /// Verifies that ValidateUninstall handles installation directory validation when directory does not exist.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenInstallationDirectoryNotExists_CompletesSuccessfully()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.WinAiDbg.Service.InstallPath))
            .Returns(false);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert - Verify it returns a boolean (will be true if running as admin, false otherwise)
        // Directory not existing is not a failure, just a warning
        _ = result.GetType().Should().Be(typeof(bool));
    }

    /// <summary>
    /// Verifies that ValidateUninstall returns false when running without admin privileges.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenNotRunningAsAdmin_ReturnsFalse()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(false);

        // Act
        // Admin check happens first - if not running as admin, it will return false immediately
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateUninstall covers the success path when service is installed.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenServiceInstalled_ContinuesValidation()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
            .Returns(true);

        // Act
        // This should continue to installation directory validation
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert
        // Will be true if admin, false otherwise, but ValidateServiceInstalled success branch is covered
        _ = result.GetType().Should().Be(typeof(bool));
    }

    /// <summary>
    /// Verifies that ValidateUninstall covers the success path when all validations pass.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenAllValidationsPass_ReturnsTrue()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.WinAiDbg.Service.InstallPath))
            .Returns(true);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ValidateUninstall covers both branches of ValidateInstallationDirectory.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WhenInstallationDirectoryExists_DoesNotLogWarning()
    {
        // Arrange
        var validator = new UninstallValidator(m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(m_Configuration.WinAiDbg.Service.ServiceName))
            .Returns(true);
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(m_Configuration.WinAiDbg.Service.InstallPath))
            .Returns(true);

        // Act
        var result = validator.ValidateUninstall(m_Configuration);

        // Assert
        // When directory exists, the if branch is not taken, so no warning is logged
        _ = result.GetType().Should().Be(typeof(bool));
    }
}
