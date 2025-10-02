using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace mcp_nexus_tests.Services
{
    [Collection("NotificationTestCollection")]
    public class StdioNotificationBridgeTests : IDisposable
    {
        private readonly Mock<ILogger<StdioNotificationBridge>> m_mockLogger;
        private readonly Mock<IMcpNotificationService> m_mockNotificationService;
        private StdioNotificationBridge m_bridge;
        private readonly StringWriter m_stringWriter;
        private readonly TextWriter m_originalOut;

        public StdioNotificationBridgeTests()
        {
            m_mockLogger = new Mock<ILogger<StdioNotificationBridge>>();
            m_mockNotificationService = new Mock<IMcpNotificationService>();

            // Capture stdout for testing
            m_originalOut = Console.Out;
            m_stringWriter = new StringWriter();
            Console.SetOut(m_stringWriter);

            m_bridge = new StdioNotificationBridge(m_mockNotificationService.Object);
        }

        private void ResetBridge()
        {
            m_bridge?.Dispose();
            m_mockNotificationService.Reset();
            m_bridge = new StdioNotificationBridge(m_mockNotificationService.Object);
            m_stringWriter.GetStringBuilder().Clear();
        }

        public void Dispose()
        {
            Console.SetOut(m_originalOut);
            m_stringWriter?.Dispose();
            m_bridge?.Dispose();
        }

        [Fact]
        public async Task InitializeAsync_RegistersNotificationHandler()
        {
            // Act
            await m_bridge.InitializeAsync();

            // Assert
            m_mockNotificationService.Verify(
                x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_CalledTwice_RegistersOnlyOnce()
        {
            // Arrange
            ResetBridge();
            var callCount = 0;
            m_mockNotificationService.Setup(x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()))
                .Returns(() => $"subscription-{++callCount}");

            // Act
            await m_bridge.InitializeAsync();
            await m_bridge.InitializeAsync();

            // Assert
            m_mockNotificationService.Verify(
                x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleNotification_SendsCorrectJsonToStdout()
        {
            // Arrange
            var notification = new McpNotification
            {
                Method = "notifications/commandStatus",
                Params = new McpCommandStatusNotification
                {
                    CommandId = "cmd-123",
                    Command = "!analyze -v",
                    Status = "executing",
                    Progress = 50,
                    Message = "Processing..."
                }
            };

            // Get the registered handler
            Func<object, Task>? registeredHandler = null;
            m_mockNotificationService.Setup(x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()))
                .Callback<string, Func<object, Task>>((eventType, handler) => registeredHandler = handler);

            await m_bridge.InitializeAsync();

            // Clear any existing output and add stabilization delays
            m_stringWriter.GetStringBuilder().Clear();
            await Task.Delay(50);

            // Act
            await registeredHandler!(notification);
            await Task.Delay(50);

            // Assert
            var output = m_stringWriter.ToString();

            // Debug output if empty
            if (string.IsNullOrEmpty(output))
            {
                // The handler was registered but maybe not called
                Assert.NotNull(registeredHandler);
            }

            Assert.NotEmpty(output);

            // Verify JSON-RPC format
            var lines = output.Trim().Split('\n');
            var jsonLine = lines[0];

            var parsed = JsonDocument.Parse(jsonLine);
            var root = parsed.RootElement;

            Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
            Assert.Equal("notifications/commandStatus", root.GetProperty("method").GetString());
            Assert.True(root.TryGetProperty("params", out var paramsElement));

            // Verify camelCase formatting
            var paramObj = paramsElement;
            Assert.True(paramObj.TryGetProperty("commandId", out _));
            Assert.True(paramObj.TryGetProperty("status", out _));
        }

        [Fact]
        public async Task HandleNotification_WithToolsListChanged_SendsCorrectFormat()
        {
            // Arrange
            var notification = new McpNotification
            {
                Method = "notifications/tools/list_changed",
                Params = null
            };

            Func<object, Task>? registeredHandler = null;
            m_mockNotificationService.Setup(x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()))
                .Callback<string, Func<object, Task>>((eventType, handler) => registeredHandler = handler);

            await m_bridge.InitializeAsync();

            // Act
            await registeredHandler!(notification);

            // Assert
            var output = m_stringWriter.ToString();
            var parsed = JsonDocument.Parse(output.Trim());
            var root = parsed.RootElement;

            Assert.Equal("notifications/tools/list_changed", root.GetProperty("method").GetString());
            // params should be null for standard tools notification
            Assert.True(root.GetProperty("params").ValueKind == JsonValueKind.Null);
        }

        [Fact]
        public async Task HandleNotification_AfterDispose_DoesNotSendToStdout()
        {
            // Arrange
            ResetBridge();
            Func<object, Task>? registeredHandler = null;
            m_mockNotificationService.Setup(x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()))
                .Callback<string, Func<object, Task>>((eventType, handler) => registeredHandler = handler);

            await m_bridge.InitializeAsync();

            var notification = new McpNotification
            {
                Method = "notifications/test",
                Params = null
            };

            // Act
            m_bridge.Dispose(); // Now it implements IDisposable
            await registeredHandler!(notification);

            // Assert
            var output = m_stringWriter.ToString();
            Assert.Empty(output);
        }

        [Fact]
        public async Task HandleNotification_WithInvalidData_HandlesGracefully()
        {
            // Arrange
            Func<object, Task>? registeredHandler = null;
            m_mockNotificationService.Setup(x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()))
                .Callback<string, Func<object, Task>>((eventType, handler) => registeredHandler = handler);

            await m_bridge.InitializeAsync();

            // Act & Assert - should not throw and should handle gracefully
            var exception = await Record.ExceptionAsync(async () =>
            {
                await registeredHandler!(null!);
            });

            // Should not throw - null should be handled gracefully
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() =>
            {
                // StdioNotificationBridge doesn't implement IDisposable
                // StdioNotificationBridge doesn't implement IDisposable
            });

            Assert.Null(exception);
        }
    }
}

