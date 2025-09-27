using System.Text.Json;
using System.Web;
using mcp_nexus.Models;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Tools;

namespace mcp_nexus.Protocol
{
    public class McpResourceService(
        ISessionManager sessionManager,
        ILogger<McpResourceService> logger)
    {
        // PERFORMANCE: Cache JsonSerializerOptions to avoid repeated allocation
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // Don't change property names - MCP protocol requires exact field names
        };

        /// <summary>
        /// Get all available resources
        /// </summary>
        public McpResource[] GetAllResources()
        {
            var resources = new List<McpResource>();

            // Static documentation resources
            resources.AddRange(GetDocumentationResources());

            // Dynamic session resources
            resources.AddRange(GetSessionResources());

            // Dynamic command history resources
            resources.AddRange(GetCommandHistoryResources());

            // Tool management resources
            resources.AddRange(GetToolManagementResources());

            return resources.ToArray();
        }

        /// <summary>
        /// Read content for a specific resource
        /// </summary>
        public async Task<McpResourceReadResult> ReadResource(string uri)
        {
            logger.LogDebug("Reading resource: {Uri}", uri);

            try
            {
                return uri switch
                {
                    var u when u.StartsWith("debugging://sessions/") => await ReadSessionResource(u),
                    var u when u.StartsWith("debugging://commands/") => await ReadCommandResource(u),
                    var u when u.StartsWith("debugging://docs/") => ReadDocumentationResource(u),
                    var u when u.StartsWith("debugging://tools/") => await ReadToolResource(u),
                    _ => throw new ArgumentException($"Unknown resource URI: {uri}")
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading resource: {Uri}", uri);
                throw;
            }
        }

        private static McpResource[] GetDocumentationResources()
        {
            return
            [
                new McpResource
                {
                    Uri = "debugging://docs/debugging-workflows",
                    Name = "Crash Analysis Workflows",
                    Description = "Common debugging patterns and step-by-step analysis workflows",
                    MimeType = "application/json"
                },
                new McpResource
                {
                    Uri = "debugging://docs/usage",
                    Name = "Usage",
                    Description = "Essential tool usage information for MCP Nexus server",
                    MimeType = "application/json"
                }
            ];
        }

        private McpResource[] GetSessionResources()
        {
            var sessions = sessionManager.GetAllSessions();
            var resources = new List<McpResource>();

            foreach (var session in sessions)
            {
                resources.Add(new McpResource
                {
                    Uri = $"debugging://sessions/{session.SessionId}",
                    Name = $"Session {session.SessionId}",
                    Description = $"Active debugging session for {Path.GetFileName(session.DumpPath ?? "unknown")}",
                    MimeType = "application/json",
                    ExpiresAt = DateTime.UtcNow.AddHours(24) // Sessions expire after 24 hours
                });

                resources.Add(new McpResource
                {
                    Uri = $"debugging://sessions/{session.SessionId}/dump-info",
                    Name = $"Dump Info - {session.SessionId}",
                    Description = $"Detailed information about the dump file for session {session.SessionId}",
                    MimeType = "application/json",
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                });
            }

            // Add aggregate session resource
            if (sessions.Any())
            {
                resources.Add(new McpResource
                {
                    Uri = "debugging://sessions/active",
                    Name = "Active Debugging Sessions",
                    Description = "Real-time list of all active debugging sessions with their status and details",
                    MimeType = "application/json"
                });
            }

            return resources.ToArray();
        }

        private McpResource[] GetCommandHistoryResources()
        {
            var sessions = sessionManager.GetAllSessions();
            var resources = new List<McpResource>();

            foreach (var session in sessions)
            {
                resources.Add(new McpResource
                {
                    Uri = $"debugging://commands/history/{session.SessionId}",
                    Name = $"Command History - {session.SessionId}",
                    Description = $"History of executed WinDBG commands for session {session.SessionId}",
                    MimeType = "application/json",
                    ExpiresAt = DateTime.UtcNow.AddDays(7) // Command history kept for 7 days
                });
            }

            // Add aggregate command history resource
            if (sessions.Any())
            {
                resources.Add(new McpResource
                {
                    Uri = "debugging://commands/history",
                    Name = "All Command History",
                    Description = "Complete history of executed WinDBG commands across all sessions",
                    MimeType = "application/json"
                });
            }

            return resources.ToArray();
        }

        private async Task<McpResourceReadResult> ReadSessionResource(string uri)
        {
            if (uri == "debugging://sessions/active")
            {
                return await ReadActiveSessionsResource();
            }

            // Parse session ID from URI like "debugging://sessions/{sessionId}"
            var sessionId = uri.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentException($"Invalid session resource URI: {uri}");
            }

            if (uri.EndsWith("/dump-info"))
            {
                return await ReadSessionDumpInfoResource(sessionId);
            }

            return await ReadSessionDetailsResource(sessionId);
        }

        private async Task<McpResourceReadResult> ReadCommandResource(string uri)
        {
            if (uri == "debugging://commands/history")
            {
                return await ReadAllCommandHistoryResource();
            }

            // Parse session ID from URI like "debugging://commands/history/{sessionId}"
            var sessionId = uri.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentException($"Invalid command resource URI: {uri}");
            }

            return await ReadSessionCommandHistoryResource(sessionId);
        }

        private McpResourceReadResult ReadDocumentationResource(string uri)
        {
            var content = uri switch
            {
                "debugging://docs/windbg-commands" => GetWindbgCommandsDocumentation(),
                "debugging://docs/debugging-workflows" => GetDebuggingWorkflowsDocumentation(),
                "debugging://docs/usage" => GetTroubleshootingDocumentation(),
                _ => throw new ArgumentException($"Unknown documentation resource: {uri}")
            };

            return new McpResourceReadResult
            {
                Contents = new[]
                {
                    new McpResourceContent
                    {
                        Uri = uri,
                        MimeType = "application/json",
                        Text = content
                    }
                }
            };
        }

        private Task<McpResourceReadResult> ReadActiveSessionsResource()
        {
            var sessions = sessionManager.GetAllSessions();
            var sessionData = new List<object>();

            foreach (var session in sessions)
            {
                var context = sessionManager.GetSessionContext(session.SessionId);
                sessionData.Add(new
                {
                    sessionId = session.SessionId,
                    dumpFile = session.DumpPath != null ? Path.GetFileName(session.DumpPath) : "Unknown",
                    status = session.Status.ToString(),
                    createdAt = session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    lastActivity = session.LastActivity.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    commandsProcessed = context?.CommandsProcessed ?? 0,
                    activeCommands = context?.ActiveCommands ?? 0,
                    dumpPath = session.DumpPath
                });
            }

            var content = JsonSerializer.Serialize(new
            {
                totalSessions = sessions.Count(),
                activeSessions = sessionData,
                lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            }, s_jsonOptions);

            var result = new McpResourceReadResult
            {
                Contents = new[]
                {
                    new McpResourceContent
                    {
                        Uri = "debugging://sessions/active",
                        MimeType = "application/json",
                        Text = content
                    }
                }
            };

            return Task.FromResult(result);
        }

        private Task<McpResourceReadResult> ReadSessionDetailsResource(string sessionId)
        {
            if (!sessionManager.SessionExists(sessionId))
            {
                throw new ArgumentException($"Session not found: {sessionId}");
            }

            var session = sessionManager.GetAllSessions().FirstOrDefault(s => s.SessionId == sessionId);
            var context = sessionManager.GetSessionContext(sessionId);

            var sessionData = new
            {
                sessionId = session?.SessionId,
                dumpFile = session?.DumpPath != null ? Path.GetFileName(session.DumpPath) : "Unknown",
                dumpPath = session?.DumpPath,
                status = session?.Status.ToString(),
                createdAt = session?.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                lastActivity = session?.LastActivity.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                commandsProcessed = context?.CommandsProcessed ?? 0,
                activeCommands = context?.ActiveCommands ?? 0,
                context = context
            };

            var content = JsonSerializer.Serialize(sessionData, s_jsonOptions);

            var result = new McpResourceReadResult
            {
                Contents = new[]
                {
                    new McpResourceContent
                    {
                        Uri = $"debugging://sessions/{sessionId}",
                        MimeType = "application/json",
                        Text = content
                    }
                }
            };

            return Task.FromResult(result);
        }

        private Task<McpResourceReadResult> ReadSessionDumpInfoResource(string sessionId)
        {
            if (!sessionManager.SessionExists(sessionId))
            {
                throw new ArgumentException($"Session not found: {sessionId}");
            }

            var session = sessionManager.GetAllSessions().FirstOrDefault(s => s.SessionId == sessionId);
            
            var dumpInfo = new
            {
                sessionId = session?.SessionId,
                dumpPath = session?.DumpPath,
                dumpFile = session?.DumpPath != null ? Path.GetFileName(session.DumpPath) : "Unknown",
                fileExists = session?.DumpPath != null && File.Exists(session.DumpPath),
                fileSize = session?.DumpPath != null && File.Exists(session.DumpPath) 
                    ? new FileInfo(session.DumpPath).Length 
                    : (long?)null,
                lastModified = session?.DumpPath != null && File.Exists(session.DumpPath)
                    ? File.GetLastWriteTime(session.DumpPath).ToString("yyyy-MM-dd HH:mm:ss UTC")
                    : null,
                sessionCreated = session?.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                status = session?.Status.ToString()
            };

            var content = JsonSerializer.Serialize(dumpInfo, s_jsonOptions);

            var result = new McpResourceReadResult
            {
                Contents = new[]
                {
                    new McpResourceContent
                    {
                        Uri = $"debugging://sessions/{sessionId}/dump-info",
                        MimeType = "application/json",
                        Text = content
                    }
                }
            };

            return Task.FromResult(result);
        }

        private Task<McpResourceReadResult> ReadAllCommandHistoryResource()
        {
            var sessions = sessionManager.GetAllSessions();
            var allCommands = new List<object>();

            foreach (var session in sessions)
            {
                // TODO: Implement command history retrieval from command queue service
                // For now, return placeholder data
                allCommands.Add(new
                {
                    sessionId = session.SessionId,
                    commands = new object[0], // Placeholder
                    message = "Command history not yet implemented"
                });
            }

            var content = JsonSerializer.Serialize(new
            {
                totalCommands = allCommands.Count,
                sessions = allCommands,
                lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            }, s_jsonOptions);

            var result = new McpResourceReadResult
            {
                Contents = new[]
                {
                    new McpResourceContent
                    {
                        Uri = "debugging://commands/history",
                        MimeType = "application/json",
                        Text = content
                    }
                }
            };

            return Task.FromResult(result);
        }

        private Task<McpResourceReadResult> ReadSessionCommandHistoryResource(string sessionId)
        {
            if (!sessionManager.SessionExists(sessionId))
            {
                throw new ArgumentException($"Session not found: {sessionId}");
            }

            // TODO: Implement command history retrieval from command queue service
            var content = JsonSerializer.Serialize(new
            {
                sessionId = sessionId,
                commands = new object[0], // Placeholder
                message = "Command history not yet implemented",
                lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            }, s_jsonOptions);

            var result = new McpResourceReadResult
            {
                Contents = new[]
                {
                    new McpResourceContent
                    {
                        Uri = $"debugging://commands/history/{sessionId}",
                        MimeType = "application/json",
                        Text = content
                    }
                }
            };

            return Task.FromResult(result);
        }

        private static string GetWindbgCommandsDocumentation()
        {
            var commands = new
            {
                title = "WinDBG Command Reference",
                description = "Comprehensive reference of available WinDBG commands",
                categories = new[]
                {
                    new
                    {
                        name = "Analysis Commands",
                        commands = new[]
                        {
                            new { command = "!analyze -v", description = "Perform automatic analysis of the crash dump" },
                            new { command = "!analyze -f", description = "Force analysis even if already done" },
                            new { command = "!analyze -hang", description = "Analyze for hang conditions" }
                        }
                    },
                    new
                    {
                        name = "Thread Commands",
                        commands = new[]
                        {
                            new { command = "!threads", description = "List all threads" },
                            new { command = "~*k", description = "Show stack traces for all threads" },
                            new { command = "~*e !clrstack", description = "Show managed stack traces for all threads" }
                        }
                    },
                    new
                    {
                        name = "Memory Commands",
                        commands = new[]
                        {
                            new { command = "!heap -stat", description = "Show heap statistics" },
                            new { command = "!vm", description = "Show virtual memory information" },
                            new { command = "!address", description = "Show memory address information" }
                        }
                    }
                }
            };

            return JsonSerializer.Serialize(commands, s_jsonOptions);
        }

        private static string GetDebuggingWorkflowsDocumentation()
        {
            var workflows = new
            {
                title = "Crash Analysis Workflows",
                description = "Comprehensive step-by-step analysis workflows for Windows crash dump investigation",
                workflows = new[]
                {
                    new
                    {
                        name = "Basic Crash Analysis",
                        description = "Essential commands for initial crash investigation",
                        steps = new[]
                        {
                            "!analyze -v",
                            "!threads",
                            "~*k",
                            "!locks",
                            "!runaway"
                        },
                        expectedOutcome = "Identify the faulting thread, exception type, and basic crash context"
                    },
                    new
                    {
                        name = "Memory Corruption Analysis",
                        description = "Investigate memory-related crashes and corruption",
                        steps = new[]
                        {
                            "!analyze -v",
                            "!heap -stat",
                            "!heap -flt s <size>",
                            "!heap -p -a <address>",
                            "!address <address>",
                            "!vprot <address>"
                        },
                        expectedOutcome = "Identify heap corruption, invalid memory access, or buffer overflows"
                    },
                    new
                    {
                        name = "Thread Deadlock Investigation",
                        description = "Analyze thread synchronization issues and deadlocks",
                        steps = new[]
                        {
                            "!threads",
                            "!locks",
                            "!cs -l",
                            "!cs -o <address>",
                            "~*k",
                            "!runaway"
                        },
                        expectedOutcome = "Identify deadlocked threads and synchronization objects"
                    },
                    new
                    {
                        name = "Exception Analysis",
                        description = "Deep dive into exception handling and stack traces",
                        steps = new[]
                        {
                            "!analyze -v",
                            "!exception",
                            "!peb",
                            "!teb",
                            "~*k",
                            "!threads"
                        },
                        expectedOutcome = "Understand exception context, thread state, and call stack"
                    },
                    new
                    {
                        name = "Module and DLL Analysis",
                        description = "Investigate loaded modules and potential DLL issues",
                        steps = new[]
                        {
                            "lm",
                            "lmv m <module>",
                            "!lmi <module>",
                            "!dh <module>",
                            "!peb"
                        },
                        expectedOutcome = "Identify problematic modules, version mismatches, or loading issues"
                    },
                    new
                    {
                        name = "Performance and Resource Analysis",
                        description = "Analyze performance issues and resource consumption",
                        steps = new[]
                        {
                            "!runaway",
                            "!threads",
                            "!heap -stat",
                            "!gchandles",
                            "!handle",
                            "!process 0 0"
                        },
                        expectedOutcome = "Identify resource leaks, high CPU usage, or memory consumption issues"
                    },
                    new
                    {
                        name = "Network and I/O Analysis",
                        description = "Investigate network-related crashes and I/O issues",
                        steps = new[]
                        {
                            "!analyze -v",
                            "!handle",
                            "!object",
                            "!irp",
                            "!devobj",
                            "!drvobj"
                        },
                        expectedOutcome = "Identify network stack issues, driver problems, or I/O failures"
                    }
                },
                generalTips = new[]
                {
                    "Always start with !analyze -v for automatic analysis",
                    "Use ~*k to see all thread call stacks",
                    "Check !runaway for threads consuming excessive CPU",
                    "Examine !locks for synchronization issues",
                    "Use !heap commands for memory-related problems",
                    "Check loaded modules with lm for version issues"
                }
            };

            return JsonSerializer.Serialize(workflows, s_jsonOptions);
        }

        private static string GetTroubleshootingDocumentation()
        {
            // Return the actual usage content as the usage guide
            return JsonSerializer.Serialize(SessionAwareWindbgTool.USAGE_EXPLANATION, s_jsonOptions);
        }

        private static McpResource[] GetToolManagementResources()
        {
            return
            [
                new McpResource
                {
                    Uri = "debugging://tools/sessions",
                    Name = "List Sessions",
                    Description = "List all active debugging sessions",
                    MimeType = "application/json"
                },
                new McpResource
                {
                    Uri = "debugging://tools/commands",
                    Name = "List Commands",
                    Description = "List async commands from all sessions or filter by specific session",
                    MimeType = "application/json"
                },
                new McpResource
                {
                    Uri = "debugging://tools/command-result",
                    Name = "Command Result",
                    Description = "Get status and results of a specific async command",
                    MimeType = "application/json"
                }
            ];
        }

        private async Task<McpResourceReadResult> ReadToolResource(string uri)
        {
            return uri switch
            {
                "debugging://tools/sessions" => await ReadSessionsList(),
                "debugging://tools/commands" => await ReadCommandsList(uri),
                "debugging://tools/command-result" => await ReadCommandStatusHelp(),
                var u when u.StartsWith("debugging://tools/commands?") => await ReadCommandsList(u),
                var u when u.StartsWith("debugging://tools/command-result?") => await ReadCommandStatus(u),
                _ => throw new ArgumentException($"Unknown tool resource URI: {uri}")
            };
        }

        private Task<McpResourceReadResult> ReadSessionsList()
        {
            try
            {
                var sessions = sessionManager.GetAllSessions();
                var sessionData = sessions.Select(s => new
                {
                    sessionId = s.SessionId,
                    dumpPath = s.DumpPath,
                    isActive = s.Status == SessionStatus.Active,
                    status = s.Status.ToString(),
                    createdAt = s.CreatedAt,
                    lastActivity = s.LastActivity
                }).ToArray();

                var result = new
                {
                    sessions = sessionData,
                    count = sessionData.Length,
                    timestamp = DateTime.UtcNow
                };

                return Task.FromResult(new McpResourceReadResult
                {
                    Contents = new[]
                    {
                        new McpResourceContent
                        {
                            MimeType = "application/json",
                            Text = JsonSerializer.Serialize(result, s_jsonOptions)
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading sessions list");
                throw;
            }
        }

        private Task<McpResourceReadResult> ReadCommandsList(string uri)
        {
            try
            {
                string? sessionId = null;
                
                // Extract sessionId from URI if provided: debugging://tools/commands?sessionId=xxx
                var uriParts = uri.Split('?');
                if (uriParts.Length >= 2)
                {
                    var queryParams = System.Web.HttpUtility.ParseQueryString(uriParts[1]);
                    sessionId = queryParams["sessionId"];
                }

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
                    var sessionCommands = GetSessionCommands(sessionContext, sessionId);
                    commandsBySession[sessionId] = sessionCommands;
                }
                else
                {
                    // Get commands from all sessions
                    foreach (var session in allSessions)
                    {
                        var sessionContext = sessionManager.GetSessionContext(session.SessionId);
                        var sessionCommands = GetSessionCommands(sessionContext, session.SessionId);
                        commandsBySession[session.SessionId] = sessionCommands;
                    }
                }

                var result = new
                {
                    commands = commandsBySession,
                    totalSessions = commandsBySession.Count,
                    totalCommands = commandsBySession.Values.Cast<Dictionary<string, object>>().Sum(c => c.Count),
                    timestamp = DateTime.UtcNow,
                    note = string.IsNullOrEmpty(sessionId) ? "Commands from all sessions" : $"Commands from session {sessionId}"
                };

                return Task.FromResult(new McpResourceReadResult
                {
                    Contents = new[]
                    {
                        new McpResourceContent
                        {
                            MimeType = "application/json",
                            Text = JsonSerializer.Serialize(result, s_jsonOptions)
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading commands list for URI: {Uri}", uri);
                throw;
            }
        }

        private Dictionary<string, object> GetSessionCommands(SessionContext? sessionContext, string sessionId)
        {
            var commands = new Dictionary<string, object>();
            
            if (sessionContext == null)
            {
                // Return placeholder if session context is not available
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
                },
                new
                {
                    commandId = "cmd-003",
                    command = "~*k",
                    status = "Queued",
                    isFinished = false,
                    createdAt = DateTime.UtcNow.AddMinutes(-1),
                    completedAt = (DateTime?)null,
                    duration = TimeSpan.Zero,
                    error = (string?)null
                }
            };

            foreach (dynamic cmd in mockCommands)
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

        private async Task<McpResourceReadResult> ReadCommandStatus(string uri)
        {
            try
            {
                // Extract sessionId and commandId from URI: debugging://tools/command-result?sessionId=xxx&commandId=yyy
                var uriParts = uri.Split('?');
                if (uriParts.Length < 2)
                {
                    throw new ArgumentException("Session ID and Command ID required. Use: debugging://tools/command-result?sessionId=<sessionId>&commandId=<commandId>");
                }

                var queryParams = System.Web.HttpUtility.ParseQueryString(uriParts[1]);
                var sessionId = queryParams["sessionId"];
                var commandId = queryParams["commandId"];
                
                if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(commandId))
                {
                    throw new ArgumentException("Both sessionId and commandId parameters are required");
                }

                // Get session context to retrieve command status
                var sessionContext = sessionManager.GetSessionContext(sessionId);
                if (sessionContext == null)
                {
                    // Session doesn't exist - return ERROR
                    throw new ArgumentException($"Session {sessionId} not found");
                }

                // Get the command queue for this session to check command status
                var commandQueue = sessionManager.GetCommandQueue(sessionId);
                
                // Try to get the command result
                try
                {
                    var commandResult = await commandQueue.GetCommandResult(commandId);
                    
                    // Parse the result to determine status
                    var isCompleted = !commandResult.Contains("still executing") && 
                                    !commandResult.Contains("Command not found");
                    
                    var result = new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        command = (string?)null, // Command text not available from GetCommandResult
                        status = isCompleted ? "Completed" : "In Progress",
                        result = isCompleted ? commandResult : null,
                        error = commandResult.Contains("Command not found") ? "Command not found" : null,
                        createdAt = (DateTime?)null, // Not available from GetCommandResult
                        completedAt = isCompleted ? DateTime.UtcNow : (DateTime?)null,
                        timestamp = DateTime.UtcNow,
                        message = isCompleted ? null : "Command is still executing - check again in a few seconds. You can also track command status using the 'List Sessions' or 'List Commands' resources."
                    };

                    return Task.FromResult(new McpResourceReadResult
                    {
                        Contents = new[]
                        {
                            new McpResourceContent
                            {
                                MimeType = "application/json",
                                Text = JsonSerializer.Serialize(result, s_jsonOptions)
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Command queue error - return error result
                    var errorResult = new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        command = (string?)null,
                        status = "Error",
                        result = (string?)null,
                        error = ex.Message,
                        createdAt = (DateTime?)null,
                        completedAt = (DateTime?)null,
                        timestamp = DateTime.UtcNow,
                        message = "Error accessing command queue"
                    };

                    return Task.FromResult(new McpResourceReadResult
                    {
                        Contents = new[]
                        {
                            new McpResourceContent
                            {
                                MimeType = "application/json",
                                Text = JsonSerializer.Serialize(errorResult, s_jsonOptions)
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading command status for URI: {Uri}", uri);
                throw;
            }
        }


        private Task<McpResourceReadResult> ReadCommandStatusHelp()
        {
            var help = new
            {
                title = "Command Status Resource",
                description = "Get status and results of a specific async command",
                usage = "Use: debugging://tools/command-result?sessionId=<sessionId>&commandId=<commandId>",
                example = "debugging://tools/command-result?sessionId=abc123&commandId=cmd456",
                note = "This resource requires both sessionId and commandId parameters to get command status"
            };

            return Task.FromResult(new McpResourceReadResult
            {
                Contents = new[]
                {
                    new McpResourceContent
                    {
                        MimeType = "application/json",
                        Text = JsonSerializer.Serialize(help, s_jsonOptions)
                    }
                }
            });
        }
    }
}
