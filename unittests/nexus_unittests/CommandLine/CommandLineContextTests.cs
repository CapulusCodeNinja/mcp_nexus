using FluentAssertions;

using Nexus.CommandLine;

using Xunit;

namespace Nexus.Tests.CommandLine;

/// <summary>
/// Unit tests for the <see cref="CommandLineContext"/> class.
/// </summary>
public class CommandLineContextTests
{
    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when args is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullArgs_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new CommandLineContext(null!));
    }

    /// <summary>
    /// Verifies that constructor accepts empty array.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyArgs_Succeeds()
    {
        // Act
        var context = new CommandLineContext(Array.Empty<string>());

        // Assert
        _ = context.Args.Should().NotBeNull();
        _ = context.Args.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that constructor stores args correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithArgs_StoresArgs()
    {
        // Arrange
        var args = new[] { "--http", "--port", "5000" };

        // Act
        var context = new CommandLineContext(args);

        // Assert
        _ = context.Args.Should().BeSameAs(args);
    }

    /// <summary>
    /// Verifies that IsHttpMode returns true when --http argument is present.
    /// </summary>
    [Fact]
    public void IsHttpMode_WithHttpArgument_ReturnsTrue()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });

        // Act
        var result = context.IsHttpMode;

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsHttpMode is case insensitive.
    /// </summary>
    /// <param name="arg">HTTP mode argument with varying casing.</param>
    [Theory]
    [InlineData("--HTTP")]
    [InlineData("--Http")]
    [InlineData("--HtTp")]
    public void IsHttpMode_WithDifferentCasing_ReturnsTrue(string arg)
    {
        // Arrange
        var context = new CommandLineContext(new[] { arg });

        // Act
        var result = context.IsHttpMode;

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsHttpMode returns false when --http argument is absent.
    /// </summary>
    [Fact]
    public void IsHttpMode_WithoutHttpArgument_ReturnsFalse()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--stdio" });

        // Act
        var result = context.IsHttpMode;

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsStdioMode returns true when --stdio argument is present.
    /// </summary>
    [Fact]
    public void IsStdioMode_WithStdioArgument_ReturnsTrue()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--stdio" });

        // Act
        var result = context.IsStdioMode;

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsStdioMode returns false when --stdio argument is absent.
    /// </summary>
    [Fact]
    public void IsStdioMode_WithoutStdioArgument_ReturnsFalse()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });

        // Act
        var result = context.IsStdioMode;

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsServiceMode returns true when --service argument is present.
    /// </summary>
    [Fact]
    public void IsServiceMode_WithServiceArgument_ReturnsTrue()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--service" });

        // Act
        var result = context.IsServiceMode;

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsServiceMode returns false when --service argument is absent.
    /// </summary>
    [Fact]
    public void IsServiceMode_WithoutServiceArgument_ReturnsFalse()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });

        // Act
        var result = context.IsServiceMode;

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsInstallMode returns true when --install argument is present.
    /// </summary>
    [Fact]
    public void IsInstallMode_WithInstallArgument_ReturnsTrue()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--install" });

        // Act
        var result = context.IsInstallMode;

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsInstallMode returns false when --install argument is absent.
    /// </summary>
    [Fact]
    public void IsInstallMode_WithoutInstallArgument_ReturnsFalse()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });

        // Act
        var result = context.IsInstallMode;

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsUpdateMode returns true when --update argument is present.
    /// </summary>
    [Fact]
    public void IsUpdateMode_WithUpdateArgument_ReturnsTrue()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--update" });

        // Act
        var result = context.IsUpdateMode;

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsUpdateMode returns false when --update argument is absent.
    /// </summary>
    [Fact]
    public void IsUpdateMode_WithoutUpdateArgument_ReturnsFalse()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });

        // Act
        var result = context.IsUpdateMode;

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsUninstallMode returns true when --uninstall argument is present.
    /// </summary>
    [Fact]
    public void IsUninstallMode_WithUninstallArgument_ReturnsTrue()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--uninstall" });

        // Act
        var result = context.IsUninstallMode;

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsUninstallMode returns false when --uninstall argument is absent.
    /// </summary>
    [Fact]
    public void IsUninstallMode_WithoutUninstallArgument_ReturnsFalse()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });

        // Act
        var result = context.IsUninstallMode;

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that multiple mode flags can be detected.
    /// </summary>
    [Fact]
    public void MultipleFlags_WithMultipleArguments_DetectsAll()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http", "--install", "--update" });

        // Act & Assert
        _ = context.IsHttpMode.Should().BeTrue();
        _ = context.IsInstallMode.Should().BeTrue();
        _ = context.IsUpdateMode.Should().BeTrue();
        _ = context.IsStdioMode.Should().BeFalse();
        _ = context.IsServiceMode.Should().BeFalse();
        _ = context.IsUninstallMode.Should().BeFalse();
    }
}
