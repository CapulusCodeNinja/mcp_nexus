using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mcp_nexus.Protocol.Middleware;

namespace mcp_nexus.Protocol.Tests.Middleware;

/// <summary>
/// Unit tests for ContentTypeValidationMiddleware class.
/// Tests content-type header validation for JSON-RPC requests.
/// </summary>
public class ContentTypeValidationMiddlewareTests
{
    private readonly ContentTypeValidationMiddleware m_Middleware;
    private readonly RequestDelegate m_NextDelegate;
    private bool m_NextCalled;

    public ContentTypeValidationMiddlewareTests()
    {
        var logger = NullLogger<ContentTypeValidationMiddleware>.Instance;
        m_NextCalled = false;
        m_NextDelegate = (HttpContext context) =>
        {
            m_NextCalled = true;
            return Task.CompletedTask;
        };
        m_Middleware = new ContentTypeValidationMiddleware(m_NextDelegate, logger);
    }

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        var logger = NullLogger<ContentTypeValidationMiddleware>.Instance;

        var action = () => new ContentTypeValidationMiddleware(null!, logger);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new ContentTypeValidationMiddleware(m_NextDelegate, null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task InvokeAsync_WithGetRequest_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithPostAndApplicationJson_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithPostAndApplicationJsonWithCharset_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json; charset=utf-8";

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithPostToRootAndInvalidContentType_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "text/plain";
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WithPostToRootAndNullContentType_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = null;
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WithPostToRootAndEmptyContentType_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "";
        context.Response.Body = new MemoryStream();

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WithPutRequest_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "PUT";

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }
}

