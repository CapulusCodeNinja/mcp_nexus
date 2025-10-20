# MCP Nexus - AI Agent Instructions

## Project Overview

MCP Nexus is a sophisticated Model Context Protocol (MCP) server designed for Windows crash dump analysis and debugging. It provides a comprehensive suite of tools for analyzing memory dumps using WinDBG/CDB debugger integration, with advanced features including session management, command queuing, circuit breakers, health monitoring, and intelligent caching.

## Architecture

### Core Components
- **Session Management**: Multi-session debugging with isolated CDB processes
- **Command Queue System**: Asynchronous command execution with resilience patterns
- **Command Batching**: Intelligent batching of multiple commands for improved throughput
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

**🚫 NO EXCEPTIONS ARE ALLOWED - ALL RULES ARE ABSOLUTE 🚫**

### 🔥 IMMEDIATE REQUIREMENTS (Check These FIRST!)

1. **✅ ALL TESTS MUST PASS**: Run `dotnet test` - ALL tests must be green before any submission. NO EXCEPTIONS.
2. **✅ VERSION MUST BE UPDATED**: Increment the build version in `mcp_nexus.csproj` (e.g., 1.0.6.12 → 1.0.6.13). MANDATORY.
   - The following version fields MUST exist and MUST have the same value: `Version`, `AssemblyVersion`, and `FileVersion`. Update all three together. NO EXCEPTIONS.
3. **✅ README.md MUST BE UPDATED**: Update test count and coverage in README.md badges AND Test Statistics section. MANDATORY.
4. **✅ MINIMUM COVERAGE THRESHOLDS MUST BE MAINTAINED**: ABSOLUTE REQUIREMENT.
   - **Line Coverage**: Must NEVER fall below **75%**. NO EXCEPTIONS.
   - **Branch Coverage**: Must NEVER fall below **75%**. NO EXCEPTIONS.
   - Run `dotnet test --collect:"XPlat Code Coverage"` to verify coverage before submission. MANDATORY.
   - If any change causes coverage to drop below these thresholds, add tests to restore coverage. ABSOLUTE REQUIREMENT.
5. **✅ NO COMPILATION ERRORS**: Code must build with zero warnings and zero errors. ABSOLUTE REQUIREMENT.
6. **✅ NO DEAD CODE**: Remove unused code, methods, or files. NO EXCEPTIONS.
7. **✅ FORMATTING & HYGIENE VERIFIED (SOLUTION-WIDE)**: MANDATORY.
   - Run repository-wide formatting and style enforcement before submitting any change:
     - `dotnet format style`
     - `dotnet format analyzers`
     - `dotnet format whitespace`
   - There MUST be no formatting diffs left. Use `--verify-no-changes` in CI. NO EXCEPTIONS.
   - Absolutely NO commented-out code or commented-out `using` directives anywhere. ABSOLUTE.
   - Absolutely NO unused `using` directives. Remove them. MANDATORY.
   - This rule applies to ALL files in the repository, not just edited ones. NO EXCEPTIONS.

### 📋 DETAILED GROUND RULES

#### Code Structure and Quality
* **Atomicity and Clarity:** Methods must be **atomic, focused, and build successfully with zero warnings** in all configurations. NO EXCEPTIONS.
* **Code Cleanliness:** There must be **no unused code**, **dead code**, **commented-out code**, or **unused import/using directives** within any file. ABSOLUTE REQUIREMENT.
* **File-Class Parity:** Each **top-level class or interface must reside in its own dedicated file**, with the filename matching the class/interface name. MANDATORY.
* **Standardized Formatting:** All code must be **perfectly formatted and aligned** according to the project's established standards (enforced by `dotnet format` or equivalent tooling). Apply this across the entire solution for every change. NO EXCEPTIONS.
* **Concurrency and Performance Integrity:** As this is a **server application**, code must adhere to **proper concurrency practices**. Introduction of **blocking operations, excessive/long `Thread.Sleep` calls, or potential deadlocks/livelocks is strictly prohibited**. The code must be performant and thread-safe. ABSOLUTE REQUIREMENT.
* **No Arbitrary Delays:** **Sleeps, `Task.Delay`, `Thread.Sleep`, and similar blocking delays should be avoided as much as possible**. Use deterministic synchronization mechanisms like `TaskCompletionSource`, `SemaphoreSlim`, `CancellationToken`, or proper async/await patterns instead. **Static delays or sleeps bigger than 100ms in the tests are not acceptable without any exception**. NO EXCEPTIONS.
* **Proper Concurrency is Mandatory:** **Proper concurrency practices are mandatory and there is no exception**. All asynchronous operations must use proper async/await patterns, cancellation tokens, and thread-safe synchronization primitives. Blocking operations, race conditions, and improper synchronization are strictly prohibited. ABSOLUTE REQUIREMENT.
* **Proper Encapsulation is Mandatory:** **Proper encapsulation is mandatory and there is no exception**. All private fields must be properly encapsulated, public APIs must be minimal and well-defined, and internal implementation details must not leak through public interfaces. NO EXCEPTIONS.

#### Testing and Documentation
* **100% Test Pass Rate:** **All unit tests must pass** (be "green") before submission. NO EXCEPTIONS.
* **Minimum Coverage Thresholds:** **Code coverage must NEVER fall below the mandatory thresholds**. ABSOLUTE REQUIREMENT.
  - **Line Coverage**: Must be **≥75%** at all times. NO EXCEPTIONS.
  - **Branch Coverage**: Must be **≥75%** at all times. NO EXCEPTIONS.
  - If any code change causes coverage to drop below these thresholds, additional tests MUST be added before submission. MANDATORY.
  - Run `dotnet test --collect:"XPlat Code Coverage"` to verify coverage before any submission. ABSOLUTE REQUIREMENT.
* **Testing Integrity:** **No existing or new tests may be excluded, ignored, or removed** without prior architectural review and approval. ABSOLUTE REQUIREMENT.
* **Flaky Tests Are NOT Acceptable:** **Flaky tests are NOT acceptable!** Tests that pass sometimes and fail other times indicate poor test design and must be fixed immediately. NO EXCEPTIONS.
* **Test Isolation Is Mandatory:** **Isolation issues in tests are NOT acceptable!** Tests must be completely isolated from each other and must not depend on execution order or shared state. ABSOLUTE REQUIREMENT.
* **Zero Tolerance for Test Failures:** **Failing test is a failing test independent of the reason, there are no exceptions and no acceptable failure interpretation!** All tests must pass consistently, every time, without any excuses. NO EXCEPTIONS.
* **Mocking is Mandatory:** **Mocking should be used for testing without exception**. All external dependencies, services, and collaborators must be mocked to ensure test isolation and deterministic behavior. NO EXCEPTIONS.
* **Documentation Synchronization:** All relevant **documentation** (e.g., Markdown documentation, README, or designated build manifest files) must be **up-to-date and reflect the current state** of the codebase and tests. MANDATORY.

#### Versioning and Scope
* **Version Increment:** Only the **build version component** (the last digit/identifier) in the project's versioning scheme must be incremented for this change. (Assuming a **Major.Minor.Patch.Build** semantic versioning standard.) NO EXCEPTIONS.
* **Version Field Equality:** In `mcp_nexus.csproj`, the `Version`, `AssemblyVersion`, and `FileVersion` values MUST be identical at all times. When bumping the version, update all three fields to the same value. ABSOLUTE REQUIREMENT.

### 🎯 COMMON VIOLATIONS TO AVOID

**❌ DON'T FORGET (ALL MANDATORY - NO EXCEPTIONS):**
- Running tests before submission - ABSOLUTE REQUIREMENT
- Updating version numbers - MANDATORY
- Updating README.md with new test counts and Test Statistics section - ABSOLUTE REQUIREMENT
- **Verifying coverage thresholds (≥75% line, ≥75% branch)** - ABSOLUTE REQUIREMENT
- Removing dead/unused code - NO EXCEPTIONS
- Checking for compilation errors - MANDATORY
- **Fixing flaky tests immediately** - ABSOLUTE REQUIREMENT
- **Ensuring test isolation** - NO EXCEPTIONS
- **Making tests pass consistently every time** - MANDATORY
- **Avoiding arbitrary delays (`Task.Delay`, `Thread.Sleep`)** - NO EXCEPTIONS
- **Using proper concurrency patterns** - ABSOLUTE REQUIREMENT
- **Maintaining proper encapsulation** - MANDATORY
- **Using mocking for all external dependencies** - NO EXCEPTIONS
- **Organizing namespaces correctly** - ABSOLUTE REQUIREMENT
- **Splitting large directories into logical sub-namespaces** - NO EXCEPTIONS
- **Mirroring test structure to production structure** - ABSOLUTE REQUIREMENT
- **Maintaining test namespace alignment with production** - NO EXCEPTIONS

**✅ ALWAYS DO (ALL MANDATORY - NO EXCEPTIONS):**
- Run `dotnet test` and verify all tests pass - ABSOLUTE REQUIREMENT
- Update version in `mcp_nexus.csproj` - MANDATORY
- Update test count and Test Statistics section in README.md - NO EXCEPTIONS
- **Run coverage and verify ≥75% line coverage and ≥75% branch coverage** - ABSOLUTE REQUIREMENT
- Remove any unused code or files - ABSOLUTE REQUIREMENT
- Verify zero compilation warnings/errors - MANDATORY
- **Fix any test that fails intermittently** - NO EXCEPTIONS
- **Ensure tests are completely isolated** - ABSOLUTE REQUIREMENT
- **Make tests deterministic and reliable** - MANDATORY
- **Use deterministic synchronization instead of delays** - NO EXCEPTIONS
- **Implement proper async/await patterns** - ABSOLUTE REQUIREMENT
- **Maintain strict encapsulation boundaries** - MANDATORY
- **Mock all external dependencies in tests** - NO EXCEPTIONS
- **Organize classes in logical, cohesive namespaces** - ABSOLUTE REQUIREMENT
- **Split directories with more than 5-7 classes into sub-namespaces** - NO EXCEPTIONS
- **Ensure test project structure mirrors production structure exactly** - ABSOLUTE REQUIREMENT
- **Use `.Tests` suffix for test namespaces matching production namespaces** - NO EXCEPTIONS

---

## Coding Standards

### XML Documentation Requirements
**CRITICAL - ABSOLUTE REQUIREMENT**: Every public and private method, property, constructor, enum, and class MUST have complete XML documentation. NO EXCEPTIONS.

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
- **Classes**: PascalCase (e.g., `SessionManager`, `CommandQueueService`) - MANDATORY
- **Methods**: PascalCase (e.g., `CreateSessionAsync`, `QueueCommand`) - ABSOLUTE REQUIREMENT
- **Properties**: PascalCase (e.g., `SessionId`, `IsHealthy`) - NO EXCEPTIONS
- **Fields (including static fields)**: camelCase with `m_` prefix followed by PascalCase (e.g., `m_Logger`, `m_Sessions`, `m_CommandQueue`) - MANDATORY.
  - Static fields are treated as member variables for naming purposes and MUST also use the `m_` prefix (NOT `s_`) - ABSOLUTE REQUIREMENT.
  - Example: use `m_PathPattern` instead of `s_PathPattern` - NO EXCEPTIONS.
- **Constants**: PascalCase (e.g., `MaxSessions`, `DefaultTimeout`) - MANDATORY

**IMPORTANT - ABSOLUTE REQUIREMENT**: Member variables must follow the pattern `m_PascalCase` where the first letter after the underscore is uppercase. This ensures consistency with C# naming conventions and improves code readability. NO EXCEPTIONS.
- **Enums**: PascalCase (e.g., `CommandState`, `CircuitState`) - MANDATORY

### Time Policy
- Use local time consistently across the codebase - ABSOLUTE REQUIREMENT.
- Prefer `DateTime.Now` over `DateTime.UtcNow` and `DateTimeOffset.Now` over `DateTimeOffset.UtcNow` in any possible case - NO EXCEPTIONS.
- Logs, timestamps, metrics, and persisted times should all use local time to avoid confusion - MANDATORY.

### Code Organization
- **One class per file**: Each class in its own file - ABSOLUTE REQUIREMENT
- **Namespace structure**: `mcp_nexus.{Category}` (e.g., `mcp_nexus.Session`, `mcp_nexus.CommandQueue`) - MANDATORY
- **Using statements**: Grouped by system, third-party, then project namespaces - NO EXCEPTIONS
- **Member variable placement**: **ALL member variables (fields, properties, constants) MUST be declared at the TOP of the class definition, before any methods or constructors** - ABSOLUTE REQUIREMENT
- **Method ordering**: Constructors, public methods, private methods, dispose pattern - MANDATORY

### Error Handling
- **Exception types**: Use specific exception types (`SessionNotFoundException`, `CircuitBreakerOpenException`) - ABSOLUTE REQUIREMENT
- **Logging levels**: MANDATORY
  - `LogError` for exceptions and failures - NO EXCEPTIONS
  - `LogWarning` for recoverable issues - ABSOLUTE REQUIREMENT
  - `LogInformation` for important events - MANDATORY
  - `LogDebug` for detailed debugging - NO EXCEPTIONS
- **Graceful degradation**: Always provide fallback behavior - ABSOLUTE REQUIREMENT

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
3. **Monitor**: `nexus_get_dump_analyze_commands_status` - Bulk status polling (recommended)
4. **Retrieve**: `nexus_read_dump_analyze_command_result` - Get individual results
5. **Close**: `nexus_close_dump_analyze_session` - Clean up resources

**Efficient Monitoring Pattern:**
- Use `nexus_get_dump_analyze_commands_status` to poll ALL commands in a session at once
- Much more efficient than individual command polling
- Returns status, timing, and progress for all commands
- Use `nexus_read_dump_analyze_command_result` only when status shows "Completed"

### Command Queue System
- **Sequential execution**: Commands execute in FIFO order per session
- **Timeout handling**: Configurable timeouts with heartbeat monitoring
- **Recovery mechanisms**: Automatic retry and circuit breaker patterns
- **Status tracking**: Real-time status updates and progress reporting

### Efficient Command Monitoring

**Bulk Status Polling Pattern (Recommended):**
```json
// 1. Queue multiple commands
nexus_enqueue_async_dump_analyze_command(sessionId, "!analyze -v")
nexus_enqueue_async_dump_analyze_command(sessionId, "kL")
nexus_enqueue_async_dump_analyze_command(sessionId, "!threads")

// 2. Poll ALL commands at once (efficient)
nexus_get_dump_analyze_commands_status(sessionId)
// Returns: Status of all commands with timing and progress

// 3. Get individual results when completed
nexus_read_dump_analyze_command_result(sessionId, "cmd-123")
```

**Benefits of Bulk Polling:**
- **Single API call** instead of multiple individual polls
- **Reduced network overhead** and latency
- **Better performance** for monitoring multiple commands
- **Unified view** of all command statuses in one response
- **Easier to implement** efficient polling loops

**When to Use Individual Polling:**
- Only when you need the **full command output** (not just status)
- When a specific command shows "Completed" status
- For **debugging** specific command issues

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
- **One-to-One Correspondence**: Every test file MUST correspond to exactly one production source file - ABSOLUTE REQUIREMENT
- **Naming Convention**: Test files must follow the pattern `{ProductionFileName}Tests.cs` - MANDATORY
- **No Orphaned Tests**: All tests for a production class must be in the corresponding test file - NO EXCEPTIONS
- **No Generic Test Files**: Test files with generic names (e.g., `UtilityTests.cs`, `HelperTests.cs`) are prohibited - ABSOLUTE REQUIREMENT
- **Consolidation Required**: If multiple test files exist for the same production class, they must be consolidated - MANDATORY

#### Test Structure Requirements
- **Test Project Mirroring**: **Test project structure MUST exactly mirror production code structure**. Test directories and namespaces must follow the same organization as production code. ABSOLUTE REQUIREMENT.
- **Test Namespace Convention**: **Test namespaces must match production namespaces with `.Tests` suffix**. For example: `mcp_nexus.CommandQueue.Batching` → `mcp_nexus.CommandQueue.Batching.Tests`. NO EXCEPTIONS.
- **Test Directory Alignment**: **Test directories must mirror production directories exactly**. If production has `CommandQueue/Batching/`, tests must have `CommandQueue/Batching/` with `.Tests` namespace. MANDATORY.
- **Test Class Organization**: **Test classes must be organized in the same sub-namespaces as their production counterparts**. NO EXCEPTIONS.

### Unit Tests
- **Coverage Thresholds**: **Mandatory minimum coverage requirements** - ABSOLUTE REQUIREMENT
  - **Line Coverage**: Must be **≥75%** at all times. NO EXCEPTIONS.
  - **Branch Coverage**: Must be **≥75%** at all times. NO EXCEPTIONS.
  - Aim for 100% coverage where feasible, but never drop below these mandatory thresholds. MANDATORY.
- **Mocking**: Use mocks for external dependencies - MANDATORY
- **Mocking is Mandatory**: **Mocking should be used for testing without exception**. All external dependencies, services, and collaborators must be mocked to ensure test isolation and deterministic behavior. NO EXCEPTIONS.
- **Edge cases**: Test error conditions and boundary values - NO EXCEPTIONS

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
1. Read and understand the existing code structure - MANDATORY
2. Check for existing XML documentation - ABSOLUTE REQUIREMENT
3. Verify test coverage for affected areas - NO EXCEPTIONS
4. Consider impact on session isolation - MANDATORY

### During Development
1. Follow the XML documentation standards strictly - ABSOLUTE REQUIREMENT
2. Use appropriate logging levels - MANDATORY
3. Handle exceptions gracefully - NO EXCEPTIONS
4. Consider performance implications - ABSOLUTE REQUIREMENT
5. Maintain thread safety - MANDATORY

### After Changes
1. Verify all XML documentation is complete - ABSOLUTE REQUIREMENT
2. Run existing tests - MANDATORY
3. Add new tests for new functionality - NO EXCEPTIONS
4. Check for memory leaks - ABSOLUTE REQUIREMENT
5. Verify session isolation is maintained - MANDATORY
6. Run `dotnet format` (style, analyzers, whitespace) solution-wide and ensure no changes remain (`--verify-no-changes`) - ABSOLUTE REQUIREMENT.

## Special Considerations

### Session Management
- **Isolation**: Each session must be completely isolated - ABSOLUTE REQUIREMENT
- **Cleanup**: Always clean up resources when sessions close - MANDATORY
- **Limits**: Respect maximum session limits - NO EXCEPTIONS
- **Context**: Maintain session context for notifications - ABSOLUTE REQUIREMENT

### Command Execution
- **Asynchronous**: All command execution must be asynchronous - MANDATORY
- **Timeout**: Implement proper timeout handling - NO EXCEPTIONS
- **Recovery**: Provide recovery mechanisms for failures - ABSOLUTE REQUIREMENT
- **Status**: Always provide clear status information - MANDATORY


This project represents a sophisticated debugging platform that requires careful attention to documentation, error handling, and resource management. Every change should maintain the high standards established for XML documentation and system reliability. ALL REQUIREMENTS ARE ABSOLUTE - NO EXCEPTIONS ARE ALLOWED.

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
- AI can track extension progress with `nexus_get_dump_analyze_commands_status(sessionId)` (bulk polling)
- AI can get individual results with `nexus_read_dump_analyze_command_result(ext-xxx)`
- Same polling mechanism as regular commands
- Consistent UX for AI agents
- **Efficient monitoring**: Use bulk status polling to monitor extension + regular commands together

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

8. AI polls: nexus_get_dump_analyze_commands_status(sessionId)
   ├─> Returns: Status of ALL commands including "ext-123"
   └─> Shows progress for extension and any other commands

9. AI gets result: nexus_read_dump_analyze_command_result("ext-123")
   └─> Returns: Extension output when status shows "Completed"
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

## Async Command Execution (v1.0.7.5)

### Problem

Extension scripts using synchronous `Invoke-NexusCommand` cannot leverage command batching because each command blocks until completion, preventing multiple commands from being queued together.

### Solution

Added async command execution functions that queue commands without blocking, enabling the internal command batching system to improve throughput.

### New PowerShell Functions

- `Start-NexusCommand` - Queue a command, returns command ID immediately
- `Wait-NexusCommand` - Block until queued command completes
- `Get-NexusCommandResult` - Check command status without blocking
- `Start-NexusCommands` - Queue multiple commands at once

### New HTTP Endpoint

- `POST /extension-callback/queue` - Queue command without waiting (enables batching)

### Usage Example

```powershell
# Queue multiple commands (enables batching)
$ids = Start-NexusCommands -Commands @("lm", "!threads", "!peb")

# Wait for all to complete
foreach ($id in $ids) {
    $result = Wait-NexusCommand -CommandId $id
    Process-Result $result
}
```

### Benefits

- Commands queued together can be batched automatically by `BatchCommandProcessor`
- Better throughput for bulk operations
- Backward compatible with existing `Invoke-NexusCommand` (synchronous)

---

## Command Batching System Architecture

### Overview

The command batching system improves throughput by intelligently grouping multiple commands into a single execution batch. This is transparent to AI clients and maintains command dependencies by executing batches sequentially within each session.

### Core Problem Solved

**Before Batching:**
- Each command executed individually
- High overhead from process communication
- Slower throughput for multiple sequential commands
- Timeouts set per command could be conservative

**After Batching:**
- Commands automatically grouped (up to configurable limit)
- Single execution with multiple commands
- Improved overall throughput
- Adaptive timeouts based on batch size

### Key Design Decisions

#### 1. **Batching is Internal and Transparent**

**Critical Architecture Point:**
- Batching happens **inside** `IsolatedCommandQueueService`
- AI clients see **no difference** in behavior
- Commands still queued individually
- Results stored and retrieved individually
- No changes needed to MCP tools or AI integration

**Why this works:**
```
AI Client                    IsolatedCommandQueueService
   |                                   |
   |-- Queue: Command A -------------->|
   |-- Queue: Command B -------------->| [Batch: A, B, C]
   |-- Queue: Command C -------------->|     |
   |                                   |     v
   |                                   | Execute Combined
   |                                   |     |
   |<-- Result A ----------------------|     v
   |<-- Result B ----------------------| Parse & Distribute
   |<-- Result C ----------------------|
```

**No Race Conditions** because:
- Batching is session-scoped → isolated per session
- Commands execute sequentially within batch
- Results parsed and stored atomically
- Cache is thread-safe

#### 2. **Configurable Batching Behavior**

Batching is fully configurable via `appsettings.json`:

```json
{
  "McpNexus": {
    "Batching": {
      "Enabled": true,
      "MaxBatchSize": 5,
      "BatchWaitTimeoutMs": 2000,
      "BatchTimeoutMultiplier": 1.0,
      "MaxBatchTimeoutMinutes": 30,
      "ExcludedCommands": [
        "!analyze", "!dump", "!heap", "!memusage", "!runaway",
        "~*k", "!locks", "!cs", "!gchandles"
      ]
    }
  }
}
```

**Configuration Parameters:**
- **Enabled**: Enable/disable batching system-wide
- **MaxBatchSize**: Maximum commands per batch (default: 5)
- **BatchWaitTimeoutMs**: Max wait time to accumulate commands (default: 2000ms)
- **BatchTimeoutMultiplier**: Multiplier for timeout calculation (default: 1.0)
- **MaxBatchTimeoutMinutes**: Cap for batch timeout (default: 30 minutes)
- **ExcludedCommands**: Commands that should never be batched

#### 3. **Smart Command Filtering**

**Exclusion List:**
Commands that should **not** be batched:
- `!analyze` - Complex, long-running analysis
- `!dump`, `!heap`, `!memusage` - Memory-intensive operations
- `!runaway` - Performance profiling
- `~*k`, `!locks`, `!cs` - Thread analysis
- `!gchandles` - Managed heap analysis

**Why exclude these:**
- They are **long-running** and batching adds no value
- They may **change debugger state** significantly
- They produce **large output** that's better processed alone
- Batching them could exceed timeout limits

**Prefix Matching:**
The `BatchCommandFilter` uses prefix matching, so:
- Exclusion `"!analyze"` blocks: `!analyze`, `!analyze -v`, `!analyzev`
- Exclusion `"~*k"` blocks: `~*k`, `~*kv`, `~*kp`

#### 4. **Adaptive Timeout Calculation**

**Formula:**
```csharp
BatchTimeout = Min(
    BaseCommandTimeout * CommandCount * BatchTimeoutMultiplier,
    MaxBatchTimeoutMinutes
)
```

**Example:**
- Base command timeout: 10 minutes (600,000ms)
- Commands in batch: 3
- Multiplier: 1.0
- **Calculated timeout**: 30 minutes (capped at MaxBatchTimeoutMinutes)

**Why this works:**
- Larger batches get more time
- Cap prevents runaway timeouts
- Multiplier allows tuning for specific environments

#### 5. **Batch Parsing with Sentinel Markers**

**Sentinel Markers:**
```csharp
public const string BatchStart = "MCP_NEXUS_BATCH_START";
public const string BatchEnd = "MCP_NEXUS_BATCH_END";
public const string CommandSeparator = "MCP_NEXUS_CMD_SEP";
```

**Batch Command Format:**
```
.echo MCP_NEXUS_BATCH_START; .echo MCP_NEXUS_CMD_SEP_CMD-123_START; <actual command>; .echo MCP_NEXUS_CMD_SEP_CMD-123_END; .echo MCP_NEXUS_CMD_SEP_CMD-456_START; <actual command>; .echo MCP_NEXUS_CMD_SEP_CMD-456_END; .echo MCP_NEXUS_BATCH_END
```

**Why this works:**
- Each command's output is clearly delimited
- Parser can reliably extract individual results
- Unique IDs prevent cross-contamination
- Robust against commands that produce special characters

### Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              IsolatedCommandQueueService                    │
│  ┌───────────────────────────────────────────────┐         │
│  │  ProcessCommandQueueAsync()                   │         │
│  │    │                                           │         │
│  │    ├──> Take command from queue               │         │
│  │    │                                           │         │
│  │    └──> BatchCommandProcessor.ProcessCommand()│         │
│  └───────────────────────────────────────────────┘         │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│              BatchCommandProcessor                          │
│  ┌───────────────────────────────────────────────┐         │
│  │  ProcessCommandAsync(command)                 │         │
│  │    │                                           │         │
│  │    ├──> BatchCommandFilter.CanBatchCommand()  │         │
│  │    │      (Check exclusion list)              │         │
│  │    │                                           │         │
│  │    ├──> If batchable: Queue internally        │         │
│  │    │    If not: ExecuteSingleCommandAsync()   │         │
│  │    │                                           │         │
│  │    └──> BatchProcessingLoopAsync()            │         │
│  │           (Background loop)                   │         │
│  └───────────────────────────────────────────────┘         │
│                                                              │
│  Background Loop:                                           │
│  ┌───────────────────────────────────────────────┐         │
│  │  1. Wait for commands or timeout              │         │
│  │  2. Collect up to MaxBatchSize commands       │         │
│  │  3. CommandBatchBuilder.CreateBatchCommand()  │         │
│  │  4. BatchTimeoutCalculator.CalculateTimeout() │         │
│  │  5. CdbSession.ExecuteCommand(batch)          │         │
│  │  6. BatchResultParser.SplitBatchResults()     │         │
│  │  7. Store results in SessionCommandResultCache│         │
│  │  8. Complete QueuedCommand.SetResult()        │         │
│  └───────────────────────────────────────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

### Key Files

**Batching Infrastructure:**
- `mcp_nexus/CommandQueue/BatchingConfiguration.cs` - Configuration model
- `mcp_nexus/CommandQueue/BatchCommandBuilder.cs` - Builds combined command
- `mcp_nexus/CommandQueue/BatchResultParser.cs` - Parses batch output
- `mcp_nexus/CommandQueue/BatchCommandFilter.cs` - Exclusion list filtering
- `mcp_nexus/CommandQueue/BatchTimeoutCalculator.cs` - Timeout calculation
- `mcp_nexus/CommandQueue/BatchCommandProcessor.cs` - Main batching orchestration
- `mcp_nexus/Debugger/CdbSentinels.cs` - Batch sentinel markers

**Integration Points:**
- `mcp_nexus/CommandQueue/IsolatedCommandQueueService.cs` - Integrated BatchCommandProcessor
- `mcp_nexus/Session/SessionLifecycleManager.cs` - Passes batching config to queue service
- `mcp_nexus/Configuration/ServiceRegistration.cs` - DI registration for batching config
- `mcp_nexus/appsettings.json` - Batching configuration

**Tests:**
- `mcp_nexus_tests/CommandQueue/BatchCommandProcessorTests.cs` - 16 tests
- `mcp_nexus_tests/CommandQueue/BatchCommandBuilderTests.cs` - 8 tests
- `mcp_nexus_tests/CommandQueue/BatchResultParserTests.cs` - 8 tests
- `mcp_nexus_tests/CommandQueue/BatchCommandFilterTests.cs` - 8 tests
- `mcp_nexus_tests/CommandQueue/BatchTimeoutCalculatorTests.cs` - 8 tests

**Total: 48 batching-specific tests**

### Performance Considerations

#### Memory Management
- **Bounded queues**: Maximum batch size prevents unbounded growth
- **Timeout-based flushing**: Commands don't wait indefinitely
- **Per-session isolation**: Each session has its own batch processor
- **Automatic cleanup**: Batch processor disposed with session

#### Concurrency
- **Thread safety**: All batch operations are thread-safe
- **Background processing**: Batching happens on dedicated background task
- **No blocking**: Queue operations don't block command submission
- **Cancellation support**: Proper cancellation token handling

### Batching Behavior Examples

#### Example 1: Simple Batching
```
Time  | Event
------|----------------------------------------------
0ms   | AI queues: "lm"
10ms  | AI queues: "!threads"
20ms  | AI queues: "!peb"
2000ms| Batch timeout reached
      | → Execute batch: "lm", "!threads", "!peb"
      | → Parse results
      | → Store in cache
```

#### Example 2: Immediate Batch (Max Size Reached)
```
Time  | Event
------|----------------------------------------------
0ms   | AI queues 5 commands (all batchable)
1ms   | MaxBatchSize (5) reached
      | → Execute batch immediately
      | → No wait for timeout
```

#### Example 3: Mixed Batchable/Non-Batchable
```
Time  | Event
------|----------------------------------------------
0ms   | AI queues: "lm" (batchable)
10ms  | AI queues: "!analyze -v" (excluded)
      | → "lm" added to batch queue
      | → "!analyze -v" executed immediately (single)
20ms  | AI queues: "!threads" (batchable)
      | → Added to batch queue with "lm"
2000ms| Batch timeout
      | → Execute batch: "lm", "!threads"
```

### Common Pitfalls to Avoid

1. ❌ **Don't add commands to exclusion list without reason**
   - ✅ Only exclude truly long-running or state-changing commands

2. ❌ **Don't set MaxBatchSize too high**
   - ✅ Keep it reasonable (3-10) to avoid timeout issues

3. ❌ **Don't disable batching to "fix" issues**
   - ✅ Investigate root cause and adjust configuration

4. ❌ **Don't assume batching changes command order**
   - ✅ Commands within a batch execute in order

5. ❌ **Don't forget to update BatchingConfiguration when changing appsettings.json**
   - ✅ Keep model in sync with config file

### Future Enhancements (Not Yet Implemented)

- **Dynamic batch sizing**: Adjust based on command execution times
- **Smart command grouping**: Group related commands together
- **Batch statistics**: Track batch efficiency metrics
- **Adaptive exclusion list**: Learn which commands batch poorly

---

## For Future AI Assistants

When working on this project:
1. **Read AGENTS.md first** - Understand the architecture before making changes - ABSOLUTE REQUIREMENT
2. **Follow the established patterns** - Especially for extensions and batching - MANDATORY
3. **PowerShell-only for extension scripts** - No Python - NO EXCEPTIONS
4. **Add tests for new features** - Maintain test coverage - ABSOLUTE REQUIREMENT
5. **Zero-regression policy** - All existing tests must pass - MANDATORY
6. **Update this document** - If architecture changes significantly - NO EXCEPTIONS

### Extension System Key Principle

**Extensions orchestrate workflows externally, using callbacks for individual commands. - ABSOLUTE REQUIREMENT**

This design prevents deadlocks, maintains session isolation, and gives AI agents a reliable way to execute complex debugging workflows. NO EXCEPTIONS.

### Command Batching Key Principle

**Batching improves throughput internally, while being completely transparent to AI clients. - MANDATORY**

This design maintains command dependencies, prevents race conditions, and provides configurable performance optimization without requiring changes to AI integration. ABSOLUTE REQUIREMENT.
