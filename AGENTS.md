# MCP Nexus - AI Agent Instructions

## Project Overview

MCP Nexus is a sophisticated Model Context Protocol (MCP) server designed for Windows crash dump analysis and debugging. Built with a modular architecture, it provides a comprehensive suite of tools for analyzing memory dumps using WinDBG/CDB debugger integration, with advanced features including session management, command queuing, command batching, and extension support.

## Modular Architecture

### Library Structure

The project follows a strict modular architecture with clear separation of concerns:

```
nexus/                      - Main application (entry point, hosting, CLI)
nexus_config/               - Configuration and logging infrastructure
nexus_engine/               - Core debug engine (CDB sessions, command queue)
nexus_engine_batch/         - Command batching system (self-contained)
nexus_protocol/             - MCP protocol layer (tools, resources, HTTP/Stdio)
nexus_setup/                - Service installation and management
nexus_external_apis/            - Shared utilities (file system, process, registry, service)
nexus_extensions/           - Extension system (PowerShell workflow support)
```

### Library Responsibilities

#### `nexus` - Main Application
**Purpose**: Application entry point, hosting, and server mode management.

**Key Components**:
- `Program.cs` - Application entry point
- `CommandLine/` - CLI context and server mode definitions
- `Hosting/` - Hosted services for HTTP, Stdio, and Service modes
- `Startup/` - Application startup orchestration and banner

**Dependencies**: All other libraries (top-level composition root)

**Namespace**: `nexus.*`

#### `nexus_config` - Configuration & Logging
**Purpose**: Centralized configuration loading and logging infrastructure.

**Key Components**:
- `IConfigurationProvider` - Configuration abstraction
- `ILoggingConfigurator` - Logging setup abstraction
- `Internal/ConfigurationLoader.cs` - Configuration file loading
- `Internal/LoggingConfiguration.cs` - NLog configuration
- `Models/SharedConfiguration.cs` - Shared configuration models
- `appsettings.json` - Main configuration file

**Dependencies**: Microsoft.Extensions.Configuration, NLog

**Namespace**: `nexus.config.*`

#### `nexus_engine` - Debug Engine
**Purpose**: Core debugging functionality with CDB session and command queue management.

**Key Components**:
- `DebugEngine.cs` - Main engine implementation (`IDebugEngine`)
- `ICdbSession.cs` - CDB session abstraction
- `Internal/DebugSession.cs` - Session implementation
- `Internal/CdbSession.cs` - CDB process wrapper
- `Internal/CommandQueue.cs` - Asynchronous command queue
- `Internal/QueuedCommand.cs` - Command model
- `Models/` - Command state, session state, command info
- `Configuration/DebugEngineConfiguration.cs` - Engine settings

**Dependencies**: `nexus_external_apis`, `nexus_config`, `nexus_engine_batch`

**Namespace**: `nexus.engine.*`

**Key Design**: The engine always delegates to the batching library for command processing. The batching library decides whether to batch commands or execute them individually based on configuration.

#### `nexus_engine_batch` - Command Batching (NEW)
**Purpose**: Self-contained command batching system for improved throughput.

**Key Components**:
- `IBatchProcessor` - Public batching interface
- `Command.cs` - Command DTO
- `CommandResult.cs` - Result DTO
- `Internal/BatchProcessor.cs` - Batching orchestrator
- `Internal/BatchCommandBuilder.cs` - Batch command construction
- `Internal/BatchResultParser.cs` - Batch output parsing
- `Internal/BatchCommandFilter.cs` - Batching eligibility logic
- `Internal/BatchSentinels.cs` - Sentinel marker constants
- `Configuration/BatchingConfiguration.cs` - Batching settings

**Dependencies**: Microsoft.Extensions.Logging.Abstractions only

**Namespace**: `nexus.engine.batch.*`

**Key Design**: 
- **Self-contained library** - No dependencies on other project libraries
- **Simple public API**: `List<Command> BatchCommands(List<Command>)` and `List<CommandResult> UnbatchResults(List<CommandResult>)`
- **Internal decision-making**: Decides whether to batch based on configuration, command count, and excluded command list
- **Transparent to engine**: Engine treats batched commands as single commands
- **Sentinel-based parsing**: Uses unique markers to split batch results

#### `nexus_protocol` - MCP Protocol Layer
**Purpose**: MCP protocol implementation with tools, resources, and transport (HTTP/Stdio).

**Key Components**:
- `ProtocolServer.cs` - MCP server lifecycle (`IProtocolServer`)
- `Tools/` - MCP tool implementations
  - `OpenSessionTool.cs` - `nexus_open_dump_analyze_session`
  - `EnqueueCommandTool.cs` - `nexus_enqueue_async_dump_analyze_command`
  - `GetCommandsStatusTool.cs` - `nexus_get_dump_analyze_commands_status`
  - `ReadCommandResultTool.cs` - `nexus_read_dump_analyze_command_result`
  - `CancelCommandTool.cs` - `nexus_cancel_dump_analyze_command`
  - `CloseSessionTool.cs` - `nexus_close_dump_analyze_session`
- `Resources/` - MCP resource implementations
- `Configuration/HttpServerSetup.cs` - HTTP server configuration factory
- `Middleware/` - HTTP middleware (logging, content-type validation)
- `Models/` - MCP response models, notifications

**Dependencies**: `nexus_engine`, `nexus_engine_batch`, `nexus_external_apis`, `nexus_config`, ModelContextProtocol SDKs

**Namespace**: `nexus.protocol.*`

**Key Design**: 
- Protocol logic lives entirely in this library
- Provides factory methods for creating configured WebApplication/Host
- Tools are auto-discovered via `McpServerTool` attributes
- Registers `IBatchProcessor` and `IDebugEngine` in DI

#### `nexus_setup` - Service Management
**Purpose**: Windows Service installation, update, and management.

**Key Components**:
- `Core/ServiceInstaller.cs` - Service installation logic
- `Core/ServiceUpdater.cs` - Service update and backup
- `Management/ServiceController.cs` - Service control operations
- `Validation/` - Configuration and path validation
- `Models/` - Service configuration models

**Dependencies**: `nexus_external_apis`, `nexus_config`, `nexus_protocol`

**Namespace**: `nexus.setup.*`

#### `nexus_external_apis` - Shared Utilities
**Purpose**: Cross-cutting utilities for file system, process management, registry, and service operations.

**Key Components**:
- `FileSystem/IFileSystem.cs` - File system abstraction
- `ProcessManagement/IProcessManager.cs` - Process abstraction
- `Registry/IRegistryManager.cs` - Registry abstraction
- `ServiceManagement/IServiceController.cs` - Service control abstraction

**Dependencies**: Microsoft.Extensions.Logging.Abstractions only

**Namespace**: `nexus.external_apis.*`

**Key Design**: All utilities are abstracted behind interfaces for testability

**Testing Policy**: This library is **EXCLUDED from unit tests and coverage requirements**. These are thin wrappers around .NET framework classes (File, Directory, Process, Registry, ServiceController) used solely for dependency injection. They are tested indirectly through other components that mock these interfaces. See "Testing Requirements > Coverage Exclusions" for details.

#### `nexus_extensions` - Extension System
**Purpose**: PowerShell-based extension system for complex debugging workflows.

**Key Components**:
- `ExtensionManager.cs` - Extension discovery and loading (`IExtensionManager`)
- `ExtensionExecutor.cs` - Extension process execution (`IExtensionExecutor`)
- `ExtensionTokenValidator.cs` - Security token management (`IExtensionTokenValidator`)
- `Infrastructure/ProcessWrapper.cs` - Process abstraction
- `Models/` - Extension metadata, parameters, results
- `Configuration/ExtensionConfiguration.cs` - Extension settings

**Dependencies**: `nexus_external_apis`, `nexus_config`

**Namespace**: `nexus.extensions.*`

### Key Design Patterns

- **Dependency Injection**: All libraries use constructor injection for loose coupling
- **Interface Segregation**: Each library exposes minimal public interfaces
- **Self-Contained Libraries**: No cross-usage of internal classes between libraries
- **Single Responsibility**: Each library has one cohesive purpose
- **Factory Pattern**: Protocol library provides configuration factories
- **Abstraction**: All external dependencies (file system, process, etc.) are abstracted

### Service Registration Architecture

**CRITICAL**: Each library must register ONLY its own services. Cross-library service registration violates modular architecture.

#### Common Violations to Avoid

❌ **NEVER do this**:
- Protocol library registering engine services
- Engine library registering protocol services  
- Forwarding configuration objects between libraries
- Forwarding logger factories between libraries
- Cross-library parameter passing

✅ **ALWAYS do this**:
- Each library registers only its own services
- Libraries resolve dependencies from DI container
- Configuration loaded by each library from IConfiguration
- Follow established pattern from nexus_config

### Technology Stack

- **.NET 8**: Primary framework
- **ASP.NET Core**: HTTP server and middleware
- **NLog**: Advanced logging framework
- **System.CommandLine**: CLI argument parsing (planned migration to new CLI library)
- **ModelContextProtocol SDKs**: MCP server implementation
  - `ModelContextProtocol.Server` - Core MCP functionality
  - `ModelContextProtocol.AspNetCore` - ASP.NET Core integration
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
- **Namespace structure**: Library-specific namespaces (e.g., `nexus.engine.*`, `nexus.protocol.*`, `nexus.engine.batch.*`) - MANDATORY
- **Using statements**: Grouped by system, third-party, then project namespaces - NO EXCEPTIONS
- **Member variable placement**: **ALL member variables (fields, properties, constants) MUST be declared at the TOP of the class definition, before any methods or constructors** - ABSOLUTE REQUIREMENT
- **Method ordering**: Constructors, public methods, private methods, dispose pattern - MANDATORY

### Error Handling
- **Exception types**: Use specific exception types (`ArgumentException`, `InvalidOperationException`, etc.) - ABSOLUTE REQUIREMENT
- **Logging levels**: MANDATORY
  - `LogError` for exceptions and failures - NO EXCEPTIONS
  - `LogWarning` for recoverable issues - ABSOLUTE REQUIREMENT
  - `LogInformation` for important events - MANDATORY
  - `LogDebug` for detailed debugging - NO EXCEPTIONS
- **Graceful degradation**: Always provide fallback behavior - ABSOLUTE REQUIREMENT

## Domain Knowledge

### MCP (Model Context Protocol) Integration

The protocol layer (`nexus_protocol`) implements the MCP specification with auto-discovered tools and resources.

**Key MCP Concepts**:
- **Tools**: MCP tools are auto-discovered via `[McpServerTool]` attributes on static methods
- **Resources**: MCP resources provide data access (sessions, commands)
- **Transport**: Supports both HTTP and Stdio transport modes
- **Standardized responses**: Always include `usage` field as last entry

**MCP Tools Provided**:
1. `nexus_open_dump_analyze_session` - Create debugging session with dump file
2. `nexus_enqueue_async_dump_analyze_command` - Queue WinDBG command for execution
3. `nexus_get_dump_analyze_commands_status` - Get status of all commands (bulk polling)
4. `nexus_read_dump_analyze_command_result` - Read individual command result
5. `nexus_cancel_dump_analyze_command` - Cancel queued/executing command
6. `nexus_close_dump_analyze_session` - Close session and cleanup resources

### Windows Debugging with CDB

The engine library (`nexus_engine`) manages CDB (Console Debugger) processes for dump analysis.

**Key Concepts**:
- **One CDB process per session**: Complete isolation between debugging sessions
- **Asynchronous command queue**: Commands execute sequentially per session
- **Symbol path support**: Configure symbol server and local symbol directories
- **Dump file analysis**: Focus on post-mortem debugging of `.dmp` files
- **Command output parsing**: Extract structured information from CDB text output

**Common WinDBG/CDB Commands**:
- `!analyze -v` - Automated crash analysis
- `kL` - Display call stack with source line information
- `lm` - List loaded modules
- `!threads` - Display all threads
- `!peb` - Process Environment Block information
- `lsa <address>` - Load source file for address

### Session Lifecycle

**1. Create Session**
```
Tool: nexus_open_dump_analyze_session
Input: { dumpFilePath, symbolPath }
Output: { sessionId, status }
```

**2. Queue Commands**
```
Tool: nexus_enqueue_async_dump_analyze_command
Input: { sessionId, command, timeoutSeconds }
Output: { commandId, sessionId, status }
```

**3. Monitor Progress (Bulk Polling - Recommended)**
```
Tool: nexus_get_dump_analyze_commands_status
Input: { sessionId }
Output: { commands: [ { commandId, state, queuedTime, startTime, endTime } ] }
```

**4. Retrieve Results**
```
Tool: nexus_read_dump_analyze_command_result
Input: { sessionId, commandId }
Output: { commandId, command, output, success }
```

**5. Close Session**
```
Tool: nexus_close_dump_analyze_session
Input: { sessionId }
Output: { sessionId, closed: true }
```

### Efficient Monitoring Pattern

**Bulk Status Polling (Recommended)**:
```json
// 1. Queue multiple commands
nexus_enqueue_async_dump_analyze_command(sessionId, "!analyze -v")
nexus_enqueue_async_dump_analyze_command(sessionId, "kL")
nexus_enqueue_async_dump_analyze_command(sessionId, "!threads")

// 2. Poll ALL commands at once (efficient)
nexus_get_dump_analyze_commands_status(sessionId)
// Returns: Status of all commands with timing info

// 3. Read individual results when completed
nexus_read_dump_analyze_command_result(sessionId, "cmd-123")
```

**Benefits**:
- Single API call for all command statuses
- Reduced network overhead
- Better performance for monitoring multiple commands
- Unified view of session progress

### Command Batching System

The batching library (`nexus_engine_batch`) transparently improves throughput by grouping commands.

**Key Principles**:
- **Transparent to clients**: AI agents see no difference in behavior
- **Engine-driven**: The engine always calls the batching library for every command set
- **Library decides**: Batching library determines if commands should be batched
- **Sequential execution**: Batched commands execute in order within the batch
- **Individual results**: Each command gets its own result, as if executed separately

**How It Works**:
1. Engine collects available commands from queue
2. Engine passes commands to `IBatchProcessor.BatchCommands()`
3. Batching library decides to batch or return as single commands based on:
   - Batching enabled in configuration
   - Minimum batch size (default: 2)
   - Maximum batch size (default: 5)
   - Excluded commands (e.g., `!analyze`, `!dump`, `!heap`)
4. Engine executes the returned commands (batched or single)
5. Engine passes results to `IBatchProcessor.UnbatchResults()`
6. Batching library extracts individual results using sentinel markers
7. Engine stores individual results in cache

**Configuration** (`appsettings.json`):
```json
{
  "McpNexus": {
    "DebugEngine": {
      "Batching": {
        "Enabled": true,
        "MinBatchSize": 2,
        "MaxBatchSize": 5,
        "ExcludedCommands": [
          "!analyze", "!dump", "!heap", "!memusage", 
          "!runaway", "~*k", "!locks", "!cs", "!gchandles"
        ]
      }
    }
  }
}
```

**Sentinel Markers**:
The batching library uses unique sentinel markers to identify command boundaries:
- `MCP_NEXUS_COMMAND_SEPARATOR_{commandId}_START`
- `MCP_NEXUS_COMMAND_SEPARATOR_{commandId}_END`

## Testing Requirements

### Test File Organization
- **One-to-One Correspondence**: Every test file MUST correspond to exactly one production source file - ABSOLUTE REQUIREMENT
- **Naming Convention**: Test files must follow the pattern `{ProductionFileName}Tests.cs` - MANDATORY
- **No Orphaned Tests**: All tests for a production class must be in the corresponding test file - NO EXCEPTIONS
- **No Generic Test Files**: Test files with generic names (e.g., `UtilityTests.cs`, `HelperTests.cs`) are prohibited - ABSOLUTE REQUIREMENT
- **Consolidation Required**: If multiple test files exist for the same production class, they must be consolidated - MANDATORY

#### Test Structure Requirements
- **Test Project Mirroring**: **Test project structure MUST exactly mirror production code structure**. Test directories and namespaces must follow the same organization as production code. ABSOLUTE REQUIREMENT.
- **Test Namespace Convention**: **Test namespaces must match production namespaces with `.Tests` suffix**. For example: `nexus.engine.batch` → `nexus.engine.batch.Tests`. NO EXCEPTIONS.
- **Test Directory Alignment**: **Test directories must mirror production directories exactly**. If production has `CommandQueue/Batching/`, tests must have `CommandQueue/Batching/` with `.Tests` namespace. MANDATORY.
- **Test Class Organization**: **Test classes must be organized in the same sub-namespaces as their production counterparts**. NO EXCEPTIONS.

### Test Project Structure

The solution contains multiple test projects, each corresponding to a production library:

```
unittests/
  ├── nexus_unittests/              - Tests for nexus (main app)
  ├── nexus_config_unittests/       - Tests for nexus_config
  ├── nexus_engine_unittests/       - Tests for nexus_engine
  ├── nexus_engine_batch_unittests/ - Tests for nexus_engine_batch
  ├── nexus_protocol_unittests/     - Tests for nexus_protocol
  ├── nexus_setup_unittests/        - Tests for nexus_setup
  └── nexus_extensions_unittests/   - Tests for nexus_extensions

Note: nexus_external_apis is EXCLUDED from testing (see Coverage Exclusions).
```

### Unit Test Strategy

**Purpose**: Fast, isolated tests that verify individual component behavior with mocked dependencies.

**Characteristics**:
- **Execution Speed**: Fast (milliseconds to seconds per test) - MANDATORY
- **Dependencies**: All external dependencies mocked (file system, process, network, etc.) - ABSOLUTE REQUIREMENT
- **Scope**: Individual classes, methods, and components in isolation - NO EXCEPTIONS
- **Examples**: Service logic, batching, parsing, validation, state management - MANDATORY
- **Naming Convention**: `{ProductionFileName}Tests.cs` (e.g., `BatchProcessor.cs` → `BatchProcessorTests.cs`) - ABSOLUTE REQUIREMENT
- **Namespace Convention**: Match production namespace with `.Tests` suffix (e.g., `nexus.engine.batch.Tests`) - NO EXCEPTIONS

**When to Write Unit Tests**:
- Testing business logic and algorithms - MANDATORY
- Validating input/output transformations - ABSOLUTE REQUIREMENT
- Testing error handling and edge cases - NO EXCEPTIONS
- Verifying state transitions and lifecycle management - MANDATORY

**Mocking Requirements**:
- Mock all external dependencies (IFileSystem, IProcessManager, etc.) - ABSOLUTE REQUIREMENT
- Use Moq for mocking - MANDATORY
- Ensure `InternalsVisibleTo` includes `DynamicProxyGenAssembly2` for Moq - NO EXCEPTIONS

### Coverage Requirements

**Mandatory Minimum Thresholds** - ABSOLUTE REQUIREMENT:
- **Line Coverage**: Must be **≥75%** at all times. NO EXCEPTIONS.
- **Branch Coverage**: Must be **≥75%** at all times. NO EXCEPTIONS.
- Run `dotnet test --collect:"XPlat Code Coverage"` to verify coverage before submission. MANDATORY.
- If any code change causes coverage to drop below these thresholds, additional tests MUST be added before submission. ABSOLUTE REQUIREMENT.

**Coverage Exclusions** - ABSOLUTE REQUIREMENT:
- **`nexus_external_apis`**: This library is EXCLUDED from unit tests and coverage requirements. NO EXCEPTIONS.
  - Rationale: `nexus_external_apis` contains thin wrappers around .NET framework classes (File, Directory, Process, Registry, ServiceController) that delegate directly to framework APIs with minimal logic.
  - These wrappers exist solely for dependency injection and mocking in other projects' tests.
  - Testing these wrappers would essentially test the .NET framework itself, providing minimal value.
  - The library is thoroughly tested indirectly through integration with other components that mock these interfaces.
- **DO NOT create `nexus_external_apis_unittests`** - MANDATORY
- **DO NOT include `nexus_external_apis` in coverage calculations** - ABSOLUTE REQUIREMENT

**Coverage Best Practices**:
- Aim for 100% coverage where feasible
- Focus on critical paths and error handling
- Test all public APIs thoroughly
- Cover edge cases and boundary conditions

### Shared Ground Rules

**ALL ground rules from the "MANDATORY GROUND RULES" section apply to all tests. NO EXCEPTIONS.**

Specifically:
- **Coverage Thresholds**: All projects must maintain ≥75% line coverage and ≥75% branch coverage - ABSOLUTE REQUIREMENT
- **No Flaky Tests**: Tests must pass consistently, every time - NO EXCEPTIONS
- **Test Isolation**: Tests must be completely isolated from each other - MANDATORY
- **No Arbitrary Delays**: No `Task.Delay` or `Thread.Sleep` >100ms - ABSOLUTE REQUIREMENT
- **Proper Concurrency**: Use deterministic synchronization (TaskCompletionSource, SemaphoreSlim) - NO EXCEPTIONS
- **Mocking**: Mock all external dependencies - MANDATORY
- **Naming Convention**: Use `{ProductionFileName}Tests.cs` pattern - ABSOLUTE REQUIREMENT
- **Namespace Alignment**: Mirror production namespace structure with `.Tests` suffix - NO EXCEPTIONS

## Performance Considerations

### Memory Management
- **Resource cleanup**: Proper disposal of CDB processes and sessions
- **Bounded queues**: Limit queue sizes to prevent memory exhaustion
- **Careful with caching**: Only cache what's necessary

### Concurrency
- **Thread safety**: All public methods must be thread-safe
- **Async patterns**: Use `async/await` for I/O operations
- **Cancellation**: Support `CancellationToken` for long-running operations
- **No blocking**: Avoid `Thread.Sleep` and blocking synchronization primitives

### Scalability
- **Session limits**: Configurable maximum concurrent sessions
- **Command queuing**: Prevents resource exhaustion
- **Batching**: Improves throughput for multiple commands

## Security Considerations

### Path Validation
- **Absolute paths only**: Validate dump file paths are absolute
- **File existence**: Check files exist before creating sessions
- **Symbol path validation**: Validate symbol server URLs and local paths

### Input Validation
- **Command validation**: Basic validation of WinDBG commands
- **Parameter validation**: Check all input parameters
- **Timeout limits**: Enforce maximum timeout values

## Common Patterns and Anti-Patterns

### ✅ Good Patterns
- Use dependency injection for testability
- Implement proper disposal patterns (IDisposable)
- Abstract external dependencies behind interfaces
- Provide comprehensive error messages
- Log important events and errors
- Use factory methods for complex object creation
- Keep libraries self-contained with minimal dependencies

### ❌ Anti-Patterns to Avoid
- Don't expose internal implementation details across libraries
- Don't use generic exception types (use specific exceptions)
- Don't ignore cancellation tokens
- Don't create memory leaks with event handlers
- Don't hardcode configuration values
- Don't add cross-dependencies between peer libraries
- Don't use static mutable state

## Configuration

### Main Configuration File

**Location**: `nexus_config/appsettings.json`

**Key Sections**:
```json
{
  "McpNexus": {
    "Server": {
      "Host": "localhost",
      "Port": 5000
    },
    "DebugEngine": {
      "MaxConcurrentSessions": 10,
      "DefaultCommandTimeoutSeconds": 300,
      "Batching": {
        "Enabled": true,
        "MinBatchSize": 2,
        "MaxBatchSize": 5,
        "ExcludedCommands": [ "!analyze", "!dump", "!heap" ]
      }
    },
    "Extensions": {
      "Enabled": true,
      "ExtensionsPath": "extensions",
      "CallbackPort": 0
    }
  }
}
```

### Logging Configuration

**Location**: `nexus_config/nlog.config`

NLog is used for all logging with consistent formatting across the application.

## Development Workflow

### Before Making Changes
1. Read and understand the existing code structure - MANDATORY
2. Identify which library needs changes - ABSOLUTE REQUIREMENT
3. Check for existing XML documentation - NO EXCEPTIONS
4. Verify test coverage for affected areas - MANDATORY
5. Consider impact on other libraries - ABSOLUTE REQUIREMENT

### During Development
1. Follow the XML documentation standards strictly - ABSOLUTE REQUIREMENT
2. Use appropriate logging levels - MANDATORY
3. Handle exceptions gracefully - NO EXCEPTIONS
4. Consider performance implications - ABSOLUTE REQUIREMENT
5. Maintain thread safety - MANDATORY
6. Keep libraries self-contained - NO EXCEPTIONS

### After Changes
1. Verify all XML documentation is complete - ABSOLUTE REQUIREMENT
2. Run existing tests - MANDATORY
3. Add new tests for new functionality - NO EXCEPTIONS
4. Check for memory leaks - ABSOLUTE REQUIREMENT
5. Run `dotnet format` (style, analyzers, whitespace) solution-wide - MANDATORY
6. Verify zero compilation warnings/errors - ABSOLUTE REQUIREMENT
7. Update version in project file - MANDATORY
8. Update README.md if needed - NO EXCEPTIONS

## Key Architecture Principles

### Library Independence
- **Self-contained**: Each library should be as self-contained as possible
- **Minimal dependencies**: Only reference libraries you directly need
- **No circular dependencies**: Dependency graph must be acyclic
- **Interface-based contracts**: Libraries communicate via interfaces

### Dependency Flow
```
nexus (entry point)
  ↓
nexus_protocol ←→ nexus_setup
  ↓                ↓
nexus_engine   nexus_external_apis
  ↓                ↑
nexus_engine_batch  |
                    |
nexus_config ←──────┘
nexus_extensions ──→ nexus_external_apis
```

### Command Batching Integration

**Critical Flow**:
1. **Engine receives commands** → Collects from queue
2. **Engine calls batching library** → `BatchCommands(commands)`
3. **Batching library decides** → Batch or single based on configuration
4. **Engine executes** → Treats batched commands as single commands
5. **Engine gets results** → Raw output from CDB
6. **Engine calls batching library** → `UnbatchResults(results)`
7. **Batching library parses** → Extracts individual results using sentinels
8. **Engine stores results** → Individual results in cache

**Key Insight**: The engine doesn't know or care if batching happened. It just delegates to the batching library and processes what it gets back.

## Extension System Architecture

The extension system (`nexus_extensions`) allows complex debugging workflows to be implemented as external PowerShell scripts that make callbacks to execute individual commands.

**Key Concepts**:
- **External processes**: Extensions run as separate PowerShell processes
- **HTTP callbacks**: Extensions call back to the server via HTTP REST API
- **Token authentication**: Each extension gets a unique, short-lived security token
- **Localhost-only**: Callback API only listens on 127.0.0.1
- **Automatic cleanup**: Extensions are killed when their session closes

**Extension Discovery**:
- Extensions are stored in `extensions/` directory
- Each extension has `metadata.json` and `<name>.ps1` files
- Auto-discovered at startup
- Can be added dynamically by restarting server

**Helper Module**:
- `extensions/modules/McpNexusExtensions.psm1` provides helper functions
- `Invoke-NexusCommand` - Execute command and wait for result
- `Start-NexusCommand` - Queue command without blocking
- `Wait-NexusCommand` - Wait for queued command to complete
- `Get-NexusCommandResult` - Check command status

## Logging Severity Policy (NO EXCEPTIONS)

The following logging severity levels MUST be used consistently across the entire product. These definitions are binding and there are NO EXCEPTIONS.

- ERROR: Fatal issue from which the product cannot recover.
- WARN: Something unusual happened; the product can handle it autonomously.
- INFO: Normal logs with interesting and helpful information for end users.
- DEBUG: Normal logs with useful technical information for product developers only.
- TRACE: Same as DEBUG but reserved for very high-frequency logs (more than once per second).

Enforcement:
- Choose the lowest severity that accurately conveys the situation per the definitions above.
- Do not use ERROR for recoverable or expected conditions.
- Prefer INFO for user-relevant milestones and state changes; prefer DEBUG/TRACE for internal details.
- TRACE should be disabled by default in production configurations.

---

## For Future AI Assistants

When working on this project:

1. **Read this document first** - Understand the modular architecture before making changes - ABSOLUTE REQUIREMENT
2. **Identify the correct library** - Changes should be made in the appropriate library - MANDATORY
3. **Follow all ground rules** - Every rule is mandatory with NO EXCEPTIONS - ABSOLUTE REQUIREMENT
4. **Maintain library independence** - Don't add cross-dependencies between peer libraries - NO EXCEPTIONS
5. **Test thoroughly** - All tests must pass with ≥75% coverage - MANDATORY
6. **Document everything** - Complete XML documentation for all public and private members - ABSOLUTE REQUIREMENT
7. **Run dotnet format** - Format the entire solution before submission - MANDATORY
8. **Update versions** - Increment build version in project file - ABSOLUTE REQUIREMENT

### Modular Architecture Summary

- **nexus**: Entry point and hosting
- **nexus_config**: Configuration and logging
- **nexus_engine**: Core debug engine
- **nexus_engine_batch**: Command batching (self-contained)
- **nexus_protocol**: MCP protocol layer
- **nexus_setup**: Service management
- **nexus_external_apis**: Shared utilities (EXCLUDED from testing - see Coverage Exclusions)
- **nexus_extensions**: Extension system

Each library is self-contained with minimal dependencies. The batching library is completely independent and the engine always delegates to it for command processing.

**Note**: `nexus_external_apis` is excluded from unit testing and coverage requirements as it contains only thin wrappers around .NET framework APIs used for dependency injection.

---

This project represents a sophisticated, modular debugging platform that requires careful attention to architecture, documentation, testing, and reliability. Every change should maintain the high standards established for code quality and system reliability. ALL REQUIREMENTS ARE ABSOLUTE - NO EXCEPTIONS ARE ALLOWED.
