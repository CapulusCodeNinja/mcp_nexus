using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mcp_nexus.Engine;
using mcp_nexus.Protocol.Resources;
using System.Text.Json;

namespace mcp_nexus.Protocol.Tests.Resources;

/// <summary>
/// Unit tests for CommandsResource class.
/// Tests command listing resource with mocked dependencies.
/// </summary>
public class CommandsResourceTests
{
    private readonly Mock<IDebugEngine> m_MockDebugEngine;
    private readonly IServiceProvider m_ServiceProvider;

    public CommandsResourceTests()
    {
        m_MockDebugEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        services.AddSingleton(m_MockDebugEngine.Object);
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        m_ServiceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Commands_ReturnsEmptyList()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        result.Should().NotBeNullOrEmpty();

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        json.RootElement.GetProperty("commands").GetArrayLength().Should().Be(0);
        json.RootElement.GetProperty("note").GetString().Should().Contain("IDebugEngine");
    }

    [Fact]
    public async Task Commands_IncludesTimestamp()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        var json = JsonDocument.Parse(result);
        json.RootElement.TryGetProperty("timestamp", out var timestampProperty).Should().BeTrue();
    }


    [Fact]
    public async Task Commands_ReturnsValidJson()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        var action = () => JsonDocument.Parse(result);
        action.Should().NotThrow();
    }
}

