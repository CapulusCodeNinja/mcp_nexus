using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mcp_nexus.Protocol.Middleware;
using System.Text;

namespace mcp_nexus.Protocol.Tests.Middleware;

/// <summary>
/// Unit tests for ResponseFormattingMiddleware class.
/// Tests JSON-RPC response formatting and error handling.
/// </summary>
public class ResponseFormattingMiddlewareTests
{
    private readonly ResponseFormattingMiddleware m_Middleware;

    public ResponseFormattingMiddlewareTests()
    {
        var logger = NullLogger<ResponseFormattingMiddleware>.Instance;
        m_Middleware = new ResponseFormattingMiddleware((HttpContext context) => Task.CompletedTask, logger);
    }

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        var logger = NullLogger<ResponseFormattingMiddleware>.Instance;

        var action = () => new ResponseFormattingMiddleware(null!, logger);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        RequestDelegate next = (HttpContext context) => Task.CompletedTask;

        var action = () => new ResponseFormattingMiddleware(next, null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task InvokeAsync_WithNormalExecution_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext context) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new ResponseFormattingMiddleware(next, NullLogger<ResponseFormattingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithException_ReturnsErrorResponse()
    {
        RequestDelegate next = (HttpContext context) =>
        {
            throw new InvalidOperationException("Test error");
        };
        var middleware = new ResponseFormattingMiddleware(next, NullLogger<ResponseFormattingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().StartWith("application/json");

        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("Test error");
        responseBody.Should().Contain("\"jsonrpc\":\"2.0\"");
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_Returns500()
    {
        RequestDelegate next = (HttpContext context) =>
        {
            throw new ArgumentException("Invalid argument");
        };
        var middleware = new ResponseFormattingMiddleware(next, NullLogger<ResponseFormattingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("Invalid argument");
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentNullException_Returns500()
    {
        RequestDelegate next = (HttpContext context) =>
        {
            throw new ArgumentNullException("param", "Parameter is null");
        };
        var middleware = new ResponseFormattingMiddleware(next, NullLogger<ResponseFormattingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("Parameter is null");
    }

    [Fact]
    public async Task InvokeAsync_WithFileNotFoundException_Returns500()
    {
        RequestDelegate next = (HttpContext context) =>
        {
            throw new FileNotFoundException("File not found");
        };
        var middleware = new ResponseFormattingMiddleware(next, NullLogger<ResponseFormattingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("File not found");
    }
}

