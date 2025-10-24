using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using nexus.protocol.Middleware;

namespace nexus.protocol.unittests.Middleware;

/// <summary>
/// Unit tests for ContentTypeValidationMiddleware class.
/// Tests content-type header validation for JSON-RPC requests.
/// </summary>
public class ContentTypeValidationMiddlewareTests
{
    private readonly ContentTypeValidationMiddleware m_Middleware;
    private readonly RequestDelegate m_NextDelegate;
    private bool m_NextCalled;

    /// <summary>
    /// Initializes a new instance of the ContentTypeValidationMiddlewareTests class.
    /// </summary>
    public ContentTypeValidationMiddlewareTests()
    {
        var logger = NullLogger<ContentTypeValidationMiddleware>.Instance;
        m_NextCalled = false;
        m_NextDelegate = (HttpContext context) =>
        {
            m_NextCalled = true;
            return Task.CompletedTask;
        };
        m_Middleware = new ContentTypeValidationMiddleware(m_NextDelegate);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when next delegate is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        var logger = NullLogger<ContentTypeValidationMiddleware>.Instance;

        var action = () => new ContentTypeValidationMiddleware(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new ContentTypeValidationMiddleware(m_NextDelegate);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with GET request.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithGetRequest_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with POST and application/json content type.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithPostAndApplicationJson_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with POST and application/json with charset.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithPostAndApplicationJsonWithCharset_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json; charset=utf-8";

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync returns 400 Bad Request with POST to root and invalid content type.
    /// </summary>
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

    /// <summary>
    /// Verifies that InvokeAsync returns 400 Bad Request with POST to root and null content type.
    /// </summary>
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

    /// <summary>
    /// Verifies that InvokeAsync returns 400 Bad Request with POST to root and empty content type.
    /// </summary>
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

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware with PUT request.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithPutRequest_CallsNext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "PUT";

        await m_Middleware.InvokeAsync(context);

        m_NextCalled.Should().BeTrue();
    }
}
