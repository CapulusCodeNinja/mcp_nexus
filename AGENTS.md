# WinAiDbg - AI Agent Instructions

## Project Overview

WinAiDbg is a sophisticated Model Context Protocol (MCP) server designed for Windows crash dump analysis and debugging. Built with a modular architecture, it provides a comprehensive suite of tools for analyzing memory dumps using WinDBG/CDB debugger integration, with advanced features including session management, command queuing, command batching, and extension support.

## Modular Architecture

### Library Structure

The project follows a strict modular architecture with clear separation of concerns:

```
winaidbg/                      - Main application (entry point, hosting, CLI)
winaidbg_config/               - Configuration and logging infrastructure
winaidbg_engine/               - Core debug engine + subcomponents (see below)
  winaidbg_engine/             - Core engine (CDB sessions, command queue)
  winaidbg_engine_batch/       - Command batching system (self-contained)
  winaidbg_engine_dump_check/  - Dump validation (dumpchk integration)
  winaidbg_engine_extensions/  - PowerShell-based extension system
  winaidbg_engine_share/       - Shared engine primitives (IDs, models, utilities)
winaidbg_protocol/             - MCP protocol layer (tools, resources, HTTP/Stdio)
winaidbg_setup/                - Service installation and management
winaidbg_external_apis/        - Shared utilities (file system, process, registry, service)
winaidbg_web/                  - Static admin UI and docs
unittests/                     - Unit tests (mirrors production structure)
```

### Library Responsibilities

#### `winaidbg` - Main Application
**Purpose**: Application entry point, hosting, and server mode management.

#### `winaidbg_config` - Configuration & Logging
**Purpose**: Centralized configuration loading and logging infrastructure.

#### `winaidbg_engine` - Debug Engine
**Purpose**: Core debugging functionality with CDB session and command queue management.

#### `winaidbg_engine/winaidbg_engine_batch` - Command Batching (NEW)
**Purpose**: Self-contained command batching system for improved throughput.

#### `winaidbg_protocol` - MCP Protocol Layer
**Purpose**: MCP protocol implementation with tools, resources, and transport (HTTP/Stdio).

#### `winaidbg_setup` - Service Management
**Purpose**: Windows Service installation, update, and management.

#### `winaidbg_external_apis` - Shared Utilities
**Purpose**: Cross-cutting utilities for file system, process management, registry, and service operations.

#### `winaidbg_engine/winaidbg_engine_extensions` - Extension System
**Purpose**: PowerShell-based extension system for complex debugging workflows.

### Key Design Patterns

- **Dependency Injection**: All libraries use constructor injection for loose coupling
- **Interface Segregation**: Each library exposes minimal public interfaces
- **Self-Contained Libraries**: No cross-usage of internal classes between libraries
- **Single Responsibility**: Each library has one cohesive purpose
- **Abstraction**: All external dependencies (file system, process, etc.) are abstracted

### Technology Stack

- **.NET 8**: Primary framework
- **ASP.NET Core**: HTTP server and middleware
- **NLog**: Advanced logging framework
- **System.CommandLine**: CLI argument parsing (planned migration to new CLI library)
- **ModelContextProtocol SDKs**: MCP server implementation
  - `ModelContextProtocol.Server` - Core MCP functionality
  - `ModelContextProtocol.AspNetCore` - ASP.NET Core integration
- **WinDBG/CDB**: Windows debugging tools integration

## Documentation as Source of Truth

- The `documentation/` folder contains user-facing reference material for **configuration**, **integrations**, and **features**.
- AI agents should treat these docs as a primary source of product behavior **only when they match the current implementation**.
- **When code/config changes affect behavior**, agents must update the relevant `documentation/**/*.md` files in the same change so the docs stay accurate and current.

## 🚨 CRITICAL: MANDATORY GROUND RULES - READ FIRST! 🚨

**⚠️ THESE RULES ARE NON-NEGOTIABLE AND MUST BE FOLLOWED FOR EVERY CODE CHANGE ⚠️**

**❌ VIOLATIONS WILL RESULT IN IMMEDIATE REJECTION ❌**

**🚫 NO EXCEPTIONS ARE ALLOWED - ALL RULES ARE ABSOLUTE 🚫**

### 🔥 IMMEDIATE REQUIREMENTS (Check These FIRST!)

1. **✅ ALL TESTS MUST PASS**: Run `dotnet test` - ALL tests must be green before any submission. NO EXCEPTIONS.
2. **✅ VERSION MUST BE UPDATED**: Increment the build version in `Directory.Build.props` (e.g., 1.1.6.12 → 1.1.6.13 via `VersionSuffix`). MANDATORY.
   - The following version fields MUST exist and MUST have the same value: `Version`, `AssemblyVersion`, and `FileVersion`. Keep all three identical. NO EXCEPTIONS.
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
* **Version Field Equality:** In `Directory.Build.props`, the `Version`, `AssemblyVersion`, and `FileVersion` values MUST be identical at all times. When bumping the version, keep all three fields the same value. ABSOLUTE REQUIREMENT.

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
- Update version in `Directory.Build.props` - MANDATORY
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
- **Namespace structure**: Library-specific namespaces (e.g., `WinAiDbg.Engine.*`, `WinAiDbg.Protocol.*`, `WinAiDbg.Engine.Batch.*`) - MANDATORY
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
