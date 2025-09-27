using System;
using System.Linq;
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
        [McpServerResource, Description("📊 SESSIONS: List all active debugging sessions with status and activity information")]
        public static Task<string> Sessions(
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

                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading sessions list");
                throw;
            }
        }

        [McpServerResource, Description("COMMANDS: List async commands from all sessions with status and timing information")]
        public static Task<string> Commands(
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

                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading commands list");
                throw;
            }
        }


        [McpServerResource, Description("📚 WORKFLOWS: Common debugging patterns and step-by-step analysis workflows for crash dump investigation")]
        public static Task<string> Workflows(
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

            return Task.FromResult(JsonSerializer.Serialize(workflows, new JsonSerializerOptions { WriteIndented = true }));
        }

        [McpServerResource, Description("❓ USAGE: Essential tool usage information and API reference for MCP Nexus server")]
        public static Task<string> Usage(
            IServiceProvider serviceProvider)
        {
            var usage = new
            {
                title = "MCP Nexus Usage Guide",
                description = "Essential tool usage information for MCP Nexus server using official MCP SDK",
                overview = new
                {
                    tools_purpose = "Tools perform actions and operations on the server. They take parameters and return results.",
                    resources_purpose = "Resources provide data and documentation. They are read-only and typically have no parameters.",
                    transport_modes = new[] { "STDIO (recommended for AI clients)", "HTTP (basic functionality only)" }
                },
                available_tools = new[]
                {
                    new
                    {
                        name = "nexus_open_dump_analyze_session",
                        description = "🚀 OPEN SESSION: Create a new debugging session for a crash dump file",
                        parameters = new[] { "dumpPath (required)", "symbolsPath (optional)" },
                        returns = "sessionId that MUST be used for all subsequent operations"
                    },
                    new
                    {
                        name = "nexus_close_dump_analyze_session", 
                        description = "🔒 CLOSE SESSION: Close an active debugging session and clean up resources",
                        parameters = new[] { "sessionId (required)" },
                        returns = "Confirmation of session closure"
                    },
                    new
                    {
                        name = "nexus_enqueue_async_dump_analyze_command",
                        description = "⚡ QUEUE COMMAND: Queue a WinDBG command for execution in a debugging session",
                        parameters = new[] { "sessionId (required)", "command (required)" },
                        returns = "commandId for tracking command execution"
                    },
                    new
                    {
                        name = "nexus_read_dump_analyze_command_result",
                        description = "📋 READ COMMAND RESULT: Get the full WinDBG output and status of a specific async command",
                        parameters = new[] { "sessionId (required)", "commandId (required)" },
                        returns = "Complete command output, status, and execution details"
                    }
                },
                available_resources = new[]
                {
                    new
                    {
                        name = "sessions",
                        description = "📊 SESSIONS: List all active debugging sessions",
                        parameters = "None (returns all sessions)",
                        returns = "Array of session information including status, dump path, and activity"
                    },
                    new
                    {
                        name = "commands", 
                        description = "📋 COMMANDS: List async commands from all sessions",
                        parameters = "None (returns all commands)",
                        returns = "Array of command information including status, timing, and results"
                    },
                    new
                    {
                        name = "workflows",
                        description = "📚 WORKFLOWS: Common debugging patterns and step-by-step analysis workflows",
                        parameters = "None",
                        returns = "Structured workflows for crash analysis patterns"
                    },
                    new
                    {
                        name = "usage",
                        description = "❓ USAGE: This usage guide and essential information",
                        parameters = "None",
                        returns = "Complete usage information and API reference"
                    }
                },
                example_workflow = new[]
                {
                    "1. Use 'nexus_open_dump_analyze_session' to create a session (get sessionId)",
                    "2. Use 'nexus_enqueue_async_dump_analyze_command' to queue WinDBG commands (get commandId)",
                    "3. Use 'nexus_read_dump_analyze_command_result' to get command results",
                    "4. Use 'sessions' or 'commands' resources to monitor activity",
                    "5. Use 'nexus_close_dump_analyze_session' when done"
                },
                important_notes = new[]
                {
                    "All commands are asynchronous - use the commandId to track progress",
                    "Resources are parameterless and return all available data",
                    "Use 'commands' resource to monitor command status",
                    "Always close sessions when no longer needed to free resources"
                },
                transport_limitations = new
                {
                    stdio = "✅ Full functionality with all features",
                    http = "⚠️ Basic functionality only - limited features"
                }
            };

            return Task.FromResult(JsonSerializer.Serialize(usage, new JsonSerializerOptions { WriteIndented = true }));
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

            // Get real command data from the session's command queue
            var commandQueue = sessionContext.CommandQueue;
            var allCommands = commandQueue.GetAllCommands();
            
            var realCommands = allCommands.Select(cmd => new
            {
                commandId = cmd.Id,
                command = cmd.Command,
                status = GetCommandStatus(cmd),
                isFinished = cmd.State == CommandState.Completed || cmd.State == CommandState.Failed || cmd.State == CommandState.Cancelled,
                createdAt = cmd.QueueTime,
                completedAt = cmd.CompletedAt,
                duration = cmd.CompletedAt.HasValue ? cmd.CompletedAt.Value - cmd.QueueTime : DateTime.UtcNow - cmd.QueueTime,
                error = cmd.Error,
                progress = new
                {
                    queuePosition = GetQueuePositionForCommand(cmd, allCommands),
                    progressPercentage = GetProgressPercentageForCommand(cmd, allCommands),
                    elapsed = GetElapsedTimeForCommand(cmd),
                    eta = GetEtaTimeForCommand(cmd),
                    message = GetCommandMessage(cmd, allCommands)
                }
            }).ToArray();

            // Apply filters
            var filteredCommands = realCommands.AsEnumerable();

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

        /// <summary>
        /// Gets the status string for a command
        /// </summary>
        private static string GetCommandStatus(QueuedCommand cmd)
        {
            return cmd.State switch
            {
                CommandState.Queued => "Queued",
                CommandState.Executing => "Executing",
                CommandState.Completed => "Completed",
                CommandState.Failed => "Failed",
                CommandState.Cancelled => "Cancelled",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Calculates queue position for a command
        /// </summary>
        private static int GetQueuePositionForCommand(QueuedCommand cmd, List<QueuedCommand> allCommands)
        {
            var queuedCommands = allCommands.Where(c => c.State == CommandState.Queued).ToList();
            var executingCommands = allCommands.Where(c => c.State == CommandState.Executing).ToList();
            
            if (cmd.State == CommandState.Executing)
            {
                return 0; // Currently executing
            }
            
            if (cmd.State == CommandState.Queued)
            {
                var position = queuedCommands.IndexOf(cmd);
                return executingCommands.Count + position; // +1 for each executing command
            }
            
            return -1; // Not in queue
        }

        /// <summary>
        /// Calculates progress percentage for a command
        /// </summary>
        private static int GetProgressPercentageForCommand(QueuedCommand cmd, List<QueuedCommand> allCommands)
        {
            if (cmd.State == CommandState.Completed)
                return 100;
            
            if (cmd.State == CommandState.Failed || cmd.State == CommandState.Cancelled)
                return 0;
            
            var queuePosition = GetQueuePositionForCommand(cmd, allCommands);
            var elapsed = DateTime.UtcNow - cmd.QueueTime;
            
            // Base progress from queue position (0-50%)
            var queueProgress = Math.Max(0, Math.Min(50, (10 - queuePosition) * 5));
            
            // Time-based progress that always increases (0-50%)
            var timeProgress = Math.Min(50, (int)(elapsed.TotalMinutes * 2)); // 2% per minute
            
            // Combine both for total progress (0-100%)
            var totalProgress = Math.Min(100, queueProgress + timeProgress);
            
            // Ensure minimum progress based on elapsed time to show activity
            var minProgress = Math.Min(95, (int)(elapsed.TotalSeconds * 0.5)); // 0.5% per second
            
            return Math.Max(totalProgress, minProgress);
        }

        /// <summary>
        /// Generates dynamic message for a command
        /// </summary>
        private static string GetCommandMessage(QueuedCommand cmd, List<QueuedCommand> allCommands)
        {
            if (cmd.State == CommandState.Completed)
                return "Command completed successfully";
            
            if (cmd.State == CommandState.Failed)
                return $"Command failed: {cmd.Error ?? "Unknown error"}";
            
            if (cmd.State == CommandState.Cancelled)
                return "Command was cancelled";
            
            if (cmd.State == CommandState.Executing)
            {
                var elapsed = DateTime.UtcNow - cmd.QueueTime;
                var remainingMinutes = Math.Max(0, (int)((TimeSpan.FromMinutes(10) - elapsed).TotalMinutes));
                var remainingSeconds = Math.Max(0, (int)((TimeSpan.FromMinutes(10) - elapsed).TotalSeconds % 60));
                return $"Command is currently executing (elapsed: {elapsed.TotalMinutes:F1} minutes, remaining: {remainingMinutes} minutes {remainingSeconds} seconds)";
            }
            
            if (cmd.State == CommandState.Queued)
            {
                var queuePosition = GetQueuePositionForCommand(cmd, allCommands);
                var elapsed = DateTime.UtcNow - cmd.QueueTime;
                var progressPercentage = GetProgressPercentageForCommand(cmd, allCommands);
                
                var baseMessage = queuePosition switch
                {
                    0 => "Command is next in queue - will start executing soon",
                    1 => "Command is 2nd in queue - almost ready to execute",
                    2 => "Command is 3rd in queue - waiting for 2 commands ahead",
                    3 => "Command is 4th in queue - waiting for 3 commands ahead",
                    4 => "Command is 5th in queue - waiting for 4 commands ahead",
                    _ when queuePosition <= 10 => $"Command is {queuePosition + 1}th in queue - waiting for {queuePosition} commands ahead",
                    _ => $"Command is position {queuePosition + 1} in queue - waiting for {queuePosition} commands ahead"
                };
                
                var progressInfo = $" (Progress: {progressPercentage}%, Elapsed: {elapsed.TotalMinutes:F1}min)";
                var timeInfo = remainingMinutes > 0 ? $", ETA: {remainingMinutes}min {remainingSeconds}s" : ", ETA: <1min";
                
                return $"{baseMessage}{progressInfo}{timeInfo}";
            }
            
            return "Command status unknown";
        }

        /// <summary>
        /// Gets elapsed time for a command
        /// </summary>
        private static string? GetElapsedTimeForCommand(QueuedCommand cmd)
        {
            if (cmd.State == CommandState.Completed || cmd.State == CommandState.Failed || cmd.State == CommandState.Cancelled)
                return null;

            var elapsed = DateTime.UtcNow - cmd.QueueTime;
            return $"{elapsed.TotalMinutes:F1}min";
        }

        /// <summary>
        /// Gets ETA time for a command
        /// </summary>
        private static string? GetEtaTimeForCommand(QueuedCommand cmd)
        {
            if (cmd.State == CommandState.Completed || cmd.State == CommandState.Failed || cmd.State == CommandState.Cancelled)
                return null;

            var elapsed = DateTime.UtcNow - cmd.QueueTime;
            var remaining = TimeSpan.FromMinutes(10) - elapsed;
            
            if (remaining.TotalMinutes <= 0)
                return "<1min";
            
            var remainingMinutes = (int)remaining.TotalMinutes;
            var remainingSeconds = (int)(remaining.TotalSeconds % 60);
            
            return $"{remainingMinutes}min {remainingSeconds}s";
        }
    }
}

