using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mcp_nexus.Engine;
using mcp_nexus.Protocol.Resources;
using System.Text.Json;

namespace mcp_nexus.Protocol.Tests.Resources;

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
}

