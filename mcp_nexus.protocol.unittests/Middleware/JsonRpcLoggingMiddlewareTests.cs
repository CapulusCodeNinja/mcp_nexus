using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mcp_nexus.Protocol.Middleware;
using System.Text;

namespace mcp_nexus.Protocol.Tests.Middleware;

/// <summary>
/// Unit tests for JsonRpcLoggingMiddleware class.
/// Tests JSON-RPC request/response logging.
/// </summary>
public class JsonRpcLoggingMiddlewareTests
{
    private readonly JsonRpcLoggingMiddleware m_Middleware;
    private readonly RequestDelegate m_NextDelegate;
    private bool m_NextCalled;

    public JsonRpcLoggingMiddlewareTests()
    {
        var logger = NullLogger<JsonRpcLoggingMiddleware>.Instance;
        m_NextCalled = false;
        m_NextDelegate = (HttpContext context) =>
        {
            m_NextCalled = true;
            return Task.CompletedTask;
        };
        m_Middleware = new JsonRpcLoggingMiddleware(m_NextDelegate, logger);
    }

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        var logger = NullLogger<JsonRpcLoggingMiddleware>.Instance;

        var action = () => new JsonRpcLoggingMiddleware(null!, logger);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new JsonRpcLoggingMiddleware(m_NextDelegate, null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task InvokeAsync_WithValidRequest_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/mcp";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"method\":\"test\"}"));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithGetRequest_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/mcp";
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithNonSeekableRequestBody_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/mcp";
        context.Request.Body = new NonSeekableStream();
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithLargeRequestBody_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/mcp";
        var largeBody = new string('x', 11000); // Over 10KB limit
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(largeBody));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Helper class to simulate a non-seekable stream.
    /// </summary>
    private class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }
}

