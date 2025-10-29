using Microsoft.Extensions.Hosting;

using Moq;

using Nexus.Hosting;
using Nexus.Protocol;

using NLog;

using Xunit;

namespace Nexus.Unittests.Hosting;

/// <summary>
/// Unit tests for ServiceHostedService.
/// </summary>
public class ServiceHostedServiceTests
{
    private readonly Logger m_Logger;
    private readonly Mock<IProtocolServer> m_MockProtocolServer;
    private readonly Mock<IHostApplicationLifetime> m_MockLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceHostedServiceTests"/> class.
    /// </summary>
    public ServiceHostedServiceTests()
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_MockProtocolServer = new Mock<IProtocolServer>();
        m_MockLifetime = new Mock<IHostApplicationLifetime>();
    }

    /// <summary>
    /// Verifies constructor throws when protocol server is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenProtocolServerIsNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            new ServiceHostedService(null!, m_MockLifetime.Object));
    }

    /// <summary>
    /// Verifies constructor throws when lifetime is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLifetimeIsNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            new ServiceHostedService(m_MockProtocolServer.Object, null!));
    }

    /// <summary>
    /// Verifies StartAsync calls protocol server StartAsync.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task StartAsync_CallsProtocolServerStartAsync()
    {
        // Arrange
        var service = new ServiceHostedService(m_MockProtocolServer.Object, m_MockLifetime.Object);
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);

        // Assert
        m_MockProtocolServer.Verify(x => x.StartAsync(cts.Token), Times.Once);
    }

    /// <summary>
    /// Verifies StopAsync calls protocol server StopAsync.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task StopAsync_CallsProtocolServerStopAsync()
    {
        // Arrange
        var service = new ServiceHostedService(m_MockProtocolServer.Object, m_MockLifetime.Object);
        var cts = new CancellationTokenSource();

        // Act
        await service.StopAsync(cts.Token);

        // Assert
        m_MockProtocolServer.Verify(x => x.StopAsync(cts.Token), Times.Once);
    }
}
