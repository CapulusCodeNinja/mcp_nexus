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
/// Unit tests for SessionsResource class.
/// Tests session listing resource with mocked dependencies.
/// </summary>
public class SessionsResourceTests
{
    private readonly Mock<IDebugEngine> m_MockDebugEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsResourceTests"/> class.
    /// </summary>
    public SessionsResourceTests()
    {
        m_MockDebugEngine = new Mock<IDebugEngine>();

        var services = new ServiceCollection();
        _ = services.AddSingleton(m_MockDebugEngine.Object);
        _ = services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that Sessions returns empty list when no sessions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_ReturnsEmptyList()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        _ = result.Should().NotBeNullOrEmpty();

        var json = JsonDocument.Parse(result);
        _ = json.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        _ = json.RootElement.GetProperty("sessions").GetArrayLength().Should().Be(0);
        _ = json.RootElement.GetProperty("note").GetString().Should().Contain("IDebugEngine");
    }

    /// <summary>
    /// Verifies that Sessions includes timestamp in response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_IncludesTimestamp()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        var json = JsonDocument.Parse(result);
        _ = json.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Sessions returns valid JSON.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_ReturnsValidJson()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        var action = () => JsonDocument.Parse(result);
        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Sessions returns error response when exception occurs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_WithException_ReturnsErrorResponse()
    {
        // Create a service provider that throws when getting ILoggerFactory
        var mockServiceProvider = new Mock<IServiceProvider>();
        _ = mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Throws(new InvalidOperationException("Test error"));

        var result = await SessionsResource.Sessions(mockServiceProvider.Object);

        var json = JsonDocument.Parse(result);
        _ = json.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        _ = json.RootElement.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        _ = errorProperty.GetString().Should().Contain("Test error");
    }

    /// <summary>
    /// Verifies that Sessions throws exception when service provider is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_WithNullServiceProvider_ThrowsException()
    {
        var action = async () => await SessionsResource.Sessions(null!);
        _ = await action.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that Sessions JSON format is indented.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Sessions_JsonFormat_IsIndented()
    {
        var result = await SessionsResource.Sessions(m_ServiceProvider);

        _ = result.Should().Contain("\n"); // Indented JSON contains newlines
    }
}
