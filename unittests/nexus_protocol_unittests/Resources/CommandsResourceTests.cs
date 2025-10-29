using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Nexus.Engine.Share;
using Nexus.Protocol.Resources;

using Xunit;

namespace Nexus.Protocol.Unittests.Resources;

/// <summary>
/// Unit tests for CommandsResource class.
/// Tests command listing resource with mocked dependencies.
/// </summary>
public class CommandsResourceTests
{
    private readonly Mock<IDebugEngine> m_MockDebugEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandsResourceTests"/> class.
    /// </summary>
    public CommandsResourceTests()
    {
        m_MockDebugEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        _ = services.AddSingleton(m_MockDebugEngine.Object);
        _ = services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that Commands returns empty list when no commands exist.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Commands_ReturnsEmptyList()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().NotBeNullOrEmpty();

        var json = JsonDocument.Parse(result);
        _ = json.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        _ = json.RootElement.GetProperty("commands").GetArrayLength().Should().Be(0);
        _ = json.RootElement.GetProperty("note").GetString().Should().Contain("IDebugEngine");
    }

    /// <summary>
    /// Verifies that Commands includes timestamp in response.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Commands_IncludesTimestamp()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        var json = JsonDocument.Parse(result);
        _ = json.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Commands returns valid JSON.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Commands_ReturnsValidJson()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        var action = () => JsonDocument.Parse(result);
        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Commands returns error response when exception occurs.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Commands_WithException_ReturnsErrorResponse()
    {
        // Create a service provider that throws when getting ILoggerFactory
        var mockServiceProvider = new Mock<IServiceProvider>();
        _ = mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Throws(new InvalidOperationException("Test error"));

        var result = await CommandsResource.Commands(mockServiceProvider.Object);

        var json = JsonDocument.Parse(result);
        _ = json.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        _ = json.RootElement.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        _ = errorProperty.GetString().Should().Contain("Test error");
    }

    /// <summary>
    /// Verifies that Commands throws exception when service provider is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Commands_WithNullServiceProvider_ThrowsException()
    {
        var action = async () => await CommandsResource.Commands(null!);
        _ = await action.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that Commands JSON format is indented.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Commands_JsonFormat_IsIndented()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("\n"); // Indented JSON contains newlines
    }
}
