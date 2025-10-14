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
   - The following version fields MUST exist and MUST have the same value: `Version`, `AssemblyVersion`, and `FileVersion`. Update all three together.
3. **✅ README.md MUST BE UPDATED**: Update test count and coverage in README.md badges AND Test Statistics section
4. **✅ NO COMPILATION ERRORS**: Code must build with zero warnings and zero errors
5. **✅ NO DEAD CODE**: Remove unused code, methods, or files
6. **✅ FORMATTING & HYGIENE VERIFIED (SOLUTION-WIDE)**:
   - Run repository-wide formatting and style enforcement before submitting any change:
     - `dotnet format style`
     - `dotnet format analyzers`
     - `dotnet format whitespace`
   - There MUST be no formatting diffs left. Use `--verify-no-changes` in CI.
   - Absolutely NO commented-out code or commented-out `using` directives anywhere.
   - Absolutely NO unused `using` directives. Remove them.
   - This rule applies to ALL files in the repository, not just edited ones.

### 📋 DETAILED GROUND RULES

#### Code Structure and Quality
* **Atomicity and Clarity:** Methods must be **atomic, focused, and build successfully with zero warnings** in all configurations.
* **Code Cleanliness:** There must be **no unused code**, **dead code**, **commented-out code**, or **unused import/using directives** within any file.
* **File-Class Parity:** Each **top-level class or interface must reside in its own dedicated file**, with the filename matching the class/interface name.
* **Standardized Formatting:** All code must be **perfectly formatted and aligned** according to the project's established standards (enforced by `dotnet format` or equivalent tooling). Apply this across the entire solution for every change.
* **Concurrency and Performance Integrity:** As this is a **server application**, code must adhere to **proper concurrency practices**. Introduction of **blocking operations, excessive/long `Thread.Sleep` calls, or potential deadlocks/livelocks is strictly prohibited**. The code must be performant and thread-safe.

#### Testing and Documentation
* **100% Test Pass Rate:** **All unit tests must pass** (be "green") before submission.
* **Testing Integrity:** **No existing or new tests may be excluded, ignored, or removed** without prior architectural review and approval.
* **Documentation Synchronization:** All relevant **documentation** (e.g., Markdown documentation, README, or designated build manifest files) must be **up-to-date and reflect the current state** of the codebase and tests.

#### Versioning and Scope
* **Version Increment:** Only the **build version component** (the last digit/identifier) in the project's versioning scheme must be incremented for this change. (Assuming a **Major.Minor.Patch.Build** semantic versioning standard.)
* **Version Field Equality:** In `mcp_nexus.csproj`, the `Version`, `AssemblyVersion`, and `FileVersion` values MUST be identical at all times. When bumping the version, update all three fields to the same value.

### 🎯 COMMON VIOLATIONS TO AVOID

**❌ DON'T FORGET:**
- Running tests before submission
- Updating version numbers
- Updating README.md with new test counts and Test Statistics section
- Removing dead/unused code
- Checking for compilation errors

**✅ ALWAYS DO:**
- Run `dotnet test` and verify all tests pass
- Update version in `mcp_nexus.csproj`
- Update test count and Test Statistics section in README.md
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
- **Member variable placement**: **ALL member variables (fields, properties, constants) MUST be declared at the TOP of the class definition, before any methods or constructors**
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

### Test File Organization
- **One-to-One Correspondence**: Every test file MUST correspond to exactly one production source file
- **Naming Convention**: Test files must follow the pattern `{ProductionFileName}Tests.cs`
- **No Orphaned Tests**: All tests for a production class must be in the corresponding test file
- **No Generic Test Files**: Test files with generic names (e.g., `UtilityTests.cs`, `HelperTests.cs`) are prohibited
- **Consolidation Required**: If multiple test files exist for the same production class, they must be consolidated

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
6. Run `dotnet format` (style, analyzers, whitespace) solution-wide and ensure no changes remain (`--verify-no-changes`).

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

---

## Extension System Architecture (v1.0.6.65)

### Overview

The extension system allows complex debugging workflows to be implemented as external PowerShell scripts that can make callbacks to the MCP server to execute WinDBG commands. This solves the problem of AI agents having difficulty orchestrating multi-step debugging sequences.

### Core Problem Solved

**Before:** AI agents would fail when trying to execute workflows like:
1. Get stack trace with `kL`
2. Parse output to extract return addresses
3. Run `lsa` on each address to download sources
4. Aggregate results

**After:** These workflows are implemented as extension scripts that handle the orchestration internally while making callbacks to execute individual commands.

### Session Lifecycle Coupling

**Implemented:** Extensions are automatically killed when their session closes.

**Behavior:**
- When `SessionLifecycleManager.CloseSessionAsync` is called:
  1. Queries `ExtensionCommandTracker` for all running extensions in the session
  2. Calls `ExtensionExecutor.KillExtension` for each running extension
  3. Logs a warning: `🔪 Killed extension script command {CommandId} ({ExtensionName}) due to session {SessionId} closure`
  4. Cleans up tracking data with `ExtensionCommandTracker.RemoveSessionCommands`

**Timeout Behavior:**
- Extensions have a configurable timeout (default: 30 minutes, set in `metadata.json`)
- When timeout expires:
  1. Extension process is killed with `process.Kill(entireProcessTree: true)`
  2. Logs a warning: `🔪 Killed extension script '{Extension}' (command {CommandId}) due to timeout after X seconds (timeout: Yms)`
  3. Extension result is marked as failed with `OperationCanceledException`
- To disable timeout, set `"timeout": 0` in `metadata.json`

**Result Persistence:**
- Extension results are stored in the same `SessionCommandResultCache` as standard commands
- Results persist until the session closes (consistent with standard commands)
- Automatic cleanup when session ends
- Same memory management and LRU eviction as standard commands
- Results are stored in both `ExtensionCommandTracker` (for state/progress) and `SessionCommandResultCache` (for persistence)

### Key Design Decisions

#### 1. **Extension Scripts Run as Separate Processes (NOT in Command Queue)**

**Critical Architecture Point:**
- Extensions execute as **standalone processes** (PowerShell, etc.)
- They **do NOT** block the command queue
- They **submit commands to the queue** like any other client
- Each callback command goes through normal queue flow

**Why this works:**
```
Extension Process          Command Queue (Serial)
     |                            |
     |-- Callback: Execute "kL" -->|
     |                          [kL executes]
     |<--- Result of "kL" ---------|
     |-- Parse output...
     |-- Callback: Execute "lsa" ->|
     |                          [lsa executes]
     |<--- Result of "lsa" -------|
     |-- Aggregate...
```

**No Deadlock** because:
- Extension is external → doesn't block queue
- Each callback is a normal command in queue
- Commands execute sequentially as designed

#### 2. **Unified Command Tracking**

Extensions get their own `commandId` (prefixed with `ext-`) which allows:
- AI can track extension progress with `nexus_read_dump_analyze_command_result(ext-xxx)`
- Same polling mechanism as regular commands
- Consistent UX for AI agents

#### 3. **Security: Localhost-Only HTTP Callbacks with Token Authentication**

**Callback Mechanism:**
- Extensions use HTTP REST API to callback
- API binds to `127.0.0.1` (localhost only)
- Each extension gets a unique, short-lived token
- Token is session-scoped and revoked after completion

**Why HTTP over Named Pipes:**
- Simple, standard protocol
- Easy to debug with curl/Postman
- Cross-platform (for future)
- PowerShell has excellent HTTP support

**AI Protection:**
- AI **never sees** callback URLs or tokens
- Tokens validated by server before execution
- If AI tries to call callback endpoint directly → 403 with helpful message

#### 4. **PowerShell-Only (No Python)**

**Decision:** Support only PowerShell scripts initially.

**Reasoning:**
- PowerShell is standard on Windows
- Excellent text parsing capabilities
- Native JSON support
- Good HTTP client (Invoke-RestMethod)
- Users are Windows debugging experts (likely know PowerShell)

**Implementation:** `ExtensionExecutor` validates `scriptType == "powershell"`

#### 5. **Helper Module for Extension Developers**

**File:** `mcp_nexus/extensions/modules/McpNexusExtensions.psm1`

**Purpose:** Abstract callback complexity from extension developers.

**Example:**
```powershell
Import-Module "$PSScriptRoot\..\modules\McpNexusExtensions.psm1"

# Simple command execution - complexity hidden
$stack = Invoke-NexusCommand "kL"

# Parse and execute more commands
$addresses = Parse-StackAddresses $stack
foreach ($addr in $addresses) {
    $source = Invoke-NexusCommand "lsa $addr"
    # Process source...
}
```

#### 6. **Extension Discovery: Convention over Configuration**

**Structure:**
```
extensions/
  ├── modules/
  │   └── McpNexusExtensions.psm1    # Shared helper
  ├── stack_with_sources/
  │   ├── metadata.json               # Auto-discovered
  │   └── stack_with_sources.ps1      # Script
  ├── basic_crash_analysis/
  │   ├── metadata.json
  │   └── basic_crash_analysis.ps1
  └── [extension_name]/
      ├── metadata.json
      └── [script].ps1
```

**Discovery:** 
- `ExtensionManager` scans `extensions/` for `metadata.json`
- Validates metadata and script file existence
- Loads all extensions at startup

**Deployment:**
- Extensions auto-copy to `bin/` during build
- Users can drop new extensions and restart server
- No recompilation needed

### Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         AI Agent                            │
└───────────────┬─────────────────────────────────────────────┘
                │ MCP Tool Call
                ▼
┌─────────────────────────────────────────────────────────────┐
│  nexus_execute_extension(sessionId, "stack_with_sources")  │
│  Returns: { commandId: "ext-xxx", status: "Queued" }       │
└───────────────┬─────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│              ExtensionCommandTracker                        │
│  • Tracks: commandId → ExtensionCommandInfo                │
│  • States: Queued → Executing → Completed                  │
│  • Progress: callback count, progress messages             │
└───────────────┬─────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│              ExtensionExecutor                              │
│  • Spawns: PowerShell process                              │
│  • Injects: Environment variables (token, callback URL)    │
│  • Monitors: Output, errors, progress                      │
│  • Manages: Lifecycle (timeout, cancellation)              │
└───────────────┬─────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│         Extension Script (PowerShell Process)               │
│  ┌───────────────────────────────────────────────┐         │
│  │ Import-Module McpNexusExtensions.psm1         │         │
│  │                                                 │         │
│  │ $result1 = Invoke-NexusCommand "kL"           │─────┐   │
│  │ # Parse...                                     │     │   │
│  │ $result2 = Invoke-NexusCommand "lsa 0x..."    │─────┤   │
│  │ # Aggregate...                                 │     │   │
│  │                                                 │     │   │
│  │ Return-Json $results                           │     │   │
│  └───────────────────────────────────────────────┘     │   │
└────────────────────────────────────────────────────────┼───┘
                                                          │
                 ┌────────────────────────────────────────┘
                 │ HTTP POST (Bearer Token)
                 ▼
┌─────────────────────────────────────────────────────────────┐
│         ExtensionCallbackController                         │
│  POST /extension-callback/execute                          │
│  • Validates: Token, Localhost-only                        │
│  • Enqueues: Command in session queue                      │
│  • Waits: For command completion                           │
│  • Returns: Command result                                 │
└───────────────┬─────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│            ICommandQueueService (Session)                   │
│  • Executes: Commands serially (one at a time)            │
│  • CDB Process: Single-threaded command execution          │
│  • Returns: Command result to callback                     │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow Example: Stack with Sources

```
1. AI calls: nexus_execute_extension("stack_with_sources")
   ├─> ExtensionCommandTracker: Track "ext-123"
   ├─> ExtensionTokenValidator: Create token
   └─> ExtensionExecutor: Start PowerShell

2. PowerShell script loads helper module
   └─> Has access to: Invoke-NexusCommand function

3. Script executes: Invoke-NexusCommand "kL"
   ├─> HTTP POST to localhost:port/extension-callback/execute
   ├─> Headers: Authorization: Bearer <token>
   ├─> Body: { command: "kL", timeoutSeconds: 300 }
   └─> Waits for response

4. ExtensionCallbackController receives request
   ├─> Validates: Token (session-scoped, time-limited)
   ├─> Validates: Request from localhost only
   ├─> Increments: Callback count in tracker
   ├─> Enqueues: Command in session's queue
   └─> Waits: For command completion (blocking for THIS request)

5. Command executes in CDB session (serialized)
   └─> Result returned to controller

6. Controller returns result to script
   └─> Script continues with next step

7. Script completes, returns JSON output
   └─> ExtensionCommandTracker stores result

8. AI polls: nexus_read_dump_analyze_command_result("ext-123")
   └─> Returns: Extension output when completed
```

### Key Files

**Extension Infrastructure:**
- `mcp_nexus/Extensions/ExtensionManager.cs` - Discovery and metadata loading
- `mcp_nexus/Extensions/ExtensionExecutor.cs` - Script process management
- `mcp_nexus/Extensions/ExtensionCommandTracker.cs` - Command lifecycle tracking
- `mcp_nexus/Extensions/ExtensionTokenValidator.cs` - Security tokens
- `mcp_nexus/Extensions/ExtensionCallbackController.cs` - HTTP callback API
- `mcp_nexus/Extensions/ExtensionMetadata.cs` - Extension definition model

**Extension Scripts:**
- `mcp_nexus/extensions/modules/McpNexusExtensions.psm1` - Helper functions
- `mcp_nexus/extensions/stack_with_sources/` - Downloads sources for stack frames
- `mcp_nexus/extensions/basic_crash_analysis/` - Basic crash analysis workflow
- `mcp_nexus/extensions/memory_corruption_analysis/` - Memory corruption patterns
- `mcp_nexus/extensions/thread_deadlock_investigation/` - Deadlock analysis

**Integration Points:**
- `mcp_nexus/Tools/McpNexusTools.cs` - Added `nexus_execute_extension` tool
- `mcp_nexus/Configuration/ServiceRegistration.cs` - DI registration
- `mcp_nexus/appsettings.json` - Extension configuration

**Tests:**
- `mcp_nexus_tests/Extensions/ExtensionManagerTests.cs` - 28 tests
- `mcp_nexus_tests/Extensions/ExtensionTokenValidatorTests.cs` - 19 tests
- `mcp_nexus_tests/Extensions/ExtensionCommandTrackerTests.cs` - 21 tests
- `mcp_nexus_tests/Extensions/ExtensionCallbackControllerTests.cs` - 8 tests
- `mcp_nexus_tests/Extensions/ExtensionExecutorTests.cs` - 40 tests

### Configuration

**appsettings.json:**
```json
{
  "McpNexus": {
    "Extensions": {
      "Enabled": true,
      "ExtensionsPath": "extensions",
      "CallbackPort": 0  // 0 = use MCP server port
    }
  }
}
```

### Extension Metadata Format

**metadata.json:**
```json
{
  "name": "extension_name",
  "description": "What this extension does",
  "version": "1.0.0",
  "author": "Author Name",
  "scriptType": "powershell",
  "scriptFile": "script.ps1",
  "timeout": 1800000,
  "requires": ["McpNexusExtensions"],
  "parameters": [
    {
      "name": "paramName",
      "type": "string",
      "description": "Parameter description",
      "required": false,
      "default": "defaultValue"
    }
  ]
}
```

### Creating a New Extension

1. **Create folder:** `extensions/my_extension/`
2. **Create metadata.json** (see format above)
3. **Create PowerShell script:**
```powershell
# Import helper module
Import-Module "$PSScriptRoot\..\modules\McpNexusExtensions.psm1" -Force

# Write progress
Write-NexusProgress "Starting analysis..."

# Execute commands
$output1 = Invoke-NexusCommand "!analyze -v"
$output2 = Invoke-NexusCommand "lm"

# Return structured result
@{
    success = $true
    analysis = $output1
    modules = $output2
} | ConvertTo-Json
```

4. **Restart server** - Extension is auto-discovered

### Timeout Behavior

**Regular Commands:** 
- Default: 300 seconds
- Enforced by command queue

**Extensions:**
- No strict timeout (or very long)
- Depends on callback timeouts
- Individual callbacks have normal timeouts
- Overall extension can run as long as needed

### Security Considerations

1. **Token-based authentication** - Short-lived, session-scoped
2. **Localhost-only binding** - Callback API not network-accessible
3. **AI isolation** - AI never sees tokens or callback URLs
4. **Process isolation** - Extensions run in separate processes
5. **Token revocation** - Automatic cleanup after completion

### Testing Strategy

**Unit Tests:** Test each component in isolation
- Mock file system for ExtensionManager
- Mock processes for ExtensionExecutor
- Mock HTTP context for ExtensionCallbackController

**Integration Tests:** Test full workflow (future)
- Real PowerShell execution
- Actual callback communication
- End-to-end extension execution

### Common Pitfalls to Avoid

1. ❌ **Don't make extensions blocking commands in the queue**
   - ✅ They are separate processes that submit commands

2. ❌ **Don't expose callback URLs/tokens to AI**
   - ✅ Use MCP tools, let server handle callbacks

3. ❌ **Don't add Python support without discussion**
   - ✅ PowerShell-only by design

4. ❌ **Don't create extensions that bypass session isolation**
   - ✅ All commands go through session's command queue

5. ❌ **Don't add `*.py` files to build output**
   - ✅ Only `*.ps1`, `*.psm1`, `*.json` files

### Future Enhancements (Not Yet Implemented)

- **Dynamic timeout:** Extend based on callback activity
- **Progress streaming:** Real-time progress to AI
- **Extension hot-reload:** Discover new extensions without restart
- **Extension dependencies:** Extensions calling other extensions

---

## For Future AI Assistants

When working on this project:
1. **Read AGENTS.md first** - Understand the architecture before making changes
2. **Follow the established patterns** - Especially for extensions
3. **PowerShell-only for extension scripts** - No Python
4. **Add tests for new features** - Maintain test coverage
5. **Zero-regression policy** - All existing tests must pass
6. **Update this document** - If architecture changes significantly

### Extension System Key Principle

**Extensions orchestrate workflows externally, using callbacks for individual commands.**

This design prevents deadlocks, maintains session isolation, and gives AI agents a reliable way to execute complex debugging workflows.
