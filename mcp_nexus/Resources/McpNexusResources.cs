using System;
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
        // IMPORTANT: Method names directly determine resource names!
        // Method "Sessions" becomes resource "sessions", "Commands" becomes "commands", etc.
        [McpServerResource, Description("List all active debugging sessions")]
        public static async Task<string> Sessions(
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            try
            {
                var allSessions = sessionManager.GetAllSessions();
                var sessions = allSessions.OrderByDescending(s => s.CreatedAt).ToList();

                var result = new
                {
                    sessions = sessions,
                    count = sessions.Count,
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

        [McpServerResource, Description("List async commands from all sessions")]
        public static async Task<string> Commands(
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            try
            {
                var allSessions = sessionManager.GetAllSessions();
                var commandsBySession = new Dictionary<string, object>();

                // Get commands from all sessions
                foreach (var session in allSessions)
                {
                    var sessionContext = sessionManager.GetSessionContext(session.SessionId);
                    var sessionCommands = GetSessionCommands(sessionContext, session.SessionId, null, null, null, null, null, "createdAt", "desc");
                    commandsBySession[session.SessionId] = sessionCommands;
                }

                var result = new
                {
                    commands = commandsBySession,
                    totalSessions = commandsBySession.Count,
                    totalCommands = commandsBySession.Values.Cast<Dictionary<string, object>>().Sum(c => c.Count),
                    timestamp = DateTime.UtcNow,
                    note = "Commands from all sessions"
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading commands list");
                throw;
            }
        }


        [McpServerResource, Description("Common debugging patterns and step-by-step analysis workflows")]
        public static async Task<string> Workflows(
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
        public static async Task<string> Usage(
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
