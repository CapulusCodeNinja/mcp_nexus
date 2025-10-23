using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using nexus.Hosting;
using nexus.protocol;
using Xunit;

namespace nexus_unittests.Hosting;

/// <summary>
/// Unit tests for HttpServerHostedService.
/// </summary>
public class HttpServerHostedServiceTests
{
    private readonly ILogger<HttpServerHostedService> m_Logger;
    private readonly Mock<IProtocolServer> m_MockProtocolServer;
    private readonly Mock<IHostApplicationLifetime> m_MockLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServerHostedServiceTests"/> class.
    /// </summary>
    public HttpServerHostedServiceTests()
    {
        m_Logger = NullLogger<HttpServerHostedService>.Instance;
        m_MockProtocolServer = new Mock<IProtocolServer>();
        m_MockLifetime = new Mock<IHostApplicationLifetime>();
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HttpServerHostedService(null!, m_MockProtocolServer.Object, m_MockLifetime.Object));
    }

    /// <summary>
    /// Verifies constructor throws when protocol server is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenProtocolServerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HttpServerHostedService(m_Logger, null!, m_MockLifetime.Object));
    }

    /// <summary>
    /// Verifies constructor throws when lifetime is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLifetimeIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HttpServerHostedService(m_Logger, m_MockProtocolServer.Object, null!));
    }

    /// <summary>
    /// Verifies StartAsync calls protocol server StartAsync.
    /// </summary>
    [Fact]
    public async Task StartAsync_CallsProtocolServerStartAsync()
    {
        // Arrange
        var service = new HttpServerHostedService(m_Logger, m_MockProtocolServer.Object, m_MockLifetime.Object);
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);

        // Assert
        m_MockProtocolServer.Verify(x => x.StartAsync(cts.Token), Times.Once);
    }

    /// <summary>
    /// Verifies StopAsync calls protocol server StopAsync.
    /// </summary>
    [Fact]
    public async Task StopAsync_CallsProtocolServerStopAsync()
    {
        // Arrange
        var service = new HttpServerHostedService(m_Logger, m_MockProtocolServer.Object, m_MockLifetime.Object);
        var cts = new CancellationTokenSource();

        // Act
        await service.StopAsync(cts.Token);

        // Assert
        m_MockProtocolServer.Verify(x => x.StopAsync(cts.Token), Times.Once);
    }
}

