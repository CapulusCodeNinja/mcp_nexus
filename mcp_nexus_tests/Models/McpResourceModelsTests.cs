using System;
using Xunit;
using mcp_nexus.Models;
using mcp_nexus.Session.Models;

namespace mcp_nexus_tests.Models
{
    /// <summary>
    /// Tests for McpResource model classes - simple data containers
    /// </summary>
    public class McpResourceModelsTests
    {
        [Fact]
        public void McpResource_DefaultValues_AreCorrect()
        {
            // Act
            var resource = new McpResource();

            // Assert
            Assert.Equal(string.Empty, resource.Uri);
            Assert.Equal(string.Empty, resource.Name);
            Assert.Equal(string.Empty, resource.Description);
            Assert.Null(resource.MimeType);
        }

        [Fact]
        public void McpResource_WithValues_SetsProperties()
        {
            // Arrange
            const string uri = "test://resource/123";
            const string name = "Test Resource";
            const string description = "A test resource for unit testing";
            const string mimeType = "application/json";

            // Act
            var resource = new McpResource
            {
                Uri = uri,
                Name = name,
                Description = description,
                MimeType = mimeType
            };

            // Assert
            Assert.Equal(uri, resource.Uri);
            Assert.Equal(name, resource.Name);
            Assert.Equal(description, resource.Description);
            Assert.Equal(mimeType, resource.MimeType);
        }

        [Fact]
        public void McpResource_WithNullValues_HandlesGracefully()
        {
            // Act
            var resource = new McpResource
            {
                Uri = null!,
                Name = null!,
                Description = null!,
                MimeType = null
            };

            // Assert
            Assert.Null(resource.Uri);
            Assert.Null(resource.Name);
            Assert.Null(resource.Description);
            Assert.Null(resource.MimeType);
        }

        [Fact]
        public void McpResourceContent_DefaultValues_AreCorrect()
        {
            // Act
            var content = new McpResourceContent();

            // Assert
            Assert.Equal(string.Empty, content.Uri);
            Assert.Equal("text/plain", content.MimeType);
            Assert.Null(content.Text);
            Assert.Null(content.Blob);
        }

        [Fact]
        public void McpResourceContent_WithText_SetsProperties()
        {
            // Arrange
            const string uri = "test://content/text";
            const string mimeType = "text/plain";
            const string text = "This is test content";

            // Act
            var content = new McpResourceContent
            {
                Uri = uri,
                MimeType = mimeType,
                Text = text
            };

            // Assert
            Assert.Equal(uri, content.Uri);
            Assert.Equal(mimeType, content.MimeType);
            Assert.Equal(text, content.Text);
            Assert.Null(content.Blob);
        }

        [Fact]
        public void McpResourceContent_WithBlob_SetsProperties()
        {
            // Arrange
            const string uri = "test://content/blob";
            const string mimeType = "application/octet-stream";
            const string blob = "SGVsbG8gV29ybGQ="; // Base64 encoded "Hello World"

            // Act
            var content = new McpResourceContent
            {
                Uri = uri,
                MimeType = mimeType,
                Blob = blob
            };

            // Assert
            Assert.Equal(uri, content.Uri);
            Assert.Equal(mimeType, content.MimeType);
            Assert.Null(content.Text);
            Assert.Equal(blob, content.Blob);
        }

        [Fact]
        public void McpResourceContent_WithBothTextAndBlob_SetsBoth()
        {
            // Arrange
            const string uri = "test://content/both";
            const string mimeType = "multipart/mixed";
            const string text = "Text content";
            const string blob = "VGVzdCBCbG9i"; // Base64 encoded "Test Blob"

            // Act
            var content = new McpResourceContent
            {
                Uri = uri,
                MimeType = mimeType,
                Text = text,
                Blob = blob
            };

            // Assert
            Assert.Equal(uri, content.Uri);
            Assert.Equal(mimeType, content.MimeType);
            Assert.Equal(text, content.Text);
            Assert.Equal(blob, content.Blob);
        }

        [Fact]
        public void McpResourceReadResult_DefaultValues_AreCorrect()
        {
            // Act
            var result = new McpResourceReadResult();

            // Assert
            Assert.NotNull(result.Contents);
            Assert.Empty(result.Contents);
        }

        [Fact]
        public void McpResourceReadResult_WithContents_SetsProperties()
        {
            // Arrange
            var contents = new[]
            {
                new McpResourceContent { Uri = "test://1", Text = "Content 1" },
                new McpResourceContent { Uri = "test://2", Text = "Content 2" }
            };

            // Act
            var result = new McpResourceReadResult
            {
                Contents = contents
            };

            // Assert
            Assert.Equal(contents, result.Contents);
            Assert.Equal(2, result.Contents.Length);
        }

        [Fact]
        public void McpResourceReadResult_WithNullContents_HandlesGracefully()
        {
            // Act
            var result = new McpResourceReadResult
            {
                Contents = null!
            };

            // Assert
            Assert.Null(result.Contents);
        }

        [Fact]
        public void McpResourcesListResult_DefaultValues_AreCorrect()
        {
            // Act
            var result = new McpResourcesListResult();

            // Assert
            Assert.NotNull(result.Resources);
            Assert.Empty(result.Resources);
        }

        [Fact]
        public void McpResourcesListResult_WithResources_SetsProperties()
        {
            // Arrange
            var resources = new[]
            {
                new McpResource { Uri = "test://resource/1", Name = "Resource 1" },
                new McpResource { Uri = "test://resource/2", Name = "Resource 2" },
                new McpResource { Uri = "test://resource/3", Name = "Resource 3" }
            };

            // Act
            var result = new McpResourcesListResult
            {
                Resources = resources
            };

            // Assert
            Assert.Equal(resources, result.Resources);
            Assert.Equal(3, result.Resources.Length);
        }

        [Fact]
        public void McpResourcesListResult_WithNullResources_HandlesGracefully()
        {
            // Act
            var result = new McpResourcesListResult
            {
                Resources = null!
            };

            // Assert
            Assert.Null(result.Resources);
        }

        [Fact]
        public void McpSessionEventNotification_DefaultValues_AreCorrect()
        {
            // Act
            var notification = new McpSessionEventNotification();

            // Assert
            Assert.Equal(string.Empty, notification.SessionId);
            Assert.Equal(string.Empty, notification.EventType);
            Assert.Equal(string.Empty, notification.Message);
            Assert.Null(notification.Context);
            // Timestamp is set to DateTimeOffset.Now by default, so we just check it's not MinValue
            Assert.True(notification.Timestamp > DateTimeOffset.MinValue);
        }

        [Fact]
        public void McpSessionEventNotification_WithValues_SetsProperties()
        {
            // Arrange
            var timestamp = DateTimeOffset.UtcNow;
            const string sessionId = "session-123";
            const string eventType = "SESSION_CREATED";
            const string message = "Session created successfully";
            var context = new SessionContext { SessionId = "session-123", Status = "active" };

            // Act
            var notification = new McpSessionEventNotification
            {
                SessionId = sessionId,
                EventType = eventType,
                Message = message,
                Context = context,
                Timestamp = timestamp
            };

            // Assert
            Assert.Equal(sessionId, notification.SessionId);
            Assert.Equal(eventType, notification.EventType);
            Assert.Equal(message, notification.Message);
            Assert.Equal(context, notification.Context);
            Assert.Equal(timestamp, notification.Timestamp);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("test")]
        [InlineData("SESSION_CREATED")]
        [InlineData("SESSION_DESTROYED")]
        [InlineData("SESSION_ERROR")]
        public void McpSessionEventNotification_WithVariousEventTypes_SetsCorrectly(string eventType)
        {
            // Act
            var notification = new McpSessionEventNotification
            {
                EventType = eventType
            };

            // Assert
            Assert.Equal(eventType, notification.EventType);
        }

        [Fact]
        public void McpSessionEventNotification_WithNullValues_HandlesGracefully()
        {
            // Act
            var notification = new McpSessionEventNotification
            {
                SessionId = null!,
                EventType = null!,
                Message = null!,
                Context = null
            };

            // Assert
            Assert.Null(notification.SessionId);
            Assert.Null(notification.EventType);
            Assert.Null(notification.Message);
            Assert.Null(notification.Context);
        }
    }
}
