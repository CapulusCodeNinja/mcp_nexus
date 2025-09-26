using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mcp_nexus.Controllers;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Models;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mcp_nexus_tests.Controllers
{
    public class McpNotificationControllerTests
    {
        private readonly McpNotificationController m_controller;
        private readonly Mock<IMcpNotificationService> m_mockNotificationService;
        private readonly Mock<ILogger<McpNotificationController>> m_mockLogger;

        public McpNotificationControllerTests()
        {
            m_mockNotificationService = new Mock<IMcpNotificationService>();
            m_mockLogger = new Mock<ILogger<McpNotificationController>>();
            m_controller = new McpNotificationController(m_mockNotificationService.Object, m_mockLogger.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Mcp-Session-Id"] = "test-session-123";
            httpContext.Response.Body = new MemoryStream();
            
            m_controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task SendTestNotification_WithValidRequest_CallsNotificationService()
        {
            // Arrange
            var request = new TestNotificationRequest
            {
                Method = "test/notification",
                Params = new { message = "test message", value = 42 }
            };

            // Act
            var result = await m_controller.SendTestNotification(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            m_mockNotificationService.Verify(
                x => x.SendNotificationAsync("test/notification", request.Params),
                Times.Once);

            // Verify response structure
            Assert.NotNull(response);
            var responseType = response.GetType();
            var messageProperty = responseType.GetProperty("message");
            var methodProperty = responseType.GetProperty("method");
            
            Assert.NotNull(messageProperty);
            Assert.NotNull(methodProperty);
            Assert.Equal("Test notification sent", messageProperty.GetValue(response));
            Assert.Equal("test/notification", methodProperty.GetValue(response));
        }

        [Fact]
        public async Task SendTestNotification_WithEmptyMethod_CallsNotificationService()
        {
            // Arrange
            var request = new TestNotificationRequest
            {
                Method = "",
                Params = null
            };

            // Act
            var result = await m_controller.SendTestNotification(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            m_mockNotificationService.Verify(
                x => x.SendNotificationAsync("", null),
                Times.Once);
        }

        [Fact]
        public async Task SendTestNotification_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            var request = new TestNotificationRequest
            {
                Method = "test/error",
                Params = new { }
            };

            m_mockNotificationService
                .Setup(x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new InvalidOperationException("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                m_controller.SendTestNotification(request));
        }

        [Fact]
        public void StreamNotifications_SetsCorrectHeaders()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestAborted = new CancellationToken(true); // Immediately cancelled to exit quickly
            httpContext.Response.Body = new MemoryStream();
            
            m_controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var task = m_controller.StreamNotifications();

            // Assert - Headers should be set
            Assert.Equal("text/event-stream", httpContext.Response.Headers["Content-Type"]);
            Assert.Equal("no-cache", httpContext.Response.Headers["Cache-Control"]);
            Assert.Equal("keep-alive", httpContext.Response.Headers["Connection"]);
            Assert.Equal("*", httpContext.Response.Headers["Access-Control-Allow-Origin"]);
            Assert.Equal("Mcp-Session-Id", httpContext.Response.Headers["Access-Control-Allow-Headers"]);
        }

        [Fact]
        public async Task StreamNotifications_RegistersHandlerWithNotificationService()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestAborted = new CancellationToken(true); // Immediately cancelled
            httpContext.Response.Body = new MemoryStream();
            
            m_controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            await m_controller.StreamNotifications();

            // Assert
            m_mockNotificationService.Verify(
                x => x.RegisterNotificationHandler(It.IsAny<Func<McpNotification, Task>>()),
                Times.Once);
        }

        [Fact]
        public async Task StreamNotifications_WithSessionId_UsesProvidedSessionId()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Mcp-Session-Id"] = "custom-session-456";
            httpContext.RequestAborted = new CancellationToken(true);
            httpContext.Response.Body = new MemoryStream();
            
            m_controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            await m_controller.StreamNotifications();

            // Assert
            Assert.Equal("custom-session-456", httpContext.Response.Headers["Mcp-Session-Id"]);
        }

        [Fact]
        public async Task StreamNotifications_WithoutSessionId_GeneratesNewSessionId()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestAborted = new CancellationToken(true);
            httpContext.Response.Body = new MemoryStream();
            
            m_controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            await m_controller.StreamNotifications();

            // Assert
            Assert.True(httpContext.Response.Headers.ContainsKey("Mcp-Session-Id"));
            var sessionId = httpContext.Response.Headers["Mcp-Session-Id"].ToString();
            Assert.False(string.IsNullOrEmpty(sessionId));
            Assert.True(Guid.TryParse(sessionId, out _)); // Should be a valid GUID
        }

        [Fact]
        public void TestNotificationRequest_PropertiesSetCorrectly()
        {
            // Arrange & Act
            var request = new TestNotificationRequest
            {
                Method = "test/method",
                Params = new { key = "value" }
            };

            // Assert
            Assert.Equal("test/method", request.Method);
            Assert.NotNull(request.Params);
        }

        [Fact]
        public void TestNotificationRequest_DefaultValues()
        {
            // Act
            var request = new TestNotificationRequest();

            // Assert
            Assert.Equal(string.Empty, request.Method);
            Assert.Null(request.Params);
        }
    }
}

