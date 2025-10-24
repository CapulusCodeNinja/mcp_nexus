using Microsoft.AspNetCore.Http;

using Nexus.Protocol.Middleware;

namespace Nexus.Protocol.Unittests.Middleware;

/// <summary>
/// Unit tests for JsonRpcLoggingMiddleware class.
/// Tests request/response logging and sanitization logic.
/// </summary>
public class JsonRpcLoggingMiddlewareTests
{
    /// <summary>
    /// Verifies that constructor throws ArgumentNullException for null next delegate.
    /// </summary>
    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new JsonRpcLoggingMiddleware(null!));
    }

    /// <summary>
    /// Verifies that InvokeAsync calls next middleware for root POST.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithRootPostRequest_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync skips logging for GET requests.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithGetRequest_SkipsLogging()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync skips logging for non-root paths.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithNonRootPath_SkipsLogging()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/health";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync copies response body correctly.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_CopiesResponseBodyToOriginalStream()
    {
        // Arrange
        const string responseText = "test response";
        RequestDelegate next = async (HttpContext ctx) => await ctx.Response.WriteAsync(responseText);

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        responseBody.Position = 0;
        var reader = new StreamReader(responseBody);
        var actualResponse = await reader.ReadToEndAsync();
        _ = actualResponse.Should().Be(responseText);
    }

    /// <summary>
    /// Verifies that InvokeAsync handles empty request body.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithEmptyRequestBody_HandlesGracefully()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream(); // Empty body
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync reads and preserves request body.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_PreservesRequestBodyForNextMiddleware()
    {
        // Arrange
        const string requestBody = "{\"jsonrpc\":\"2.0\",\"method\":\"test\"}";
        var capturedBody = string.Empty;

        RequestDelegate next = async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body);
            capturedBody = await reader.ReadToEndAsync();
        };

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";

        var requestStream = new MemoryStream();
        var writer = new StreamWriter(requestStream);
        await writer.WriteAsync(requestBody);
        await writer.FlushAsync();
        requestStream.Position = 0;
        context.Request.Body = requestStream;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = capturedBody.Should().Be(requestBody);
    }

    /// <summary>
    /// Verifies that InvokeAsync handles PUT method by skipping logging.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithPutMethod_SkipsLogging()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "PUT";
        context.Request.Path = "/";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync restores original response stream.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_RestoresOriginalResponseStream()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => Task.CompletedTask;
        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream();

        var originalStream = new MemoryStream();
        context.Response.Body = originalStream;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.Body.Should().BeSameAs(originalStream);
    }

    /// <summary>
    /// Verifies that InvokeAsync handles large request bodies.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithLargeRequestBody_HandlesCorrectly()
    {
        // Arrange
        var largeBody = new string('x', 2000); // Larger than 1000 char truncation limit
        var nextCalled = false;

        RequestDelegate next = (HttpContext _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";

        var requestStream = new MemoryStream();
        var writer = new StreamWriter(requestStream);
        await writer.WriteAsync(largeBody);
        await writer.FlushAsync();
        requestStream.Position = 0;
        context.Request.Body = requestStream;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that InvokeAsync handles exceptions from next middleware.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenNextMiddlewareThrows_PropagatesException()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => throw new InvalidOperationException("Test error");
        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await middleware.InvokeAsync(context));
    }

    /// <summary>
    /// Verifies that InvokeAsync with POST to "/" enables buffering.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithPostToRoot_EnablesRequestBuffering()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => Task.CompletedTask;
        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Request body position should be reset to 0 after reading
        _ = context.Request.Body.Position.Should().Be(0);
    }
}
