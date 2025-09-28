# Development Guide

**Windows Crash Dump Analysis Platform Development**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📋 Tools](TOOLS.md) | [📚 Resources](RESOURCES.md) | [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md)

## 🏗️ Architecture Overview

MCP Nexus is built with a modular architecture designed for Windows crash dump analysis and AI integration. The platform combines Microsoft's debugging tools with intelligent analysis workflows to provide comprehensive crash investigation capabilities.

### Core Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    AI Integration Layer                     │
├─────────────────────────────────────────────────────────────┤
│  MCP Protocol Handler  │  Real-time Notifications  │  AI   │
│                        │                           │ Client│
└─────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────┐
│                 Analysis Engine Core                        │
├─────────────────────────────────────────────────────────────┤
│  Crash Analysis  │  Memory Analysis  │  Thread Analysis    │
│  Performance     │  Pattern          │  Workflow           │
│  Analysis        │  Recognition      │  Engine             │
└─────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────┐
│              Microsoft Debugging Tools Integration         │
├─────────────────────────────────────────────────────────────┤
│  WinDBG/CDB Wrapper  │  Symbol Resolution  │  Command Queue │
│  Process Management  │  Error Handling     │  Result Cache  │
└─────────────────────────────────────────────────────────────┘
```

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
- **Pattern Recognition**: AI-powered identification of common crash patterns
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
- AI-powered pattern recognition
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
    Description = "🔍 MEMORY ANALYSIS: Analyzes heap corruption and memory leaks with AI-powered pattern recognition",
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
// Register new analysis service
builder.Services.AddScoped<IMemoryAnalysisService, MemoryAnalysisService>();

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
- **All tests**: ~4-5 seconds
- **527 tests**: All using proper mocking for fast execution
- **Coverage**: 46%+ line coverage with comprehensive analysis testing
- **Analysis Tests**: 7 dedicated test classes for analysis functionality

### Analysis Testing

Comprehensive test coverage includes:
- **Unit Tests**: Core analysis service functionality
- **Integration Tests**: End-to-end analysis workflows
- **Transport Tests**: Both stdio and HTTP analysis delivery
- **Bridge Tests**: Stdio notification bridge functionality
- **Mock Tests**: Proper mocking for fast test execution

## 🔄 Adding New Analysis Workflows

### 1. Define Workflow Schema

Add workflow definition to `McpWorkflowService`:

```csharp
new AnalysisWorkflow
{
    Id = "driver-crash-analysis",
    Name = "Driver Crash Analysis",
    Description = "Comprehensive analysis workflow for kernel driver crashes",
    Complexity = "Advanced",
    EstimatedTime = "30-60 minutes",
    Steps = new[]
    {
        new WorkflowStep
        {
            Step = 1,
            Command = "!analyze -v",
            Description = "Run comprehensive crash analysis",
            ExpectedOutcome = "Identify crash type and driver involved"
        },
        new WorkflowStep
        {
            Step = 2,
            Command = "!irql",
            Description = "Check IRQL level and context",
            ExpectedOutcome = "Understand system state at crash"
        },
        new WorkflowStep
        {
            Step = 3,
            Command = "lm",
            Description = "List loaded modules and drivers",
            ExpectedOutcome = "Identify problematic driver"
        }
    },
    CommonIssues = new[]
    {
        "Driver memory corruption",
        "IRQL not less than or equal",
        "Driver timeout",
        "System instability"
    }
}
```

### 2. Implement Workflow Logic

Add workflow execution to `AnalysisWorkflowService`:

```csharp
public async Task<AnalysisResult> ExecuteDriverCrashAnalysisAsync(
    string sessionId, string workflowId, CancellationToken cancellationToken)
{
    var workflow = GetWorkflow(workflowId);
    var results = new List<CommandResult>();
    
    foreach (var step in workflow.Steps)
    {
        await _notificationService.NotifyWorkflowProgressAsync(
            sessionId, workflowId, step.Step, workflow.Steps.Length, 
            $"Executing step {step.Step}: {step.Description}");
        
        var result = await ExecuteWorkflowStep(sessionId, step, cancellationToken);
        results.Add(result);
        
        if (!result.Success)
        {
            break; // Stop on first failure
        }
    }
    
    return new AnalysisResult
    {
        WorkflowId = workflowId,
        SessionId = sessionId,
        Steps = results,
        Success = results.All(r => r.Success),
        CompletedAt = DateTime.UtcNow
    };
}
```

### 3. Add Workflow Notifications

```csharp
// Workflow start
await _notificationService.NotifyWorkflowStartedAsync(
    sessionId, workflowId, "Driver Crash Analysis");

// Step progress
await _notificationService.NotifyWorkflowProgressAsync(
    sessionId, workflowId, currentStep, totalSteps, 
    $"Executing step {currentStep}: {stepDescription}");

// Workflow completion
await _notificationService.NotifyWorkflowCompletedAsync(
    sessionId, workflowId, success, results);
```

## 🔧 Adding New Analysis Patterns

### 1. Define Pattern Schema

Add pattern definition to `AnalysisPatternService`:

```csharp
new AnalysisPattern
{
    Id = "buffer-overflow",
    Name = "Buffer Overflow",
    ExceptionCode = "0xC0000005",
    Description = "Buffer overflow causing access violation",
    CommonCauses = new[]
    {
        "Insufficient bounds checking",
        "Unsafe string operations",
        "Array index out of bounds",
        "Stack overflow"
    },
    AnalysisCommands = new[]
    {
        "!analyze -v",
        "kb",
        "!address",
        "!heap -p -a"
    },
    Severity = "High",
    Frequency = "Very Common",
    PreventionStrategies = new[]
    {
        "Use bounds checking",
        "Implement stack canaries",
        "Use safe string functions",
        "Enable compiler security features"
    }
}
```

### 2. Implement Pattern Recognition

Add pattern recognition logic to `PatternRecognitionService`:

```csharp
public async Task<PatternMatch> RecognizePatternAsync(
    string sessionId, string analysisResult)
{
    var patterns = await GetAnalysisPatternsAsync();
    var matches = new List<PatternMatch>();
    
    foreach (var pattern in patterns)
    {
        var confidence = CalculatePatternConfidence(analysisResult, pattern);
        if (confidence > 0.7) // 70% confidence threshold
        {
            matches.Add(new PatternMatch
            {
                Pattern = pattern,
                Confidence = confidence,
                MatchedFeatures = ExtractMatchedFeatures(analysisResult, pattern)
            });
        }
    }
    
    return matches.OrderByDescending(m => m.Confidence).FirstOrDefault();
}
```

## 📊 Performance Optimization

### Memory Management

**Session Management**:
- Implement session pooling
- Use weak references for large objects
- Implement automatic cleanup
- Monitor memory usage

**Command Execution**:
- Use async/await patterns
- Implement command queuing
- Use cancellation tokens
- Cache frequently used results

### Analysis Optimization

**Parallel Processing**:
- Execute independent commands in parallel
- Use Task.Run for CPU-intensive operations
- Implement work stealing for load balancing
- Use concurrent collections

**Caching Strategy**:
- Cache symbol information
- Cache analysis results
- Implement LRU cache eviction
- Use memory-mapped files for large data

## 🔒 Security Considerations

### File Access Security

**Dump File Access**:
- Validate file paths
- Check file permissions
- Sanitize user input
- Implement access controls

**Symbol Server Security**:
- Use HTTPS for symbol downloads
- Validate symbol signatures
- Implement rate limiting
- Cache symbols securely

### Process Security

**Debugging Process Security**:
- Run with minimal privileges
- Isolate debugging processes
- Implement process sandboxing
- Monitor resource usage

**Network Security**:
- Use secure connections
- Implement authentication
- Validate all inputs
- Log security events

## 🚀 Deployment

### Development Environment

```bash
# Clone repository
git clone https://github.com/your-username/mcp_nexus.git
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

## 🔮 Future Enhancements

### Planned Features

**AI Integration**:
- Machine learning for pattern recognition
- Automated analysis suggestions
- Intelligent workflow selection
- Natural language analysis queries

**Analysis Capabilities**:
- Advanced memory analysis
- Performance profiling
- Security vulnerability detection
- Automated report generation

**Platform Improvements**:
- Cloud-based analysis
- Team collaboration features
- Advanced visualization
- Integration with CI/CD pipelines

### Contributing

1. Fork the repository
2. Create a feature branch
3. Add your analysis tool implementation
4. Write comprehensive tests
5. Update documentation
6. Ensure all quality gates pass:
   - ✅ Build with zero warnings
   - ✅ All tests passing (527 tests)
   - ✅ No excluded/disabled tests
7. Submit a pull request

---

## Next Steps

- **🔍 Overview:** [OVERVIEW.md](OVERVIEW.md) - Understand the analysis capabilities
- **📋 Tools:** [TOOLS.md](TOOLS.md) - Learn about available analysis tools
- **📚 Resources:** [RESOURCES.md](RESOURCES.md) - MCP Resources reference
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Configure your environment
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Set up AI integration