using FluentAssertions;

using Moq;

using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.Security;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Setup.Validation;

using NLog;

using Xunit;

namespace Nexus.Setup.Unittests.Validation;

/// <summary>
/// Unit tests for the <see cref="BaseValidator"/> class.
/// </summary>
public class BaseValidatorTests
{
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IServiceController> m_ServiceControllerMock;
    private readonly Mock<IAdministratorChecker> m_AdministratorCheckerMock;
    private readonly Logger m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseValidatorTests"/> class.
    /// </summary>
    public BaseValidatorTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
        m_ServiceControllerMock = new Mock<IServiceController>();
        m_AdministratorCheckerMock = new Mock<IAdministratorChecker>();
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new TestableBaseValidator(m_Logger, null!, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object));
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when serviceController is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServiceController_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, null!, m_AdministratorCheckerMock.Object));
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when administratorChecker is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullAdministratorChecker_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object, null!));
    }

    /// <summary>
    /// Verifies that constructor succeeds with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Act
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        // Assert
        _ = validator.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ValidateAdministratorPrivileges returns true when running as admin.
    /// </summary>
    [Fact]
    public void ValidateAdministratorPrivileges_WhenRunningAsAdmin_ReturnsTrue()
    {
        // Arrange
        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(true);
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        // Act
        var result = validator.PublicValidateAdministratorPrivileges();

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ValidateAdministratorPrivileges returns false when not running as admin.
    /// </summary>
    [Fact]
    public void ValidateAdministratorPrivileges_WhenNotRunningAsAdmin_ReturnsFalse()
    {
        // Arrange
        _ = m_AdministratorCheckerMock.Setup(ac => ac.IsRunningAsAdministrator())
            .Returns(false);
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        // Act
        var result = validator.PublicValidateAdministratorPrivileges();

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateDirectoryPermissions returns true when parent directory exists.
    /// </summary>
    [Fact]
    public void ValidateDirectoryPermissions_WithExistingParentDirectory_ReturnsTrue()
    {
        // Arrange
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);
        var directoryPath = @"C:\Program Files\MCP-Nexus\installation";
        var directoryName = "Installation";

        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(@"C:\Program Files"))
            .Returns(true);

        // Act
        var result = validator.PublicValidateDirectoryPermissions(directoryPath, directoryName);

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ValidateDirectoryPermissions returns false when parent directory does not exist.
    /// </summary>
    [Fact]
    public void ValidateDirectoryPermissions_WithNonExistentParentDirectory_ReturnsFalse()
    {
        // Arrange
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);
        var directoryPath = @"C:\NonExistent\MCP-Nexus\installation";
        var directoryName = "Installation";

        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(@"C:\NonExistent"))
            .Returns(false);

        // Act
        var result = validator.PublicValidateDirectoryPermissions(directoryPath, directoryName);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateDirectoryPermissions handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void ValidateDirectoryPermissions_WithException_ReturnsFalse()
    {
        // Arrange
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);
        var directoryPath = @"C:\Program Files\MCP-Nexus\installation";
        var directoryName = "Installation";

        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Throws<UnauthorizedAccessException>();

        // Act
        var result = validator.PublicValidateDirectoryPermissions(directoryPath, directoryName);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateDirectoryPermissions handles empty directory path.
    /// </summary>
    [Fact]
    public void ValidateDirectoryPermissions_WithEmptyPath_ReturnsTrue()
    {
        // Arrange
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object, m_AdministratorCheckerMock.Object);

        // Act
        var result = validator.PublicValidateDirectoryPermissions(string.Empty, "Test");

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Testable concrete implementation of BaseValidator for testing.
    /// </summary>
    private class TestableBaseValidator : BaseValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestableBaseValidator"/> class for tests.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        /// <param name="administratorChecker">Administrator checker abstraction.</param>
        public TestableBaseValidator(Logger logger, IFileSystem fileSystem, IServiceController serviceController, IAdministratorChecker administratorChecker)
            : base(fileSystem, serviceController, administratorChecker)
        {
            _ = logger; // parameter intentionally unused in accessor
        }

        /// <summary>
        /// Exposes the protected administrator privileges validation.
        /// </summary>
        /// <returns>True if validation passes; otherwise false.</returns>
        public bool PublicValidateAdministratorPrivileges()
        {
            return ValidateAdministratorPrivileges();
        }

        /// <summary>
        /// Exposes the protected directory permissions validation.
        /// </summary>
        /// <param name="directoryPath">Path to validate.</param>
        /// <param name="directoryName">Friendly directory name used in messages.</param>
        /// <returns>True if permissions are sufficient; otherwise false.</returns>
        public bool PublicValidateDirectoryPermissions(string directoryPath, string directoryName)
        {
            return ValidateDirectoryPermissions(directoryPath, directoryName);
        }
    }
}
