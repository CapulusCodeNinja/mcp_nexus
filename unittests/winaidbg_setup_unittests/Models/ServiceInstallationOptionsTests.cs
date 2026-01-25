using WinAiDbg.Setup.Models;

using Xunit;

namespace WinAiDbg.Setup_unittests.Models;

/// <summary>
/// Unit tests for ServiceInstallationOptions.
/// </summary>
public class ServiceInstallationOptionsTests
{
    /// <summary>
    /// Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new ServiceInstallationOptions();

        // Assert
        Assert.Equal("WinAiDbg", options.ServiceName);
        Assert.Equal("WinAiDbg Debugging Server", options.DisplayName);
        Assert.Equal("Model Context Protocol server for Windows debugging tools", options.Description);
        Assert.Equal(string.Empty, options.ExecutablePath);
        Assert.Equal(ServiceStartMode.Automatic, options.StartMode);
        Assert.Equal(ServiceAccount.LocalSystem, options.Account);
        Assert.Null(options.AccountUsername);
        Assert.Null(options.AccountPassword);
    }

    /// <summary>
    /// Verifies that properties can be set.
    /// </summary>
    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var options = new ServiceInstallationOptions
        {
            // Act
            ServiceName = "TestService",
            DisplayName = "Test Display Name",
            Description = "Test Description",
            ExecutablePath = @"C:\test\service.exe",
            StartMode = ServiceStartMode.Manual,
            Account = ServiceAccount.Custom,
            AccountUsername = "TestUser",
            AccountPassword = "TestPassword",
        };

        // Assert
        Assert.Equal("TestService", options.ServiceName);
        Assert.Equal("Test Display Name", options.DisplayName);
        Assert.Equal("Test Description", options.Description);
        Assert.Equal(@"C:\test\service.exe", options.ExecutablePath);
        Assert.Equal(ServiceStartMode.Manual, options.StartMode);
        Assert.Equal(ServiceAccount.Custom, options.Account);
        Assert.Equal("TestUser", options.AccountUsername);
        Assert.Equal("TestPassword", options.AccountPassword);
    }
}
