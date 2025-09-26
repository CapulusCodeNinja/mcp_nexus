using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Debugger;
using mcp_nexus.Exceptions;

namespace mcp_nexus.Protocol
{
    public class McpProtocolService(
        McpToolDefinitionService m_toolDefinitionService,
        McpToolExecutionService m_toolExecutionService,
        McpResourceService m_resourceService,
        ILogger<McpProtocolService> m_logger)
    {
        public async Task<object?> ProcessRequest(JsonElement requestElement)
        {
            object? requestId = null;
            try
            {
                var request = ParseRequest(requestElement);
                if (request == null)
                {
                    return CreateErrorResponse(null, -32600, "Invalid Request - malformed JSON-RPC");
                }

                requestId = request.Id;
                m_logger.LogDebug("Processing MCP request: {Method}", request.Method);

                var result = await ExecuteMethod(request);
                
                // Handle notifications - they should not return responses
                if (result == null)
                {
                    return null; // No response for notifications
                }
                
                return CreateSuccessResponse(request.Id, result);
            }
            catch (McpToolException ex)
            {
                m_logger.LogWarning(ex, "MCP tool error: {Message}", ex.Message);
                return CreateErrorResponse(requestId, ex.ErrorCode, ex.Message, ex.ErrorData);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error processing MCP request");
                return CreateErrorResponse(requestId, -32603, "Internal error", ex.Message);
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
                    Id = requestElement.TryGetProperty("id", out var idProperty)
                        ? idProperty.ValueKind switch
                        {
                            JsonValueKind.Number => idProperty.GetInt32(),
                            JsonValueKind.String => idProperty.GetString(),
                            _ => null
                        }
                        : null
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

        private async Task<object?> ExecuteMethod(McpRequest request)
        {
            return request.Method switch
            {
                "initialize" => HandleInitialize(),
                "notifications/initialized" => HandleNotificationInitialized(),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCall(request.Params),
                "resources/list" => HandleResourcesList(),
                "resources/read" => await HandleResourcesRead(request.Params),
                "notifications/cancelled" => HandleNotificationCancelled(request.Params),
                _ => throw new McpToolException(-32601, $"Method not found: {request.Method}")
            };
        }

        private object HandleInitialize()
        {
            // Return initialization response
            return new McpInitializeResult();
        }

        private object? HandleNotificationInitialized()
        {
            m_logger.LogDebug("Received MCP initialization notification");
            // Notifications should not return responses according to JSON-RPC spec
            return null; // Return null to indicate no response should be sent
        }

        private object? HandleNotificationCancelled(JsonElement? paramsElement)
        {
            if (paramsElement != null && paramsElement.Value.TryGetProperty("requestId", out var requestIdProp))
            {
                var requestId = requestIdProp.ToString();
                m_logger.LogInformation("Received cancellation notification for request ID: {RequestId}", requestId);

                if (paramsElement.Value.TryGetProperty("reason", out var reasonProp))
                {
                    var reason = reasonProp.GetString();
                    m_logger.LogDebug("Cancellation reason: {Reason}", reason);
                }

                // TODO: Implement cancellation through session manager or tool execution service
                // In the new session-aware architecture, cancellation should be handled per-session
                m_logger.LogDebug("Cancellation request received for request ID: {RequestId} (session-aware cancellation not yet implemented)", requestId);
            }
            else
            {
                m_logger.LogDebug("Received cancellation notification without request ID");
            }

            // Notifications should not return responses according to JSON-RPC spec
            return null; // Return null to indicate no response should be sent
        }

        private object HandleToolsList()
        {
            var tools = m_toolDefinitionService.GetAllTools();
            return new McpToolsListResult { Tools = tools };
        }

        private object HandleResourcesList()
        {
            var resources = m_resourceService.GetAllResources();
            return new McpResourcesListResult { Resources = resources };
        }

        private async Task<object> HandleResourcesRead(JsonElement? paramsElement)
        {
            if (paramsElement == null || !paramsElement.Value.TryGetProperty("uri", out var uriProperty))
            {
                throw new McpToolException(-32602, "Missing required parameter: uri");
            }

            var uri = uriProperty.GetString();
            if (string.IsNullOrEmpty(uri))
            {
                throw new McpToolException(-32602, "Invalid uri parameter");
            }

            return await m_resourceService.ReadResource(uri);
        }

        private async Task<object> HandleToolsCall(JsonElement? paramsElement)
        {
            if (paramsElement == null)
            {
                throw new McpToolException(-32602, "Missing params");
            }

            var @params = paramsElement.Value;

            if (!@params.TryGetProperty("name", out var nameProperty))
            {
                throw new McpToolException(-32602, "Missing tool name");
            }

            var toolName = nameProperty.GetString();
            if (string.IsNullOrEmpty(toolName))
            {
                throw new McpToolException(-32602, "Invalid tool name");
            }

            var arguments = @params.TryGetProperty("arguments", out var argsProperty)
                ? argsProperty
                : new JsonElement();

            return await m_toolExecutionService.ExecuteTool(toolName, arguments);
        }

        private static McpSuccessResponse CreateSuccessResponse(object? id, object result)
        {
            return new McpSuccessResponse
            {
                Id = id,
                Result = result
            };
        }

        private static McpErrorResponse CreateErrorResponse(object? id, int code, string message, object? data = null)
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

    }
}

