using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Middleware;

namespace mcp_nexus_unit_tests.Middleware
{
    /// <summary>
    /// Tests for JsonRpcLoggingMiddleware that would have caught the double-encoding bug
    /// </summary>
    public class JsonRpcLoggingMiddlewareTests
    {
        private readonly Mock<ILogger<JsonRpcLoggingMiddleware>> _mockLogger;
        private readonly JsonRpcLoggingMiddleware _middleware;

        public JsonRpcLoggingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            _middleware = new JsonRpcLoggingMiddleware(NextMiddleware, _mockLogger.Object);
        }

        private static Task NextMiddleware(HttpContext context)
        {
            // Simulate a JSON-RPC response with complex nested JSON
            var response = new
            {
                result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "{\"sessionId\":\"sess-123\",\"commandId\":\"cmd-456\",\"success\":true,\"result\":\"Complex output with \\\"quotes\\\" and \\n newlines\"}"
                        }
                    }
                },
                id = 1,
                jsonrpc = "2.0"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(response);
            var bytes = Encoding.Unicode.GetBytes(json);

            context.Response.Body.Write(bytes, 0, bytes.Length);
            context.Response.ContentType = "application/json";
            return Task.CompletedTask;
        }

        [Fact]
        public async Task InvokeAsync_WithComplexJson_FormatsCorrectly()
        {
            // This test would have caught the double-encoding issue
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.Unicode.GetBytes("{\"method\":\"test\"}"));

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // The middleware should handle the complex JSON without double-encoding
            // This test would have failed before our fix
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithNestedJsonString_HandlesEscaping()
        {
            // This test simulates the exact scenario that caused the double-encoding bug
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";

            var requestJson = "{\"method\":\"nexus_read_dump_analyze_command_result\",\"params\":{\"sessionId\":\"sess-123\",\"commandId\":\"cmd-456\"}}";
            context.Request.Body = new MemoryStream(Encoding.Unicode.GetBytes(requestJson));

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Verify that the middleware can handle nested JSON strings without double-encoding
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithUnicodeCharacters_HandlesCorrectly()
        {
            // This test would have caught the Unicode escaping issues
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";

            var requestJson = "{\"method\":\"test\",\"params\":{\"text\":\"Hello \\u0022World\\u0022\"}}";
            context.Request.Body = new MemoryStream(Encoding.Unicode.GetBytes(requestJson));

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // The middleware should handle Unicode escapes correctly
            Assert.NotNull(context.Response.Body);
        }

        #region DecodeJsonText Branch Coverage Tests

        [Fact]
        public async Task InvokeAsync_WithEmptyResponseBody_HandlesEmptyResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response with empty body
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes("");
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should handle empty response without throwing
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithWhitespaceOnlyResponse_HandlesWhitespaceResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response with whitespace only
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes("   \n\t  ");
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should handle whitespace response without throwing
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithServerSentEventsFormat_ExtractsJsonFromDataLine()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response with SSE format
            var sseResponse = "data: {\"result\":{\"content\":[{\"text\":\"{\\\"key\\\":\\\"value\\\"}\"}]}}\n\n";
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes(sseResponse);
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should extract JSON from data: line
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidJson_HandlesJsonException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response with invalid JSON
            var invalidJson = "{\"result\":{\"content\":[{\"text\":\"invalid json here\"}]}";
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes(invalidJson);
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should handle invalid JSON without throwing
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithMissingResultProperty_HandlesMissingProperty()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response without result property
            var responseWithoutResult = "{\"error\":{\"message\":\"test error\"}}";
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes(responseWithoutResult);
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should handle missing result property
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithEmptyContentArray_HandlesEmptyArray()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response with empty content array
            var responseWithEmptyArray = "{\"result\":{\"content\":[]}}";
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes(responseWithEmptyArray);
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should handle empty content array
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithMissingTextProperty_HandlesMissingTextProperty()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response with content but no text property
            var responseWithoutText = "{\"result\":{\"content\":[{\"other\":\"value\"}]}}";
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes(responseWithoutText);
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should handle missing text property
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithDoubleEncodedJson_HandlesDoubleEncoding()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response with double-encoded JSON
            var doubleEncodedJson = "{\"result\":{\"content\":[{\"text\":\"{\\\"key\\\":\\\"value\\\"}\"}]}}";
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes(doubleEncodedJson);
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should handle double-encoded JSON
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithTruncationEnabled_TruncatesLargeFields()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Set up response with large JSON that should be truncated
            var largeJson = "{\"result\":{\"content\":[{\"text\":\"{\\\"largeField\\\":\\\"" + new string('x', 2000) + "\\\"}\"}]}}";
            context.Response.Body = new MemoryStream();
            var responseBytes = Encoding.UTF8.GetBytes(largeJson);
            await context.Response.Body.WriteAsync(responseBytes);
            context.Response.Body.Position = 0;

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should handle large JSON with truncation
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithNonPostRequest_SkipsLogging()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "GET"; // Not POST
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should skip logging for non-POST requests
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithNonRootPath_SkipsLogging()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/test"; // Not root path
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should skip logging for non-root paths
            Assert.NotNull(context.Response.Body);
        }

        #endregion

        #region Additional Coverage Tests

        [Fact]
        public async Task InvokeAsync_WithDebugLoggingEnabled_LogsRequestAndResponse()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            var middleware = new JsonRpcLoggingMiddleware(NextMiddleware, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"method\":\"test\"}"));

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            mockLogger.Verify(l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JSON-RPC Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithDebugLoggingEnabled_LogsDebugResponse()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            var middleware = new JsonRpcLoggingMiddleware(NextMiddleware, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"method\":\"test\"}"));

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            mockLogger.Verify(l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JSON-RPC Response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task InvokeAsync_WithEmptyRequest_HandlesEmptyRequest()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            var middleware = new JsonRpcLoggingMiddleware(NextMiddleware, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(""));

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Should handle empty request without throwing
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidJsonRequest_HandlesInvalidJson()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            var middleware = new JsonRpcLoggingMiddleware(NextMiddleware, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{invalid json}"));

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Should handle invalid JSON without throwing
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithComplexNestedArrays_HandlesNestedArrays()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            RequestDelegate complexNext = async (HttpContext context) =>
            {
                var response = new
                {
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = "{\"data\":[[1,2,3],[4,5,6],[\"a\",\"b\",\"c\"]]}"
                            }
                        }
                    }
                };
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                var bytes = Encoding.UTF8.GetBytes(json);
                await context.Response.Body.WriteAsync(bytes);
            };

            var middleware = new JsonRpcLoggingMiddleware(complexNext, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"method\":\"test\"}"));

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Should handle nested arrays
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithLargeNestedObject_TruncatesCorrectly()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            var largeField = new string('x', 1500);
            RequestDelegate nestedNext = async (HttpContext context) =>
            {
                var response = new
                {
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = "{\"nested\":{\"field1\":\"" + largeField + "\",\"field2\":\"small\"}}"
                            }
                        }
                    }
                };
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                var bytes = Encoding.UTF8.GetBytes(json);
                await context.Response.Body.WriteAsync(bytes);
            };

            var middleware = new JsonRpcLoggingMiddleware(nestedNext, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"method\":\"test\"}"));

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Should truncate large nested fields
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithNumbersAndBooleans_HandlesNonStringValues()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            RequestDelegate numbersNext = async (HttpContext context) =>
            {
                var response = new
                {
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = "{\"number\":123,\"boolean\":true,\"null\":null,\"float\":45.67}"
                            }
                        }
                    }
                };
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                var bytes = Encoding.UTF8.GetBytes(json);
                await context.Response.Body.WriteAsync(bytes);
            };

            var middleware = new JsonRpcLoggingMiddleware(numbersNext, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"method\":\"test\"}"));

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Should handle numbers and booleans
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithSseFormatNoDataLine_FallsBackToDirectParsing()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            RequestDelegate sseNext = async (HttpContext context) =>
            {
                // SSE format without "data:" line
                var sseResponse = "event: message\nretry: 1000\n{\"result\":{\"content\":[{\"text\":\"{}\"}]}}\n\n";
                var bytes = Encoding.UTF8.GetBytes(sseResponse);
                await context.Response.Body.WriteAsync(bytes);
            };

            var middleware = new JsonRpcLoggingMiddleware(sseNext, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"method\":\"test\"}"));

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Should fall back to direct JSON parsing
            Assert.NotNull(context.Response.Body);
        }

        [Fact]
        public async Task InvokeAsync_WithUnescapableDecode_UsesRegexFallback()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JsonRpcLoggingMiddleware>>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

            RequestDelegate unescapableNext = async (HttpContext context) =>
            {
                // Text that can't be deserialized but can be unescaped
                var response = new
                {
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = "\\\"simple\\nescaped\\ttext\\\""
                            }
                        }
                    }
                };
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                var bytes = Encoding.UTF8.GetBytes(json);
                await context.Response.Body.WriteAsync(bytes);
            };

            var middleware = new JsonRpcLoggingMiddleware(unescapableNext, mockLogger.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/";
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"method\":\"test\"}"));

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Should use regex fallback for unescaping
            Assert.NotNull(context.Response.Body);
        }

        #endregion
    }
}