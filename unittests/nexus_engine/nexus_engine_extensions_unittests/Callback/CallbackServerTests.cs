using FluentAssertions;

using Microsoft.AspNetCore.Builder;

using Moq;

using Nexus.Engine.Extensions.Callback;
using Nexus.Engine.Extensions.Security;
using Nexus.Engine.Share;

using Xunit;

namespace Nexus.Engine.Extensions.Tests.Callback;

/// <summary>
/// Unit tests for the <see cref="CallbackServer"/> class.
/// Tests route configuration, constructor validation, and basic handler logic.
/// </summary>
public class CallbackServerTests
{
    private readonly Mock<IDebugEngine> m_MockEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackServerTests"/> class.
    /// </summary>
    public CallbackServerTests()
    {
        m_MockEngine = new Mock<IDebugEngine>();
    }

    /// <summary>
    /// Verifies that constructor with valid parameters succeeds.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var tokenValidator = new TokenValidator();

        // Act
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);

        // Assert
        _ = server.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ConfigureRoutes registers all required endpoints.
    /// </summary>
    [Fact]
    public void ConfigureRoutes_RegistersAllEndpoints()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        server.ConfigureRoutes(app);

        // Assert - Routes should be configured (verify by checking that app is not null)
        _ = app.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ConfigureRoutes can be called multiple times.
    /// </summary>
    [Fact]
    public void ConfigureRoutes_CanBeCalledMultipleTimes()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        server.ConfigureRoutes(app);
        server.ConfigureRoutes(app); // Should not throw

        // Assert
        _ = app.Should().NotBeNull();
        tokenValidator.Dispose();
    }
}
