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
using mcp_nexus.Extensions;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace mcp_nexus.Tools
{
    /// <summary>
    /// Provides Model Context Protocol (MCP) tools for the MCP Nexus server.
    /// Contains static methods that expose debugging session management, command execution,
    /// and crash dump analysis capabilities as MCP tools for AI clients to use.
    /// </summary>
    [McpServerToolType]
    public static class McpNexusTools
    {
        /// <summary>
        /// Creates a new debugging session for a crash dump file.
        /// Returns sessionId that MUST be used for all subsequent operations.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="dumpPath">Full path to the crash dump file (.dmp).</param>
        /// <param name="symbolsPath">Optional path to symbol files directory.</param>
        /// <returns>An object containing session information including sessionId.</returns>
        [McpServerTool, Description("🚀 OPEN SESSION: Create a new debugging session for a crash dump file. Returns sessionId that MUST be used for all subsequent operations.")]
        public static async Task<object> nexus_open_dump_analyze_session(
            IServiceProvider serviceProvider,
            [Description("Full path to the crash dump file (.dmp)")] string dumpPath,
            [Description("Optional path to symbol files directory")] string? symbolsPath = null)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("McpNexusTools");
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("🔓 Opening new debugging session for dump: {DumpPath}", dumpPath);

            // Handle case where MCP framework passes "null" as string instead of actual null
            if (symbolsPath == "null" || string.IsNullOrWhiteSpace(symbolsPath))
            {
                symbolsPath = null;
            }

            try
            {
                // Validate dump file exists before creating session
                if (!File.Exists(dumpPath))
                {
                    logger.LogError("Dump file does not exist: {DumpPath}", dumpPath);

                    var errorResponse = new
                    {
                        sessionId = (string?)null,
                        dumpFile = Path.GetFileName(dumpPath),
                        commandId = (string?)null,
                        status = "Failed",
                        operation = "nexus_open_dump_analyze_session",
                        message = $"Dump file does not exist: {dumpPath}"
                    };

                    return errorResponse;
                }

                var sessionId = await sessionManager.CreateSessionAsync(dumpPath, symbolsPath);
                
                // CRITICAL: Validate session was actually created and exists
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    logger.LogError("Session creation returned null or empty session ID");
                    throw new InvalidOperationException("Session creation failed - no session ID returned");
                }
                
                // Verify session exists in session manager (only if session creation succeeded)
                try
                {
                    var context = sessionManager.GetSessionContext(sessionId);
                    if (context == null)
                    {
                        logger.LogError("Session {SessionId} was created but context is null - session may not exist", sessionId);
                        throw new InvalidOperationException($"Session {sessionId} was created but context is null");
                    }
                }
                catch (Exception ex) when (!(ex is InvalidOperationException))
                {
                    // If GetSessionContext fails for any reason other than our validation, log but don't fail
                    logger.LogWarning(ex, "Could not verify session context for {SessionId}, but session creation succeeded", sessionId);
                }

                var response = new
                {
                    sessionId = sessionId,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    status = "Success",
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Session created successfully: {sessionId}. Use 'sessions' resource to manage sessions."
                };

                logger.LogInformation("✅ Session {SessionId} created and validated successfully", sessionId);
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
                    status = "Failed",
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
                    status = "Failed",
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Failed to create debugging session: {ex.Message}"
                };

                return errorResponse;
            }
        }

        /// <summary>
        /// Closes an active debugging session and cleans up resources.
        /// Use this when done with a session.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
        /// <returns>An object containing session closure information.</returns>
        [McpServerTool, Description("🔒 CLOSE SESSION: Close an active debugging session and clean up resources. Use this when done with a session.")]
        public static async Task<object> nexus_close_dump_analyze_session(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("McpNexusTools");
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("🔒 Closing debugging session: {SessionId}", sessionId);

            try
            {
                logger.LogInformation("[Tool] About to call SessionExists for {SessionId}", sessionId);
                if (!sessionManager.SessionExists(sessionId))
                {
                    var notFoundResponse = new
                    {
                        sessionId = sessionId,
                        status = "Failed",
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
                    status = "Success",
                    operation = "nexus_close_dump_analyze_session",
                    message = $"Session {sessionId} closed successfully"
                };

                logger.LogInformation("Session {SessionId} closed successfully", sessionId);
                return response;
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning("Invalid session ID (null): {Message}", ex.Message);
                var errorResponse = new
                {
                    sessionId = sessionId,
                    status = "Failed",
                    operation = "nexus_close_dump_analyze_session",
                    message = "Session ID cannot be null"
                };
                return errorResponse;
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Invalid session ID (empty/whitespace): {Message}", ex.Message);
                var errorResponse = new
                {
                    sessionId = sessionId,
                    status = "Failed",
                    operation = "nexus_close_dump_analyze_session",
                    message = "Session ID cannot be empty or whitespace"
                };
                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close debugging session: {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    status = "Failed",
                    operation = "nexus_close_dump_analyze_session",
                    message = $"Failed to close session: {ex.Message}"
                };

                return errorResponse;
            }
        }

        /// <summary>
        /// Queues a WinDBG command for execution in a debugging session.
        /// Returns commandId for tracking.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
        /// <param name="command">WinDBG command to execute (e.g., '!analyze -v', 'k', '!threads').</param>
        /// <returns>An object containing command information including commandId.</returns>
        [McpServerTool, Description("⚡ QUEUE COMMAND: Queue a WinDBG command for execution in a debugging session. Returns commandId for tracking.")]
        public static async Task<object> nexus_enqueue_async_dump_analyze_command(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("WinDBG command to execute (e.g., '!analyze -v', 'k', '!threads')")] string command)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("McpNexusTools");
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("⚡ Queuing command '{Command}' for session: {SessionId}", command, sessionId);

            try
            {
                // Try to get queue without throwing to avoid transient races, log details if missing
                if (!sessionManager.TryGetCommandQueue(sessionId, out var commandQueue) || commandQueue == null)
                {
                    // Extended retry loop to handle command queue initialization race condition
                    // Background processing task needs time to initialize after session creation
                    for (int attempt = 1; attempt <= 20; attempt++)
                    {
                        // Use exponential backoff instead of fixed delay
                        var delayMs = Math.Min(1000, 50 * (int)Math.Pow(1.5, attempt - 1));
                        await Task.Delay(delayMs);

                        if (sessionManager.TryGetCommandQueue(sessionId, out commandQueue) && commandQueue != null)
                        {
                            // Additional check: verify the queue is actually ready to accept commands
                            if (commandQueue is IsolatedCommandQueueService isolatedQueue && !isolatedQueue.IsReady())
                            {
                                logger.LogTrace("Command queue found but not ready for {SessionId} after {Attempt} attempts", sessionId, attempt);
                                commandQueue = null; // Reset to continue retry loop
                                continue;
                            }

                            logger.LogTrace("Command queue became available for {SessionId} after {Attempt} attempts ({Ms}ms)",
                                sessionId, attempt, delayMs);
                            break;
                        }
                    }

                    if (commandQueue == null)
                    {
                        var queueMissingResponse = new
                        {
                            sessionId = sessionId,
                            commandId = (string?)null,
                            status = "Failed",
                            operation = "nexus_enqueue_async_dump_analyze_command",
                            message = $"Session {sessionId} is not ready to accept commands (queue unavailable). Please retry shortly."
                        };

                        // Add extra diagnostics to help troubleshooting
                        try
                        {
                            var activeCount = sessionManager.GetActiveSessions()?.Count() ?? -1;
                            var allCount = sessionManager.GetAllSessions()?.Count() ?? -1;
                            logger.LogWarning("Command queue unavailable for session: {SessionId}. Likely transient immediately after creation. ActiveSessions={Active}, AllSessions={All}",
                                sessionId, activeCount, allCount);
                        }
                        catch { }
                        return Task.FromResult((object)queueMissingResponse);
                    }
                }

                var context = sessionManager.GetSessionContext(sessionId);
                logger.LogTrace("Session context resolved for {SessionId}: Status={Status}, LastActivity={LastActivity}",
                    sessionId, context.Status, context.LastActivity);
                var commandId = commandQueue.QueueCommand(command);

                // Get queue position for the newly queued command
                var queueStatus = commandQueue.GetQueueStatus().ToList();
                var queuePosition = queueStatus.FindIndex(q => q.Id == commandId);
                var totalInQueue = queueStatus.Count;

                var response = new
                {
                    sessionId = sessionId,
                    commandId = commandId,
                    command = command,
                    status = "Queued",
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Command queued successfully. Queue position: {queuePosition + 1} of {totalInQueue}. Use the 'commands' resource to monitor all commands or the 'nexus_read_dump_analyze_command_result' tool to get specific results.",
                    timeoutMinutes = 10,
                    queuePosition = queuePosition + 1,
                    totalInQueue = totalInQueue
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
                    status = "Failed",
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Failed to queue command: {ex.Message}"
                };

                return await Task.FromResult((object)errorResponse);
            }
        }

        /// <summary>
        /// Gets the full WinDBG output and status of a specific async command.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
        /// <param name="commandId">Command ID from nexus_enqueue_async_dump_analyze_command.</param>
        /// <returns>An object containing command result information including output and status.</returns>
        [McpServerTool, Description("📋 READ COMMAND RESULT: Get the full WinDBG output and status of a specific async command")]
        public static async Task<object> nexus_read_dump_analyze_command_result(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("Command ID from nexus_enqueue_async_dump_analyze_command")] string commandId)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("McpNexusTools");
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            try
            {
                // CRITICAL: Validate session ID format and existence
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    logger.LogError("Command result read failed: Session ID is null or empty");
                    return new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        status = "Failed",
                        error = "Session ID cannot be null or empty",
                        operation = "nexus_read_dump_analyze_command_result",
                        usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                    };
                }
                
                if (string.IsNullOrWhiteSpace(commandId))
                {
                    logger.LogError("Command result read failed: Command ID is null or empty");
                    return new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        status = "Failed",
                        error = "Command ID cannot be null or empty",
                        operation = "nexus_read_dump_analyze_command_result",
                        usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                    };
                }
                
                if (!sessionManager.SessionExists(sessionId))
                {
                    logger.LogError("Command result read failed: Session {SessionId} does not exist", sessionId);
                    return new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        status = "Failed",
                        error = $"Session {sessionId} not found. Use 'sessions' resource to see available sessions.",
                        operation = "nexus_read_dump_analyze_command_result",
                        usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                    };
                }

                // Check if this is an extension command (starts with "ext-")
                if (commandId.StartsWith("ext-"))
                {
                    var extensionTracker = serviceProvider.GetService<IExtensionCommandTracker>();
                    if (extensionTracker != null)
                    {
                        var extInfo = extensionTracker.GetCommandInfo(commandId);
                        var extResult = extensionTracker.GetCommandResult(commandId);
                        
                        if (extInfo != null)
                        {
                            // Return extension command result
                            var extIsCompleted = extInfo.IsCompleted;
                            var extProgressPercentage = extIsCompleted ? 100 : Math.Min(95, (int)(extInfo.Elapsed.TotalSeconds * 0.5));
                            
                            return new
                            {
                                sessionId = sessionId,
                                commandId = commandId,
                                operation = "nexus_read_dump_analyze_command_result",
                                status = extInfo.State.ToString(),
                                extensionName = extInfo.ExtensionName,
                                result = extIsCompleted ? extResult?.Output : null,
                                error = extResult?.ErrorMessage,
                                completedAt = extInfo.CompletedAt,
                                timestamp = DateTimeOffset.Now,
                                message = extIsCompleted ? "Extension completed" : extInfo.ProgressMessage ?? "Extension is running",
                                progress = new
                                {
                                    progressPercentage = extProgressPercentage,
                                    elapsed = FormatElapsedTime(extInfo.Elapsed),
                                    callbackCount = extInfo.CallbackCount,
                                    executionTime = extIsCompleted ? FormatExecutionTime(extInfo.Elapsed) : null
                                },
                                aiGuidance = new
                                {
                                    nextSteps = extIsCompleted ? new[]
                                    {
                                        "Review the extension output for analysis results",
                                        "Extension output is typically structured JSON with multiple command results"
                                    } : new[]
                                    {
                                        "Extension is running normally - extensions execute multiple commands",
                                        "Wait for completion - extensions may take several minutes"
                                    }
                                },
                                usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                            };
                        }
                    }
                }

                // Get command information and result by checking both tracker and cache
                var (commandInfo, commandResult) = await sessionManager.GetCommandInfoAndResultAsync(sessionId, commandId);
                if (commandInfo == null)
                {
                    return new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        status = "Failed",
                        error = $"Command {commandId} not found. Use nexus_list_commands to see available commands.",
                        operation = "nexus_read_dump_analyze_command_result",
                        usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                    };
                }

                // Calculate progress percentage based on queue position and elapsed time
                var progressPercentage = CalculateProgressPercentage(commandInfo.QueuePosition, commandInfo.Elapsed);

                // Generate status message for non-completed commands
                var baseStatusMessage = commandInfo.State switch
                {
                    CommandState.Queued => GetQueuedStatusMessage(commandInfo.QueuePosition, commandInfo.Elapsed, commandInfo.Remaining),
                    CommandState.Executing => $"Command is running normally (elapsed: {commandInfo.Elapsed.TotalMinutes:F1} minutes). This is expected for complex commands.",
                    CommandState.Cancelled => "Command was cancelled",
                    CommandState.Failed => "Command execution failed",
                    _ => "Command status unknown"
                };

                // Add polling recommendation to the message for non-completed commands
                var nextCheckIn = GetNextCheckInRecommendation(commandInfo.State, commandInfo.QueuePosition);
                var statusMessage = commandInfo.IsCompleted ? baseStatusMessage : $"{baseStatusMessage}. {nextCheckIn}";

                var isCompleted = commandInfo.IsCompleted;
                var isFailed = commandInfo.State == CommandState.Failed;
                var isCancelled = commandInfo.State == CommandState.Cancelled;
                var isExecuting = commandInfo.State == CommandState.Executing;
                var isQueued = commandInfo.State == CommandState.Queued;

                // Create a more descriptive status that combines state and completion info
                var status = commandInfo.State switch
                {
                    CommandState.Completed => "Success",
                    _ => commandInfo.State.ToString()
                };

                var result = new
                {
                    sessionId = sessionId,
                    commandId = commandId,
                    operation = "nexus_read_dump_analyze_command_result",
                    status = status,
                    result = isCompleted ? commandResult : null,
                    error = (isFailed || isCancelled) ? (commandResult?.ErrorMessage ?? commandInfo.State.ToString()) : null,
                    completedAt = isCompleted ? DateTimeOffset.Now : (DateTimeOffset?)null,
                    timestamp = DateTimeOffset.Now,
                    message = isCompleted ? null : statusMessage,
                    progress = new
                    {
                        queuePosition = commandInfo.QueuePosition,
                        progressPercentage = progressPercentage,
                        elapsed = isCompleted ? null : FormatElapsedTime(commandInfo.Elapsed),
                        eta = isCompleted ? null : GetEtaTime(commandInfo.Remaining),
                        executionTime = isCompleted ? FormatExecutionTime(commandInfo.Elapsed) : null,
                        checkAgain = isCompleted ? null : nextCheckIn
                    },
                    aiGuidance = new
                    {
                        nextSteps = isCompleted ? new[]
                        {
                            "Analyze the command output for debugging insights",
                            "Execute follow-up commands based on results and use output to guide next for analysis commands",
                            "⚠️ IMPORTANT: Call 'nexus_close_dump_analyze_session' when analysis is complete"
                        } : (isFailed || isCancelled) ? new[]
                        {
                            "Command encountered an issue - check the error details",
                            "Consider retrying with a different approach using simpler commands if complex ones are failing"
                        } : new[]
                        {
                            "Command is running normally - this is expected for complex operations",
                            "Wait for completion - no action needed check status again later please"
                        },
                        statusExplanation = GetStatusExplanation(commandInfo.State.ToString()),
                    },
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
                    status = "Failed",
                    error = $"Failed to get command result: {ex.Message}",
                    operation = "nexus_read_dump_analyze_command_result",
                    usage = SessionAwareWindbgTool.USAGE_EXPLANATION
                };
            }
        }

        /// <summary>
        /// Formats elapsed time with appropriate precision
        /// </summary>
        /// <param name="elapsed">The elapsed time</param>
        /// <returns>Formatted elapsed time string</returns>
        private static string FormatElapsedTime(TimeSpan elapsed)
        {
            if (elapsed.TotalMinutes >= 1)
            {
                var minutes = (int)elapsed.TotalMinutes;
                var seconds = elapsed.Seconds;
                return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";
            }
            else if (elapsed.TotalSeconds >= 1)
            {
                return $"{elapsed.TotalSeconds:F1}s";
            }
            else
            {
                return $"{elapsed.TotalMilliseconds:F0}ms";
            }
        }

        /// <summary>
        /// Formats execution time for completed commands
        /// </summary>
        /// <param name="executionTime">The execution time</param>
        /// <returns>Formatted execution time string</returns>
        private static string FormatExecutionTime(TimeSpan executionTime)
        {
            if (executionTime.TotalMinutes >= 1)
            {
                var minutes = (int)executionTime.TotalMinutes;
                var seconds = executionTime.Seconds;
                return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";
            }
            else if (executionTime.TotalSeconds >= 1)
            {
                return $"{executionTime.TotalSeconds:F1}s";
            }
            else
            {
                return $"{executionTime.TotalMilliseconds:F0}ms";
            }
        }

        /// <summary>
        /// Extracts elapsed time from command result message
        /// </summary>
        /// <param name="commandResult">The command result message to parse.</param>
        /// <returns>The extracted elapsed time string, or null if no elapsed time is found.</returns>
        private static string? GetElapsedTime(string commandResult)
        {
            if (string.IsNullOrEmpty(commandResult))
                return null;

            // Look for "Elapsed: X.Xmin" pattern (with decimal point)
            var elapsedMatch = System.Text.RegularExpressions.Regex.Match(commandResult, @"Elapsed: ([\d]+\.[\d]+min)");
            if (elapsedMatch.Success)
            {
                return elapsedMatch.Groups[1].Value;
            }

            // Look for "Elapsed: Xmin Ys" pattern
            var elapsedMinSecMatch = System.Text.RegularExpressions.Regex.Match(commandResult, @"Elapsed: ([\d]+min [\d]+s)");
            if (elapsedMinSecMatch.Success)
            {
                return elapsedMinSecMatch.Groups[1].Value;
            }

            // Look for "Elapsed: Xmin" pattern (without decimal or seconds)
            var elapsedMinMatch = System.Text.RegularExpressions.Regex.Match(commandResult, @"Elapsed: ([\d]+min)");
            if (elapsedMinMatch.Success)
            {
                return elapsedMinMatch.Groups[1].Value;
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

        /// <summary>
        /// Gets a formatted ETA time string from a TimeSpan.
        /// </summary>
        /// <param name="remaining">The remaining time.</param>
        /// <returns>A formatted ETA time string, or null if the time is invalid.</returns>
        private static string? GetEtaTime(TimeSpan remaining)
        {
            if (remaining.TotalMinutes <= 0)
                return "<1min";

            var remainingMinutes = (int)remaining.TotalMinutes;
            var remainingSeconds = (int)(remaining.TotalSeconds % 60);

            return $"{remainingMinutes}min {remainingSeconds}s";
        }

        /// <summary>
        /// Calculates the progress percentage for a queued command based on queue position and elapsed time.
        /// </summary>
        /// <param name="queuePosition">The position of the command in the queue (0-based).</param>
        /// <param name="elapsed">The time elapsed since the command was queued.</param>
        /// <returns>A progress percentage between 0 and 100.</returns>
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

        /// <summary>
        /// Gets a formatted status message for a queued command with progress details.
        /// </summary>
        /// <param name="queuePosition">The position of the command in the queue.</param>
        /// <param name="elapsed">The time elapsed since the command was queued.</param>
        /// <param name="remaining">The estimated remaining time for the command.</param>
        /// <returns>A formatted status message with progress details.</returns>
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

        /// <summary>
        /// Gets a base status message describing the command's current state.
        /// </summary>
        /// <param name="queuePosition">The position of the command in the queue.</param>
        /// <param name="elapsed">The time elapsed since the command was queued.</param>
        /// <returns>A base status message describing the command's current state.</returns>
        private static string GetBaseMessage(int queuePosition, TimeSpan elapsed)
        {
            return queuePosition switch
            {
                0 => "Command is next in queue",
                1 => "Command is 2nd in queue",
                2 => "Command is 3rd in queue",
                3 => "Command is 4th in queue",
                4 => "Command is 5th in queue",
                _ when queuePosition <= 10 => $"Command is {queuePosition + 1}th in queue",
                _ => $"Command is position {queuePosition + 1} in queue"
            };
        }

        /// <summary>
        /// Provides intelligent polling recommendations based on command state and queue position.
        /// </summary>
        /// <param name="state">The current state of the command.</param>
        /// <param name="queuePosition">The position of the command in the queue.</param>
        /// <returns>A recommendation for when to check back on the command.</returns>
        private static string GetNextCheckInRecommendation(CommandState state, int queuePosition)
        {
            return state switch
            {
                CommandState.Queued => queuePosition switch
                {
                    0 => "Check again in about 1-3 seconds",
                    1 => "Check again in about 3-5 seconds",
                    2 => "Check again in about 5-7 seconds",
                    3 => "Check again in about 7-9 seconds",
                    4 => "Check again in about 9-13 seconds",
                    _ => "Check again in about 15-30 seconds"
                },
                CommandState.Executing => "Check again in 1-3 seconds",
                CommandState.Cancelled => "No need to check again",
                CommandState.Failed => "No need to check again",
                _ => "Check again in 5 seconds"
            };
        }

        /// <summary>
        /// Gets a clear explanation of what the command status means
        /// </summary>
        /// <param name="status">The command status string</param>
        /// <returns>A clear explanation of the status</returns>
        private static string GetStatusExplanation(string status)
        {
            return status switch
            {
                "Queued" => "Command is waiting in the execution queue",
                "Executing" => "Command is currently running in the debugger - this is normal",
                "Completed" => "Command finished successfully and results are available",
                "Failed" => "Command encountered an error during execution",
                "Cancelled" => "Command was cancelled before completion",
                "Timeout" => "Command exceeded maximum execution time",
                _ => $"Command status: {status}"
            };
        }

        /// <summary>
        /// Queues an extension script for execution in a debugging session.
        /// Extensions are PowerShell scripts that can execute multiple commands and implement sophisticated analysis patterns.
        /// Returns commandId for tracking.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="sessionId">Session ID from nexus_open_dump_analyze_session.</param>
        /// <param name="extensionName">Name of the extension to execute (e.g., 'stack_with_sources', 'basic_crash_analysis').</param>
        /// <param name="parameters">Optional JSON parameters to pass to the extension.</param>
        /// <returns>An object containing extension execution information including commandId.</returns>
        [McpServerTool, Description("🔧 QUEUE EXTENSION: Queue an extension script for execution in a debugging session. Returns commandId for tracking. Extensions can execute multiple commands and implement sophisticated analysis patterns.")]
        public static async Task<object> nexus_enqueue_async_extension_command(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("Extension name (e.g., 'stack_with_sources', 'basic_crash_analysis', 'memory_corruption_analysis', 'thread_deadlock_investigation')")] string extensionName,
            [Description("Optional JSON parameters for the extension")] object? parameters = null)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("McpNexusTools");
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
            var extensionManager = serviceProvider.GetService<IExtensionManager>();
            var extensionExecutor = serviceProvider.GetService<IExtensionExecutor>();
            var extensionTracker = serviceProvider.GetService<IExtensionCommandTracker>();
            var tokenValidator = serviceProvider.GetService<IExtensionTokenValidator>();

            logger.LogInformation("⚡ Queuing extension '{Extension}' for session: {SessionId}", extensionName, sessionId);

            // Check if extensions are available FIRST (before checking session)
            if (extensionManager == null || extensionExecutor == null || extensionTracker == null || tokenValidator == null)
            {
                return new
                {
                    sessionId = sessionId,
                    commandId = (string?)null,
                    extensionName = extensionName,
                    status = "Failed",
                    operation = "nexus_enqueue_async_extension_command",
                    message = "Extension system is not enabled or not properly configured"
                };
            }

            try
            {
                // Validate extension exists BEFORE validating session (fail fast)
                if (!extensionManager.ExtensionExists(extensionName))
                {
                    var availableExtensions = extensionManager.GetAllExtensions().Select(e => e.Name).ToList();
                    return new
                    {
                        sessionId = sessionId,
                        commandId = (string?)null,
                        extensionName = extensionName,
                        status = "Failed",
                        operation = "nexus_enqueue_async_extension_command",
                        message = $"Extension '{extensionName}' not found. Available extensions: {string.Join(", ", availableExtensions)}",
                        availableExtensions = availableExtensions
                    };
                }

                // Validate session exists
                if (!sessionManager.SessionExists(sessionId))
                {
                    return new
                    {
                        sessionId = sessionId,
                        commandId = (string?)null,
                        extensionName = extensionName,
                        status = "Failed",
                        operation = "nexus_enqueue_async_extension_command",
                        message = $"Session {sessionId} not found. Use 'sessions' resource to see available sessions."
                    };
                }

                // Generate command ID for this extension execution
                var commandId = $"ext-{Guid.NewGuid()}";

                // Track the extension command
                extensionTracker.TrackExtension(commandId, sessionId, extensionName, parameters);

                // Create callback token
                var callbackToken = tokenValidator.CreateToken(sessionId, commandId);

                // Start extension execution asynchronously (don't wait)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        extensionTracker.UpdateState(commandId, CommandState.Executing);

                        var result = await extensionExecutor.ExecuteAsync(
                            extensionName,
                            sessionId,
                            parameters,
                            commandId,
                            progressMessage => extensionTracker.UpdateProgress(commandId, progressMessage));

                        extensionTracker.StoreResult(commandId, result);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Extension {Extension} failed for session {SessionId}", extensionName, sessionId);
                        extensionTracker.StoreResult(commandId, new ExtensionResult
                        {
                            Success = false,
                            Error = ex.Message,
                            ExitCode = -1
                        });
                    }
                    finally
                    {
                        // Revoke token after extension completes
                        tokenValidator.RevokeToken(callbackToken);
                    }
                });

                var response = new
                {
                    sessionId = sessionId,
                    commandId = commandId,
                    extensionName = extensionName,
                    status = "Queued",
                    operation = "nexus_enqueue_async_extension_command",
                    message = $"Extension '{extensionName}' queued successfully. Use the 'commands' resource to monitor all commands or the 'nexus_read_dump_analyze_command_result' tool to get results.",
                    note = "Extensions may take several minutes to complete as they execute multiple debugging commands",
                    timeoutMinutes = 30 // Extensions have longer timeout than regular commands
                };

                logger.LogInformation("Extension {Extension} queued successfully with command ID {CommandId}", extensionName, commandId);
                return await Task.FromResult((object)response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue extension for session: {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    commandId = (string?)null,
                    extensionName = extensionName,
                    status = "Failed",
                    operation = "nexus_enqueue_async_extension_command",
                    message = $"Failed to queue extension: {ex.Message}"
                };

                return await Task.FromResult((object)errorResponse);
            }
        }
    }
}
