using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Tools;
using mcp_nexus.Exceptions;

namespace mcp_nexus.Protocol
{
    public class McpToolExecutionService(
        SessionAwareWindbgTool sessionAwareWindbgTool,
        ILogger<McpToolExecutionService> logger)
    {
        public async Task<object> ExecuteTool(string toolName, JsonElement arguments)
        {
            logger.LogDebug("Executing tool: {ToolName}", toolName);

            try
            {
                return toolName switch
                {
                    "nexus_open_dump" => await ExecuteOpenWindbgDump(arguments),
                    "nexus_close_dump" => await ExecuteCloseWindbgDump(),
                    "nexus_exec_debugger_command_async" => await ExecuteRunWindbgCmdAsync(arguments),
                    "nexus_debugger_command_status" => await ExecuteGetCommandStatus(arguments),
                    _ => throw new McpToolException(-32602, $"Unknown tool: {toolName}")
                };
            }
            catch (McpToolException)
            {
                // Re-throw MCP tool exceptions to be handled by the protocol service
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
                throw new McpToolException(-32603, ex.Message, ex);
            }
        }

        private async Task<object> ExecuteOpenWindbgDump(JsonElement arguments)
        {
            // Open dump file using session-aware implementation
            logger.LogDebug("Opening dump file: {DumpPath}", arguments);
            
            var dumpPath = GetRequiredStringArgument(arguments, "dumpPath");
            if (dumpPath == null)
                throw new McpToolException(-32602, "❌ MISSING DUMP PATH: You must provide a 'dumpPath' parameter! " +
                    "🔧 RECOVERY: Add 'dumpPath' parameter with full path to a .dmp file " +
                    "💡 EXAMPLE: {\"dumpPath\": \"C:\\\\path\\\\to\\\\crash.dmp\"}");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await sessionAwareWindbgTool.nexus_open_dump(dumpPath, symbolsPath);

            // Return the structured JSON response (not wrapped in text)
            return JsonSerializer.Deserialize<object>(result) ?? new object();
        }

        private Task<object> ExecuteCloseWindbgDump()
        {
            // Sessions automatically expire after 30 minutes of inactivity, but allow manual close for good practice
            logger.LogInformation("Manual session closure requested - this is good practice even though sessions auto-expire");
            
            return Task.FromResult<object>(new
            {
                success = true,
                message = "✅ Session close acknowledged! Sessions automatically expire after 30 minutes of inactivity, but manual closure is good practice.",
                info = "Sessions will clean up automatically, but you're following best practices by explicitly closing."
            });
        }

        private async Task<object> ExecuteRunWindbgCmdAsync(JsonElement arguments)
        {
            // Extract command and sessionId from arguments
            var command = GetRequiredStringArgument(arguments, "command");
            var sessionId = GetOptionalStringArgument(arguments, "sessionId");
            
            if (command == null)
                throw new McpToolException(-32602, "❌ MISSING COMMAND: You must provide a 'command' parameter. " +
                    "🔧 RECOVERY: Add 'command' parameter with a WinDbg command like '!analyze -v', 'k', 'lm', etc.");
                
            // FALLBACK AUTO-DETECTION: Not recommended, but helps AI clients that don't follow proper workflow
            if (sessionId == null)
            {
                logger.LogWarning("⚠️ IMPROPER API USAGE: sessionId parameter missing - falling back to auto-detection");
                logger.LogWarning("🚨 THIS IS NOT RECOMMENDED: Always include sessionId for proper API usage");
                
                // Get all active sessions and find the most recent one
                var activeSessions = await sessionAwareWindbgTool.GetActiveSessionsAsync();
                if (activeSessions?.Any() == true)
                {
                    if (activeSessions.Count() > 1)
                    {
                        logger.LogWarning("⚠️ MULTIPLE SESSIONS DETECTED: Found {Count} active sessions, using most recent", activeSessions.Count());
                        var sessionList = string.Join(", ", activeSessions.Select(s => s.SessionId));
                        logger.LogWarning("📋 Available sessions: {SessionList}", sessionList);
                    }
                    
                    sessionId = activeSessions.OrderByDescending(s => s.CreatedAt).First().SessionId;
                    logger.LogWarning("🔍 AUTO-DETECTED session: {SessionId} (most recent active session)", sessionId);
                    logger.LogWarning("💡 BEST PRACTICE: Include sessionId explicitly: {{\"command\": \"{Command}\", \"sessionId\": \"{SessionId}\"}}", command, sessionId);
                }
                else
                {
                    throw new McpToolException(-32602, "❌ NO ACTIVE SESSIONS FOUND: You must provide a 'sessionId' parameter or have an active session! " +
                        "🔧 RECOVERY STEPS: " +
                        "1️⃣ First call nexus_open_dump with a .dmp file path to create a session " +
                        "2️⃣ Extract the 'sessionId' from the response JSON " +
                        "3️⃣ Retry this command with both 'command' AND 'sessionId' parameters " +
                        "4️⃣ REMEMBER: This command returns commandId, then call nexus_debugger_command_status(commandId) for results " +
                        "💡 EXAMPLE: {\"command\": \"!analyze -v\", \"sessionId\": \"sess-000001-abc12345\"} " +
                        "🚨 ASYNC WORKFLOW: nexus_exec_debugger_command_async → nexus_debugger_command_status");
                }
            }

            logger.LogDebug("Executing command '{Command}' for session '{SessionId}'", command, sessionId);
            
            try
            {
                var result = await sessionAwareWindbgTool.nexus_exec_debugger_command_async(sessionId, command);
                
                // If auto-detection was used, modify the JSON response to include warning
                if (GetOptionalStringArgument(arguments, "sessionId") == null)
                {
                    // Parse the JSON response to modify it
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(result);
                    var originalMessage = jsonResponse.GetProperty("message").GetString() ?? "";
                    var commandIdValue = jsonResponse.GetProperty("commandId").GetString() ?? "";
                    
                    var warningMessage = "🚨 AUTO-DETECTION WARNING: sessionId was missing and auto-detected!\n" +
                                       "⚠️ THIS IS NOT RECOMMENDED: Always include sessionId parameter for proper API usage.\n" +
                                       $"💡 CORRECT USAGE: {{\"command\": \"{command}\", \"sessionId\": \"{sessionId}\"}}\n" +
                                       "🎯 Auto-detection used most recent session - this may not be what you intended!\n\n" +
                                       "🚨 ASYNC WORKFLOW REMINDER: This command only returns a commandId!\n" +
                                       "🔄 NEXT STEP REQUIRED: Call nexus_debugger_command_status(commandId) to get actual results!\n" +
                                       "📡 Commands execute asynchronously - don't expect immediate results!\n\n" +
                                       "--- ORIGINAL RESPONSE ---\n" + originalMessage;
                    
                    // Create a modified response with the warning but preserve the simple structure
                    var modifiedResponse = new
                    {
                        commandId = commandIdValue,
                        sessionId = sessionId,
                        message = warningMessage,
                        nextStep = $"nexus_debugger_command_status('{commandIdValue}')",
                        warning = "AUTO_DETECTION_USED"
                    };
                    
                    return modifiedResponse;
                }
                
                // Return the structured JSON response (not just text)
                return JsonSerializer.Deserialize<object>(result) ?? new object();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute command '{Command}' for session '{SessionId}'", command, sessionId);
                throw new McpToolException(-32603, $"Command execution failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ExecuteGetCommandStatus(JsonElement arguments)
        {
            // Get command status using session-aware implementation
            var commandId = GetRequiredStringArgument(arguments, "commandId");
            if (commandId == null)
                throw new McpToolException(-32602, "❌ MISSING COMMAND ID: You must provide a 'commandId' parameter! " +
                    "🔧 RECOVERY: Add 'commandId' parameter from nexus_exec_debugger_command_async response " +
                    "🚨 ASYNC WORKFLOW: nexus_exec_debugger_command_async returns commandId → use it here to get results " +
                    "💡 EXAMPLE: {\"commandId\": \"cmd-12345-abc\"} " +
                    "📡 This is how you get actual debugger command output!");

            logger.LogDebug("Getting command status for commandId: {CommandId}", commandId);
            
            try
            {
                var result = await sessionAwareWindbgTool.nexus_debugger_command_status(commandId);
                // Return the structured JSON response (not wrapped in text)
                return JsonSerializer.Deserialize<object>(result) ?? new object();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get command status for commandId: {CommandId}", commandId);
                throw new McpToolException(-32603, $"Command status check failed: {ex.Message}", ex);
            }
        }



        private static string? GetRequiredStringArgument(JsonElement arguments, string name)
        {
            if (!arguments.TryGetProperty(name, out var property))
                return null;

            var value = property.GetString();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static string? GetOptionalStringArgument(JsonElement arguments, string name)
        {
            return arguments.TryGetProperty(name, out var property) ? property.GetString() : null;
        }

        private static object CreateTextResult(string text)
        {
            return new McpToolResult
            {
                Content =
                [
                    new McpContent
                    {
                        Type = "text",
                        Text = text
                    }
                ]
            };
        }

    }
}







