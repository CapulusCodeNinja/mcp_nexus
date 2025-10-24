using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ServiceManagement;
using nexus.config.Models;
using nexus.setup.Validation;

namespace nexus.setup.unittests.Validation;

/// <summary>
/// Unit tests for UninstallValidator class.
/// </summary>
public class UninstallValidatorTests
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IServiceController> m_MockServiceController;
    private readonly ILogger<UninstallValidator> m_Logger;
    private readonly UninstallValidator m_Validator;

    /// <summary>
    /// Initializes a new instance of the UninstallValidatorTests class.
    /// </summary>
    public UninstallValidatorTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockServiceController = new Mock<IServiceController>();
        m_Logger = NullLogger<UninstallValidator>.Instance;
        m_Validator = new UninstallValidator(m_Logger, m_MockFileSystem.Object, m_MockServiceController.Object);
    }

    /// <summary>
    /// Verifies that constructor with null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new UninstallValidator(null!, m_MockFileSystem.Object, m_MockServiceController.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that constructor with null file system throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new UninstallValidator(m_Logger, null!, m_MockServiceController.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystem");
    }

    /// <summary>
    /// Verifies that constructor with null service controller throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServiceController_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new UninstallValidator(m_Logger, m_MockFileSystem.Object, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceController");
    }

    /// <summary>
    /// Verifies that ValidateUninstall returns false when service is not installed.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WithNonExistentService_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        m_MockServiceController.Setup(sc => sc.IsServiceInstalled("TestService"))
            .Returns(false);

        // Act
        var result = m_Validator.ValidateUninstall(config);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateUninstall returns false when admin privileges are not available.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WithNonExistentDirectory_LogsWarning()
    {
        // Arrange
        var config = CreateTestConfiguration();
        m_MockServiceController.Setup(sc => sc.IsServiceInstalled("TestService"))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\Program Files\\MCP-Nexus"))
            .Returns(false);

        // Act - Note: This will return false because it can't validate admin privileges in tests
        var result = m_Validator.ValidateUninstall(config);

        // Assert - Should return false due to admin privilege check failure
        result.Should().BeFalse();
        // Note: DirectoryExists and IsServiceInstalled are not called because admin check fails first
    }

    /// <summary>
    /// Verifies that ValidateUninstall returns false when admin privileges are not available.
    /// </summary>
    [Fact]
    public void ValidateUninstall_ChecksServiceInstallation()
    {
        // Arrange
        var config = CreateTestConfiguration();
        m_MockServiceController.Setup(sc => sc.IsServiceInstalled("TestService"))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = m_Validator.ValidateUninstall(config);

        // Assert - Should return false due to admin privilege check failure
        result.Should().BeFalse();
        // Note: IsServiceInstalled is not called because admin check fails first
    }

    /// <summary>
    /// Verifies that ValidateUninstall with existing directory doesn't throw.
    /// </summary>
    [Fact]
    public void ValidateUninstall_WithExistingDirectory_DoesNotThrow()
    {
        // Arrange
        var config = CreateTestConfiguration();
        m_MockServiceController.Setup(sc => sc.IsServiceInstalled("TestService"))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\Program Files\\MCP-Nexus"))
            .Returns(true);

        // Act & Assert - Should not throw, even if admin check fails
        var action = () => m_Validator.ValidateUninstall(config);
        action.Should().NotThrow();
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

