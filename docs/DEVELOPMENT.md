# Development Guide

**Windows Crash Dump Analysis Platform Development**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📋 Tools](TOOLS.md) | [📚 Resources](RESOURCES.md) | [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md)

## 🏗️ Architecture Overview

MCP Nexus is built with a modular architecture designed for Windows crash dump analysis and AI integration. The platform combines Microsoft's debugging tools with intelligent analysis workflows to provide comprehensive crash investigation capabilities.

### Core Architecture

#### AI Integration Layer
| Component | Description |
|-----------|-------------|
| **MCP Protocol Handler** | Handles Model Context Protocol communication |
| **Real-time Notifications** | Live progress updates during analysis |
| **AI Client** | Interface for AI-powered analysis requests |

#### Analysis Engine Core
| Component | Description |
|-----------|-------------|
| **Crash Analysis** | Core crash dump processing and analysis |
| **Memory Analysis** | Memory corruption and leak detection |
| **Thread Analysis** | Thread state and deadlock analysis |
| **Performance Analysis** | Performance bottleneck identification |
| **Pattern Recognition** | Common crash pattern detection |
| **Workflow Engine** | Orchestrates analysis workflows |

#### Microsoft Debugging Tools Integration
| Component | Description |
|-----------|-------------|
| **WinDBG/CDB Wrapper** | Interface to Microsoft debugging tools |
| **Symbol Resolution** | Symbol loading and resolution |
| **Command Queue** | Asynchronous command execution |
| **Process Management** | Process attachment and control |
| **Error Handling** | Robust error recovery and logging |
| **Result Cache** | Cached analysis results for performance |

### Component Details

#### AI Integration Layer
- **MCP Protocol Handler**: Manages Model Context Protocol communication
- **Real-time Notifications**: Provides live updates during analysis
- **AI Client Interface**: Seamless integration with AI systems

#### Analysis Engine Core
- **Crash Analysis**: Automated crash investigation and root cause identification
- **Memory Analysis**: Heap corruption, memory leaks, and allocation analysis
- **Thread Analysis**: Deadlock detection and synchronization issue analysis
- **Performance Analysis**: CPU usage, resource exhaustion, and bottleneck identification
- **Pattern Recognition**: Identification of common crash patterns
- **Workflow Engine**: Orchestrates complex analysis workflows

#### Microsoft Debugging Tools Integration
- **WinDBG/CDB Wrapper**: Safe execution of debugging commands
- **Symbol Resolution**: Automatic symbol loading and caching
- **Command Queue**: Asynchronous command execution with progress tracking
- **Process Management**: Lifecycle management of debugging processes
- **Error Handling**: Robust error handling and recovery
- **Result Cache**: Intelligent caching of analysis results

## 🔧 Core Services

### Session Management

**ThreadSafeSessionManager**:
- Manages crash dump analysis sessions
- Thread-safe session operations
- Automatic session cleanup
- Resource management and disposal

**Key Features**:
- Concurrent session support
- Session timeout handling
- Memory cleanup and disposal
- Error recovery and resilience

### Command Execution

**CommandQueueService**:
- Asynchronous command execution
- Progress tracking and notifications
- Command queuing and prioritization
- Timeout handling and cancellation

**Key Features**:
- Non-blocking command execution
- Real-time progress updates
- Command cancellation support
- Error handling and recovery

### Analysis Engine

**CrashAnalysisService**:
- Automated crash analysis workflows
- Pattern recognition and classification
- Root cause identification
- Analysis report generation

**Key Features**:
- Multi-step analysis workflows
- Pattern recognition
- Structured result generation
- Comprehensive reporting

### Notification System

**McpNotificationService**:
- Real-time progress notifications
- Command status updates
- Session recovery events
- Server health monitoring

**Key Features**:
- Server-initiated notifications
- Progress tracking (0-100%)
- Heartbeat monitoring
- Error notifications

## 🛠️ Adding New Analysis Tools

### 1. Define Tool Schema

Add tool definition to `McpToolDefinitionService`:

```csharp
new McpToolSchema
{
    Name = "nexus_analyze_memory_corruption",
    Description = "🔍 MEMORY ANALYSIS: Analyzes heap corruption and memory leaks with pattern recognition",
    InputSchema = new
    {
        type = "object",
        properties = new
        {
            sessionId = new { type = "string", description = "Active session ID" },
            analysisType = new { 
                type = "string", 
                enum = new[] { "heap", "leak", "corruption", "all" },
                description = "Type of memory analysis to perform"
            },
            timeoutMinutes = new { 
                type = "number", 
                description = "Analysis timeout in minutes (default: 15)" 
            }
        },
        required = new[] { "sessionId" }
    }
}
```

### 2. Implement Analysis Logic

Add execution logic to `McpToolExecutionService`:

```csharp
case "nexus_analyze_memory_corruption":
    await _notificationService.NotifyCommandStatusAsync(
        commandId, "nexus_analyze_memory_corruption", "executing", 0, "Starting memory analysis...");
    
    var sessionId = arguments["sessionId"]?.ToString();
    var analysisType = arguments["analysisType"]?.ToString() ?? "all";
    var timeoutMinutes = int.Parse(arguments["timeoutMinutes"]?.ToString() ?? "15");
    
    // Perform memory analysis
    var analysisResult = await PerformMemoryAnalysis(sessionId, analysisType, timeoutMinutes);
    
    await _notificationService.NotifyCommandStatusAsync(
        commandId, "nexus_analyze_memory_corruption", "completed", 100, "Memory analysis completed");
    
    return new
    {
        sessionId = sessionId,
        analysisType = analysisType,
        result = analysisResult,
        success = true,
        message = "Memory analysis completed successfully"
    };
```

### 3. Add Notifications

Use `IMcpNotificationService` for real-time updates:

```csharp
// Progress updates
await _notificationService.NotifyCommandStatusAsync(
    commandId, toolName, "executing", 25, "Analyzing heap structure...");

await _notificationService.NotifyCommandStatusAsync(
    commandId, toolName, "executing", 50, "Checking for memory leaks...");

await _notificationService.NotifyCommandStatusAsync(
    commandId, toolName, "executing", 75, "Identifying corruption patterns...");

// Completion
await _notificationService.NotifyCommandStatusAsync(
    commandId, toolName, "completed", 100, "Memory analysis completed");
```

### 4. Register Services

Update dependency injection in `Program.cs`:

```csharp
// Register tool execution
builder.Services.AddScoped<IMcpToolExecutionService, McpToolExecutionService>();
```

### 5. Write Tests

Add comprehensive test coverage:

```csharp
[Fact]
public async Task MemoryAnalysis_WithValidSession_ReturnsAnalysisResult()
{
    // Arrange
    var sessionId = "test-session-123";
    var analysisType = "heap";
    var arguments = new Dictionary<string, object>
    {
        ["sessionId"] = sessionId,
        ["analysisType"] = analysisType
    };

    // Act
    var result = await _toolExecutionService.ExecuteToolAsync(
        "nexus_analyze_memory_corruption", arguments, "cmd-123");

    // Assert
    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.Contains("heap analysis", result.Message.ToLower());
}

[Fact]
public async Task MemoryAnalysis_WithInvalidSession_ReturnsError()
{
    // Arrange
    var sessionId = "invalid-session";
    var arguments = new Dictionary<string, object>
    {
        ["sessionId"] = sessionId
    };

    // Act & Assert
    await Assert.ThrowsAsync<McpToolException>(() =>
        _toolExecutionService.ExecuteToolAsync(
            "nexus_analyze_memory_corruption", arguments, "cmd-123"));
}
```

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test --logger "console;verbosity=minimal" --nologo

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --logger "console;verbosity=minimal" --nologo

# Generate coverage report
reportgenerator -reports:"mcp_nexus_tests/TestResults/*/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html

# Run specific test categories
dotnet test --filter "FullyQualifiedName~CrashAnalysis" --logger "console;verbosity=minimal" --nologo
dotnet test --filter "FullyQualifiedName~MemoryAnalysis" --logger "console;verbosity=minimal" --nologo
dotnet test --filter "FullyQualifiedName~ThreadAnalysis" --logger "console;verbosity=minimal" --nologo
dotnet test --filter "Notification" --logger "console;verbosity=minimal" --nologo
```

### Test Performance

The test suite is optimized for speed:
- **All tests**: ~56 seconds
- **1,189 tests**: All using proper mocking for fast execution
- **Coverage**: 46%+ line coverage with comprehensive analysis testing
- **Analysis Tests**: 15 test categories covering all major functionality

### Analysis Testing

Comprehensive test coverage includes:
- **Unit Tests**: Core analysis service functionality
- **Integration Tests**: End-to-end analysis workflows
- **Transport Tests**: Both stdio and HTTP analysis delivery
- **Bridge Tests**: Stdio notification bridge functionality
- **Mock Tests**: Proper mocking for fast test execution


## 📊 Performance Features

### Caching System

**IntelligentCacheService**:
- **Memory-based caching** - 100MB default limit with configurable size
- **TTL support** - 30-minute default time-to-live for cache entries
- **Automatic cleanup** - Expired entries cleaned every 5 minutes
- **Memory monitoring** - Tracks cache memory usage and triggers eviction
- **Generic implementation** - Supports any key/value type

### Metrics Collection

**AdvancedMetricsService**:
- **Performance counters** - Tracks command executions, session operations
- **Success/failure tracking** - Monitors operation success rates
- **Histogram support** - Tracks operation timing and distributions
- **Automatic collection** - Metrics gathered every 30 seconds
- **Resource monitoring** - Memory and performance statistics

### Symbol Caching

**Symbol Server Configuration**:
- **Local symbol cache** - Cached symbols for faster loading
- **Multiple symbol servers** - Microsoft, NuGet, Avast symbol sources
- **Hot/Cold cache strategy** - Fast local access with server fallback
- **Configurable timeouts** - 5-minute command, 5-minute symbol server timeouts

## 🔒 Security Features

### Command Validation

**Implemented Security**:
- **Dangerous command blocking** - Commands like `format`, `del`, `shutdown` are blocked
- **Path traversal protection** - Prevents `../` directory traversal attacks
- **SQL injection protection** - Basic SQL injection pattern detection
- **Input validation** - Empty and whitespace-only commands are rejected
- **Command length limits** - Commands limited to 1000 characters maximum

**Security Service**:
- `AdvancedSecurityService.ValidateCommand()` method
- `SecurityValidationResult` class for validation results
- Regex patterns for path traversal and SQL injection detection

## 🚀 Deployment

### Development Environment

```bash
# Clone repository
git clone https://github.com/CapulusCodeNinja/mcp_nexus.git
cd mcp_nexus

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Start development server
dotnet run --project mcp_nexus/mcp_nexus.csproj
```

### Production Deployment

```bash
# Build for production
dotnet publish -c Release -o ./publish

# Install as Windows service
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --install

# Start service
sc start "MCP-Nexus"
```

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["mcp_nexus/mcp_nexus.csproj", "mcp_nexus/"]
RUN dotnet restore "mcp_nexus/mcp_nexus.csproj"
COPY . .
WORKDIR "/src/mcp_nexus"
RUN dotnet build "mcp_nexus.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mcp_nexus.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mcp_nexus.dll"]
```

---

## Next Steps

- **🔍 Overview:** [OVERVIEW.md](OVERVIEW.md) - Understand the analysis capabilities
- **📋 Tools:** [TOOLS.md](TOOLS.md) - Learn about available analysis tools
- **📚 Resources:** [RESOURCES.md](RESOURCES.md) - MCP Resources reference
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Configure your environment
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Set up AI integration