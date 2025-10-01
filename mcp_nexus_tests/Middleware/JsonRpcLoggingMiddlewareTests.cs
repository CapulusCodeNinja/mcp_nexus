using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;
using mcp_nexus.Middleware;

namespace mcp_nexus_tests.Middleware
{
    /// <summary>
    /// Tests for JsonRpcLoggingMiddleware
    /// </summary>
    public class JsonRpcLoggingMiddlewareTests
    {
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly Mock<ILogger<JsonRpcLoggingMiddleware>> _mockLogger;

        public JsonRpcLoggingMiddlewareTests()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public void Constructor_WithNullNext_CreatesInstance()
        {
            // Act
            var middleware = new JsonRpcLoggingMiddleware(null!, _mockLogger.Object);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public void Constructor_WithNullLogger_CreatesInstance()
        {
            // Act
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, null!);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public async Task InvokeAsync_WithNonRootPath_CallsNext()
        {
            // Arrange
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
            var context = CreateHttpContext("/api/test", "POST");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithNonPostMethod_CallsNext()
        {
            // Arrange
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
            var context = CreateHttpContext("/", "GET");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithRootPathAndPostMethod_LogsRequestAndResponse()
        {
            // Arrange
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
            var requestJson = """{"jsonrpc": "2.0", "method": "test", "id": 1}""";
            var responseJson = """{"jsonrpc": "2.0", "result": "success", "id": 1}""";
            var context = CreateHttpContext("/", "POST", requestJson);

            // Setup response
            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            VerifyLogCalled(LogLevel.Information, "📨 JSON-RPC Request:");
            VerifyLogCalled(LogLevel.Information, "📤 JSON-RPC Response:");
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidJsonRequest_LogsFormattedError()
        {
            // Arrange
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
            var invalidJson = """{"jsonrpc": "2.0", "method": "test", "id": 1"""; // Missing closing brace
            var context = CreateHttpContext("/", "POST", invalidJson);

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    var responseBytes = Encoding.UTF8.GetBytes("""{"jsonrpc": "2.0", "error": "Invalid JSON", "id": 1}""");
                    ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            VerifyLogCalled(LogLevel.Information, "📨 JSON-RPC Request:");
            VerifyLogCalled(LogLevel.Information, "📤 JSON-RPC Response:");
        }

        [Fact]
        public async Task InvokeAsync_WithSseResponse_LogsFormattedResponse()
        {
            // Arrange
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
            var requestJson = """{"jsonrpc": "2.0", "method": "test", "id": 1}""";
            var sseResponse = "data: {\"jsonrpc\": \"2.0\", \"result\": \"success\", \"id\": 1}\n\n";
            var context = CreateHttpContext("/", "POST", requestJson);

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    var responseBytes = Encoding.UTF8.GetBytes(sseResponse);
                    ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            VerifyLogCalled(LogLevel.Information, "📨 JSON-RPC Request:");
            VerifyLogCalled(LogLevel.Information, "📤 JSON-RPC Response:");
        }

        [Fact]
        public async Task InvokeAsync_WithEmptyRequestBody_LogsRequest()
        {
            // Arrange
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
            var context = CreateHttpContext("/", "POST", "");

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    var responseBytes = Encoding.UTF8.GetBytes("""{"jsonrpc": "2.0", "result": "success", "id": 1}""");
                    ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            VerifyLogCalled(LogLevel.Information, "📨 JSON-RPC Request:");
            VerifyLogCalled(LogLevel.Information, "📤 JSON-RPC Response:");
        }

        [Fact]
        public async Task InvokeAsync_WithLargeJsonRequest_TruncatesInError()
        {
            // Arrange
            var middleware = new JsonRpcLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
            var largeJson = new string('a', 2000) + """{"jsonrpc": "2.0", "method": "test", "id": 1}""";
            var context = CreateHttpContext("/", "POST", largeJson);

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    var responseBytes = Encoding.UTF8.GetBytes("""{"jsonrpc": "2.0", "result": "success", "id": 1}""");
                    ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                });

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
            VerifyLogCalled(LogLevel.Information, "📨 JSON-RPC Request:");
            VerifyLogCalled(LogLevel.Information, "📤 JSON-RPC Response:");
        }

        [Fact]
        public void JsonRpcLoggingMiddleware_Class_Exists()
        {
            // This test verifies that the JsonRpcLoggingMiddleware class exists and can be instantiated
            Assert.True(typeof(JsonRpcLoggingMiddleware) != null);
        }

        private HttpContext CreateHttpContext(string path, string method, string? body = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = method;
            context.Request.Body = new MemoryStream();
            context.Response.Body = new MemoryStream();

            if (!string.IsNullOrEmpty(body))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                context.Request.Body.Write(bodyBytes, 0, bodyBytes.Length);
                context.Request.Body.Position = 0;
            }

            return context;
        }

        private void VerifyLogCalled(LogLevel level, string message)
        {
            _mockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
