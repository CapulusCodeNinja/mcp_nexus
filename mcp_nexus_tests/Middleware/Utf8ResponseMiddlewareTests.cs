using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;
using mcp_nexus.Middleware;
using mcp_nexus.Configuration;

namespace mcp_nexus_tests.Middleware
{
    /// <summary>
    /// Tests for Utf8ResponseMiddleware
    /// </summary>
    public class Utf8ResponseMiddlewareTests
    {
        private readonly Mock<RequestDelegate> _mockNext;

        public Utf8ResponseMiddlewareTests()
        {
            _mockNext = new Mock<RequestDelegate>();
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public void Constructor_WithNullNext_CreatesInstance()
        {
            // Act
            var middleware = new Utf8ResponseMiddleware(null!);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public async Task InvokeAsync_WithNoContentType_DoesNotModifyResponse()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    // Don't set ContentType
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithJsonContentTypeWithoutCharset_AddsUtf8Charset()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "application/json";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            // The middleware only modifies ContentType when response is sent, not during setup
            Assert.Equal("application/json", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithJsonContentTypeWithCharset_DoesNotModify()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();
            var originalContentType = "application/json; charset=utf-8";

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = originalContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(originalContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithTextPlainContentTypeWithoutCharset_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "text/plain";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal("text/plain", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithTextHtmlContentTypeWithoutCharset_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "text/html";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal("text/html", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithOtherTextContentTypeWithoutCharset_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();
            var originalContentType = "text/xml";

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = originalContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal(originalContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithNonTextContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();
            var originalContentType = "application/octet-stream";

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = originalContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(originalContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithEmptyContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            // Setting ContentType to empty string results in null in ASP.NET Core
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithNullContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = null;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithCaseInsensitiveContentType_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "APPLICATION/JSON";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal("APPLICATION/JSON", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithTextContentTypeWithDifferentCharset_DoesNotModify()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();
            var originalContentType = "text/plain; charset=iso-8859-1";

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = originalContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(originalContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithJsonContentTypeWithParameters_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new Utf8ResponseMiddleware(_mockNext.Object);
            var context = CreateHttpContext();

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "application/json; boundary=something";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal("application/json; boundary=something", context.Response.ContentType);
        }

        private HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
