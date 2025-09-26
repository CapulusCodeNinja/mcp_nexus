using Xunit;
using Microsoft.Extensions.DependencyInjection;
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
using System.Collections.Generic;

namespace mcp_nexus_tests.Integration
{
    public class NotificationIntegrationTests : IDisposable
    {
        private readonly ServiceProvider m_serviceProvider;
        private readonly IMcpNotificationService m_notificationService;
        private readonly IStdioNotificationBridge m_stdiooBridge;
        private readonly StringWriter m_stringWriter;
        private readonly TextWriter m_originalOut;

        public NotificationIntegrationTests()
        {
            // Setup DI container
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IMcpNotificationService, McpNotificationService>();
            services.AddSingleton<IStdioNotificationBridge, StdioNotificationBridge>();

            m_serviceProvider = services.BuildServiceProvider();
            m_notificationService = m_serviceProvider.GetRequiredService<IMcpNotificationService>();
            m_stdiooBridge = m_serviceProvider.GetRequiredService<IStdioNotificationBridge>();

            // Capture stdout
            m_originalOut = Console.Out;
            m_stringWriter = new StringWriter();
            Console.SetOut(m_stringWriter);
        }

        public void Dispose()
        {
            Console.SetOut(m_originalOut);
            m_stringWriter?.Dispose();
            m_serviceProvider?.Dispose();
        }

        [Fact]
        public async Task EndToEnd_NotificationFlow_WorksCorrectly()
        {
            // Arrange
            await m_stdiooBridge.InitializeAsync();

            // Act - Send a command status notification
            await m_notificationService.NotifyCommandStatusAsync(
                "cmd-integration-test",
                "!analyze -v", 
                "executing",
                75,
                "Integration test in progress",
                null,
                null);

            // Assert
            var output = m_stringWriter.ToString();
            Assert.NotEmpty(output);

            var parsed = JsonDocument.Parse(output.Trim());
            var root = parsed.RootElement;

            // Verify JSON-RPC structure
            Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
            Assert.Equal("notifications/commandStatus", root.GetProperty("method").GetString());

            // Verify command status parameters
            var paramsObj = root.GetProperty("params");
            Assert.Equal("cmd-integration-test", paramsObj.GetProperty("commandId").GetString());
            Assert.Equal("!analyze -v", paramsObj.GetProperty("command").GetString());
            Assert.Equal("executing", paramsObj.GetProperty("status").GetString());
            Assert.Equal(75, paramsObj.GetProperty("progress").GetInt32());
            Assert.Equal("Integration test in progress", paramsObj.GetProperty("message").GetString());
        }

        [Fact]
        public async Task EndToEnd_MultipleNotificationTypes_AllWork()
        {
            // Arrange
            await m_stdiooBridge.InitializeAsync();
            var notifications = new List<string>();

            // Clear the existing captured output
            m_stringWriter.GetStringBuilder().Clear();

            // Act - Send different notification types
            await m_notificationService.NotifyCommandStatusAsync("cmd1", "test", "queued");
            await m_notificationService.NotifyToolsListChangedAsync();
            await m_notificationService.NotifyServerHealthAsync("healthy", true, 2, 1);

            // Assert
            var output = m_stringWriter.ToString();
            var lines = output.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            Assert.Equal(3, lines.Length);

            // Verify each notification
            var commandStatus = JsonDocument.Parse(lines[0]);
            Assert.Equal("notifications/commandStatus", commandStatus.RootElement.GetProperty("method").GetString());

            var toolsChanged = JsonDocument.Parse(lines[1]);
            Assert.Equal("notifications/tools/list_changed", toolsChanged.RootElement.GetProperty("method").GetString());

            var serverHealth = JsonDocument.Parse(lines[2]);
            Assert.Equal("notifications/serverHealth", serverHealth.RootElement.GetProperty("method").GetString());

            // multiStringWriter.Dispose(); // Not needed anymore
        }

        [Fact]
        public async Task EndToEnd_ServiceRegistration_BothModesSupported()
        {
            // This test verifies that both HTTP and stdio modes can use the same notification service
            
            // Arrange - Simulate both HTTP and stdio bridges registering
            var httpHandlerCalled = false;
            // var stdioHandlerCalled = false; // Not used in this test

            // Register HTTP-style handler
            m_notificationService.RegisterNotificationHandler(notification =>
            {
                httpHandlerCalled = true;
                return Task.CompletedTask;
            });

            // Register stdio bridge
            await m_stdiooBridge.InitializeAsync();

            // Override to track stdio calls
            var testStringWriter = new StringWriter();
            Console.SetOut(testStringWriter);

            // Act
            await m_notificationService.NotifyCommandStatusAsync("test", "cmd", "executing");

            // Assert
            Assert.True(httpHandlerCalled, "HTTP handler should be called");
            
            var stdioOutput = testStringWriter.ToString();
            Assert.NotEmpty(stdioOutput); // Stdio handler should produce output

            testStringWriter.Dispose();
        }

        [Fact]
        public async Task EndToEnd_NoHandlers_GracefulDegradation()
        {
            // Arrange - Don't initialize any bridges
            
            // Act - Should not throw, just log and continue
            var exception = await Record.ExceptionAsync(async () =>
            {
                await m_notificationService.NotifyCommandStatusAsync("test", "cmd", "executing");
            });

            // Assert
            Assert.Null(exception);
            
            // No output should be produced since no handlers registered
            var output = m_stringWriter.ToString();
            Assert.Empty(output);
        }
    }
}

