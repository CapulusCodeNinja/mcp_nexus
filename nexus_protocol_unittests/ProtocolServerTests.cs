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

    public ProtocolServerTests()
    {
        var logger = NullLogger<ProtocolServer>.Instance;
        m_Server = new ProtocolServer(logger);
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        var logger = NullLogger<ProtocolServer>.Instance;
        var server = new ProtocolServer(logger);

        server.Should().NotBeNull();
        server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new ProtocolServer(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task StartAsync_WhenNotRunning_StartsServer()
    {
        await m_Server.StartAsync();

        m_Server.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        await m_Server.StartAsync();

        var action = async () => await m_Server.StartAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Protocol server is already running.");
    }

    [Fact]
    public async Task StopAsync_WhenRunning_StopsServer()
    {
        await m_Server.StartAsync();

        await m_Server.StopAsync();

        m_Server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_DoesNotThrow()
    {
        var action = async () => await m_Server.StopAsync();

        await action.Should().NotThrowAsync();
        m_Server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void SetConfiguration_WithValidConfiguration_SetsConfiguration()
    {
        var config = new { Port = 8080, Host = "localhost" };

        var action = () => m_Server.SetConfiguration(config);

        action.Should().NotThrow();
    }

    [Fact]
    public void SetConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var action = () => m_Server.SetConfiguration(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public async Task SetConfiguration_WhenServerIsRunning_ThrowsInvalidOperationException()
    {
        await m_Server.StartAsync();
        var config = new { Port = 8080 };

        var action = () => m_Server.SetConfiguration(config);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot change configuration while server is running. Stop the server first.");
    }

    [Fact]
    public async Task Dispose_WhenRunning_StopsServerAndDisposes()
    {
        await m_Server.StartAsync();
        m_Server.IsRunning.Should().BeTrue();

        m_Server.Dispose();

        m_Server.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Dispose_WhenNotRunning_Disposes()
    {
        var action = () => m_Server.Dispose();

        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        m_Server.Dispose();

        var action = () => m_Server.Dispose();

        action.Should().NotThrow();
    }

    [Fact]
    public async Task StartAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        m_Server.Dispose();

        var action = async () => await m_Server.StartAsync();

        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task StopAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        m_Server.Dispose();

        var action = async () => await m_Server.StopAsync();

        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void SetConfiguration_AfterDispose_ThrowsObjectDisposedException()
    {
        m_Server.Dispose();

        var action = () => m_Server.SetConfiguration(new { });

        action.Should().Throw<ObjectDisposedException>();
    }
}

