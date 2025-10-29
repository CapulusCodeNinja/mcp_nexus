using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Nexus.CommandLine;
using Nexus.Startup;

using Xunit;

namespace Nexus.Tests;

/// <summary>
/// Unit tests for the <see cref="Program"/> class.
/// </summary>
public class ProgramTests
{
    /// <summary>
    /// Verifies that CreateHostBuilder creates a valid host builder for HTTP mode.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_WithHttpMode_CreatesHostBuilder()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);

        // Assert
        _ = hostBuilder.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateHostBuilder creates a valid host builder for Stdio mode.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_WithStdioMode_CreatesHostBuilder()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--stdio" });

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);

        // Assert
        _ = hostBuilder.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateHostBuilder creates a valid host builder for Service mode.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_WithServiceMode_CreatesHostBuilder()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--service" });

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);

        // Assert
        _ = hostBuilder.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateHostBuilder registers CommandLineContext in DI.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_RegistersCommandLineContext()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);
        var host = hostBuilder.Build();
        var registeredContext = host.Services.GetService<CommandLineContext>();

        // Assert
        _ = registeredContext.Should().NotBeNull();
        _ = registeredContext.Should().BeSameAs(context);
    }

    /// <summary>
    /// Verifies that CreateHostBuilder registers MainHostedService.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_RegistersMainHostedService()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);
        var host = hostBuilder.Build();
        var hostedServices = host.Services.GetServices<IHostedService>();

        // Assert
        _ = hostedServices.Should().Contain(s => s.GetType() == typeof(MainHostedService));
    }

    /// <summary>
    /// Verifies that CreateHostBuilder with empty args creates host builder.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_WithEmptyArgs_CreatesHostBuilder()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);

        // Assert
        _ = hostBuilder.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateHostBuilder for Install mode creates host builder.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_WithInstallMode_CreatesHostBuilder()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--install" });

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);

        // Assert
        _ = hostBuilder.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateHostBuilder for Update mode creates host builder.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_WithUpdateMode_CreatesHostBuilder()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--update" });

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);

        // Assert
        _ = hostBuilder.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateHostBuilder for Uninstall mode creates host builder.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_WithUninstallMode_CreatesHostBuilder()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--uninstall" });

        // Act
        var hostBuilder = Program.CreateHostBuilder(context);

        // Assert
        _ = hostBuilder.Should().NotBeNull();
    }
}
