using nexus.CommandLine;
using nexus.Hosting;
using Xunit;

namespace nexus_unittests.Hosting;

/// <summary>
/// Unit tests for ServerModeContext.
/// </summary>
public class ServerModeContextTests
{
    /// <summary>
    /// Verifies that constructor sets mode correctly.
    /// </summary>
    [Theory]
    [InlineData(ServerMode.Http)]
    [InlineData(ServerMode.Stdio)]
    [InlineData(ServerMode.Service)]
    public void Constructor_SetsMode(ServerMode mode)
    {
        // Act
        var context = new ServerModeContext(mode);

        // Assert
        Assert.Equal(mode, context.Mode);
    }

    /// <summary>
    /// Verifies that mode property is read-only.
    /// </summary>
    [Fact]
    public void Mode_IsReadOnly()
    {
        // Arrange
        var context = new ServerModeContext(ServerMode.Http);

        // Assert
        var property = typeof(ServerModeContext).GetProperty(nameof(ServerModeContext.Mode));
        Assert.NotNull(property);
        Assert.Null(property!.SetMethod);
    }
}

