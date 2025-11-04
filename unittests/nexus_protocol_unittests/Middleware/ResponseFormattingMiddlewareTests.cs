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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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

    /// <summary>
    /// Verifies that InvokeAsync handles OperationCanceledException gracefully without sending error response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WithOperationCanceledException_DoesNotSendErrorResponse()
    {
        RequestDelegate next = (HttpContext context) => throw new OperationCanceledException();
        var middleware = new ResponseFormattingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        // Should not have written to response body
        _ = context.Response.StatusCode.Should().Be(200); // Default status code
        context.Response.Body.Position = 0;
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        _ = responseBody.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that InvokeAsync handles exception when writing to response fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InvokeAsync_WhenWritingResponseFails_HandlesGracefully()
    {
        RequestDelegate next = (HttpContext context) => throw new InvalidOperationException("Test error");
        var middleware = new ResponseFormattingMiddleware(next);
        var context = new DefaultHttpContext();
        var throwingStream = new ThrowingMemoryStream();
        context.Response.Body = throwingStream;

        // Should not throw even if writing fails
        await middleware.InvokeAsync(context);
    }

    /// <summary>
    /// Memory stream that throws exception on write.
    /// </summary>
    private class ThrowingMemoryStream : MemoryStream
    {
        /// <summary>
        /// Writes to the stream, throwing exception after first write.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Position > 0)
            {
                throw new IOException("Write failed");
            }

            base.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes to the stream asynchronously, throwing exception after first write.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The write task.</returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Position > 0 ? throw new IOException("Write failed") : base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}
