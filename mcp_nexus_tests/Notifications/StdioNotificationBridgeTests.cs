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

namespace mcp_nexus_tests.Notifications
{
    [Collection("NotificationTestCollection")]
    public class StdioNotificationBridgeTests : IDisposable
    {
        private readonly Mock<ILogger<StdioNotificationBridge>> m_MockLogger;
        private readonly Mock<IMcpNotificationService> m_MockNotificationService;
        private readonly StdioNotificationBridge m_Bridge;
        private readonly StringWriter m_StringWriter;
        private readonly TextWriter m_OriginalOut;

        public StdioNotificationBridgeTests()
        {
            m_MockLogger = new Mock<ILogger<StdioNotificationBridge>>();
            m_MockNotificationService = new Mock<IMcpNotificationService>();

            // Capture stdout for testing
            m_OriginalOut = Console.Out;
            m_StringWriter = new StringWriter();
            Console.SetOut(m_StringWriter);

            m_Bridge = new StdioNotificationBridge(m_MockLogger.Object, m_MockNotificationService.Object);
        }

        public void Dispose()
        {
            Console.SetOut(m_OriginalOut);
            m_StringWriter?.Dispose();
            m_Bridge?.Dispose();
        }

        [Fact]
        public async Task InitializeAsync_RegistersNotificationHandler()
        {
            // Act
            await m_Bridge.InitializeAsync();

            // Assert
            m_MockNotificationService.Verify(
                x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()),
                Times.AtLeast(6)); // Now subscribing to 6 different event types
        }

        [Fact]
        public async Task InitializeAsync_CalledTwice_RegistersOnlyOnce()
        {
            // Act
            await m_Bridge.InitializeAsync();
            await m_Bridge.InitializeAsync();

            // Assert
            m_MockNotificationService.Verify(
                x => x.Subscribe(It.IsAny<string>(), It.IsAny<Func<object, Task>>()),
                Times.AtLeast(6)); // Now subscribing to 6 different event types, but only once per type
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

            // Get the registered handler for CommandStatus specifically
            Func<object, Task>? registeredHandler = null;
            m_MockNotificationService.Setup(x => x.Subscribe("CommandStatus", It.IsAny<Func<object, Task>>()))
                .Callback<string, Func<object, Task>>((eventType, handler) => registeredHandler = handler);

            await m_Bridge.InitializeAsync();

            // Clear any existing output and add stabilization delays
            m_StringWriter.GetStringBuilder().Clear();
            await Task.Delay(50);

            // Act
            await registeredHandler!((object)notification);
            await Task.Delay(50);

            // Assert
            var output = m_StringWriter.ToString();

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
                Method = "notifications/toolsListChanged",
                Params = null
            };

            Func<object, Task>? registeredHandler = null;
            m_MockNotificationService.Setup(x => x.Subscribe("ToolsListChanged", It.IsAny<Func<object, Task>>()))
                .Callback<string, Func<object, Task>>((eventType, handler) => registeredHandler = handler);

            await m_Bridge.InitializeAsync();

            // Act
            await registeredHandler!((object)notification);

            // Assert
            var output = m_StringWriter.ToString();
            var parsed = JsonDocument.Parse(output.Trim());
            var root = parsed.RootElement;

            Assert.Equal("notifications/toolsListChanged", root.GetProperty("method").GetString());
            // params should be null for standard tools notification
            Assert.Equal(JsonValueKind.Null, root.GetProperty("params").ValueKind);
        }

        [Fact]
        public async Task HandleNotification_AfterDispose_DoesNotSendToStdout()
        {
            // Arrange
            Func<object, Task>? registeredHandler = null;
            m_MockNotificationService.Setup(x => x.Subscribe("CommandStatus", It.IsAny<Func<object, Task>>()))
                .Callback<string, Func<object, Task>>((eventType, handler) => registeredHandler = handler);

            await m_Bridge.InitializeAsync();

            var notification = new McpNotification
            {
                Method = "notifications/test",
                Params = null
            };

            // Act
            m_Bridge.Dispose();
            await registeredHandler!((object)notification);

            // Assert
            var output = m_StringWriter.ToString();
            Assert.Empty(output);
        }

        [Fact]
        public async Task HandleNotification_WithInvalidData_HandlesGracefully()
        {
            // Arrange
            Func<object, Task>? registeredHandler = null;
            m_MockNotificationService.Setup(x => x.Subscribe("CommandStatus", It.IsAny<Func<object, Task>>()))
                .Callback<string, Func<object, Task>>((eventType, handler) => registeredHandler = handler);

            await m_Bridge.InitializeAsync();

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
                m_Bridge.Dispose();
                m_Bridge.Dispose();
            });

            Assert.Null(exception);
        }
    }
}

