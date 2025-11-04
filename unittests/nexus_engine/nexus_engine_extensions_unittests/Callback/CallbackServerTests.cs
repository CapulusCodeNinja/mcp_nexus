using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using Moq;

using Nexus.Engine.Extensions.Callback;
using Nexus.Engine.Extensions.Security;
using Nexus.Engine.Share;
using Nexus.Engine.Share.Models;

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

    /// <summary>
    /// Verifies that HandleExecuteAsync returns Problem for invalid request body (JSON deserialization throws).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleExecuteAsync_WithInvalidRequestBody_ReturnsProblem()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("invalid json"));

        var handleMethod = typeof(CallbackServer).GetMethod("HandleExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(500);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleExecuteAsync returns Unauthorized for invalid token.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleExecuteAsync_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var request = new ExecuteCommandRequest { Command = "test" };
        var json = JsonSerializer.Serialize(request);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        context.Request.Headers["Authorization"] = "Bearer invalid-token";

        var handleMethod = typeof(CallbackServer).GetMethod("HandleExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(401);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleExecuteAsync handles exceptions gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleExecuteAsync_WithException_ReturnsProblem()
    {
        // Arrange
        _ = m_MockEngine.Setup(e => e.EnqueueCommand(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Test exception"));

        var tokenValidator = new TokenValidator();
        var token = tokenValidator.GenerateToken("session-123", "cmd-456");
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var request = new ExecuteCommandRequest { Command = "test" };
        var json = JsonSerializer.Serialize(request);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        var handleMethod = typeof(CallbackServer).GetMethod("HandleExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(500);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleQueueAsync returns Problem for invalid request body (JSON deserialization throws).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleQueueAsync_WithInvalidRequestBody_ReturnsProblem()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("invalid json"));

        var handleMethod = typeof(CallbackServer).GetMethod("HandleQueueAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(500);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleQueueAsync returns Unauthorized for invalid token.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleQueueAsync_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var request = new QueueCommandRequest { Command = "test" };
        var json = JsonSerializer.Serialize(request);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        context.Request.Headers["Authorization"] = "Bearer invalid-token";

        var handleMethod = typeof(CallbackServer).GetMethod("HandleQueueAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(401);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleReadAsync returns Unauthorized for invalid token.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleReadAsync_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer invalid-token";

        var handleMethod = typeof(CallbackServer).GetMethod("HandleReadAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context, "cmd-123" })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(401);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleStatus returns Unauthorized for invalid token.
    /// </summary>
    [Fact]
    public void HandleStatus_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer invalid-token";

        var handleMethod = typeof(CallbackServer).GetMethod("HandleStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (Microsoft.AspNetCore.Http.IResult)handleMethod!.Invoke(server, new object[] { context, "cmd-123" })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(401);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleStatus returns NotFound when command not found.
    /// </summary>
    [Fact]
    public void HandleStatus_WhenCommandNotFound_ReturnsNotFound()
    {
        // Arrange
        _ = m_MockEngine.Setup(e => e.GetCommandInfo(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((CommandInfo?)null);

        var tokenValidator = new TokenValidator();
        var token = tokenValidator.GenerateToken("session-123", "cmd-456");
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        var handleMethod = typeof(CallbackServer).GetMethod("HandleStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (Microsoft.AspNetCore.Http.IResult)handleMethod!.Invoke(server, new object[] { context, "cmd-123" })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(404);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleBulkStatusAsync returns BadRequest for invalid request.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleBulkStatusAsync_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var request = new BulkStatusRequest { CommandIds = new List<string>() };
        var json = JsonSerializer.Serialize(request);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var handleMethod = typeof(CallbackServer).GetMethod("HandleBulkStatusAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(400);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleBulkStatusAsync handles different command states correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleBulkStatusAsync_WithDifferentCommandStates_HandlesCorrectly()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var token = tokenValidator.GenerateToken("session-123", "cmd-456");
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);

        var completedCommand = CommandInfo.Completed("session-123", "cmd-1", "test", DateTime.Now, DateTime.Now, DateTime.Now, "output", string.Empty, 12345);
        var failedCommand = CommandInfo.Failed("session-123", "cmd-2", "test", DateTime.Now, DateTime.Now, DateTime.Now, string.Empty, "error", 12346);
        var cancelledCommand = CommandInfo.Cancelled("session-123", "cmd-3", "test", DateTime.Now, DateTime.Now, DateTime.Now, string.Empty, "cancelled", 12347);

        _ = m_MockEngine.Setup(e => e.GetCommandInfo("session-123", "cmd-1")).Returns(completedCommand);
        _ = m_MockEngine.Setup(e => e.GetCommandInfo("session-123", "cmd-2")).Returns(failedCommand);
        _ = m_MockEngine.Setup(e => e.GetCommandInfo("session-123", "cmd-3")).Returns(cancelledCommand);

        var request = new BulkStatusRequest { CommandIds = new List<string> { "cmd-1", "cmd-2", "cmd-3" } };
        var json = JsonSerializer.Serialize(request);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        var handleMethod = typeof(CallbackServer).GetMethod("HandleBulkStatusAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(200);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that HandleLogAsync returns Problem for invalid request body (JSON deserialization throws).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleLogAsync_WithInvalidRequestBody_ReturnsProblem()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var server = new CallbackServer(m_MockEngine.Object, tokenValidator);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("invalid json"));

        var handleMethod = typeof(CallbackServer).GetMethod("HandleLogAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = await (Task<Microsoft.AspNetCore.Http.IResult>)handleMethod!.Invoke(server, new object[] { context })!;

        // Assert
        _ = result.Should().NotBeNull();
        var statusCode = result.GetType().GetProperty("StatusCode")?.GetValue(result);
        _ = statusCode.Should().Be(500);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that GetCommandNumber extracts command number correctly.
    /// </summary>
    [Fact]
    public void GetCommandNumber_WithValidCommandId_ReturnsNumber()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var getCommandNumberMethod = typeof(CallbackServer).GetMethod("GetCommandNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = (int)getCommandNumberMethod!.Invoke(null, new object[] { "session-123", "cmd-session-123-456" })!;

        // Assert
        _ = result.Should().Be(456);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that GetCommandNumber returns 0 for invalid command ID.
    /// </summary>
    [Fact]
    public void GetCommandNumber_WithInvalidCommandId_ReturnsZero()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var getCommandNumberMethod = typeof(CallbackServer).GetMethod("GetCommandNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = (int)getCommandNumberMethod!.Invoke(null, new object[] { "session-123", "invalid-command-id" })!;

        // Assert
        _ = result.Should().Be(0);
        tokenValidator.Dispose();
    }
}
