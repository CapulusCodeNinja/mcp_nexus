using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using mcp_nexus.Services;
using mcp_nexus.Models;

namespace mcp_nexus.Controllers
{
    [ApiController]
    [Route("mcp")]
    public class McpController(McpProtocolService mcpProtocolService, ILogger<McpController> logger)
        : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> HandleMcpRequest()
        {
            var sessionId = Request.Headers["Mcp-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            Response.Headers["Mcp-Session-Id"] = sessionId;

            OperationLogger.LogInfo(logger, OperationLogger.Operations.Http, "NEW MCP REQUEST (Session: {SessionId})", sessionId);
            OperationLogger.LogDebug(logger, OperationLogger.Operations.Http, "Request Headers: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value.ToArray())}")));

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
                    logger.LogDebug("Request Params: {Params}", JsonSerializer.Serialize(paramsProp, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
                }
                else
                {
                    logger.LogDebug("No params in request");
                }

                var response = await mcpProtocolService.ProcessRequest(requestElement);
                logger.LogInformation("ProcessRequest completed for method '{Method}' - Response type: {ResponseType}", method, response.GetType().Name);

                var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                logger.LogInformation("Sending response for method '{Method}' (Session: {SessionId})", method, sessionId);
                logger.LogDebug("Full JSON response:\n{Response}", responseJson);

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

