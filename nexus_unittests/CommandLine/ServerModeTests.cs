using nexus.CommandLine;
using Xunit;

namespace nexus_unittests.CommandLine;

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
    }

    /// <summary>
    /// Verifies that enum has exactly three values.
    /// </summary>
    [Fact]
    public void ServerMode_HasExactlyThreeValues()
    {
        // Act
        var values = Enum.GetValues(typeof(ServerMode));

        // Assert
        Assert.Equal(3, values.Length);
    }

    /// <summary>
    /// Verifies that enum values can be converted to strings.
    /// </summary>
    [Theory]
    [InlineData(ServerMode.Http, "Http")]
    [InlineData(ServerMode.Stdio, "Stdio")]
    [InlineData(ServerMode.Service, "Service")]
    public void ServerMode_ToString_ReturnsExpectedValue(ServerMode mode, string expected)
    {
        // Act
        var result = mode.ToString();

        // Assert
        Assert.Equal(expected, result);
    }
}

