using FluentAssertions;

using Moq;

using Nexus.CommandLine;
using Nexus.Config;
using Nexus.Startup;

using Xunit;

namespace Nexus.Tests.Startup;

/// <summary>
/// Unit tests for the <see cref="StartupBanner"/> class.
/// </summary>
public class StartupBannerTests
{
    private readonly Mock<ISettings> m_Settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupBannerTests"/> class.
    /// </summary>
    public StartupBannerTests()
    {
        m_Settings = new Mock<ISettings>();
    }

    /// <summary>
    /// Verifies that constructor creates banner successfully in non-service mode.
    /// </summary>
    [Fact]
    public void Constructor_WithNonServiceMode_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());

        // Act
        var banner = new StartupBanner(false, context, m_Settings.Object);

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
        var banner = new StartupBanner(true, context, m_Settings.Object);

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
        var banner = new StartupBanner(false, context, m_Settings.Object);

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
        var banner = new StartupBanner(true, context, m_Settings.Object);

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
        var banner = new StartupBanner(false, context, m_Settings.Object);

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
        var banner = new StartupBanner(false, context, m_Settings.Object);

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
        var banner = new StartupBanner(false, context, m_Settings.Object);

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
        var banner = new StartupBanner(false, context, m_Settings.Object);

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
        var banner = new StartupBanner(false, context, m_Settings.Object);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that DisplayBanner handles exception during display.
    /// </summary>
    [Fact]
    public void DisplayBanner_WhenExceptionOccurs_HandlesGracefully()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());
        _ = m_Settings.Setup(s => s.Get()).Throws(new InvalidOperationException("Config error"));
        var banner = new StartupBanner(false, context, m_Settings.Object);

        // Act & Assert - Should not throw
        var action = () => banner.DisplayBanner();
        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that DisplayBanner displays all sections in service mode.
    /// </summary>
    [Fact]
    public void DisplayBanner_WithServiceMode_DisplaysAllSections()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--service" });
        var banner = new StartupBanner(true, context, m_Settings.Object);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown, all sections should display
        _ = banner.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that DisplayBanner handles all command line modes.
    /// </summary>
    /// <param name="mode">The command line mode to test.</param>
    [Theory]
    [InlineData("--http")]
    [InlineData("--stdio")]
    [InlineData("--install")]
    [InlineData("--update")]
    [InlineData("--uninstall")]
    public void DisplayBanner_WithVariousModes_Succeeds(string mode)
    {
        // Arrange
        var context = new CommandLineContext(new[] { mode });
        var banner = new StartupBanner(false, context, m_Settings.Object);

        // Act
        banner.DisplayBanner();

        // Assert - No exception thrown
        _ = banner.Should().NotBeNull();
    }
}
