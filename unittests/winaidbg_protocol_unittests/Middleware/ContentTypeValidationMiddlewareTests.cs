using FluentAssertions;

using Microsoft.AspNetCore.Http;

using WinAiDbg.Protocol.Middleware;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Middleware;

/// <summary>
/// Unit tests for ContentTypeValidationMiddleware class.
/// Tests content-type validation logic for JSON-RPC requests.
/// </summary>
public class ContentTypeValidationMiddlewareTests
{
    /// <summary>
    /// Verifies that constructor throws ArgumentNullException for null next delegate.
    /// </summary>
    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new ContentTypeValidationMiddleware(null!));
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware for valid content type.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithValidContentType_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "application/json";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync rejects request with missing content type.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithMissingContentType_Returns400()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = null;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeFalse();
        _ = context.Response.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Verifies that InvokeAsync rejects request with wrong content type.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithWrongContentType_Returns400()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "text/plain";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeFalse();
        _ = context.Response.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Verifies that InvokeAsync accepts application/json with charset.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithApplicationJsonAndCharset_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "application/json; charset=utf-8";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync is case-insensitive for content type.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithMixedCaseContentType_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "APPLICATION/JSON";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync skips validation for GET requests.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithGetRequest_SkipsValidation()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/";
        context.Request.ContentType = null;

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should call next even with null content type
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync skips validation for non-root paths.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithNonRootPath_SkipsValidation()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/health";
        context.Request.ContentType = null;

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should call next even with null content type
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that invalid content type response includes error details.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithInvalidContentType_ReturnsJsonRpcError()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => Task.CompletedTask;
        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "text/xml";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        _ = responseBody.Should().Contain("jsonrpc");
        _ = responseBody.Should().Contain("-32700"); // Parse error code
        _ = responseBody.Should().Contain("Content-Type must be application/json");
    }

    /// <summary>
    /// Verifies that invalid content type response sets correct content type.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithInvalidContentType_SetsJsonResponseContentType()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => Task.CompletedTask;
        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "text/plain";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.ContentType.Should().Be("application/json; charset=utf-8");
    }

    /// <summary>
    /// Verifies that empty content type is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithEmptyContentType_Returns400()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => Task.CompletedTask;
        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = string.Empty;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Verifies that whitespace content type is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithWhitespaceContentType_Returns400()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => Task.CompletedTask;
        var middleware = new ContentTypeValidationMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.ContentType = "   ";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.StatusCode.Should().Be(400);
    }
}
