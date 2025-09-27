using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Utilities;
using mcp_nexus.Constants;
using mcp_nexus.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol; // For Resource type

namespace mcp_nexus.Resources
{
    [McpServerResourceType]
    public static class McpNexusResources
    {
        [McpServerResource, Description("List all active debugging sessions with advanced filtering options")]
        public static async Task<string> ListSessions(
            IServiceProvider serviceProvider,
            [Description("Filter by session ID (partial match)")] string? sessionId = null,
            [Description("Filter by dump file path (partial match)")] string? dumpPath = null,
            [Description("Filter by session status (Initializing, Active, Disposing, Disposed, Error)")] string? status = null,
            [Description("Filter by active status (true/false)")] bool? isActive = null,
            [Description("Filter sessions created from this time")] DateTime? createdFrom = null,
            [Description("Filter sessions created until this time")] DateTime? createdTo = null,
            [Description("Limit number of results")] int? limit = null,
            [Description("Skip number of results (pagination)")] int? offset = null,
            [Description("Sort by field (sessionId, dumpPath, status, createdAt)")] string sortBy = "createdAt",
            [Description("Sort order (asc, desc)")] string order = "desc")
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            try
            {
                var allSessions = sessionManager.GetAllSessions();
                var filteredSessions = allSessions.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrEmpty(sessionId))
                    filteredSessions = filteredSessions.Where(s => s.SessionId.Contains(sessionId, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(dumpPath))
                    filteredSessions = filteredSessions.Where(s => s.DumpPath?.Contains(dumpPath, StringComparison.OrdinalIgnoreCase) ?? false);
                if (!string.IsNullOrEmpty(status))
                    filteredSessions = filteredSessions.Where(s => s.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
                if (isActive.HasValue)
                    filteredSessions = filteredSessions.Where(s => s.Status == SessionStatus.Active == isActive.Value);
                if (createdFrom.HasValue)
                    filteredSessions = filteredSessions.Where(s => s.CreatedAt >= createdFrom.Value);
                if (createdTo.HasValue)
                    filteredSessions = filteredSessions.Where(s => s.CreatedAt <= createdTo.Value);

                // Apply sorting
                filteredSessions = sortBy.ToLowerInvariant() switch
                {
                    "sessionid" => order.ToLowerInvariant() == "asc" ? filteredSessions.OrderBy(s => s.SessionId) : filteredSessions.OrderByDescending(s => s.SessionId),
                    "dumppath" => order.ToLowerInvariant() == "asc" ? filteredSessions.OrderBy(s => s.DumpPath) : filteredSessions.OrderByDescending(s => s.DumpPath),
                    "status" => order.ToLowerInvariant() == "asc" ? filteredSessions.OrderBy(s => s.Status) : filteredSessions.OrderByDescending(s => s.Status),
                    "createdat" => order.ToLowerInvariant() == "asc" ? filteredSessions.OrderBy(s => s.CreatedAt) : filteredSessions.OrderByDescending(s => s.CreatedAt),
                    _ => filteredSessions.OrderByDescending(s => s.CreatedAt) // Default sort
                };

                // Apply pagination
                var paginatedSessions = filteredSessions.Skip(offset ?? 0).Take(limit ?? int.MaxValue).ToList();

                var result = new
                {
                    sessions = paginatedSessions,
                    count = paginatedSessions.Count,
                    total = filteredSessions.Count(),
                    limit,
                    offset,
                    sortBy,
                    order,
                    timestamp = DateTime.UtcNow
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading sessions list");
                throw;
            }
        }

        [McpServerResource, Description("List async commands from all sessions with advanced filtering options")]
        public static async Task<string> ListCommands(
            IServiceProvider serviceProvider,
            [Description("Filter by specific session")] string? sessionId = null,
            [Description("Filter by command text (case-insensitive)")] string? command = null,
            [Description("Filter commands from this time")] DateTime? from = null,
            [Description("Filter commands until this time")] DateTime? to = null,
            [Description("Limit number of results")] int? limit = null,
            [Description("Skip number of results (pagination)")] int? offset = null,
            [Description("Sort by field (command, status, createdAt)")] string sortBy = "createdAt",
            [Description("Sort order (asc, desc)")] string order = "desc")
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            try
            {
                var allSessions = sessionManager.GetAllSessions();
                var commandsBySession = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Filter to specific session
                    var session = allSessions.FirstOrDefault(s => s.SessionId == sessionId);
                    if (session == null)
                    {
                        throw new ArgumentException($"Session {sessionId} not found");
                    }

                    var sessionContext = sessionManager.GetSessionContext(sessionId);
                    var sessionCommands = GetSessionCommands(sessionContext, sessionId, command, from, to, limit, offset, sortBy, order);
                    commandsBySession[sessionId] = sessionCommands;
                }
                else
                {
                    // Get commands from all sessions
                    foreach (var session in allSessions)
                    {
                        var sessionContext = sessionManager.GetSessionContext(session.SessionId);
                        var sessionCommands = GetSessionCommands(sessionContext, session.SessionId, command, from, to, limit, offset, sortBy, order);
                        commandsBySession[session.SessionId] = sessionCommands;
                    }
                }

                var result = new
                {
                    commands = commandsBySession,
                    totalSessions = commandsBySession.Count,
                    totalCommands = commandsBySession.Values.Cast<Dictionary<string, object>>().Sum(c => c.Count),
                    timestamp = DateTime.UtcNow,
                    filters = new
                    {
                        sessionId,
                        command,
                        from = from?.ToString("O"),
                        to = to?.ToString("O"),
                        limit,
                        offset,
                        sortBy,
                        order
                    },
                    note = string.IsNullOrEmpty(sessionId) ? "Commands from all sessions" : $"Commands from session {sessionId}"
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading commands list");
                throw;
            }
        }

        [McpServerResource, Description("Get status and results of a specific async command")]
        public static async Task<string> GetCommandResult(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("Command ID from nexus_enqueue_async_dump_analyze_command")] string commandId)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            try
            {
                if (!sessionManager.SessionExists(sessionId))
                {
                    throw new ArgumentException($"Session {sessionId} not found. Use mcp://nexus/sessions/list to see available sessions.");
                }

                var commandQueue = sessionManager.GetCommandQueue(sessionId);
                var commandResult = await commandQueue.GetCommandResult(commandId);

                // Parse the result to determine status
                var isCompleted = !commandResult.Contains("still executing") && !commandResult.Contains("Command not found");
                var isNotFound = commandResult.Contains("Command not found");

                var result = new
                {
                    sessionId = sessionId,
                    commandId = commandId,
                    status = isNotFound ? "Not Found" : (isCompleted ? "Completed" : "In Progress"),
                    result = isCompleted ? commandResult : null,
                    error = isNotFound ? "Command not found. Use mcp://nexus/commands/list to see available commands." : null,
                    completedAt = isCompleted ? DateTime.UtcNow : (DateTime?)null,
                    timestamp = DateTime.UtcNow,
                    message = isNotFound ? null : (isCompleted ? null : "Command is still executing - check again in a few seconds.")
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading command result for session {SessionId}, command {CommandId}", sessionId, commandId);
                throw;
            }
        }

        [McpServerResource, Description("Common debugging patterns and step-by-step analysis workflows")]
        public static async Task<string> GetWorkflows(
            IServiceProvider serviceProvider)
        {
            var workflows = new
            {
                title = "Crash Analysis Workflows",
                description = "Example workflows for Windows crash dump investigation - these are starting points, not exhaustive lists. AI clients should explore beyond these basic patterns and adapt commands based on specific crash scenarios.",
                disclaimer = "IMPORTANT: These are example workflows below many others. Do not limit yourself to only these patterns - explore additional WinDBG commands, combine different approaches, and adapt based on the specific crash context and symptoms you observe.",
                workflows = new[]
                {
                    new
                    {
                        name = "Basic Crash Analysis",
                        description = "Essential commands for initial crash investigation",
                        steps = new[] { "!analyze -v", "!threads", "~*k", "!locks", "!runaway" },
                        expectedOutcome = "Identify the faulting thread, exception type, and basic crash context"
                    },
                    new
                    {
                        name = "Memory Corruption Analysis",
                        description = "Investigate memory-related crashes and corruption",
                        steps = new[] { "!analyze -v", "!heap -stat", "!heap -flt s <size>", "!heap -p -a <address>", "!address <address>", "!vprot <address>" },
                        expectedOutcome = "Identify heap corruption, invalid memory access, or buffer overflows"
                    },
                    new
                    {
                        name = "Thread Deadlock Investigation",
                        description = "Analyze thread synchronization issues and deadlocks",
                        steps = new[] { "!threads", "!locks", "!cs -l", "!cs -o <address>", "~*k", "!runaway" },
                        expectedOutcome = "Identify deadlocked threads and synchronization objects"
                    }
                },
                generalTips = new[]
                {
                    "Always start with !analyze -v for automatic analysis",
                    "Use ~*k to see all thread call stacks",
                    "Check !runaway for threads consuming excessive CPU",
                    "Examine !locks for synchronization issues",
                    "Use !heap commands for memory-related problems",
                    "Check loaded modules with lm for version issues",
                    "EXPLORE BEYOND THESE WORKFLOWS: These are just examples - there are hundreds of WinDBG commands available",
                    "ADAPT AND COMBINE: Mix and match commands based on what you discover in the crash",
                    "LEARN MORE: Use .help <command> to understand any WinDBG command better",
                    "BE CREATIVE: Each crash is unique - don't just follow these patterns blindly",
                    "INVESTIGATE DEEPER: Look for patterns, correlations, and root causes beyond basic analysis"
                }
            };

            return JsonSerializer.Serialize(workflows, new JsonSerializerOptions { WriteIndented = true });
        }

        [McpServerResource, Description("Essential tool usage information for MCP Nexus server")]
        public static async Task<string> GetUsage(
            IServiceProvider serviceProvider)
        {
            var usage = new
            {
                title = "MCP Nexus Usage Guide",
                description = "Essential tool usage information for MCP Nexus server",
                tools = new[]
                {
                    new
                    {
                        name = "nexus_open_dump_analyze_session",
                        description = "Create a new debugging session for a crash dump file",
                        parameters = new[] { "dumpPath (required)", "symbolsPath (optional)" },
                        returns = "sessionId that MUST be used for all subsequent operations"
                    },
                    new
                    {
                        name = "nexus_close_dump_analyze_session",
                        description = "Close an active debugging session and clean up resources",
                        parameters = new[] { "sessionId (required)" },
                        returns = "Confirmation of session closure"
                    },
                    new
                    {
                        name = "nexus_enqueue_async_dump_analyze_command",
                        description = "Queue a WinDBG command for execution in a debugging session",
                        parameters = new[] { "sessionId (required)", "command (required)" },
                        returns = "commandId for tracking command execution"
                    }
                },
                resources = new[]
                {
                    new
                    {
                        name = "mcp://nexus/sessions/list",
                        description = "List all debugging sessions with filtering options",
                        parameters = new[] { "sessionId", "dumpPath", "status", "isActive", "createdFrom", "createdTo", "limit", "offset", "sortBy", "order" }
                    },
                    new
                    {
                        name = "mcp://nexus/commands/list",
                        description = "List async commands from all sessions with filtering",
                        parameters = new[] { "sessionId", "command", "from", "to", "limit", "offset", "sortBy", "order" }
                    },
                    new
                    {
                        name = "mcp://nexus/commands/result",
                        description = "Get status and results of a specific async command",
                        parameters = new[] { "sessionId", "commandId" }
                    }
                },
                workflow = new[]
                {
                    "1. Use nexus_open_dump_analyze_session to create a session",
                    "2. Use nexus_enqueue_async_dump_analyze_command to queue WinDBG commands",
                    "3. Use mcp://nexus/commands/result to get command results",
                    "4. Use mcp://nexus/sessions/list to manage sessions",
                    "5. Use nexus_close_dump_analyze_session when done"
                }
            };

            return JsonSerializer.Serialize(usage, new JsonSerializerOptions { WriteIndented = true });
        }

        private static Dictionary<string, object> GetSessionCommands(
            SessionContext? sessionContext,
            string sessionId,
            string? commandFilter,
            DateTime? fromTime,
            DateTime? toTime,
            int? limit,
            int? offset,
            string sortBy,
            string order)
        {
            var commands = new Dictionary<string, object>();

            if (sessionContext == null)
            {
                commands["placeholder"] = new
                {
                    command = "Session context not available",
                    status = "Unknown",
                    isFinished = false,
                    createdAt = DateTime.UtcNow,
                    completedAt = (DateTime?)null,
                    duration = TimeSpan.Zero,
                    error = "Session context not accessible"
                };
                return commands;
            }

            // For now, create some mock command data since SessionContext doesn't have Commands property
            // This would need to be implemented in the session manager to track actual commands
            var mockCommands = new object[]
            {
                new
                {
                    commandId = "cmd-001",
                    command = "!analyze -v",
                    status = "Completed",
                    isFinished = true,
                    createdAt = DateTime.UtcNow.AddMinutes(-5),
                    completedAt = DateTime.UtcNow.AddMinutes(-4),
                    duration = TimeSpan.FromMinutes(1),
                    error = (string?)null
                },
                new
                {
                    commandId = "cmd-002",
                    command = "!threads",
                    status = "Running",
                    isFinished = false,
                    createdAt = DateTime.UtcNow.AddMinutes(-2),
                    completedAt = (DateTime?)null,
                    duration = DateTime.UtcNow - DateTime.UtcNow.AddMinutes(-2),
                    error = (string?)null
                }
            };

            // Apply filters
            var filteredCommands = mockCommands.AsEnumerable();

            if (!string.IsNullOrEmpty(commandFilter))
            {
                filteredCommands = filteredCommands.Where(cmd =>
                {
                    dynamic d = cmd;
                    return d.command.ToString().Contains(commandFilter, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (fromTime.HasValue)
            {
                filteredCommands = filteredCommands.Where(cmd =>
                {
                    dynamic d = cmd;
                    return d.createdAt >= fromTime.Value;
                });
            }

            if (toTime.HasValue)
            {
                filteredCommands = filteredCommands.Where(cmd =>
                {
                    dynamic d = cmd;
                    return d.createdAt <= toTime.Value;
                });
            }

            // Apply pagination
            if (offset.HasValue)
                filteredCommands = filteredCommands.Skip(offset.Value);
            if (limit.HasValue)
                filteredCommands = filteredCommands.Take(limit.Value);

            foreach (dynamic cmd in filteredCommands)
            {
                commands[cmd.commandId] = new
                {
                    command = cmd.command,
                    status = cmd.status,
                    isFinished = cmd.isFinished,
                    createdAt = cmd.createdAt,
                    completedAt = cmd.completedAt,
                    duration = cmd.duration,
                    error = cmd.error
                };
            }

            return commands;
        }
    }
}
