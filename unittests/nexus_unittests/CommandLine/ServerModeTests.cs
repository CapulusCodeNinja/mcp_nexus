using Nexus.CommandLine;

using Xunit;

namespace Nexus.Unittests.CommandLine;

/// <summary>
/// Unit tests for ServerMode enum.
/// </summary>
public class ServerModeTests
{
    /// <summary>
    /// Verifies that all server modes are defined.
    /// </summary>
    [Fact]
    public void ServerMode_HasAllExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ServerMode), ServerMode.Http));
        Assert.True(Enum.IsDefined(typeof(ServerMode), ServerMode.Stdio));
        Assert.True(Enum.IsDefined(typeof(ServerMode), ServerMode.Service));
        Assert.True(Enum.IsDefined(typeof(ServerMode), ServerMode.Install));
        Assert.True(Enum.IsDefined(typeof(ServerMode), ServerMode.Update));
        Assert.True(Enum.IsDefined(typeof(ServerMode), ServerMode.Uninstall));
    }

    /// <summary>
    /// Verifies that enum has exactly six values.
    /// </summary>
    [Fact]
    public void ServerMode_HasExactlyThreeValues()
    {
        // Act
        var values = Enum.GetValues(typeof(ServerMode));

        // Assert
        Assert.Equal(6, values.Length);
    }

    /// <summary>
    /// Verifies that enum values can be converted to strings.
    /// </summary>
    /// <param name="mode">The server mode enum value under test.</param>
    /// <param name="expected">The expected string representation.</param>
    [Theory]
    [InlineData(ServerMode.Http, "Http")]
    [InlineData(ServerMode.Stdio, "Stdio")]
    [InlineData(ServerMode.Service, "Service")]
    [InlineData(ServerMode.Install, "Install")]
    [InlineData(ServerMode.Update, "Update")]
    [InlineData(ServerMode.Uninstall, "Uninstall")]
    public void ServerMode_ToString_ReturnsExpectedValue(ServerMode mode, string expected)
    {
        // Act
        var result = mode.ToString();

        // Assert
        Assert.Equal(expected, result);
    }
}
