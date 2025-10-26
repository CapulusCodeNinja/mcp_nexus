using FluentAssertions;

using Nexus.CommandLine;
using Nexus.Startup;

using Xunit;

namespace Nexus.Tests.Startup;

/// <summary>
/// Unit tests for the <see cref="MainHostedService"/> class.
/// </summary>
public class MainHostedServiceTests
{
    /// <summary>
    /// Verifies that constructor creates service successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithValidContext_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());

        // Act
        var service = new MainHostedService(context);

        // Assert
        _ = service.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that StopAsync completes successfully.
    /// </summary>
    [Fact]
    public async Task StopAsync_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());
        var service = new MainHostedService(context);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that StopAsync handles cancellation.
    /// </summary>
    [Fact]
    public async Task StopAsync_WithCancellation_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());
        var service = new MainHostedService(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await service.StopAsync(cts.Token);

        // Assert - No exception thrown
    }
}

