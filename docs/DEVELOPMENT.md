# Development Guide

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [📋 Tools](TOOLS.md) | [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md)

## Architecture

The platform follows a modular architecture with notification support:

```
MCP Nexus
├── Core Services
│   ├── McpProtocolService         # MCP protocol handling
│   ├── McpToolDefinitionService   # Tool definitions
│   ├── McpToolExecutionService    # Tool execution
│   ├── McpNotificationService     # Server-initiated notifications
│   └── StdioNotificationBridge    # Stdio notification transport
├── Transport Layer
│   ├── Stdio Transport            # stdin/stdout communication + notifications
│   └── HTTP Transport             # HTTP API endpoints + SSE notifications
├── Notification System
│   ├── Command Status Tracking    # Real-time execution progress
│   ├── Heartbeat Monitoring       # Long-running command updates
│   ├── Session Recovery           # Debugging session management
│   └── Server Health              # Server status notifications
└── Tool Modules
    ├── Debugging Tools            # WinDBG/CDB integration
    ├── Time Tools                 # Time utilities
    └── [Future Tools]             # Extensible tool system
```

## Adding New Tools

1. **Define Tool Schema**: Add tool definition to `McpToolDefinitionService`
2. **Implement Logic**: Add execution logic to `McpToolExecutionService`  
3. **Add Notifications**: Use `IMcpNotificationService` for real-time updates
4. **Register Services**: Update dependency injection in `Program.cs`
5. **Update Documentation**: Add tool description to docs
6. **Write Tests**: Add comprehensive test coverage

### Example Tool Implementation

```csharp
// 1. Add to McpToolDefinitionService
new McpToolSchema
{
    Name = "my_new_tool",
    Description = "📡 REAL-TIME: Executes with live notifications",
    InputSchema = new
    {
        type = "object",
        properties = new
        {
            parameter = new { type = "string" }
        }
    }
}

// 2. Add to McpToolExecutionService
case "my_new_tool":
    await _notificationService.NotifyCommandStatusAsync(
        commandId, "my_new_tool", "executing", 0, "Starting...");
    
    // Do work...
    
    await _notificationService.NotifyCommandStatusAsync(
        commandId, "my_new_tool", "completed", 100, "Done!");
    
    return result;
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test --logger "console;verbosity=minimal" --nologo

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --logger "console;verbosity=minimal" --nologo

# Generate coverage report (requires reportgenerator tool)
reportgenerator -reports:"mcp_nexus_tests/TestResults/*/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html

# Install reportgenerator if not already installed
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run specific test categories
dotnet test --filter "FullyQualifiedName~Models" --logger "console;verbosity=minimal" --nologo
dotnet test --filter "FullyQualifiedName~Services" --logger "console;verbosity=minimal" --nologo
dotnet test --filter "FullyQualifiedName~Integration" --logger "console;verbosity=minimal" --nologo
dotnet test --filter "Notification" --logger "console;verbosity=minimal" --nologo
```

### Test Performance

The test suite is optimized for speed:
- **All tests**: ~4-5 seconds
- **381 tests**: All using proper mocking for fast execution
- **Coverage**: 46%+ line coverage with comprehensive notification testing
- **Notification Tests**: 7 dedicated test classes for notification functionality

### Notification Testing

Comprehensive test coverage includes:
- **Unit Tests**: Core notification service functionality
- **Integration Tests**: End-to-end notification flow
- **Transport Tests**: Both stdio and HTTP notification delivery
- **Bridge Tests**: Stdio notification bridge functionality
- **Mock Tests**: Proper mocking for fast test execution

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add your tool implementation with notifications
4. Write comprehensive tests (including notification tests)
5. Update documentation
6. Ensure all quality gates pass:
   - ✅ Build with zero warnings
   - ✅ All tests passing (381 tests)
   - ✅ No excluded/disabled tests
7. Submit a pull request

## Notification System Development

### Adding New Notification Types

1. **Define Model**: Add to `McpModels.cs`
```csharp
public class MyCustomNotification
{
    public string CustomField { get; set; }
    // Add XML documentation with JSON examples
}
```

2. **Add Service Method**: Add to `IMcpNotificationService`
```csharp
Task NotifyMyCustomAsync(string customField);
```

3. **Implement Service**: Add to `McpNotificationService`
```csharp
public async Task NotifyMyCustomAsync(string customField)
{
    await SendNotificationAsync("notifications/myCustom", new MyCustomNotification
    {
        CustomField = customField
    });
}
```

4. **Write Tests**: Add comprehensive test coverage
5. **Update Capabilities**: Add to `McpCapabilities.Notifications`

### Testing Notifications

```csharp
[Fact]
public async Task MyCustomNotification_SendsCorrectly()
{
    // Arrange
    var receivedNotifications = new List<McpNotification>();
    _notificationService.RegisterNotificationHandler(notification =>
    {
        receivedNotifications.Add(notification);
        return Task.CompletedTask;
    });

    // Act
    await _notificationService.NotifyMyCustomAsync("test-value");

    // Assert
    Assert.Single(receivedNotifications);
    Assert.Equal("notifications/myCustom", receivedNotifications[0].Method);
}
```

---

## Next Steps

- **📋 Tools:** [TOOLS.md](TOOLS.md) - Study tool implementation patterns
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Environment setup for development
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Test your tools with AI clients
