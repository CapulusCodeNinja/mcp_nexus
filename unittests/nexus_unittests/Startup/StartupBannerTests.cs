using FluentAssertions;

using Nexus.CommandLine;
using Nexus.Startup;

using Xunit;

namespace Nexus.Tests.Startup;

/// <summary>
/// Unit tests for the <see cref="StartupBanner"/> class.
/// </summary>
public class StartupBannerTests
{
    /// <summary>
    /// Verifies that constructor creates banner successfully in non-service mode.
    /// </summary>
    [Fact]
    public void Constructor_WithNonServiceMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());

        // Act
        var banner = new StartupBanner(false, context);

        // Assert
        _ = banner.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that constructor creates banner successfully in service mode.
    /// </summary>
    [Fact]
    public void Constructor_WithServiceMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--service" });

        // Act
        var banner = new StartupBanner(true, context);

        // Assert
        _ = banner.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that DisplayBanner executes without throwing.
    /// </summary>
    [Fact]
    public void DisplayBanner_WithNonServiceMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());
        var banner = new StartupBanner(false, context);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that DisplayBanner executes in service mode.
    /// </summary>
    [Fact]
    public void DisplayBanner_WithServiceMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--service" });
        var banner = new StartupBanner(true, context);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that DisplayBanner handles HTTP mode.
    /// </summary>
    [Fact]
    public void DisplayBanner_WithHttpMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });
        var banner = new StartupBanner(false, context);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that DisplayBanner handles Stdio mode.
    /// </summary>
    [Fact]
    public void DisplayBanner_WithStdioMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--stdio" });
        var banner = new StartupBanner(false, context);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that DisplayBanner handles Install mode.
    /// </summary>
    [Fact]
    public void DisplayBanner_WithInstallMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--install" });
        var banner = new StartupBanner(false, context);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that DisplayBanner handles Update mode.
    /// </summary>
    [Fact]
    public void DisplayBanner_WithUpdateMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--update" });
        var banner = new StartupBanner(false, context);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that DisplayBanner handles Uninstall mode.
    /// </summary>
    [Fact]
    public void DisplayBanner_WithUninstallMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--uninstall" });
        var banner = new StartupBanner(false, context);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
    }
}
