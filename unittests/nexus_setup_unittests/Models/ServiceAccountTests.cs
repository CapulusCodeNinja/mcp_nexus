using Nexus.Setup.Models;

using Xunit;

namespace Nexus.Setup_unittests.Models;

/// <summary>
/// Unit tests for ServiceAccount enum.
/// </summary>
public class ServiceAccountTests
{
    /// <summary>
    /// Verifies that all account types are defined.
    /// </summary>
    [Fact]
    public void ServiceAccount_HasAllExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ServiceAccount), ServiceAccount.LocalSystem));
        Assert.True(Enum.IsDefined(typeof(ServiceAccount), ServiceAccount.LocalService));
        Assert.True(Enum.IsDefined(typeof(ServiceAccount), ServiceAccount.NetworkService));
        Assert.True(Enum.IsDefined(typeof(ServiceAccount), ServiceAccount.Custom));
    }

    /// <summary>
    /// Verifies that enum has exactly four values.
    /// </summary>
    [Fact]
    public void ServiceAccount_HasExactlyFourValues()
    {
        // Act
        var values = Enum.GetValues(typeof(ServiceAccount));

        // Assert
        Assert.Equal(4, values.Length);
    }
}
