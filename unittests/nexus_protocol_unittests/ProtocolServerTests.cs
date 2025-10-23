using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using nexus.protocol;

namespace nexus.protocol.unittests;

/// <summary>
/// Unit tests for ProtocolServer class.
/// Tests server lifecycle management (start, stop, configuration).
/// </summary>
public class ProtocolServerTests
{
    private readonly ProtocolServer m_Server;

    /// <summary>
    /// Initializes a new instance of the ProtocolServerTests class.
    /// </summary>
    public ProtocolServerTests()
    {
        var logger = NullLogger<ProtocolServer>.Instance;
        m_Server = new ProtocolServer(logger);
    }

    /// <summary>
    /// Verifies that ProtocolServer constructor creates instance with valid logger.
    /// </summary>
    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        var logger = NullLogger<ProtocolServer>.Instance;
        var server = new ProtocolServer(logger);

        server.Should().NotBeNull();
        server.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ProtocolServer constructor throws ArgumentNullException with null logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new ProtocolServer(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that StartAsync throws InvalidOperationException when WebApplication is not set.
    /// </summary>
    [Fact]
    public async Task StartAsync_WithoutWebApplication_ThrowsInvalidOperationException()
    {
        var action = async () => await m_Server.StartAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Server instance must be set before starting. Call SetWebApplication() for HTTP mode or set Host for Stdio mode.");
    }

    /// <summary>
    /// Verifies that StartAsync throws InvalidOperationException when server is already running.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange: Create a minimal WebApplication for testing
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0"); // Use random port
        var app = builder.Build();
        
        try
        {
            m_Server.SetWebApplication(app);
            await m_Server.StartAsync();

            // Act & Assert
            var action = async () => await m_Server.StartAsync();
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Protocol server is already running.");
        }
        finally
        {
            await m_Server.StopAsync();
            await app.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that StopAsync stops the server when running.
    /// </summary>
    [Fact]
    public async Task StopAsync_WhenRunning_StopsServer()
    {
        // Arrange: Create a minimal WebApplication for testing
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0"); // Use random port
        var app = builder.Build();
        
        try
        {
            m_Server.SetWebApplication(app);
            await m_Server.StartAsync();
            m_Server.IsRunning.Should().BeTrue();

            // Act
            await m_Server.StopAsync();

            // Assert
            m_Server.IsRunning.Should().BeFalse();
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that StopAsync does not throw when server is not running.
    /// </summary>
    [Fact]
    public async Task StopAsync_WhenNotRunning_DoesNotThrow()
    {
        var action = async () => await m_Server.StopAsync();

        await action.Should().NotThrowAsync();
        m_Server.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that SetConfiguration sets configuration with valid configuration object.
    /// </summary>
    [Fact]
    public void SetConfiguration_WithValidConfiguration_SetsConfiguration()
    {
        var config = new { Port = 8080, Host = "localhost" };

        var action = () => m_Server.SetConfiguration(config);

        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that SetConfiguration throws ArgumentNullException with null configuration.
    /// </summary>
    [Fact]
    public void SetConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var action = () => m_Server.SetConfiguration(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    /// <summary>
    /// Verifies that SetConfiguration throws InvalidOperationException when server is running.
    /// </summary>
    [Fact]
    public async Task SetConfiguration_WhenServerIsRunning_ThrowsInvalidOperationException()
    {
        // Arrange: Create a minimal WebApplication for testing
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0"); // Use random port
        var app = builder.Build();
        
        try
        {
            m_Server.SetWebApplication(app);
            await m_Server.StartAsync();
            var config = new { Port = 8080 };

            // Act & Assert
            var action = () => m_Server.SetConfiguration(config);
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot change configuration while server is running. Stop the server first.");
        }
        finally
        {
            await m_Server.StopAsync();
            await app.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that Dispose stops and disposes the server when running.
    /// </summary>
    [Fact]
    public async Task Dispose_WhenRunning_StopsServerAndDisposes()
    {
        // Arrange: Create a minimal WebApplication for testing
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0"); // Use random port
        var app = builder.Build();
        
        m_Server.SetWebApplication(app);
        await m_Server.StartAsync();
        m_Server.IsRunning.Should().BeTrue();

        // Act
        m_Server.Dispose();

        // Assert
        m_Server.IsRunning.Should().BeFalse();
        
        // Cleanup
        await app.DisposeAsync();
    }

    /// <summary>
    /// Verifies that Dispose disposes the server when not running.
    /// </summary>
    [Fact]
    public void Dispose_WhenNotRunning_Disposes()
    {
        var action = () => m_Server.Dispose();

        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Dispose can be called multiple times without throwing.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        m_Server.Dispose();

        var action = () => m_Server.Dispose();

        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that StartAsync throws ObjectDisposedException after server is disposed.
    /// </summary>
    [Fact]
    public async Task StartAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        m_Server.Dispose();

        var action = async () => await m_Server.StartAsync();

        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that StopAsync throws ObjectDisposedException after server is disposed.
    /// </summary>
    [Fact]
    public async Task StopAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        m_Server.Dispose();

        var action = async () => await m_Server.StopAsync();

        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that SetConfiguration throws ObjectDisposedException after server is disposed.
    /// </summary>
    [Fact]
    public void SetConfiguration_AfterDispose_ThrowsObjectDisposedException()
    {
        m_Server.Dispose();

        var action = () => m_Server.SetConfiguration(new { });

        action.Should().Throw<ObjectDisposedException>();
    }
}
