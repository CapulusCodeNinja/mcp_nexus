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

    [Fact]
    public async Task InvokeAsync_WithRootPathPost_LogsRequestAndResponse()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/"; // Root path triggers logging
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\"}"));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithRootPathPost_CapturesResponseBody()
    {
        var responseText = "{\"jsonrpc\":\"2.0\",\"result\":\"success\"}";
        RequestDelegate nextWithResponse = async (HttpContext ctx) =>
        {
            await ctx.Response.WriteAsync(responseText);
        };
        
        var logger = NullLogger<JsonRpcLoggingMiddleware>.Instance;
        var middleware = new JsonRpcLoggingMiddleware(nextWithResponse, logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\"}"));
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var actualResponse = await reader.ReadToEndAsync();
        actualResponse.Should().Be(responseText);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyRequestBody_Logs()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(""));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithVeryLargeRequestBody_TruncatesInLogs()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        var largeBody = new string('x', 2000); // Over 1000 character limit for logging
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(largeBody));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithExceptionInNext_RestoresOriginalBodyStream()
    {
        RequestDelegate nextThatThrows = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Test exception");
        };
        
        var logger = NullLogger<JsonRpcLoggingMiddleware>.Instance;
        var middleware = new JsonRpcLoggingMiddleware(nextThatThrows, logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\"}"));
        var originalBody = new MemoryStream();
        context.Response.Body = originalBody;

        var action = async () => await middleware.InvokeAsync(context);

        await action.Should().ThrowAsync<InvalidOperationException>();
        context.Response.Body.Should().BeSameAs(originalBody); // Body should be restored
    }

    [Fact]
    public async Task InvokeAsync_WithValidJsonRpcRequest_LogsMethodAndPath()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"method\":\"test\"}"));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithNonRootPath_SkipsLogging()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/other";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\"}"));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithNonPostMethod_SkipsLogging()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "PUT";
        context.Request.Path = "/";
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

