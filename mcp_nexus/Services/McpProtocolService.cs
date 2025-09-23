using System.Text.Json;
using mcp_nexus.Models;

namespace mcp_nexus.Services
{
    public class McpProtocolService
    {
        private readonly McpToolDefinitionService _toolDefinitionService;
        private readonly McpToolExecutionService _toolExecutionService;
        private readonly ILogger<McpProtocolService> _logger;

        public McpProtocolService(
            McpToolDefinitionService toolDefinitionService,
            McpToolExecutionService toolExecutionService,
            ILogger<McpProtocolService> logger)
        {
            _toolDefinitionService = toolDefinitionService;
            _toolExecutionService = toolExecutionService;
            _logger = logger;
        }

        public async Task<object> ProcessRequest(JsonElement requestElement)
        {
            try
            {
                var request = ParseRequest(requestElement);
                if (request == null)
                {
                    return CreateErrorResponse(0, -32600, "Invalid Request - malformed JSON-RPC");
                }

                _logger.LogInformation("Processing MCP request: {Method}", request.Method);

                var result = await ExecuteMethod(request);
                return CreateSuccessResponse(request.Id, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MCP request");
                return CreateErrorResponse(0, -32603, "Internal error", ex.Message);
            }
        }

        private static McpRequest? ParseRequest(JsonElement requestElement)
        {
            try
            {
                if (!requestElement.TryGetProperty("method", out var methodProperty))
                    return null;

                var request = new McpRequest
                {
                    Method = methodProperty.GetString() ?? string.Empty,
                    Id = requestElement.TryGetProperty("id", out var idProperty) ? idProperty.GetInt32() : 0
                };

                if (requestElement.TryGetProperty("params", out var paramsProperty))
                {
                    request.Params = paramsProperty;
                }

                return request;
            }
            catch
            {
                return null;
            }
        }

        private async Task<object> ExecuteMethod(McpRequest request)
        {
            return request.Method switch
            {
                "initialize" => HandleInitialize(),
                "notifications/initialized" => HandleNotificationInitialized(),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCall(request.Params),
                "notifications/cancelled" => HandleNotificationCancelled(request.Params),
                _ => CreateMethodNotFoundError(request.Method)
            };
        }

        private object HandleInitialize()
        {
            // Return initialization response
            return new McpInitializeResult();
        }

        private object HandleNotificationInitialized()
        {
            _logger.LogInformation("Received MCP initialization notification");
            // For notifications, we typically return an empty success response
            return new { };
        }

        private object HandleNotificationCancelled(JsonElement? paramsElement)
        {
            if (paramsElement != null && paramsElement.Value.TryGetProperty("requestId", out var requestIdProp))
            {
                var requestId = requestIdProp.ToString();
                _logger.LogWarning("Received cancellation notification for request ID: {RequestId}", requestId);

                if (paramsElement.Value.TryGetProperty("reason", out var reasonProp))
                {
                    var reason = reasonProp.GetString();
                    _logger.LogWarning("Cancellation reason: {Reason}", reason);
                }
            }
            else
            {
                _logger.LogWarning("Received cancellation notification without request ID");
            }

            // For notifications, we return an empty success response
            // Note: In the future, we could implement actual cancellation logic here
            return new { };
        }

        private object HandleToolsList()
        {
            var tools = _toolDefinitionService.GetAllTools();
            return new McpToolsListResult { Tools = tools };
        }

        private async Task<object> HandleToolsCall(JsonElement? paramsElement)
        {
            if (paramsElement == null)
            {
                return CreateParameterError("Missing params");
            }

            var @params = paramsElement.Value;

            if (!@params.TryGetProperty("name", out var nameProperty))
            {
                return CreateParameterError("Missing tool name");
            }

            var toolName = nameProperty.GetString();
            if (string.IsNullOrEmpty(toolName))
            {
                return CreateParameterError("Invalid tool name");
            }

            var arguments = @params.TryGetProperty("arguments", out var argsProperty)
                ? argsProperty
                : new JsonElement();

            return await _toolExecutionService.ExecuteTool(toolName, arguments);
        }

        private static McpSuccessResponse CreateSuccessResponse(int id, object result)
        {
            return new McpSuccessResponse
            {
                Id = id,
                Result = result
            };
        }

        private static McpErrorResponse CreateErrorResponse(int id, int code, string message, object? data = null)
        {
            return new McpErrorResponse
            {
                Id = id,
                Error = new McpError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };
        }

        private static object CreateMethodNotFoundError(string method)
        {
            return new { error = new McpError { Code = -32601, Message = $"Method not found: {method}" } };
        }

        private static object CreateParameterError(string message)
        {
            return new { error = new McpError { Code = -32602, Message = message } };
        }
    }
}
