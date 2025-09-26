using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Session;
using mcp_nexus.CommandQueue;

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
                    Uri = "debugging://docs/windbg-commands",
                    Name = "WinDBG Command Reference",
                    Description = "Comprehensive reference of available WinDBG commands with examples and usage",
                    MimeType = "application/json"
                },
                new McpResource
                {
                    Uri = "debugging://docs/debugging-workflows",
                    Name = "Debugging Workflows",
                    Description = "Common debugging patterns and step-by-step analysis workflows",
                    MimeType = "application/json"
                },
                new McpResource
                {
                    Uri = "debugging://docs/troubleshooting",
                    Name = "Troubleshooting Guide",
                    Description = "Common issues and solutions for debugging Windows applications",
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
                "debugging://docs/troubleshooting" => GetTroubleshootingDocumentation(),
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
                title = "Debugging Workflows",
                description = "Common debugging patterns and step-by-step analysis workflows",
                workflows = new[]
                {
                    new
                    {
                        name = "Crash Analysis",
                        steps = new[]
                        {
                            "!analyze -v",
                            "!threads",
                            "~*k",
                            "!locks"
                        }
                    },
                    new
                    {
                        name = "Memory Leak Investigation",
                        steps = new[]
                        {
                            "!heap -stat",
                            "!heap -flt s <size>",
                            "!heap -p -a <address>",
                            "!gchandles"
                        }
                    }
                }
            };

            return JsonSerializer.Serialize(workflows, s_jsonOptions);
        }

        private static string GetTroubleshootingDocumentation()
        {
            var troubleshooting = new
            {
                title = "Troubleshooting Guide",
                description = "Common issues and solutions for debugging Windows applications",
                issues = new[]
                {
                    new
                    {
                        problem = "Access Violation",
                        solution = "Use !analyze -v to get automatic analysis, then examine the faulting thread with ~*k"
                    },
                    new
                    {
                        problem = "Deadlock",
                        solution = "Use !locks to examine lock information and !threads to see thread states"
                    }
                }
            };

            return JsonSerializer.Serialize(troubleshooting, s_jsonOptions);
        }
    }
}
