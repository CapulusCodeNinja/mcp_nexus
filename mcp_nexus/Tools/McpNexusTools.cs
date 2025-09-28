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

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public static class McpNexusTools
    {
        [McpServerTool, Description("🚀 OPEN SESSION: Create a new debugging session for a crash dump file. Returns sessionId that MUST be used for all subsequent operations.")]
        public static async Task<object> nexus_open_dump_analyze_session(
            IServiceProvider serviceProvider,
            [Description("Full path to the crash dump file (.dmp)")] string dumpPath,
            [Description("Optional path to symbol files directory")] string? symbolsPath = null)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("🔓 Opening new debugging session for dump: {DumpPath}", dumpPath);

            try
            {
                var sessionId = await sessionManager.CreateSessionAsync(dumpPath, symbolsPath);
                var context = sessionManager.GetSessionContext(sessionId);

                var response = new
                {
                    sessionId = sessionId,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    success = true,
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Session created successfully: {sessionId}. Use 'sessions' resource to manage sessions."
                };

                logger.LogInformation("Session {SessionId} created successfully", sessionId);
                return response;
            }
            catch (SessionLimitExceededException ex)
            {
                logger.LogWarning("Session limit exceeded: {Message}", ex.Message);

                var errorResponse = new
                {
                    sessionId = (string?)null,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    success = false,
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Maximum concurrent sessions exceeded: {ex.CurrentSessions}/{ex.MaxSessions}"
                };

                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create debugging session for {DumpPath}", dumpPath);

                var errorResponse = new
                {
                    sessionId = (string?)null,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    success = false,
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Failed to create debugging session: {ex.Message}"
                };

                return errorResponse;
            }
        }

        [McpServerTool, Description("🔒 CLOSE SESSION: Close an active debugging session and clean up resources. Use this when done with a session.")]
        public static async Task<object> nexus_close_dump_analyze_session(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("🔒 Closing debugging session: {SessionId}", sessionId);

            try
            {
                if (!sessionManager.SessionExists(sessionId))
                {
                    var notFoundResponse = new
                    {
                        sessionId = sessionId,
                        success = false,
                        operation = "nexus_close_dump_analyze_session",
                        message = $"Session {sessionId} not found. Use 'sessions' resource to see available sessions."
                    };

                    logger.LogWarning("Attempted to close non-existent session: {SessionId}", sessionId);
                    return notFoundResponse;
                }

                await sessionManager.CloseSessionAsync(sessionId);

                var response = new
                {
                    sessionId = sessionId,
                    success = true,
                    operation = "nexus_close_dump_analyze_session",
                    message = $"Session {sessionId} closed successfully"
                };

                logger.LogInformation("Session {SessionId} closed successfully", sessionId);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close debugging session: {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    success = false,
                    operation = "nexus_close_dump_analyze_session",
                    message = $"Failed to close session: {ex.Message}"
                };

                return errorResponse;
            }
        }

        [McpServerTool, Description("⚡ QUEUE COMMAND: Queue a WinDBG command for execution in a debugging session. Returns commandId for tracking.")]
        public static async Task<object> nexus_enqueue_async_dump_analyze_command(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("WinDBG command to execute (e.g., '!analyze -v', 'k', '!threads')")] string command)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("⚡ Queuing command '{Command}' for session: {SessionId}", command, sessionId);

            try
            {
                if (!sessionManager.SessionExists(sessionId))
                {
                    var notFoundResponse = new
                    {
                        sessionId = sessionId,
                        commandId = (string?)null,
                        success = false,
                        operation = "nexus_enqueue_async_dump_analyze_command",
                        message = $"Session {sessionId} not found. Use 'sessions' resource to see available sessions."
                    };

                    logger.LogWarning("Attempted to queue command for non-existent session: {SessionId}", sessionId);
                    return Task.FromResult((object)notFoundResponse);
                }

                var context = sessionManager.GetSessionContext(sessionId);
                if (context == null)
                {
                    var contextErrorResponse = new
                    {
                        sessionId = sessionId,
                        commandId = (string?)null,
                        success = false,
                        operation = "nexus_enqueue_async_dump_analyze_command",
                        message = $"Session context not available for {sessionId}. Session may be in an invalid state."
                    };

                    logger.LogError("Session context not available for session: {SessionId}", sessionId);
                    return Task.FromResult((object)contextErrorResponse);
                }

                var commandQueue = sessionManager.GetCommandQueue(sessionId);
                var commandId = commandQueue.QueueCommand(command);

                var response = new
                {
                    sessionId = sessionId,
                    commandId = commandId,
                    command = command,
                    success = true,
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Command queued successfully. Estimated execution time: up to 10 minutes. Use the 'commands' resource to monitor all commands or the 'nexus_read_dump_analyze_command_result' tool to get specific results.",
                    timeoutMinutes = 10,
                    status = "queued"
                };

                logger.LogInformation("Command {CommandId} queued successfully for session {SessionId}", commandId, sessionId);
                return await Task.FromResult((object)response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue command for session: {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    commandId = (string?)null,
                    command = command,
                    success = false,
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Failed to queue command: {ex.Message}"
                };

                return await Task.FromResult((object)errorResponse);
            }
        }

        [McpServerTool, Description("📋 READ COMMAND RESULT: Get the full WinDBG output and status of a specific async command")]
        public static async Task<object> nexus_read_dump_analyze_command_result(
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
                    return new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        success = false,
                        error = $"Session {sessionId} not found. Use nexus_list_sessions to see available sessions.",
                        operation = "nexus_read_dump_analyze_command_result",
                        usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                    };
                }

                var commandQueue = sessionManager.GetCommandQueue(sessionId);

                // Get type-safe command information instead of parsing strings
                var commandInfo = commandQueue.GetCommandInfo(commandId);
                if (commandInfo == null)
                {
                    return new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        success = false,
                        error = $"Command {commandId} not found. Use nexus_list_commands to see available commands.",
                        operation = "nexus_read_dump_analyze_command_result",
                        usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                    };
                }

                // Get the actual command result for completed commands
                var commandResult = commandInfo.IsCompleted ? await commandQueue.GetCommandResult(commandId) : null;

                // Calculate progress percentage based on queue position and elapsed time
                var progressPercentage = CalculateProgressPercentage(commandInfo.QueuePosition, commandInfo.Elapsed);

                // Generate status message for non-completed commands
                var statusMessage = commandInfo.State switch
                {
                    CommandState.Queued => GetQueuedStatusMessage(commandInfo.QueuePosition, commandInfo.Elapsed, commandInfo.Remaining),
                    CommandState.Executing => $"Command is currently executing (elapsed: {commandInfo.Elapsed.TotalMinutes:F1} minutes, remaining: {commandInfo.Remaining.TotalMinutes:F0} minutes {commandInfo.Remaining.Seconds} seconds)",
                    CommandState.Cancelled => "Command was cancelled",
                    CommandState.Failed => "Command execution failed",
                    _ => "Command status unknown"
                };

                var result = new
                {
                    sessionId = sessionId,
                    commandId = commandId,
                    success = true,
                    operation = "nexus_read_dump_analyze_command_result",
                    status = commandInfo.State.ToString(),
                    result = commandInfo.IsCompleted ? commandResult : null,
                    error = (string?)null,
                    completedAt = commandInfo.IsCompleted ? DateTimeOffset.Now : (DateTimeOffset?)null,
                    timestamp = DateTimeOffset.Now,
                    message = commandInfo.IsCompleted ? null : statusMessage,
                    progress = new
                    {
                        queuePosition = commandInfo.QueuePosition,
                        progressPercentage = progressPercentage,
                        elapsed = commandInfo.IsCompleted ? null : $"{commandInfo.Elapsed.TotalMinutes:F1}min",
                        eta = commandInfo.IsCompleted ? null : GetEtaTime(commandInfo.Remaining),
                        message = commandInfo.IsCompleted ? null : statusMessage
                    },
                    nextCheckIn = commandInfo.IsCompleted ? null : GetNextCheckInRecommendation(commandInfo.State, commandInfo.QueuePosition),
                    timeoutMinutes = 10,
                    usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                };

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting command result for session {SessionId}, command {CommandId}", sessionId, commandId);

                return new
                {
                    sessionId = sessionId,
                    commandId = commandId,
                    success = false,
                    error = $"Failed to get command result: {ex.Message}",
                    operation = "nexus_read_dump_analyze_command_result",
                    usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                };
            }
        }

        /// <summary>
        /// Extracts elapsed time from command result message
        /// </summary>
        private static string? GetElapsedTime(string commandResult)
        {
            if (string.IsNullOrEmpty(commandResult))
                return null;

            // Look for "Elapsed: X.Xmin" pattern
            var elapsedMatch = System.Text.RegularExpressions.Regex.Match(commandResult, @"Elapsed: ([\d.]+)min");
            if (elapsedMatch.Success)
            {
                return $"{elapsedMatch.Groups[1].Value}min";
            }

            return null;
        }

        /// <summary>
        /// Extracts ETA time from command result message
        /// </summary>
        private static string? GetEtaTime(string commandResult)
        {
            if (string.IsNullOrEmpty(commandResult))
                return null;

            // Look for "ETA: Xmin Ys" pattern
            var etaMatch = System.Text.RegularExpressions.Regex.Match(commandResult, @"ETA: ([\d]+)min ([\d]+)s");
            if (etaMatch.Success)
            {
                return $"{etaMatch.Groups[1].Value}min {etaMatch.Groups[2].Value}s";
            }

            // Look for "ETA: <1min" pattern
            var etaMinMatch = System.Text.RegularExpressions.Regex.Match(commandResult, @"ETA: (<1min)");
            if (etaMinMatch.Success)
            {
                return etaMinMatch.Groups[1].Value;
            }

            return null;
        }

        private static string? GetEtaTime(TimeSpan remaining)
        {
            if (remaining.TotalMinutes <= 0)
                return "<1min";

            var remainingMinutes = (int)remaining.TotalMinutes;
            var remainingSeconds = (int)(remaining.TotalSeconds % 60);

            return $"{remainingMinutes}min {remainingSeconds}s";
        }

        private static int CalculateProgressPercentage(int queuePosition, TimeSpan elapsed)
        {
            // Base progress from queue position (0-50%)
            var queueProgress = Math.Max(0, Math.Min(50, (10 - queuePosition) * 5));

            // Time-based progress that always increases (0-50%)
            // This ensures progress always goes up, even if queue position doesn't change
            var timeProgress = Math.Min(50, (int)(elapsed.TotalMinutes * 2)); // 2% per minute

            // Combine both for total progress (0-100%)
            var totalProgress = Math.Min(100, queueProgress + timeProgress);

            // Ensure minimum progress based on elapsed time to show activity
            var minProgress = Math.Min(95, (int)(elapsed.TotalSeconds * 0.5)); // 0.5% per second

            return Math.Max(totalProgress, minProgress);
        }

        private static string GetQueuedStatusMessage(int queuePosition, TimeSpan elapsed, TimeSpan remaining)
        {
            if (queuePosition < 0)
            {
                return $"Command is queued for execution (estimated wait: up to 10 minutes)";
            }

            // Calculate progress percentage that ALWAYS increases over time
            var progressPercentage = CalculateProgressPercentage(queuePosition, elapsed);

            // Generate different messages based on position and elapsed time
            var baseMessage = GetBaseMessage(queuePosition, elapsed);

            // Add progress and time information
            var progressInfo = $" (Progress: {progressPercentage}%, Elapsed: {elapsed.TotalMinutes:F1}min)";
            var timeInfo = remaining.TotalMinutes > 0 ? $", ETA: {GetEtaTime(remaining)}" : ", ETA: <1min";

            return $"{baseMessage}{progressInfo}{timeInfo}";
        }

        private static string GetBaseMessage(int queuePosition, TimeSpan elapsed)
        {
            // Add time-based variations to make messages feel more dynamic
            var timeVariation = (int)(elapsed.TotalSeconds) % 4;

            return queuePosition switch
            {
                0 => timeVariation switch
                {
                    0 => "Command is next in queue - will start executing soon",
                    1 => "Command is next in queue - preparing to execute",
                    2 => "Command is next in queue - almost ready to start",
                    _ => "Command is next in queue - execution imminent"
                },
                1 => timeVariation switch
                {
                    0 => "Command is 2nd in queue - almost ready to execute",
                    1 => "Command is 2nd in queue - waiting for 1 command ahead",
                    2 => "Command is 2nd in queue - will be next soon",
                    _ => "Command is 2nd in queue - preparing for execution"
                },
                2 => timeVariation switch
                {
                    0 => "Command is 3rd in queue - waiting for 2 commands ahead",
                    1 => "Command is 3rd in queue - making progress through queue",
                    2 => "Command is 3rd in queue - moving up in line",
                    _ => "Command is 3rd in queue - queue position improving"
                },
                3 => timeVariation switch
                {
                    0 => "Command is 4th in queue - waiting for 3 commands ahead",
                    1 => "Command is 4th in queue - progressing through queue",
                    2 => "Command is 4th in queue - position advancing",
                    _ => "Command is 4th in queue - queue moving forward"
                },
                4 => timeVariation switch
                {
                    0 => "Command is 5th in queue - waiting for 4 commands ahead",
                    1 => "Command is 5th in queue - queue processing actively",
                    2 => "Command is 5th in queue - position updating",
                    _ => "Command is 5th in queue - making steady progress"
                },
                _ when queuePosition <= 10 => timeVariation switch
                {
                    0 => $"Command is {queuePosition + 1}th in queue - waiting for {queuePosition} commands ahead",
                    1 => $"Command is {queuePosition + 1}th in queue - queue processing normally",
                    2 => $"Command is {queuePosition + 1}th in queue - position tracking active",
                    _ => $"Command is {queuePosition + 1}th in queue - progress monitoring"
                },
                _ => timeVariation switch
                {
                    0 => $"Command is position {queuePosition + 1} in queue - waiting for {queuePosition} commands ahead",
                    1 => $"Command is position {queuePosition + 1} in queue - queue system active",
                    2 => $"Command is position {queuePosition + 1} in queue - processing normally",
                    _ => $"Command is position {queuePosition + 1} in queue - status updating"
                }
            };
        }

        /// <summary>
        /// Provides intelligent polling recommendations based on command state and queue position
        /// </summary>
        private static string GetNextCheckInRecommendation(CommandState state, int queuePosition)
        {
            return state switch
            {
                CommandState.Queued => queuePosition switch
                {
                    0 => "Check again in 3-5 seconds (next in queue)",
                    1 => "Check again in 5-10 seconds (2nd in queue)",
                    2 => "Check again in 10-15 seconds (3rd in queue)",
                    3 => "Check again in 15-20 seconds (4th in queue)",
                    4 => "Check again in 20-30 seconds (5th in queue)",
                    _ when queuePosition <= 10 => "Check again in 30-60 seconds (position in queue)",
                    _ => "Check again in 1-2 minutes (deep in queue)"
                },
                CommandState.Executing => "Check again in 10-30 seconds (currently executing)",
                CommandState.Cancelled => "No need to check again (command was cancelled)",
                CommandState.Failed => "No need to check again (command failed)",
                _ => "Check again in 5-10 seconds (unknown state)"
            };
        }
    }
}
