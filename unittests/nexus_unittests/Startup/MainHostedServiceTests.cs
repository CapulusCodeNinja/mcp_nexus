using FluentAssertions;

using Moq;

using Nexus.CommandLine;
using Nexus.Config;
using Nexus.Config.Models;
using Nexus.Startup;

using Xunit;

namespace Nexus.Tests.Startup;

/// <summary>
/// Unit tests for the <see cref="MainHostedService"/> class.
/// </summary>
public class MainHostedServiceTests
{
    private readonly Mock<ISettings> m_Settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainHostedServiceTests"/> class.
    /// </summary>
    public MainHostedServiceTests()
    {
        m_Settings = new Mock<ISettings>();
        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
    }

    /// <summary>
    /// Verifies that constructor creates service successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithValidContext_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());

        // Act
        var service = new MainHostedService(context, m_Settings.Object);

        // Assert
        _ = service.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that StopAsync completes successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());
        var service = new MainHostedService(context, m_Settings.Object);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that StopAsync handles cancellation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WithCancellation_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());
        var service = new MainHostedService(context, m_Settings.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await service.StopAsync(cts.Token);

        // Assert - No exception thrown
    }
}
