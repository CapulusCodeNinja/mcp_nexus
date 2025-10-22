using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using nexus.engine;
using nexus.protocol.Resources;
using System.Text.Json;

namespace nexus.protocol.unittests.Resources;

/// <summary>
/// Unit tests for SessionsResource class.
/// Tests session listing resource with mocked dependencies.
/// </summary>
public class SessionsResourceTests
{
    private readonly Mock<IDebugEngine> m_MockDebugEngine;
    private readonly IServiceProvider m_ServiceProvider;

    public SessionsResourceTests()
    {
        m_MockDebugEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        services.AddSingleton(m_MockDebugEngine.Object);
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        m_ServiceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Sessions_ReturnsEmptyList()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        result.Should().NotBeNullOrEmpty();

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        json.RootElement.GetProperty("sessions").GetArrayLength().Should().Be(0);
        json.RootElement.GetProperty("note").GetString().Should().Contain("IDebugEngine");
    }

    [Fact]
    public async Task Sessions_IncludesTimestamp()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        var json = JsonDocument.Parse(result);
        json.RootElement.TryGetProperty("timestamp", out var timestampProperty).Should().BeTrue();
    }


    [Fact]
    public async Task Sessions_ReturnsValidJson()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        var action = () => JsonDocument.Parse(result);
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Sessions_WithException_ReturnsErrorResponse()
    {
        // Create a service provider that throws when getting IDebugEngine
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = NullLogger.Instance;
        
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(mockLoggerFactory.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IDebugEngine))).Throws(new InvalidOperationException("Test error"));

        var result = await SessionsResource.Sessions(mockServiceProvider.Object);

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        json.RootElement.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Contain("Test error");
    }

    [Fact]
    public async Task Sessions_WithNullServiceProvider_ThrowsException()
    {
        var action = async () => await SessionsResource.Sessions(null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Sessions_JsonFormat_IsIndented()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        result.Should().Contain("\n"); // Indented JSON contains newlines
    }
}

