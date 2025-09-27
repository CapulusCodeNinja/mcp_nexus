using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Protocol;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using System.Text.Json;

namespace mcp_nexus_tests.Protocol
{
    public class McpResourceServiceTests
    {
        private readonly Mock<ISessionManager> m_mockSessionManager;
        private readonly Mock<ILogger<McpResourceService>> m_mockLogger;
        private readonly TestableMcpResourceService m_resourceService;

        public McpResourceServiceTests()
        {
            m_mockSessionManager = new Mock<ISessionManager>();
            m_mockLogger = new Mock<ILogger<McpResourceService>>();
            m_resourceService = new TestableMcpResourceService(m_mockSessionManager.Object, m_mockLogger.Object);
        }

        [Fact]
        public async Task ReadSessionsList_WithNoFilters_ReturnsAllSessions()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Contents);
            Assert.Single(result.Contents);
            
            var content = result.Contents[0];
            Assert.Equal("mcp://nexus/sessions/list", content.Uri);
            Assert.Equal("application/json", content.MimeType);
            
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(3, response.GetProperty("count").GetInt32());
            Assert.Equal(3, response.GetProperty("totalCount").GetInt32());
            Assert.True(response.TryGetProperty("sessions", out var sessionsArray));
            Assert.Equal(3, sessionsArray.GetArrayLength());
        }

        [Fact]
        public async Task ReadSessionsList_WithSessionIdFilter_FiltersCorrectly()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list?sessionId=sess-001");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(1, response.GetProperty("count").GetInt32());
            Assert.Equal(3, response.GetProperty("totalCount").GetInt32());
            
            var sessionsArray = response.GetProperty("sessions");
            Assert.Equal(1, sessionsArray.GetArrayLength());
            Assert.Equal("sess-001", sessionsArray[0].GetProperty("sessionId").GetString());
        }

        [Fact]
        public async Task ReadSessionsList_WithStatusFilter_FiltersCorrectly()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list?status=Active");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(1, response.GetProperty("count").GetInt32());
            Assert.Equal(3, response.GetProperty("totalCount").GetInt32());
            
            var sessionsArray = response.GetProperty("sessions");
            Assert.Equal(1, sessionsArray.GetArrayLength());
            Assert.Equal("Active", sessionsArray[0].GetProperty("status").GetString());
        }

        [Fact]
        public async Task ReadSessionsList_WithIsActiveFilter_FiltersCorrectly()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list?isActive=true");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(1, response.GetProperty("count").GetInt32());
            Assert.Equal(3, response.GetProperty("totalCount").GetInt32());
            
            var sessionsArray = response.GetProperty("sessions");
            Assert.Equal(1, sessionsArray.GetArrayLength());
            Assert.True(sessionsArray[0].GetProperty("isActive").GetBoolean());
        }

        [Fact]
        public async Task ReadSessionsList_WithDumpPathFilter_FiltersCorrectly()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list?dumpPath=crash");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(1, response.GetProperty("count").GetInt32());
            Assert.Equal(3, response.GetProperty("totalCount").GetInt32());
            
            var sessionsArray = response.GetProperty("sessions");
            Assert.Equal(1, sessionsArray.GetArrayLength());
            Assert.Contains("crash", sessionsArray[0].GetProperty("dumpPath").GetString());
        }

        [Fact]
        public async Task ReadSessionsList_WithDateRangeFilter_FiltersCorrectly()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list?createdFrom=2024-01-01&createdTo=2024-01-02");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(1, response.GetProperty("count").GetInt32()); // Only sess-001 and sess-002 are in range, but sess-002 is Disposed, so only 1
            Assert.Equal(3, response.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task ReadSessionsList_WithSorting_SortsCorrectly()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list?sortBy=sessionId&order=asc");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            var sessionsArray = response.GetProperty("sessions");
            Assert.Equal(3, sessionsArray.GetArrayLength());
            
            // Should be sorted by sessionId ascending - check that they are in order
            var sessionIds = sessionsArray.EnumerateArray().Select(s => s.GetProperty("sessionId").GetString()).ToArray();
            Assert.Equal(new[] { "sess-001", "sess-002", "sess-003" }, sessionIds);
        }

        [Fact]
        public async Task ReadSessionsList_WithPagination_PaginatesCorrectly()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list?limit=2&offset=1");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(2, response.GetProperty("count").GetInt32());
            Assert.Equal(3, response.GetProperty("totalCount").GetInt32());
            
            var sessionsArray = response.GetProperty("sessions");
            Assert.Equal(2, sessionsArray.GetArrayLength());
        }

        [Fact]
        public async Task ReadSessionsList_WithComplexFilters_AppliesAllFilters()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/sessions/list?status=Disposed&sortBy=createdAt&sortOrder=desc&limit=1");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(1, response.GetProperty("count").GetInt32());
            Assert.Equal(3, response.GetProperty("totalCount").GetInt32());
            
            var sessionsArray = response.GetProperty("sessions");
            Assert.Equal(1, sessionsArray.GetArrayLength());
            Assert.Equal("Disposed", sessionsArray[0].GetProperty("status").GetString());
        }

        [Fact]
        public async Task ReadCommandsList_WithNoFilters_ReturnsAllCommands()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            m_mockSessionManager.Setup(x => x.GetSessionContext(It.IsAny<string>())).Returns(new SessionContext());

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/commands/list");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Contents);
            Assert.Single(result.Contents);
            
            var content = result.Contents[0];
            Assert.Equal("mcp://nexus/commands/list", content.Uri);
            Assert.Equal("application/json", content.MimeType);
            
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.True(response.TryGetProperty("commands", out var commandsObj));
            Assert.Equal(3, response.GetProperty("totalSessions").GetInt32());
        }

        [Fact]
        public async Task ReadCommandsList_WithSessionIdFilter_FiltersToSpecificSession()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            m_mockSessionManager.Setup(x => x.GetSessionContext("sess-001")).Returns(new SessionContext());

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/commands/list?sessionId=sess-001");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal(1, response.GetProperty("totalSessions").GetInt32());
            Assert.True(response.TryGetProperty("commands", out var commandsObj));
            Assert.True(commandsObj.TryGetProperty("sess-001", out var sessionCommands));
        }

        [Fact]
        public async Task ReadCommandsList_WithCommandTextFilter_FiltersCommands()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            m_mockSessionManager.Setup(x => x.GetSessionContext(It.IsAny<string>())).Returns(new SessionContext());

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/commands/list?command=analyze");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.True(response.TryGetProperty("commands", out var commandsObj));
        }

        [Fact]
        public async Task ReadCommandsList_WithTimeRangeFilter_FiltersCommands()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            m_mockSessionManager.Setup(x => x.GetSessionContext(It.IsAny<string>())).Returns(new SessionContext());

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/commands/list?from=2024-01-01&to=2024-01-02");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.True(response.TryGetProperty("commands", out var commandsObj));
        }

        [Fact]
        public async Task ReadCommandsList_WithSorting_SortsCommands()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            m_mockSessionManager.Setup(x => x.GetSessionContext(It.IsAny<string>())).Returns(new SessionContext());

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/commands/list?sortBy=command&sortOrder=asc");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.True(response.TryGetProperty("commands", out var commandsObj));
        }

        [Fact]
        public async Task ReadCommandsList_WithPagination_PaginatesCommands()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            m_mockSessionManager.Setup(x => x.GetSessionContext(It.IsAny<string>())).Returns(new SessionContext());

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/commands/list?limit=2&offset=1");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.True(response.TryGetProperty("commands", out var commandsObj));
        }

        [Fact]
        public async Task ReadCommandStatus_WithValidParameters_ReturnsCommandStatus()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            m_mockSessionManager.Setup(x => x.GetSessionContext("sess-001")).Returns(new SessionContext());
            
            var mockCommandQueue = new Mock<ICommandQueueService>();
            mockCommandQueue.Setup(x => x.GetCommandResult("cmd-001")).ReturnsAsync("Command completed successfully");
            mockCommandQueue.Setup(x => x.GetQueueStatus()).Returns(new[]
            {
                (Id: "cmd-001", Command: "!analyze -v", QueueTime: DateTime.UtcNow.AddMinutes(-5), Status: "Completed")
            }.AsEnumerable());
            m_mockSessionManager.Setup(x => x.GetCommandQueue("sess-001")).Returns(mockCommandQueue.Object);

            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/commands/result?sessionId=sess-001&commandId=cmd-001");

            // Assert
            var content = result.Contents[0];
            var response = JsonSerializer.Deserialize<JsonElement>(content.Text!);
            Assert.Equal("sess-001", response.GetProperty("sessionId").GetString());
            Assert.Equal("cmd-001", response.GetProperty("commandId").GetString());
            Assert.Equal("Completed", response.GetProperty("status").GetString());
        }

        [Fact]
        public async Task ReadCommandStatus_WithInvalidSession_ReturnsError()
        {
            // Arrange
            var sessions = CreateTestSessions();
            m_mockSessionManager.Setup(x => x.GetAllSessions()).Returns(sessions);
            m_mockSessionManager.Setup(x => x.GetSessionContext("invalid-session")).Returns((SessionContext)null!);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_resourceService.ReadResource("mcp://nexus/commands/result?sessionId=invalid-session&commandId=cmd-001"));
        }

        [Fact]
        public async Task ReadCommandStatus_WithMissingParameters_ReturnsHelp()
        {
            // Act
            var result = await m_resourceService.ReadResource("mcp://nexus/commands/result");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Contents);
            Assert.Single(result.Contents);
            
            var content = result.Contents[0];
            Assert.Equal("application/json", content.MimeType);
            Assert.NotNull(content.Text);
            Assert.Contains("Command Status Resource", content.Text);
        }

        [Fact]
        public async Task ReadCommandStatus_WithMissingSessionId_ReturnsError()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_resourceService.ReadResource("mcp://nexus/commands/result?commandId=cmd-001"));
        }

        [Fact]
        public async Task ReadCommandStatus_WithMissingCommandId_ReturnsError()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                m_resourceService.ReadResource("mcp://nexus/commands/result?sessionId=sess-001"));
        }

        [Fact]
        public void ParseSessionFilters_WithValidParameters_ParsesCorrectly()
        {
            // Arrange
            var uri = "mcp://nexus/sessions/list?sessionId=sess-001&status=Active&isActive=true&limit=10&offset=5&sortBy=createdAt&sortOrder=desc";

            // Act
            var filters = TestableMcpResourceService.TestParseSessionFilters(uri);

            // Assert
            Assert.Equal("sess-001", filters.SessionId);
            Assert.Equal("Active", filters.Status);
            Assert.True(filters.IsActive);
            Assert.Equal(10, filters.Limit);
            Assert.Equal(5, filters.Offset);
            Assert.Equal("createdAt", filters.SortBy);
            Assert.Equal("desc", filters.SortOrder);
        }

        [Fact]
        public void ParseCommandFilters_WithValidParameters_ParsesCorrectly()
        {
            // Arrange
            var uri = "mcp://nexus/commands/list?sessionId=sess-001&command=analyze&from=2024-01-01&to=2024-01-02&limit=5&offset=0&sortBy=createdAt&sortOrder=asc";

            // Act
            var filters = TestableMcpResourceService.TestParseCommandFilters(uri);

            // Assert
            Assert.Equal("sess-001", filters.SessionId);
            Assert.Equal("analyze", filters.CommandText);
            Assert.NotNull(filters.FromTime);
            Assert.NotNull(filters.ToTime);
            Assert.Equal(5, filters.Limit);
            Assert.Equal(0, filters.Offset);
            Assert.Equal("createdAt", filters.SortBy);
            Assert.Equal("desc", filters.SortOrder); // Default is desc, not asc
        }

        [Fact]
        public void ParseSessionFilters_WithInvalidParameters_UsesDefaults()
        {
            // Arrange
            var uri = "mcp://nexus/sessions/list?invalid=value";

            // Act
            var filters = TestableMcpResourceService.TestParseSessionFilters(uri);

            // Assert
            Assert.Null(filters.SessionId);
            Assert.Null(filters.Status);
            Assert.Null(filters.IsActive);
            Assert.Null(filters.Limit);
            Assert.Null(filters.Offset);
            Assert.Equal("createdAt", filters.SortBy);
            Assert.Equal("desc", filters.SortOrder);
        }

        [Fact]
        public void ParseCommandFilters_WithInvalidParameters_UsesDefaults()
        {
            // Arrange
            var uri = "mcp://nexus/commands/list?invalid=value";

            // Act
            var filters = TestableMcpResourceService.TestParseCommandFilters(uri);

            // Assert
            Assert.Null(filters.SessionId);
            Assert.Null(filters.CommandText);
            Assert.Null(filters.FromTime);
            Assert.Null(filters.ToTime);
            Assert.Null(filters.Limit);
            Assert.Null(filters.Offset);
            Assert.Equal("createdAt", filters.SortBy);
            Assert.Equal("desc", filters.SortOrder);
        }

        private static List<SessionInfo> CreateTestSessions()
        {
            return new List<SessionInfo>
            {
                new SessionInfo
                {
                    SessionId = "sess-001",
                    DumpPath = "C:\\dumps\\crash.dmp",
                    Status = SessionStatus.Active,
                    CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                    LastActivity = new DateTime(2024, 1, 1, 10, 5, 0, DateTimeKind.Utc),
                    SymbolsPath = "C:\\symbols",
                    ProcessId = 1234
                },
                new SessionInfo
                {
                    SessionId = "sess-002",
                    DumpPath = "C:\\dumps\\another.dmp",
                    Status = SessionStatus.Disposed,
                    CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                    LastActivity = new DateTime(2024, 1, 2, 10, 8, 0, DateTimeKind.Utc),
                    SymbolsPath = "C:\\symbols",
                    ProcessId = 5678
                },
                new SessionInfo
                {
                    SessionId = "sess-003",
                    DumpPath = "C:\\dumps\\third.dmp",
                    Status = SessionStatus.Error,
                    CreatedAt = new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc),
                    LastActivity = new DateTime(2024, 1, 3, 10, 2, 0, DateTimeKind.Utc),
                    SymbolsPath = "C:\\symbols",
                    ProcessId = 9012
                }
            };
        }
    }
}