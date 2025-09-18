# mcp_nexus

A Model Context Protocol (MCP) server that provides various tools for debugging and system analysis, including Windows Debugger (WinDbg) integration.

## Features

### WinDBG Tool
The WinDBG Tool provides comprehensive debugging capabilities through CDB (Console Debugger) integration:

- **Session Management**: Start and stop debugging sessions with target processes or dump files
- **Command Execution**: Execute arbitrary debugger commands and retrieve output
- **Process Analysis**: Get detailed information about the current process being debugged
- **Module Management**: List all loaded modules in the current process
- **Thread Analysis**: Get current thread information and call stacks
- **Memory Operations**: Read memory at specified addresses
- **Breakpoint Management**: Set, list, and clear breakpoints
- **Execution Control**: Continue execution, step into, and step over instructions
- **Register Inspection**: Get current register values

### Time Tool
Legacy time functionality for getting current time in different cities.

## Prerequisites

- Windows Debugging Tools (for WinDBG functionality)
- .NET 8.0 or later

## Installation

1. Clone the repository
2. Install Windows Debugging Tools from Microsoft
3. Build the project: `dotnet build`
4. Run the server: `dotnet run`

## Usage

The MCP server exposes various tools that can be used by MCP clients. The WinDBG tool provides debugging capabilities similar to the Python `cdb_session.py` implementation, allowing you to:

1. Start a debugging session with a target process or dump file
2. Execute debugger commands programmatically
3. Analyze processes, threads, memory, and execution state
4. Manage breakpoints and control execution flow

## Tools Available

### WinDBG Tools
- `StartDebugSession` - Start a CDB debugging session
- `ExecuteCommand` - Execute a debugger command
- `StopDebugSession` - Stop the current debug session
- `GetProcessInfo` - Get process information
- `ListModules` - List loaded modules
- `GetThreadInfo` - Get thread information
- `GetCallStack` - Get call stack
- `GetRegisters` - Get register values
- `ReadMemory` - Read memory at address
- `SetBreakpoint` - Set a breakpoint
- `ListBreakpoints` - List all breakpoints
- `ClearBreakpoint` - Clear a breakpoint
- `ContinueExecution` - Continue execution
- `StepInto` - Step into next instruction
- `StepOver` - Step over next instruction

### Time Tools
- `GetCurrentTime` - Get current time for a city