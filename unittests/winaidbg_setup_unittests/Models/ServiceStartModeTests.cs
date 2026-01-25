using WinAiDbg.Setup.Models;

using Xunit;

namespace WinAiDbg.Setup_unittests.Models;

/// <summary>
/// Unit tests for ServiceStartMode enum.
/// </summary>
public class ServiceStartModeTests
{
    /// <summary>
    /// Verifies that all start modes are defined.
    /// </summary>
    [Fact]
    public void ServiceStartMode_HasAllExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ServiceStartMode), ServiceStartMode.Automatic));
        Assert.True(Enum.IsDefined(typeof(ServiceStartMode), ServiceStartMode.Manual));
        Assert.True(Enum.IsDefined(typeof(ServiceStartMode), ServiceStartMode.Disabled));
    }

    /// <summary>
    /// Verifies that enum has exactly three values.
    /// </summary>
    [Fact]
    public void ServiceStartMode_HasExactlyThreeValues()
    {
        // Act
        var values = Enum.GetValues(typeof(ServiceStartMode));

        // Assert
        Assert.Equal(3, values.Length);
    }
}
