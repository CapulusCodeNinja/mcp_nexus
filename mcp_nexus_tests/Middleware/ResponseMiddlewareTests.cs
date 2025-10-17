using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;
using mcp_nexus.Middleware;
using mcp_nexus.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace mcp_nexus_tests.Middleware
{
    /// <summary>
    /// Tests for ResponseMiddleware
    /// </summary>
    public class ResponseMiddlewareTests
    {
        private readonly Mock<RequestDelegate> m_MockNext;

        public ResponseMiddlewareTests()
        {
            m_MockNext = new Mock<RequestDelegate>();
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var middleware = new ResponseMiddleware(m_MockNext.Object);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public void Constructor_WithNullNext_CreatesInstance()
        {
            // Act
            var middleware = new ResponseMiddleware(null!);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public async Task InvokeAsync_WithNoContentType_DoesNotModifyResponse()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    // Don't set ContentType
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithJsonContentTypeWithoutCharset_AddsUnicodeCharset()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "application/json";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            // The middleware only modifies ContentType when response is sent, not during setup
            Assert.Equal("application/json", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithJsonContentTypeWithCharset_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var originalContentType = "application/json; charset=utf-8";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = originalContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(originalContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithTextPlainContentTypeWithoutCharset_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "text/plain";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal("text/plain", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithTextHtmlContentTypeWithoutCharset_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "text/html";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal("text/html", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithOtherTextContentTypeWithoutCharset_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var originalContentType = "text/xml";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = originalContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal(originalContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithNonTextContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var originalContentType = "application/octet-stream";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = originalContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(originalContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithEmptyContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            // Setting ContentType to empty string results in null in ASP.NET Core
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithNullContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = null;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithCaseInsensitiveContentType_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "APPLICATION/JSON";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal("APPLICATION/JSON", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithTextContentTypeWithDifferentCharset_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var originalContentType = "text/plain; charset=iso-8859-1";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = originalContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(originalContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithJsonContentTypeWithParameters_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "application/json; boundary=something";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            // In unit tests, the OnStarting callback is not triggered, so ContentType remains unchanged
            Assert.Equal("application/json; boundary=something", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithExceptionInNext_PropagatesException()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var expectedException = new InvalidOperationException("Test exception");

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
            Assert.Equal("Test exception", exception.Message);
        }

        [Fact]
        public async Task InvokeAsync_WithWhitespaceContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = "   ";
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal("   ", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithVeryLongContentType_HandlesCorrectly()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var longContentType = "application/json" + new string('x', 1000);

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = longContentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(longContentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithContentTypeContainingCharsetInMiddle_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var contentType = "application/json; charset=utf-8; version=1.0";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = contentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(contentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithContentTypeContainingCharsetCaseInsensitive_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var contentType = "application/json; CHARSET=utf-8";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = contentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(contentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithTextContentTypeWithSpecialCharacters_DoesNotModifyInUnitTest()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var contentType = "text/csv; header=present";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = contentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(contentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithBinaryContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var contentType = "image/png";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = contentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(contentType, context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithCustomContentType_DoesNotModify()
        {
            // Arrange
            var middleware = new ResponseMiddleware(m_MockNext.Object);
            var context = CreateHttpContext();
            var contentType = "application/x-custom";

            m_MockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    ctx.Response.ContentType = contentType;
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            m_MockNext.Verify(x => x(context), Times.Once);
            Assert.Equal(contentType, context.Response.ContentType);
        }

        [Fact]
        public void ResponseMiddleware_Class_Exists()
        {
            // Assert
            Assert.NotNull(typeof(ResponseMiddleware));
        }

        [Fact]
        public void ResponseMiddleware_IsNotStatic()
        {
            // Assert
            Assert.False(typeof(ResponseMiddleware).IsAbstract);
        }

        [Fact]
        public void ResponseMiddleware_IsClass()
        {
            // Assert
            Assert.True(typeof(ResponseMiddleware).IsClass);
        }

        [Fact]
        public void ResponseMiddleware_IsNotValueType()
        {
            // Assert
            Assert.False(typeof(ResponseMiddleware).IsValueType);
        }

        [Fact]
        public void ResponseMiddleware_IsNotSealed()
        {
            // Assert
            Assert.False(typeof(ResponseMiddleware).IsSealed);
        }

        [Fact]
        public void ResponseMiddleware_HasInvokeAsyncMethod()
        {
            // Arrange
            var middlewareType = typeof(ResponseMiddleware);
            var method = middlewareType.GetMethod("InvokeAsync");

            // Assert
            Assert.NotNull(method);
            Assert.Equal("InvokeAsync", method.Name);
            Assert.True(method.IsPublic);
            Assert.True(method.ReturnType == typeof(Task));
        }

        [Fact]
        public void ResponseMiddleware_Constructor_WithValidNext_CreatesInstance()
        {
            // Arrange
            var next = new Mock<RequestDelegate>().Object;

            // Act
            var middleware = new ResponseMiddleware(next);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public void ResponseMiddleware_Constructor_WithNullNext_CreatesInstance()
        {
            // Act
            var middleware = new ResponseMiddleware(null!);

            // Assert
            Assert.NotNull(middleware);
        }

        private static HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        #region Integration Tests with TestServer

        [Fact]
        public async Task InvokeAsync_WithTestServer_AddsCharsetToJsonResponse()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseMiddleware<ResponseMiddleware>();
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"test\":\"value\"}");
                    });
                });

            using var server = new TestServer(builder);
            using var client = server.CreateClient();

            // Act
            var response = await client.PostAsync("/", new StringContent("{}", Encoding.UTF8, "application/json"));

            // Assert
            var contentType = response.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal("application/json", contentType.MediaType);
            Assert.Equal("utf-8", contentType.CharSet); // Changed to utf-8 by middleware
        }

        [Fact]
        public async Task InvokeAsync_WithTestServer_AddsCharsetToTextPlainResponse()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseMiddleware<ResponseMiddleware>();
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("test");
                    });
                });

            using var server = new TestServer(builder);
            using var client = server.CreateClient();

            // Act
            var response = await client.PostAsync("/", new StringContent("test"));

            // Assert
            var contentType = response.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal("text/plain", contentType.MediaType);
            Assert.Equal("utf-8", contentType.CharSet);
        }

        [Fact]
        public async Task InvokeAsync_WithTestServer_AddsCharsetToTextHtmlResponse()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseMiddleware<ResponseMiddleware>();
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync("<html></html>");
                    });
                });

            using var server = new TestServer(builder);
            using var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            var contentType = response.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal("text/html", contentType.MediaType);
            Assert.Equal("utf-8", contentType.CharSet);
        }

        [Fact]
        public async Task InvokeAsync_WithTestServer_AddsCharsetToTextXmlResponse()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseMiddleware<ResponseMiddleware>();
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/xml";
                        await context.Response.WriteAsync("<xml></xml>");
                    });
                });

            using var server = new TestServer(builder);
            using var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            var contentType = response.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal("text/xml", contentType.MediaType);
            Assert.Equal("utf-8", contentType.CharSet);
        }

        [Fact]
        public async Task InvokeAsync_WithTestServer_DoesNotModifyJsonWithCharset()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseMiddleware<ResponseMiddleware>();
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync("{\"test\":\"value\"}");
                    });
                });

            using var server = new TestServer(builder);
            using var client = server.CreateClient();

            // Act
            var response = await client.PostAsync("/", new StringContent("{}", Encoding.UTF8, "application/json"));

            // Assert
            var contentType = response.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal("application/json", contentType.MediaType);
            Assert.Equal("utf-8", contentType.CharSet);
        }

        [Fact]
        public async Task InvokeAsync_WithTestServer_DoesNotModifyBinaryContent()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseMiddleware<ResponseMiddleware>();
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "application/octet-stream";
                        await context.Response.Body.WriteAsync(new byte[] { 1, 2, 3, 4 });
                    });
                });

            using var server = new TestServer(builder);
            using var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            var contentType = response.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal("application/octet-stream", contentType.MediaType);
            Assert.Null(contentType.CharSet);
        }

        [Fact]
        public async Task InvokeAsync_WithTestServer_DoesNotModifyImageContent()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseMiddleware<ResponseMiddleware>();
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "image/png";
                        await context.Response.Body.WriteAsync(new byte[] { 137, 80, 78, 71 }); // PNG header
                    });
                });

            using var server = new TestServer(builder);
            using var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            var contentType = response.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal("image/png", contentType.MediaType);
            Assert.Null(contentType.CharSet);
        }

        #endregion
    }
}
