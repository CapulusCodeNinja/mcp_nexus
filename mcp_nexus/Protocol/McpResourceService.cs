using System.Text.Json;
using System.Web;
using mcp_nexus.Models;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Tools;

namespace mcp_nexus.Protocol
{
    public class CommandFilters
    {
        public string? SessionId { get; set; }
        public string? CommandText { get; set; }
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string SortBy { get; set; } = "createdAt";
        public string SortOrder { get; set; } = "desc";
    }

    public class SessionFilters
    {
        public string? SessionId { get; set; }
        public string? DumpPath { get; set; }
        public string? Status { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string SortBy { get; set; } = "createdAt";
        public string SortOrder { get; set; } = "desc";
    }

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
                    var u when u.StartsWith("sessions://") => await ReadSessionResource(u),
                    var u when u.StartsWith("commands://") => await ReadCommandResource(u),
                    var u when u.StartsWith("docs://") => ReadDocumentationResource(u),
                    "sessions://list" => await ReadSessionsList(uri),
                    var u when u.StartsWith("sessions://list?") => await ReadSessionsList(u),
                    "commands://list" => await ReadCommandsList(uri),
                    "commands://result" => await ReadCommandStatusHelp(),
                    var u when u.StartsWith("commands://list?") => await ReadCommandsList(u),
                    var u when u.StartsWith("commands://result?") => await ReadCommandStatus(u),
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
                    Uri = "docs://workflows",
                    Name = "Crash Analysis Workflows",
                    Description = "Common debugging patterns and step-by-step analysis workflows",
                    MimeType = "application/json"
                },
                new McpResource
                {
                    Uri = "docs://usage",
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
                    Uri = $"sessions://{session.SessionId}",
                    Name = $"Session {session.SessionId}",
                    Description = $"Active debugging session for {Path.GetFileName(session.DumpPath ?? "unknown")}",
                    MimeType = "application/json",
                    ExpiresAt = DateTime.UtcNow.AddHours(24) // Sessions expire after 24 hours
                });

                resources.Add(new McpResource
                {
                    Uri = $"sessions://{session.SessionId}/dump-info",
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
                    Uri = "sessions://active",
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
                    Uri = $"commands://history/{session.SessionId}",
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
                    Uri = "commands://history",
                    Name = "All Command History",
                    Description = "Complete history of executed WinDBG commands across all sessions",
                    MimeType = "application/json"
                });
            }

            return resources.ToArray();
        }

        private async Task<McpResourceReadResult> ReadSessionResource(string uri)
        {
            if (uri == "sessions://active")
            {
                return await ReadActiveSessionsResource();
            }

            // Parse session ID from URI like "sessions://{sessionId}"
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
            if (uri == "commands://history")
            {
                return await ReadAllCommandHistoryResource();
            }

            // Parse session ID from URI like "commands://history/{sessionId}"
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
                "docs://workflows" => GetDebuggingWorkflowsDocumentation(),
                "docs://usage" => GetTroubleshootingDocumentation(),
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
                lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                usage = SessionAwareWindbgTool.USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
            }, s_jsonOptions);

            var result = new McpResourceReadResult
            {
                Contents = new[]
                {
                    new McpResourceContent
                    {
                        Uri = "sessions://active",
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
                throw new ArgumentException($"Session not found: {sessionId}. Use sessions://list to see available sessions.");
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
                        Uri = $"sessions://{sessionId}",
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
                throw new ArgumentException($"Session not found: {sessionId}. Use sessions://list to see available sessions.");
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
                        Uri = $"sessions://{sessionId}/dump-info",
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
                        Uri = "commands://history",
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
                throw new ArgumentException($"Session not found: {sessionId}. Use sessions://list to see available sessions.");
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
                        Uri = $"commands://history/{sessionId}",
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
                description = "Example workflows for Windows crash dump investigation - these are starting points, not exhaustive lists. AI clients should explore beyond these basic patterns and adapt commands based on specific crash scenarios.",
                disclaimer = "IMPORTANT: These are example workflows below many others. Do not limit yourself to only these patterns - explore additional WinDBG commands, combine different approaches, and adapt based on the specific crash context and symptoms you observe.",
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
                    "Check loaded modules with lm for version issues",
                    "EXPLORE BEYOND THESE WORKFLOWS: These are just examples - there are hundreds of WinDBG commands available",
                    "ADAPT AND COMBINE: Mix and match commands based on what you discover in the crash",
                    "LEARN MORE: Use .help <command> to understand any WinDBG command better",
                    "BE CREATIVE: Each crash is unique - don't just follow these patterns blindly",
                    "INVESTIGATE DEEPER: Look for patterns, correlations, and root causes beyond basic analysis"
                },
                explorationGuidance = new
                {
                    title = "Beyond These Workflows",
                    message = "These workflows are starting points, not complete solutions. Successful crash analysis requires:",
                    principles = new[]
                    {
                        "Critical thinking and pattern recognition",
                        "Understanding the specific application and its architecture",
                        "Knowledge of Windows internals and common failure modes",
                        "Ability to adapt and combine different analysis techniques",
                        "Persistence in following leads and investigating anomalies",
                        "Documentation of findings and correlation with other evidence"
                    },
                    additionalResources = new[]
                    {
                        "WinDBG help system: .help <command>",
                        "Microsoft documentation and KB articles",
                        "Community forums and expert knowledge",
                        "Application-specific debugging guides",
                        "Windows internals books and resources"
                    }
                },
                usage = SessionAwareWindbgTool.USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
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
                    Uri = "sessions://list",
                    Name = "List Sessions",
                    Description = "List all debugging sessions with advanced filtering (status, dump path, time ranges, etc.)",
                    MimeType = "application/json"
                },
                new McpResource
                {
                    Uri = "commands://list",
                    Name = "List Commands",
                    Description = "List async commands from all sessions with advanced filtering (sessionId, command text, time range, pagination, sorting)",
                    MimeType = "application/json"
                },
                new McpResource
                {
                    Uri = "commands://result",
                    Name = "Command Result",
                    Description = "Get status and results of a specific async command",
                    MimeType = "application/json"
                }
            ];
        }


        private Task<McpResourceReadResult> ReadSessionsList(string uri)
        {
            try
            {
                // Parse query parameters
                var filters = ParseSessionFilters(uri);

                var allSessions = sessionManager.GetAllSessions();
                var filteredSessions = allSessions.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrEmpty(filters.SessionId))
                    filteredSessions = filteredSessions.Where(s => s.SessionId.Contains(filters.SessionId!, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(filters.DumpPath))
                    filteredSessions = filteredSessions.Where(s => s.DumpPath.Contains(filters.DumpPath!, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(filters.Status))
                    filteredSessions = filteredSessions.Where(s => s.Status.ToString().Equals(filters.Status, StringComparison.OrdinalIgnoreCase));

                if (filters.IsActive.HasValue)
                    filteredSessions = filteredSessions.Where(s => (s.Status == SessionStatus.Active) == filters.IsActive.Value);

                if (filters.CreatedFrom.HasValue)
                    filteredSessions = filteredSessions.Where(s => s.CreatedAt >= filters.CreatedFrom.Value);

                if (filters.CreatedTo.HasValue)
                    filteredSessions = filteredSessions.Where(s => s.CreatedAt <= filters.CreatedTo.Value);

                // Apply sorting
                filteredSessions = filters.SortBy.ToLowerInvariant() switch
                {
                    "sessionid" => filters.SortOrder.ToLowerInvariant() == "asc"
                        ? filteredSessions.OrderBy(s => s.SessionId)
                        : filteredSessions.OrderByDescending(s => s.SessionId),
                    "dumppath" => filters.SortOrder.ToLowerInvariant() == "asc"
                        ? filteredSessions.OrderBy(s => s.DumpPath)
                        : filteredSessions.OrderByDescending(s => s.DumpPath),
                    "status" => filters.SortOrder.ToLowerInvariant() == "asc"
                        ? filteredSessions.OrderBy(s => s.Status)
                        : filteredSessions.OrderByDescending(s => s.Status),
                    _ => filters.SortOrder.ToLowerInvariant() == "asc"
                        ? filteredSessions.OrderBy(s => s.CreatedAt)
                        : filteredSessions.OrderByDescending(s => s.CreatedAt)
                };

                // Apply pagination
                if (filters.Offset.HasValue && filters.Offset.Value > 0)
                    filteredSessions = filteredSessions.Skip(filters.Offset.Value);

                if (filters.Limit.HasValue && filters.Limit.Value > 0)
                    filteredSessions = filteredSessions.Take(filters.Limit.Value);

                var sessionData = filteredSessions.Select(s => new
                {
                    sessionId = s.SessionId,
                    dumpPath = s.DumpPath,
                    isActive = s.Status == SessionStatus.Active,
                    status = s.Status.ToString(),
                    createdAt = s.CreatedAt,
                    lastActivity = s.LastActivity,
                    symbolsPath = s.SymbolsPath,
                    processId = s.ProcessId
                }).ToArray();

                var result = new
                {
                    sessions = sessionData,
                    count = sessionData.Length,
                    totalCount = allSessions.Count(),
                    filters = new
                    {
                        sessionId = filters.SessionId,
                        dumpPath = filters.DumpPath,
                        status = filters.Status,
                        isActive = filters.IsActive,
                        createdFrom = filters.CreatedFrom,
                        createdTo = filters.CreatedTo,
                        limit = filters.Limit,
                        offset = filters.Offset,
                        sortBy = filters.SortBy,
                        sortOrder = filters.SortOrder
                    },
                    timestamp = DateTime.UtcNow,
                    usage = SessionAwareWindbgTool.USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
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
                // Parse query parameters
                var filters = ParseCommandFilters(uri);

                var allSessions = sessionManager.GetAllSessions();
                var commandsBySession = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(filters.SessionId))
                {
                    // Filter to specific session
                    var session = allSessions.FirstOrDefault(s => s.SessionId == filters.SessionId);
                    if (session == null)
                    {
                        throw new ArgumentException($"Session {filters.SessionId} not found");
                    }

                    var sessionContext = sessionManager.GetSessionContext(filters.SessionId);
                    var sessionCommands = GetSessionCommands(sessionContext, filters.SessionId, filters);
                    commandsBySession[filters.SessionId] = sessionCommands;
                }
                else
                {
                    // Get commands from all sessions
                    foreach (var session in allSessions)
                    {
                        var sessionContext = sessionManager.GetSessionContext(session.SessionId);
                        var sessionCommands = GetSessionCommands(sessionContext, session.SessionId, filters);
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
                        sessionId = filters.SessionId,
                        commandText = filters.CommandText,
                        fromTime = filters.FromTime?.ToString("O"),
                        toTime = filters.ToTime?.ToString("O"),
                        limit = filters.Limit,
                        offset = filters.Offset,
                        sortBy = filters.SortBy,
                        sortOrder = filters.SortOrder
                    },
                    note = string.IsNullOrEmpty(filters.SessionId) ? "Commands from all sessions" : $"Commands from session {filters.SessionId}",
                    usage = SessionAwareWindbgTool.USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
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

        private Dictionary<string, object> GetSessionCommands(SessionContext? sessionContext, string sessionId, CommandFilters filters)
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
                },
                new
                {
                    commandId = "cmd-004",
                    command = "!peb",
                    status = "Completed",
                    isFinished = true,
                    createdAt = DateTime.UtcNow.AddMinutes(-10),
                    completedAt = DateTime.UtcNow.AddMinutes(-9),
                    duration = TimeSpan.FromMinutes(1),
                    error = (string?)null
                },
                new
                {
                    commandId = "cmd-005",
                    command = "!runaway",
                    status = "Completed",
                    isFinished = true,
                    createdAt = DateTime.UtcNow.AddMinutes(-15),
                    completedAt = DateTime.UtcNow.AddMinutes(-14),
                    duration = TimeSpan.FromMinutes(1),
                    error = (string?)null
                }
            };

            // Apply filters
            var filteredCommands = mockCommands.AsEnumerable();

            // Filter by command text
            if (!string.IsNullOrEmpty(filters.CommandText))
            {
                filteredCommands = filteredCommands.Where(cmd =>
                {
                    dynamic d = cmd;
                    return d.command.ToString().Contains(filters.CommandText, StringComparison.OrdinalIgnoreCase);
                });
            }

            // Filter by time range
            if (filters.FromTime.HasValue)
            {
                filteredCommands = filteredCommands.Where(cmd =>
                {
                    dynamic d = cmd;
                    return d.createdAt >= filters.FromTime.Value;
                });
            }

            if (filters.ToTime.HasValue)
            {
                filteredCommands = filteredCommands.Where(cmd =>
                {
                    dynamic d = cmd;
                    return d.createdAt <= filters.ToTime.Value;
                });
            }

            // Sort commands
            filteredCommands = filters.SortBy.ToLower() switch
            {
                "command" => filters.SortOrder.ToLower() == "asc"
                    ? filteredCommands.OrderBy(cmd => { dynamic d = cmd; return d.command; })
                    : filteredCommands.OrderByDescending(cmd => { dynamic d = cmd; return d.command; }),
                "status" => filters.SortOrder.ToLower() == "asc"
                    ? filteredCommands.OrderBy(cmd => { dynamic d = cmd; return d.status; })
                    : filteredCommands.OrderByDescending(cmd => { dynamic d = cmd; return d.status; }),
                "createdat" or "created_at" => filters.SortOrder.ToLower() == "asc"
                    ? filteredCommands.OrderBy(cmd => { dynamic d = cmd; return d.createdAt; })
                    : filteredCommands.OrderByDescending(cmd => { dynamic d = cmd; return d.createdAt; }),
                _ => filters.SortOrder.ToLower() == "asc"
                    ? filteredCommands.OrderBy(cmd => { dynamic d = cmd; return d.createdAt; })
                    : filteredCommands.OrderByDescending(cmd => { dynamic d = cmd; return d.createdAt; })
            };

            // Apply pagination
            if (filters.Offset.HasValue)
            {
                filteredCommands = filteredCommands.Skip(filters.Offset.Value);
            }

            if (filters.Limit.HasValue)
            {
                filteredCommands = filteredCommands.Take(filters.Limit.Value);
            }

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

        private static CommandFilters ParseCommandFilters(string uri)
        {
            var filters = new CommandFilters();

            var uriParts = uri.Split('?');
            if (uriParts.Length < 2) return filters;

            var queryParams = System.Web.HttpUtility.ParseQueryString(uriParts[1]);

            // Parse sessionId
            filters.SessionId = queryParams["sessionId"];

            // Parse command text filter
            filters.CommandText = queryParams["command"];

            // Parse time range filters
            if (DateTime.TryParse(queryParams["from"], out var fromTime))
                filters.FromTime = fromTime;
            if (DateTime.TryParse(queryParams["to"], out var toTime))
                filters.ToTime = toTime;

            // Parse pagination
            if (int.TryParse(queryParams["limit"], out var limit))
                filters.Limit = limit;
            if (int.TryParse(queryParams["offset"], out var offset))
                filters.Offset = offset;

            // Parse sorting
            filters.SortBy = queryParams["sortBy"] ?? "createdAt";
            filters.SortOrder = queryParams["order"] ?? "desc";

            return filters;
        }

        private static SessionFilters ParseSessionFilters(string uri)
        {
            var filters = new SessionFilters();

            var uriParts = uri.Split('?');
            if (uriParts.Length < 2) return filters;

            var queryParams = System.Web.HttpUtility.ParseQueryString(uriParts[1]);

            // Parse sessionId
            filters.SessionId = queryParams["sessionId"];

            // Parse dump path filter
            filters.DumpPath = queryParams["dumpPath"];

            // Parse status filter
            filters.Status = queryParams["status"];

            // Parse isActive filter
            if (bool.TryParse(queryParams["isActive"], out var isActive))
                filters.IsActive = isActive;

            // Parse creation time range filters
            if (DateTime.TryParse(queryParams["createdFrom"], out var createdFrom))
                filters.CreatedFrom = createdFrom;
            if (DateTime.TryParse(queryParams["createdTo"], out var createdTo))
                filters.CreatedTo = createdTo;

            // Parse pagination
            if (int.TryParse(queryParams["limit"], out var limit))
                filters.Limit = limit;
            if (int.TryParse(queryParams["offset"], out var offset))
                filters.Offset = offset;

            // Parse sorting
            filters.SortBy = queryParams["sortBy"] ?? "createdAt";
            filters.SortOrder = queryParams["order"] ?? "desc";

            return filters;
        }

        private async Task<McpResourceReadResult> ReadCommandStatus(string uri)
        {
            try
            {
                // Extract sessionId and commandId from URI: commands://result?sessionId=xxx&commandId=yyy
                var uriParts = uri.Split('?');
                if (uriParts.Length < 2)
                {
                    throw new ArgumentException("Session ID and Command ID required. Use: commands://result?sessionId=<sessionId>&commandId=<commandId>");
                }

                var queryParams = System.Web.HttpUtility.ParseQueryString(uriParts[1]);
                var sessionId = queryParams["sessionId"];
                var commandId = queryParams["commandId"];

                if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(commandId))
                {
                    throw new ArgumentException("Both sessionId and commandId parameters are required. Use sessions://list to see available sessions and commands://list to see available commands.");
                }

                // Get session context to retrieve command status
                var sessionContext = sessionManager.GetSessionContext(sessionId);
                if (sessionContext == null)
                {
                    // Session doesn't exist - return ERROR
                    throw new ArgumentException($"Session {sessionId} not found. Use sessions://list to see available sessions.");
                }

                // Get the command queue for this session to check command status
                var commandQueue = sessionManager.GetCommandQueue(sessionId);

                // Try to get the command result
                try
                {
                    var commandResult = await commandQueue.GetCommandResult(commandId);

                    // Get command details from queue status
                    var queueStatus = commandQueue.GetQueueStatus();
                    var commandInfo = queueStatus.FirstOrDefault(cmd => cmd.Id == commandId);

                    // Parse the result to determine status
                    var isCompleted = !commandResult.Contains("still executing") &&
                                    !commandResult.Contains("Command not found");
                    var isNotFound = commandResult.Contains("Command not found");

                    var result = new
                    {
                        sessionId = sessionId,
                        commandId = commandId,
                        command = commandInfo.Id == commandId ? commandInfo.Command : null,
                        status = isNotFound ? "Not Found" : (isCompleted ? "Completed" : "In Progress"),
                        result = isCompleted ? commandResult : null,
                        error = isNotFound ? "Command not found. Use commands://list to see available commands." : null,
                        createdAt = commandInfo.Id == commandId ? commandInfo.QueueTime : (DateTime?)null,
                        completedAt = isCompleted ? DateTime.UtcNow : (DateTime?)null,
                        timestamp = DateTime.UtcNow,
                        message = isNotFound ? null : (isCompleted ? null : "Command is still executing - check again in a few seconds. You can also track command status using the 'List Sessions' or 'List Commands' resources."),
                        usage = SessionAwareWindbgTool.USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                    };

                    return new McpResourceReadResult
                    {
                        Contents = new[]
                        {
                            new McpResourceContent
                            {
                                MimeType = "application/json",
                                Text = JsonSerializer.Serialize(result, s_jsonOptions)
                            }
                        }
                    };
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
                        message = "Error accessing command queue",
                        usage = SessionAwareWindbgTool.USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                    };

                    return new McpResourceReadResult
                    {
                        Contents = new[]
                        {
                            new McpResourceContent
                            {
                                MimeType = "application/json",
                                Text = JsonSerializer.Serialize(errorResult, s_jsonOptions)
                            }
                        }
                    };
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
                usage_info = "Use: commands://result?sessionId=<sessionId>&commandId=<commandId>",
                example = "commands://result?sessionId=abc123&commandId=cmd456",
                note = "This resource requires both sessionId and commandId parameters to get command status. Use sessions://list to see available sessions and commands://list to see available commands.",
                usage = SessionAwareWindbgTool.USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
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
