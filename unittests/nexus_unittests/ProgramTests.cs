using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Moq;

using Nexus.CommandLine;
using Nexus.Config;
using Nexus.Config.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Startup;

using Xunit;

namespace Nexus.Tests;

/// <summary>
/// Unit tests for the <see cref="Program"/> class.
/// </summary>
public class ProgramTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;
    private readonly Mock<IServiceController> m_ServiceController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramTests"/> class.
    /// </summary>
    public ProgramTests()
    {
        m_Settings = new Mock<ISettings>();
        m_FileSystem = new Mock<IFileSystem>();
        m_ProcessManager = new Mock<IProcessManager>();
        m_ServiceController = new Mock<IServiceController>();

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
    /// Verifies that CreateHostBuilder creates a valid host builder for HTTP mode.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_WithHttpMode_CreatesHostBuilder()
    {
        // Arrange
        var context = new CommandLineContext(new[] { "--http" });

        // Act
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);

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
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);

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
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);

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
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);
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
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);
        _ = hostBuilder.ConfigureServices(services => services.AddSingleton(m_Settings.Object));
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
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);

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
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);

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
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);

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
        var hostBuilder = Program.CreateHostBuilder(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_Settings.Object);

        // Assert
        _ = hostBuilder.Should().NotBeNull();
    }
}
