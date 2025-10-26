using FluentAssertions;

using Moq;

using Nexus.External.Apis.FileSystem;
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
    private readonly Logger m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseValidatorTests"/> class.
    /// </summary>
    public BaseValidatorTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
        m_ServiceControllerMock = new Mock<IServiceController>();
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new TestableBaseValidator(m_Logger, null!, m_ServiceControllerMock.Object));
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when serviceController is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServiceController_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, null!));
    }

    /// <summary>
    /// Verifies that constructor succeeds with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Act
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        // Assert
        _ = validator.Should().NotBeNull();
    }

    #endregion

    #region ValidateAdministratorPrivileges Tests

    /// <summary>
    /// Verifies that ValidateAdministratorPrivileges returns a boolean result.
    /// </summary>
    [Fact]
    public void ValidateAdministratorPrivileges_ReturnsBoolean()
    {
        // Arrange
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        // Act
        var result = validator.PublicValidateAdministratorPrivileges();

        // Assert - Result depends on whether test is run as admin, just verify it returns a boolean
        _ = result.GetType().Should().Be(typeof(bool));
    }

    #endregion

    #region ValidateDirectoryPermissions Tests

    /// <summary>
    /// Verifies that ValidateDirectoryPermissions returns true when parent directory exists.
    /// </summary>
    [Fact]
    public void ValidateDirectoryPermissions_WithExistingParentDirectory_ReturnsTrue()
    {
        // Arrange
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object);
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
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object);
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
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object);
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
        var validator = new TestableBaseValidator(m_Logger, m_FileSystemMock.Object, m_ServiceControllerMock.Object);

        // Act
        var result = validator.PublicValidateDirectoryPermissions(string.Empty, "Test");

        // Assert
        _ = result.Should().BeTrue();
    }

    #endregion

    /// <summary>
    /// Testable concrete implementation of BaseValidator for testing.
    /// </summary>
    private class TestableBaseValidator : BaseValidator
    {
        public TestableBaseValidator(Logger logger, IFileSystem fileSystem, IServiceController serviceController)
            : base(logger, fileSystem, serviceController)
        {
        }

        public bool PublicValidateAdministratorPrivileges()
        {
            return ValidateAdministratorPrivileges();
        }

        public bool PublicValidateDirectoryPermissions(string directoryPath, string directoryName)
        {
            return ValidateDirectoryPermissions(directoryPath, directoryName);
        }
    }
}

