using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using nexus.setup;
using nexus.setup.Core;
using nexus.setup.Models;
using Xunit;

namespace nexus.setup_unittests.Core;

/// <summary>
/// Unit tests for ServiceInstaller.
/// </summary>
public class ServiceInstallerTests
{
    private readonly ILogger<ServiceInstaller> m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstallerTests"/> class.
    /// </summary>
    public ServiceInstallerTests()
    {
        m_Logger = NullLogger<ServiceInstaller>.Instance;
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceInstaller(null!));
    }

    /// <summary>
    /// Verifies InstallServiceAsync throws when options is null.
    /// </summary>
    [Fact]
    public async Task InstallServiceAsync_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange
        var installer = new ServiceInstaller(m_Logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => installer.InstallServiceAsync(null!));
    }

    /// <summary>
    /// Verifies InstallServiceAsync throws when service name is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InstallServiceAsync_ThrowsArgumentException_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var installer = new ServiceInstaller(m_Logger);
        var options = new ServiceInstallationOptions { ServiceName = serviceName!, ExecutablePath = "test.exe" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => installer.InstallServiceAsync(options));
    }

    /// <summary>
    /// Verifies InstallServiceAsync throws when executable path is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InstallServiceAsync_ThrowsArgumentException_WhenExecutablePathIsNullOrEmpty(string? executablePath)
    {
        // Arrange
        var installer = new ServiceInstaller(m_Logger);
        var options = new ServiceInstallationOptions { ServiceName = "TestService", ExecutablePath = executablePath! };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => installer.InstallServiceAsync(options));
    }

    /// <summary>
    /// Verifies UninstallServiceAsync throws when service name is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UninstallServiceAsync_ThrowsArgumentException_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var installer = new ServiceInstaller(m_Logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => installer.UninstallServiceAsync(serviceName!));
    }

    /// <summary>
    /// Verifies IsServiceInstalled returns false for null or empty service name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsServiceInstalled_ReturnsFalse_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var installer = new ServiceInstaller(m_Logger);

        // Act
        var result = installer.IsServiceInstalled(serviceName!);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies IsServiceInstalled returns false for non-existent service.
    /// </summary>
    [Fact]
    public void IsServiceInstalled_ReturnsFalse_ForNonExistentService()
    {
        // Arrange
        var installer = new ServiceInstaller(m_Logger);

        // Act
        var result = installer.IsServiceInstalled("NonExistentService_" + Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies GetServiceStatus returns null for null or empty service name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetServiceStatus_ReturnsNull_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var installer = new ServiceInstaller(m_Logger);

        // Act
        var result = installer.GetServiceStatus(serviceName!);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies GetServiceStatus returns null for non-existent service.
    /// </summary>
    [Fact]
    public void GetServiceStatus_ReturnsNull_ForNonExistentService()
    {
        // Arrange
        var installer = new ServiceInstaller(m_Logger);

        // Act
        var result = installer.GetServiceStatus("NonExistentService_" + Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}

