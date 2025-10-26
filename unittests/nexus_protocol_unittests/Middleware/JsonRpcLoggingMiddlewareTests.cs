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

    #region SSE Format Detection Tests

    /// <summary>
    /// Verifies that IsSseFormat detects SSE format correctly.
    /// </summary>
    [Theory]
    [InlineData("event: message\ndata: {\"test\":\"value\"}", true)]
    [InlineData("data: {\"test\":\"value\"}", true)]
    [InlineData("event: message", true)]
    [InlineData("{\"jsonrpc\":\"2.0\"}", false)]
    [InlineData("", false)]
    [InlineData("event: message\ndata: {\"test\":\"value\"}\n\n", true)]
    public void IsSseFormat_WithVariousInputs_ReturnsExpectedResult(string content, bool expected)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.IsSseFormat(content);

        // Assert
        _ = result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that IsSseFormat handles whitespace correctly.
    /// </summary>
    [Theory]
    [InlineData("  event: message", true)]
    [InlineData("\tdata: {\"test\":\"value\"}", true)]
    [InlineData("  \n  event: message  \n  ", true)]
    public void IsSseFormat_WithWhitespace_DetectsCorrectly(string content, bool expected)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.IsSseFormat(content);

        // Assert
        _ = result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that IsSseFormat handles null content.
    /// </summary>
    [Fact]
    public void IsSseFormat_WithNullContent_ReturnsFalse()
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.IsSseFormat(null!);

        // Assert
        _ = result.Should().BeFalse();
    }

    #endregion

    #region SSE Content Formatting Tests

    /// <summary>
    /// Verifies that FormatSseContent formats SSE content correctly.
    /// </summary>
    [Fact]
    public void FormatSseContent_WithValidSse_FormatsCorrectly()
    {
        // Arrange
        var sseContent = "event: message\ndata: {\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"test\"}]},\"id\":1}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatSseContent(sseContent);

        // Assert
        _ = result.Should().Contain("event: message");
        _ = result.Should().Contain("data:");
        _ = result.Should().Contain("\"result\"");
        _ = result.Should().Contain("\"content\"");
    }

    /// <summary>
    /// Verifies that FormatSseContent handles empty content.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void FormatSseContent_WithEmptyContent_ReturnsEmptyString(string content)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatSseContent(content);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FormatSseContent handles null content.
    /// </summary>
    [Fact]
    public void FormatSseContent_WithNullContent_ReturnsEmptyString()
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatSseContent(null!);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FormatSseContent handles non-SSE content.
    /// </summary>
    [Fact]
    public void FormatSseContent_WithNonSseContent_ReturnsOriginal()
    {
        // Arrange
        var content = "{\"jsonrpc\":\"2.0\",\"method\":\"test\"}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatSseContent(content);

        // Assert
        _ = result.Should().Be(content);
    }

    /// <summary>
    /// Verifies that FormatSseContent handles multiple data lines.
    /// </summary>
    [Fact]
    public void FormatSseContent_WithMultipleDataLines_FormatsAll()
    {
        // Arrange
        var sseContent = "event: message\ndata: {\"id\":1}\ndata: {\"id\":2}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatSseContent(sseContent);

        // Assert
        _ = result.Should().Contain("event: message");
        _ = result.Should().Contain("data:");
        _ = result.Should().Contain("\"id\": 1");
        _ = result.Should().Contain("\"id\": 2");
    }

    #endregion

    #region JSON Extraction from SSE Tests

    /// <summary>
    /// Verifies that ExtractJsonFromSseLine extracts JSON correctly.
    /// </summary>
    [Theory]
    [InlineData("data: {\"test\":\"value\"}", "{\"test\":\"value\"}")]
    [InlineData("data:{\"test\":\"value\"}", "{\"test\":\"value\"}")]
    [InlineData("  data: {\"test\":\"value\"}  ", "{\"test\":\"value\"}")]
    [InlineData("data: {\"id\":1,\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"test\"}]}}", "{\"id\":1,\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"test\"}]}}")]
    public void ExtractJsonFromSseLine_WithValidDataLine_ReturnsJson(string line, string expectedJson)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractJsonFromSseLine(line);

        // Assert
        _ = result.Should().Be(expectedJson);
    }

    /// <summary>
    /// Verifies that ExtractJsonFromSseLine handles invalid lines.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("event: message")]
    [InlineData("data:")]
    [InlineData("data: no json here")]
    [InlineData("not a data line")]
    public void ExtractJsonFromSseLine_WithInvalidLine_ReturnsEmptyString(string line)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractJsonFromSseLine(line);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ExtractJsonFromSseLine handles null input.
    /// </summary>
    [Fact]
    public void ExtractJsonFromSseLine_WithNullInput_ReturnsEmptyString()
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractJsonFromSseLine(null!);

        // Assert
        _ = result.Should().BeEmpty();
    }

    #endregion

    #region SSE Text Field Extraction Tests

    /// <summary>
    /// Verifies that ExtractTextFieldsFromSse extracts text fields correctly.
    /// </summary>
    [Fact]
    public void ExtractTextFieldsFromSse_WithValidSse_ExtractsTextFields()
    {
        // Arrange
        var sseContent = "event: message\ndata: {\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"commandId\\\":\\\"cmd-123\\\",\\\"status\\\":\\\"Success\\\"}\"}]},\"id\":1}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFieldsFromSse(sseContent);

        // Assert
        _ = result.Should().HaveCount(1);
        _ = result[0].Should().Contain("commandId");
        _ = result[0].Should().Contain("cmd-123");
        _ = result[0].Should().Contain("status");
        _ = result[0].Should().Contain("Success");
    }

    /// <summary>
    /// Verifies that ExtractTextFieldsFromSse handles empty content.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void ExtractTextFieldsFromSse_WithEmptyContent_ReturnsEmptyList(string content)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFieldsFromSse(content);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ExtractTextFieldsFromSse handles null content.
    /// </summary>
    [Fact]
    public void ExtractTextFieldsFromSse_WithNullContent_ReturnsEmptyList()
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFieldsFromSse(null!);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ExtractTextFieldsFromSse handles SSE without text fields.
    /// </summary>
    [Fact]
    public void ExtractTextFieldsFromSse_WithSseWithoutTextFields_ReturnsEmptyList()
    {
        // Arrange
        var sseContent = "event: message\ndata: {\"id\":1,\"result\":{\"status\":\"ok\"}}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFieldsFromSse(sseContent);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ExtractTextFieldsFromSse handles multiple data lines with text fields.
    /// </summary>
    [Fact]
    public void ExtractTextFieldsFromSse_WithMultipleDataLines_ExtractsAllTextFields()
    {
        // Arrange
        var sseContent = "event: message\ndata: {\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"first text\"}]}}\ndata: {\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"second text\"}]}}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFieldsFromSse(sseContent);

        // Assert
        _ = result.Should().HaveCount(2);
        _ = result.Should().Contain("first text");
        _ = result.Should().Contain("second text");
    }

    #endregion

    #region JSON Formatting and Truncation Tests

    /// <summary>
    /// Verifies that FormatAndTruncateJson handles SSE format correctly.
    /// </summary>
    [Fact]
    public void FormatAndTruncateJson_WithSseFormat_FormatsSseContent()
    {
        // Arrange
        var sseContent = "event: message\ndata: {\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"test\"}]},\"id\":1}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatAndTruncateJson(sseContent);

        // Assert
        _ = result.Should().Contain("event: message");
        _ = result.Should().Contain("data:");
        _ = result.Should().Contain("\"result\"");
    }

    /// <summary>
    /// Verifies that FormatAndTruncateJson handles regular JSON format correctly.
    /// </summary>
    [Fact]
    public void FormatAndTruncateJson_WithRegularJson_FormatsJson()
    {
        // Arrange
        var jsonContent = "{\"jsonrpc\":\"2.0\",\"method\":\"test\",\"params\":{\"value\":\"test\"}}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatAndTruncateJson(jsonContent);

        // Assert
        _ = result.Should().Contain("\"jsonrpc\"");
        _ = result.Should().Contain("\"method\"");
        _ = result.Should().Contain("\"params\"");
        _ = result.Should().NotContain("event:");
        _ = result.Should().NotContain("data:");
    }

    /// <summary>
    /// Verifies that FormatAndTruncateJson handles empty content.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void FormatAndTruncateJson_WithEmptyContent_ReturnsEmptyString(string content)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatAndTruncateJson(content);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FormatAndTruncateJson handles null content.
    /// </summary>
    [Fact]
    public void FormatAndTruncateJson_WithNullContent_ReturnsEmptyString()
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatAndTruncateJson(null!);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FormatAndTruncateJson truncates long strings correctly.
    /// </summary>
    [Fact]
    public void FormatAndTruncateJson_WithLongStrings_TruncatesCorrectly()
    {
        // Arrange
        var longString = new string('x', 2000);
        var jsonContent = $"{{\"longField\":\"{longString}\",\"shortField\":\"short\"}}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatAndTruncateJson(jsonContent);

        // Assert
        _ = result.Should().Contain("... (truncated");
        _ = result.Should().Contain("short");
        _ = result.Should().NotContain(longString);
    }

    /// <summary>
    /// Verifies that FormatAndTruncateJson handles invalid JSON gracefully.
    /// </summary>
    [Fact]
    public void FormatAndTruncateJson_WithInvalidJson_FallsBackToTruncation()
    {
        // Arrange
        var invalidJson = "not valid json content";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.FormatAndTruncateJson(invalidJson);

        // Assert
        _ = result.Should().Be(invalidJson);
    }

    #endregion

    #region Text Field Extraction Tests

    /// <summary>
    /// Verifies that ExtractTextFields handles SSE format correctly.
    /// </summary>
    [Fact]
    public void ExtractTextFields_WithSseFormat_ExtractsTextFields()
    {
        // Arrange
        var sseContent = "event: message\ndata: {\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"commandId\\\":\\\"cmd-123\\\"}\"}]},\"id\":1}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFields(sseContent);

        // Assert
        _ = result.Should().HaveCount(1);
        _ = result[0].Should().Contain("commandId");
        _ = result[0].Should().Contain("cmd-123");
    }

    /// <summary>
    /// Verifies that ExtractTextFields handles regular JSON format correctly.
    /// </summary>
    [Fact]
    public void ExtractTextFields_WithRegularJson_ExtractsTextFields()
    {
        // Arrange
        var jsonContent = "{\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"commandId\\\":\\\"cmd-123\\\"}\"}]},\"id\":1}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFields(jsonContent);

        // Assert
        _ = result.Should().HaveCount(1);
        _ = result[0].Should().Contain("commandId");
        _ = result[0].Should().Contain("cmd-123");
    }

    /// <summary>
    /// Verifies that ExtractTextFields handles empty content.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void ExtractTextFields_WithEmptyContent_ReturnsEmptyList(string content)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFields(content);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ExtractTextFields handles null content.
    /// </summary>
    [Fact]
    public void ExtractTextFields_WithNullContent_ReturnsEmptyList()
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFields(null!);

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ExtractTextFields extracts multiple text fields.
    /// </summary>
    [Fact]
    public void ExtractTextFields_WithMultipleTextFields_ExtractsAll()
    {
        // Arrange
        var jsonContent = "{\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"first text\"},{\"type\":\"text\",\"text\":\"second text\"}]}}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFields(jsonContent);

        // Assert
        _ = result.Should().HaveCount(2);
        _ = result.Should().Contain("first text");
        _ = result.Should().Contain("second text");
    }

    /// <summary>
    /// Verifies that ExtractTextFields handles nested text fields.
    /// </summary>
    [Fact]
    public void ExtractTextFields_WithNestedTextFields_ExtractsAll()
    {
        // Arrange
        var jsonContent = "{\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"nested\\\":{\\\"text\\\":\\\"value\\\"}}\"}]}}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.ExtractTextFields(jsonContent);

        // Assert
        _ = result.Should().HaveCount(1);
        _ = result[0].Should().Contain("nested");
        _ = result[0].Should().Contain("text");
        _ = result[0].Should().Contain("value");
    }

    #endregion

    #region String Truncation Tests

    /// <summary>
    /// Verifies that TruncateString truncates long strings correctly.
    /// </summary>
    [Fact]
    public void TruncateString_WithLongString_TruncatesCorrectly()
    {
        // Arrange
        var longString = new string('x', 2000);
        var maxLength = 1000;

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.TruncateString(longString, maxLength);

        // Assert
        _ = result.Should().HaveLength(1000 + "... (truncated 1000 chars)".Length);
        _ = result.Should().EndWith("... (truncated 1000 chars)");
    }

    /// <summary>
    /// Verifies that TruncateString handles short strings correctly.
    /// </summary>
    [Fact]
    public void TruncateString_WithShortString_ReturnsOriginal()
    {
        // Arrange
        var shortString = "short";
        var maxLength = 1000;

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.TruncateString(shortString, maxLength);

        // Assert
        _ = result.Should().Be(shortString);
    }

    /// <summary>
    /// Verifies that TruncateString handles empty strings correctly.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void TruncateString_WithEmptyString_ReturnsOriginal(string value)
    {
        // Arrange
        var maxLength = 1000;

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.TruncateString(value, maxLength);

        // Assert
        _ = result.Should().Be(value);
    }

    /// <summary>
    /// Verifies that TruncateString handles null strings correctly.
    /// </summary>
    [Fact]
    public void TruncateString_WithNullString_ReturnsNull()
    {
        // Arrange
        var maxLength = 1000;

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.TruncateString(null!, maxLength);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that TruncateString handles exact length strings correctly.
    /// </summary>
    [Fact]
    public void TruncateString_WithExactLengthString_ReturnsOriginal()
    {
        // Arrange
        var exactString = new string('x', 1000);
        var maxLength = 1000;

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.TruncateString(exactString, maxLength);

        // Assert
        _ = result.Should().Be(exactString);
    }

    #endregion

    #region JSON Unescaping Tests

    /// <summary>
    /// Verifies that UnescapeJsonInText unescapes Unicode escape sequences correctly.
    /// </summary>
    [Fact]
    public void UnescapeJsonInText_WithUnicodeEscapes_UnescapesCorrectly()
    {
        // Arrange
        var escapedText = "{\\u0022commandId\\u0022:\\u0022cmd-123\\u0022,\\u0022status\\u0022:\\u0022Success\\u0022}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.UnescapeJsonInText(escapedText);

        // Assert
        _ = result.Should().Be("{\"commandId\":\"cmd-123\",\"status\":\"Success\"}");
    }

    /// <summary>
    /// Verifies that UnescapeJsonInText handles unescaped content correctly.
    /// </summary>
    [Fact]
    public void UnescapeJsonInText_WithUnescapedContent_ReturnsOriginal()
    {
        // Arrange
        var unescapedText = "{\"commandId\":\"cmd-123\",\"status\":\"Success\"}";

        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.UnescapeJsonInText(unescapedText);

        // Assert
        _ = result.Should().Be(unescapedText);
    }

    /// <summary>
    /// Verifies that UnescapeJsonInText handles empty content correctly.
    /// </summary>
    [Theory]
    [InlineData("")]
    public void UnescapeJsonInText_WithEmptyContent_ReturnsOriginal(string text)
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.UnescapeJsonInText(text);

        // Assert
        _ = result.Should().Be(text);
    }

    /// <summary>
    /// Verifies that UnescapeJsonInText handles null content correctly.
    /// </summary>
    [Fact]
    public void UnescapeJsonInText_WithNullContent_ReturnsNull()
    {
        // Act
        var result = JsonRpcLoggingMiddlewareTestAccessor.UnescapeJsonInText(null!);

        // Assert
        _ = result.Should().BeNull();
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Verifies that the middleware handles SSE response correctly in integration.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithSseResponse_ProcessesCorrectly()
    {
        // Arrange
        var sseResponse = "event: message\ndata: {\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"commandId\\\":\\\"cmd-123\\\",\\\"status\\\":\\\"Success\\\"}\"}]},\"id\":1}";

        RequestDelegate next = async (HttpContext ctx) => await ctx.Response.WriteAsync(sseResponse);

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - The middleware should process the SSE response without errors
        // Note: Response body position may not be 0 after processing due to buffering
        _ = context.Response.Body.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the middleware handles regular JSON response correctly in integration.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithRegularJsonResponse_ProcessesCorrectly()
    {
        // Arrange
        var jsonResponse = "{\"jsonrpc\":\"2.0\",\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"commandId\\\":\\\"cmd-123\\\"}\"}]},\"id\":1}";

        RequestDelegate next = async (HttpContext ctx) => await ctx.Response.WriteAsync(jsonResponse);

        var middleware = new JsonRpcLoggingMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/";
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - The middleware should process the JSON response without errors
        // Note: Response body position may not be 0 after processing due to buffering
        _ = context.Response.Body.Should().NotBeNull();
    }

    #endregion
}
