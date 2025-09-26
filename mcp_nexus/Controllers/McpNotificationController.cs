using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Models;

namespace mcp_nexus.Controllers
{
    [ApiController]
    [Route("mcp/notifications")]
    public class McpNotificationController : ControllerBase
    {
        private readonly IMcpNotificationService m_notificationService;
        private readonly ILogger<McpNotificationController> m_logger;

        public McpNotificationController(
            IMcpNotificationService notificationService,
            ILogger<McpNotificationController> logger)
        {
            m_notificationService = notificationService;
            m_logger = logger;
        }

        /// <summary>
        /// Server-Sent Events endpoint for real-time notifications
        /// </summary>
        [HttpGet("stream")]
        public async Task StreamNotifications()
        {
            var sessionId = Request.Headers["Mcp-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            
            m_logger.LogInformation("Starting notification stream for session: {SessionId}", sessionId);

            // Set up SSE headers
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Headers"] = "Mcp-Session-Id";
            Response.Headers["Mcp-Session-Id"] = sessionId;

            var cancellationToken = HttpContext.RequestAborted;
            var clientDisconnected = false;

            // Register notification handler for this client
            async Task NotificationHandler(McpNotification notification)
            {
                if (clientDisconnected || cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });

                    var sseData = $"data: {json}\n\n";
                    await Response.WriteAsync(sseData, cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);

                    m_logger.LogTrace("Sent notification to session {SessionId}: {Method}", sessionId, notification.Method);
                }
                catch (Exception ex)
                {
                    m_logger.LogWarning(ex, "Failed to send notification to session {SessionId}", sessionId);
                    clientDisconnected = true;
                }
            }

            m_notificationService.RegisterNotificationHandler(NotificationHandler);

            try
            {
                // Send initial connection confirmation
                var welcomeNotification = new McpNotification
                {
                    Method = "notifications/connected",
                    Params = new { sessionId, timestamp = DateTime.UtcNow, message = "Notification stream connected" }
                };

                await NotificationHandler(welcomeNotification);

                // Keep connection alive and send periodic heartbeats
                while (!cancellationToken.IsCancellationRequested && !clientDisconnected)
                {
                    try
                    {
                        // Send heartbeat every 30 seconds
                        await Task.Delay(30000, cancellationToken);
                        
                        if (!cancellationToken.IsCancellationRequested && !clientDisconnected)
                        {
                            var heartbeat = $"event: heartbeat\ndata: {{\"timestamp\":\"{DateTime.UtcNow:O}\"}}\n\n";
                            await Response.WriteAsync(heartbeat, cancellationToken);
                            await Response.Body.FlushAsync(cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Error sending heartbeat to session {SessionId}", sessionId);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_logger.LogDebug("Client disconnected from notification stream: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error in notification stream for session {SessionId}", sessionId);
            }
            finally
            {
                clientDisconnected = true;
                // Note: We can't unregister the handler due to ConcurrentBag limitations
                // In production, consider using a different collection type
                m_logger.LogDebug("Notification stream ended for session: {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// Test endpoint to manually trigger a notification (for testing purposes)
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationRequest request)
        {
            m_logger.LogDebug("Sending test notification: {Method}", request.Method);

            await m_notificationService.SendNotificationAsync(request.Method, request.Params);

            return Ok(new { message = "Test notification sent", method = request.Method });
        }
    }

    public class TestNotificationRequest
    {
        public string Method { get; set; } = string.Empty;
        public object? Params { get; set; }
    }
}

