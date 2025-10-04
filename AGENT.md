# MCP Nexus - AI Agent Instructions

## Project Overview

MCP Nexus is a sophisticated Model Context Protocol (MCP) server designed for Windows crash dump analysis and debugging. It provides a comprehensive suite of tools for analyzing memory dumps using WinDBG/CDB debugger integration, with advanced features including session management, command queuing, circuit breakers, health monitoring, and intelligent caching.

## Architecture

### Core Components
- **Session Management**: Multi-session debugging with isolated CDB processes
- **Command Queue System**: Asynchronous command execution with resilience patterns
- **Circuit Breaker Pattern**: Fault tolerance and failure recovery
- **Health Monitoring**: Comprehensive system health checks and metrics
- **Intelligent Caching**: Memory-efficient caching with eviction strategies
- **Notification System**: Real-time status updates and progress tracking

### Key Design Patterns
- **Dependency Injection**: Extensive use of DI container for loose coupling
- **Circuit Breaker**: Resilience against failures in external dependencies
- **Command Pattern**: Encapsulated command execution with queuing
- **Observer Pattern**: Event-driven notifications and monitoring
- **Factory Pattern**: Dynamic service creation and configuration

### Technology Stack
- **.NET 8**: Primary framework
- **ASP.NET Core**: HTTP server and middleware
- **NLog**: Advanced logging framework
- **System.CommandLine**: CLI argument parsing
- **ModelContextProtocol**: MCP server implementation
- **WinDBG/CDB**: Windows debugging tools integration

## 🚨 CRITICAL: MANDATORY GROUND RULES - READ FIRST! 🚨

**⚠️ THESE RULES ARE NON-NEGOTIABLE AND MUST BE FOLLOWED FOR EVERY CODE CHANGE ⚠️**

**❌ VIOLATIONS WILL RESULT IN IMMEDIATE REJECTION ❌**

### 🔥 IMMEDIATE REQUIREMENTS (Check These FIRST!)

1. **✅ ALL TESTS MUST PASS**: Run `dotnet test` - ALL tests must be green before any submission
2. **✅ VERSION MUST BE UPDATED**: Increment the build version in `mcp_nexus.csproj` (e.g., 1.0.6.12 → 1.0.6.13)
3. **✅ README.md MUST BE UPDATED**: Update test count and coverage in README.md badges
4. **✅ NO COMPILATION ERRORS**: Code must build with zero warnings and zero errors
5. **✅ NO DEAD CODE**: Remove unused code, methods, or files

### 📋 DETAILED GROUND RULES

#### Code Structure and Quality
* **Atomicity and Clarity:** Methods must be **atomic, focused, and build successfully with zero warnings** in all configurations.
* **Code Cleanliness:** There must be **no unused code**, **dead code**, or **unused import/using directives** within any file.
* **File-Class Parity:** Each **top-level class or interface must reside in its own dedicated file**, with the filename matching the class/interface name.
* **Standardized Formatting:** All code must be **perfectly formatted and aligned** according to the project's established standards (enforced by `dotnet format` or equivalent tooling).
* **Concurrency and Performance Integrity:** As this is a **server application**, code must adhere to **proper concurrency practices**. Introduction of **blocking operations, excessive/long `Thread.Sleep` calls, or potential deadlocks/livelocks is strictly prohibited**. The code must be performant and thread-safe.

#### Testing and Documentation
* **100% Test Pass Rate:** **All unit tests must pass** (be "green") before submission.
* **Testing Integrity:** **No existing or new tests may be excluded, ignored, or removed** without prior architectural review and approval.
* **Documentation Synchronization:** All relevant **documentation** (e.g., Markdown documentation, README, or designated build manifest files) must be **up-to-date and reflect the current state** of the codebase and tests.

#### Versioning and Scope
* **Version Increment:** Only the **build version component** (the last digit/identifier) in the project's versioning scheme must be incremented for this change. (Assuming a **Major.Minor.Patch.Build** semantic versioning standard.)

### 🎯 COMMON VIOLATIONS TO AVOID

**❌ DON'T FORGET:**
- Running tests before submission
- Updating version numbers
- Updating README.md with new test counts
- Removing dead/unused code
- Checking for compilation errors

**✅ ALWAYS DO:**
- Run `dotnet test` and verify all tests pass
- Update version in `mcp_nexus.csproj`
- Update test count in README.md
- Remove any unused code or files
- Verify zero compilation warnings/errors

---

## Coding Standards

### XML Documentation Requirements
**CRITICAL**: Every public and private method, property, constructor, enum, and class MUST have complete XML documentation:

```csharp
/// <summary>
/// Brief description of what the method does.
/// </summary>
/// <param name="paramName">Description of the parameter.</param>
/// <returns>Description of the return value.</returns>
/// <exception cref="ExceptionType">When this exception is thrown.</exception>
public ReturnType MethodName(ParameterType paramName)
```

### Naming Conventions
- **Classes**: PascalCase (e.g., `SessionManager`, `CommandQueueService`)
- **Methods**: PascalCase (e.g., `CreateSessionAsync`, `QueueCommand`)
- **Properties**: PascalCase (e.g., `SessionId`, `IsHealthy`)
- **Fields**: camelCase with `m_` prefix followed by PascalCase (e.g., `m_Logger`, `m_Sessions`, `m_CommandQueue`)
- **Constants**: PascalCase (e.g., `MaxSessions`, `DefaultTimeout`)

**IMPORTANT**: Member variables must follow the pattern `m_PascalCase` where the first letter after the underscore is uppercase. This ensures consistency with C# naming conventions and improves code readability.
- **Enums**: PascalCase (e.g., `CommandState`, `CircuitState`)

### Code Organization
- **One class per file**: Each class in its own file
- **Namespace structure**: `mcp_nexus.{Category}` (e.g., `mcp_nexus.Session`, `mcp_nexus.CommandQueue`)
- **Using statements**: Grouped by system, third-party, then project namespaces
- **Method ordering**: Constructors, public methods, private methods, dispose pattern

### Error Handling
- **Exception types**: Use specific exception types (`SessionNotFoundException`, `CircuitBreakerOpenException`)
- **Logging levels**: 
  - `LogError` for exceptions and failures
  - `LogWarning` for recoverable issues
  - `LogInformation` for important events
  - `LogDebug` for detailed debugging
- **Graceful degradation**: Always provide fallback behavior

## Domain Knowledge

### MCP (Model Context Protocol) Integration
- **Tools**: Expose debugging operations as MCP tools
- **Resources**: Provide data access through MCP resources
- **Standardized responses**: Always include `usage` field as last entry
- **Session isolation**: Each debugging session is completely isolated

### Windows Debugging
- **CDB Process Management**: Each session gets its own CDB process
- **Command Execution**: Asynchronous command queuing with timeout handling
- **Symbol Paths**: Support for symbol file directories
- **Dump Analysis**: Focus on crash dump (.dmp) file analysis

### Session Lifecycle
1. **Create**: `nexus_open_dump_analyze_session` - Creates isolated session
2. **Execute**: `nexus_enqueue_async_dump_analyze_command` - Queues commands
3. **Monitor**: Use `commands` resource to track status
4. **Retrieve**: `nexus_read_dump_analyze_command_result` - Get results
5. **Close**: `nexus_close_dump_analyze_session` - Clean up resources

### Command Queue System
- **Sequential execution**: Commands execute in FIFO order per session
- **Timeout handling**: Configurable timeouts with heartbeat monitoring
- **Recovery mechanisms**: Automatic retry and circuit breaker patterns
- **Status tracking**: Real-time status updates and progress reporting

## Performance Considerations

### Memory Management
- **Intelligent caching**: LRU eviction with memory pressure monitoring
- **Resource cleanup**: Proper disposal of CDB processes and sessions
- **Memory limits**: Configurable limits with automatic cleanup

### Concurrency
- **Thread safety**: All public methods must be thread-safe
- **Async patterns**: Use `async/await` for I/O operations
- **Cancellation**: Support `CancellationToken` for long-running operations

### Scalability
- **Session limits**: Configurable maximum concurrent sessions
- **Command queuing**: Prevents resource exhaustion
- **Circuit breakers**: Prevents cascade failures

## Testing Requirements

### Unit Tests
- **Coverage**: Aim for 100% code coverage
- **Mocking**: Use mocks for external dependencies
- **Edge cases**: Test error conditions and boundary values

### Integration Tests
- **End-to-end workflows**: Test complete debugging sessions
- **Error scenarios**: Test failure recovery and circuit breaker behavior
- **Performance tests**: Verify memory usage and response times

## Security Considerations

### Path Validation
- **WSL path conversion**: Secure conversion between WSL and Windows paths
- **Path traversal prevention**: Validate all file paths
- **UNC path blocking**: Prevent network path access

### Input Validation
- **Command sanitization**: Validate WinDBG commands
- **Parameter validation**: Check all input parameters
- **Resource limits**: Prevent resource exhaustion attacks

## Common Patterns and Anti-Patterns

### ✅ Good Patterns
- Use dependency injection for testability
- Implement proper disposal patterns
- Use circuit breakers for external dependencies
- Provide comprehensive error messages
- Log important events and errors

### ❌ Anti-Patterns to Avoid
- Don't expose internal implementation details
- Don't use generic exception types
- Don't ignore cancellation tokens
- Don't create memory leaks with event handlers
- Don't hardcode configuration values

## Tools and Dependencies

### Key NuGet Packages
- `ModelContextProtocol.Server` - MCP server implementation
- `Microsoft.AspNetCore` - Web framework
- `NLog.Web.AspNetCore` - Logging
- `System.CommandLine` - CLI parsing
- `AspNetCoreRateLimit` - Rate limiting

### External Tools
- **WinDBG/CDB**: Windows debugging tools (must be installed)
- **Symbols**: Windows symbol files for proper debugging

### Configuration Files
- `appsettings.json` - Main configuration
- `nlog.config` - Logging configuration
- `mcp_nexus.csproj` - Project dependencies

## Development Workflow

### Before Making Changes
1. Read and understand the existing code structure
2. Check for existing XML documentation
3. Verify test coverage for affected areas
4. Consider impact on session isolation

### During Development
1. Follow the XML documentation standards strictly
2. Use appropriate logging levels
3. Handle exceptions gracefully
4. Consider performance implications
5. Maintain thread safety

### After Changes
1. Verify all XML documentation is complete
2. Run existing tests
3. Add new tests for new functionality
4. Check for memory leaks
5. Verify session isolation is maintained

## Special Considerations

### Session Management
- **Isolation**: Each session must be completely isolated
- **Cleanup**: Always clean up resources when sessions close
- **Limits**: Respect maximum session limits
- **Context**: Maintain session context for notifications

### Command Execution
- **Asynchronous**: All command execution must be asynchronous
- **Timeout**: Implement proper timeout handling
- **Recovery**: Provide recovery mechanisms for failures
- **Status**: Always provide clear status information

### Error Recovery
- **Circuit breakers**: Use for external dependencies
- **Retry logic**: Implement exponential backoff
- **Graceful degradation**: Provide fallback behavior
- **User feedback**: Always inform users of issues

This project represents a sophisticated debugging platform that requires careful attention to documentation, error handling, and resource management. Every change should maintain the high standards established for XML documentation and system reliability.
