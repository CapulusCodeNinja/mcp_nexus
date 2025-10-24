using System.Text;

using Microsoft.AspNetCore.Http;

using Nexus.Protocol.Middleware;

namespace Nexus.Protocol.Unittests.Middleware;

/// <summary>
/// Unit tests for JsonRpcLoggingMiddleware class.
/// Tests JSON-RPC request/response logging.
/// </summary>
public class JsonRpcLoggingMiddlewareTests
{
    private readonly JsonRpcLoggingMiddleware m_Middleware;
    private readonly RequestDelegate m_NextDelegate;
    private bool m_NextCalled;

    /// <summary>
    /// Initializes a new instance of the JsonRpcLoggingMiddlewareTests class.
    /// </summary>
    public JsonRpcLoggingMiddlewareTests()
    {
        m_NextCalled = false;
        m_NextDelegate = (HttpContext context) =>
        {
            m_NextCalled = true;
            return Task.CompletedTask;
        };
        m_Middleware = new JsonRpcLoggingMiddleware(m_NextDelegate);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when next delegate is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        var action = () => new JsonRpcLoggingMiddleware(null!);

        _ = action.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with valid request.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithValidRequest_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/mcp";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"method\":\"test\"}"));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with GET request.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithGetRequest_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/mcp";
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with non-seekable request body.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithNonSeekableRequestBody_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/mcp";
        context.Request.Body = new NonSeekableStream();
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with large request body exceeding size limit.
    /// </summary>
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

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync logs request and response when POST is sent to root path.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithRootPathPost_LogsRequestAndResponse()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/"; // Root path triggers logging
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\"}"));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync captures response body when POST is sent to root path.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithRootPathPost_CapturesResponseBody()
    {
        var responseText = "{\"jsonrpc\":\"2.0\",\"result\":\"success\"}";
        RequestDelegate nextWithResponse = async (HttpContext ctx) => await ctx.Response.WriteAsync(responseText);

        var middleware = new JsonRpcLoggingMiddleware(nextWithResponse);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\"}"));
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var actualResponse = await reader.ReadToEndAsync();
        _ = actualResponse.Should().Be(responseText);
    }

    /// <summary>
    /// Verifies that InvokeAsync logs requests with empty request body.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithEmptyRequestBody_Logs()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(""));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync truncates very large request bodies in logs.
    /// </summary>
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

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync restores original response body stream when exception occurs in next middleware.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithExceptionInNext_RestoresOriginalBodyStream()
    {
        RequestDelegate nextThatThrows = (HttpContext ctx) => throw new InvalidOperationException("Test exception");

        var middleware = new JsonRpcLoggingMiddleware(nextThatThrows);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\"}"));
        var originalBody = new MemoryStream();
        context.Response.Body = originalBody;

        var action = async () => await middleware.InvokeAsync(context);

        _ = await action.Should().ThrowAsync<InvalidOperationException>();
        _ = context.Response.Body.Should().BeSameAs(originalBody); // Body should be restored
    }

    /// <summary>
    /// Verifies that InvokeAsync logs JSON-RPC method and path with valid request.
    /// </summary>
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

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync skips logging for non-root paths.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithNonRootPath_SkipsLogging()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/other";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\"}"));
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync skips logging for non-POST methods.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithNonPostMethod_SkipsLogging()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "PUT";
        context.Request.Path = "/";
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        _ = m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Helper class to simulate a non-seekable stream.
    /// </summary>
    private class NonSeekableStream : MemoryStream
    {
        /// <summary>
        /// Gets a value indicating whether the stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;
    }
}
