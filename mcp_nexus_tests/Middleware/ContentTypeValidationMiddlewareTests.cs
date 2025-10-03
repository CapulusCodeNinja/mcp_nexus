using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;
using mcp_nexus.Middleware;

namespace mcp_nexus_tests.Middleware
{
    /// <summary>
    /// Tests for ContentTypeValidationMiddleware
    /// </summary>
    public class ContentTypeValidationMiddlewareTests
    {
        private readonly Mock<ILogger<ContentTypeValidationMiddleware>> m_MockLogger;
        private readonly Mock<RequestDelegate> m_MockNext;

        public ContentTypeValidationMiddlewareTests()
        {
            m_MockLogger = new Mock<ILogger<ContentTypeValidationMiddleware>>();
            m_MockNext = new Mock<RequestDelegate>();
        }

        [Fact]
        public void ContentTypeValidationMiddleware_Class_Exists()
        {
            // This test verifies that the ContentTypeValidationMiddleware class exists and can be instantiated
            Assert.True(typeof(ContentTypeValidationMiddleware) != null);
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public void Constructor_WithNullNext_CreatesInstance()
        {
            // Act
            var middleware = new ContentTypeValidationMiddleware(null!, m_MockLogger.Object);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public void Constructor_WithNullLogger_CreatesInstance()
        {
            // Act
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, null!);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public async Task InvokeAsync_WithValidJsonContentType_CallsNext()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "POST", "application/json");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(next => next(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithValidJsonContentTypeWithCharset_CallsNext()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "POST", "application/json; charset=utf-8");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(next => next(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidContentType_Returns400AndDoesNotCallNext()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "POST", "text/plain");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(next => next(context), Times.Never);
            Assert.Equal(400, context.Response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithNullContentType_Returns400AndDoesNotCallNext()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "POST", null);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(next => next(context), Times.Never);
            Assert.Equal(400, context.Response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithEmptyContentType_Returns400AndDoesNotCallNext()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "POST", "");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(next => next(context), Times.Never);
            Assert.Equal(400, context.Response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithNonRootPath_CallsNext()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/api/test", "POST", "text/plain");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(next => next(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithNonPostMethod_CallsNext()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "GET", "text/plain");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(next => next(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidContentType_LogsWarning()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "POST", "text/plain");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid Content-Type received: text/plain")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidContentType_ReturnsJsonRpcErrorResponse()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "POST", "text/plain");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();

            Assert.Contains("\"jsonrpc\":\"2.0\"", responseBody);
            Assert.Contains("\"error\"", responseBody);
            Assert.Contains("\"code\":-32700", responseBody);
            Assert.Contains("\"message\":\"Parse error\"", responseBody);
            Assert.Contains("\"data\":\"Content-Type must be application/json\"", responseBody);
        }

        [Fact]
        public async Task InvokeAsync_WithValidContentType_DoesNotLogWarning()
        {
            // Arrange
            var middleware = new ContentTypeValidationMiddleware(m_MockNext.Object, m_MockLogger.Object);
            var context = CreateHttpContext("/", "POST", "application/json");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        private static HttpContext CreateHttpContext(string path, string method, string? contentType)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = method;
            context.Request.ContentType = contentType;
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
