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
    [Route("mcp")]
    public class McpController(McpProtocolService mcpProtocolService, ILogger<McpController> logger)
        : ControllerBase
    {
        // PERFORMANCE: Reuse JSON options to avoid repeated allocations
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // Don't change property names - MCP protocol requires exact field names like 'jsonrpc'
        };
        [HttpPost]
        public async Task<IActionResult> HandleMcpRequest()
        {
            var sessionId = Request.Headers["Mcp-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            Response.Headers["Mcp-Session-Id"] = sessionId;

            OperationLogger.LogInfo(logger, OperationLogger.Operations.Http, "NEW MCP REQUEST (Session: {SessionId})", sessionId);
            // PERFORMANCE: Only log headers in debug mode to avoid expensive string operations
            if (logger.IsEnabled(LogLevel.Debug))
            {
                OperationLogger.LogDebug(logger, OperationLogger.Operations.Http, "Request Headers: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value.ToArray())}")));
            }

            // Set up standard JSON response headers (NOT SSE)
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Mcp-Session-Id";

            logger.LogDebug("Set JSON response headers");

            try
            {
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                logger.LogDebug("Received MCP request body (Session: {SessionId}): {RequestBody}", sessionId, requestBody);

                var requestElement = JsonSerializer.Deserialize<JsonElement>(requestBody);

                // Extract and log key request details
                var method = requestElement.TryGetProperty("method", out var methodProp) ? methodProp.GetString() ?? "unknown" : "unknown";
                var id = requestElement.TryGetProperty("id", out var idProp) ? idProp.ToString() : "unknown";
                var jsonrpc = requestElement.TryGetProperty("jsonrpc", out var jsonrpcProp) ? jsonrpcProp.GetString() ?? "unknown" : "unknown";

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Mcp, "Parsed JSON successfully - Method: '{Method}', ID: '{Id}', JsonRPC: '{JsonRpc}'", method, id, jsonrpc);

                if (requestElement.TryGetProperty("params", out var paramsProp))
                {
                    // PERFORMANCE: Only serialize params in debug mode to avoid unnecessary allocations
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Request Params: {Params}", JsonSerializer.Serialize(paramsProp, s_jsonOptions));
                    }
                }
                else
                {
                    logger.LogDebug("No params in request");
                }

                var response = await mcpProtocolService.ProcessRequest(requestElement);
                logger.LogInformation("ProcessRequest completed for method '{Method}' - Response type: {ResponseType}", method, response.GetType().Name);

                // PERFORMANCE: Only serialize response in debug mode to avoid unnecessary allocations
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    var responseJson = JsonSerializer.Serialize(response, s_jsonOptions);
                    logger.LogDebug("Full JSON response:\n{Response}", responseJson);
                }

                logger.LogInformation("Sending response for method '{Method}' (Session: {SessionId})", method, sessionId);

                return Ok(response);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "JSON parsing error for session {SessionId}", sessionId);

                var errorResponse = new
                {
                    jsonrpc = "2.0",
                    id = (object?)null,
                    error = new { code = -32700, message = $"Parse error: {ex.Message}" }
                };

                return Ok(errorResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception for session {SessionId}", sessionId);

                var errorResponse = new
                {
                    jsonrpc = "2.0",
                    id = (object?)null,
                    error = new { code = -32000, message = $"Server error: {ex.Message}" }
                };

                return Ok(errorResponse);
            }
        }

        [HttpGet]
        public IActionResult HandleMcpGetRequest()
        {
            logger.LogInformation("=== MCP GET REQUEST ===");

            // Set up standard JSON response headers (NOT SSE)
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Mcp-Session-Id";

            logger.LogDebug("GET request - returning server info as JSON");

            // Return server information using proper typed models
            var serverInfo = new McpServerInfoResponse();

            return Ok(serverInfo);
        }

        [HttpOptions]
        public IActionResult HandlePreflight()
        {
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Mcp-Session-Id";
            return Ok();
        }
    }
}


