using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using Nexus.Protocol.Middleware;

using Xunit;

namespace Nexus.Protocol.Unittests.Middleware;

/// <summary>
/// Unit tests for ResponseFormattingMiddleware class.
/// Tests JSON-RPC response formatting and error handling.
/// </summary>
public class ResponseFormattingMiddlewareTests
{
    private readonly ResponseFormattingMiddleware m_Middleware;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseFormattingMiddlewareTests"/> class.
    /// </summary>
    public ResponseFormattingMiddlewareTests()
    {
        var logger = NullLogger<ResponseFormattingMiddleware>.Instance;
        m_Middleware = new ResponseFormattingMiddleware((HttpContext context) => Task.CompletedTask);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when next delegate is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        var logger = NullLogger<ResponseFormattingMiddleware>.Instance;

        var action = () => new ResponseFormattingMiddleware(null!);

        _ = action.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with normal execution.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InvokeAsync_WithNormalExecution_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext context) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new ResponseFormattingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync returns error response when exception occurs.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InvokeAsync_WithException_ReturnsErrorResponse()
    {
        RequestDelegate next = (HttpContext context) => throw new InvalidOperationException("Test error");

        var middleware = new ResponseFormattingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        _ = context.Response.StatusCode.Should().Be(500);
        _ = context.Response.ContentType.Should().StartWith("application/json");

        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        _ = responseBody.Should().Contain("Test error");
        _ = responseBody.Should().Contain("\"jsonrpc\":\"2.0\"");
    }

    /// <summary>
    /// Verifies that InvokeAsync returns 500 with ArgumentException.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InvokeAsync_WithArgumentException_Returns500()
    {
        RequestDelegate next = (HttpContext context) => throw new ArgumentException("Invalid argument");
        var middleware = new ResponseFormattingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        _ = context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        _ = responseBody.Should().Contain("Invalid argument");
    }

    /// <summary>
    /// Verifies that InvokeAsync returns 500 with ArgumentNullException.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InvokeAsync_WithArgumentNullException_Returns500()
    {
        RequestDelegate next = (HttpContext context) => throw new ArgumentNullException("param");
        var middleware = new ResponseFormattingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        _ = context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        _ = responseBody.Should().Contain("Value cannot be null");
    }

    /// <summary>
    /// Verifies that InvokeAsync returns 500 with FileNotFoundException.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InvokeAsync_WithFileNotFoundException_Returns500()
    {
        RequestDelegate next = (HttpContext context) => throw new FileNotFoundException("File not found");
        var middleware = new ResponseFormattingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        _ = context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        _ = responseBody.Should().Contain("File not found");
    }
}
