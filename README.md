# mcp_nexus

A Model Context Protocol (MCP) server that provides comprehensive Windows debugging capabilities, replicating the functionality of the [mcp-windbg](https://github.com/CapulusCodeNinja/mcp-windbg) Python implementation in C#.

## Features

### WinDBG Tool - Complete mcp-windbg Replication
The WinDBG Tool provides comprehensive debugging capabilities through CDB (Console Debugger) integration, replicating all functionality from the original Python mcp-windbg project:

#### Crash Dump Analysis
- **OpenWindbgDump**: Analyze Windows crash dump files using common WinDBG commands
- **CloseWindbgDump**: Unload crash dumps and release resources

#### Remote Debugging
- **OpenWindbgRemote**: Connect to remote debugging sessions using connection strings (e.g., `tcp:Port=5005,Server=192.168.0.100`)
- **CloseWindbgRemote**: Disconnect from remote debugging sessions and release resources

#### General Commands
- **RunWindbgCmd**: Execute specific WinDBG commands on loaded crash dumps or active remote sessions
- **ListWindbgDumps**: List Windows crash dump (.dmp) files in specified directories

#### Advanced Analysis Tools
- **GetSessionInfo**: Get basic information about the current debugging session
- **AnalyzeCallStack**: Analyze the current call stack with detailed information
- **AnalyzeMemory**: Get memory information and analyze memory usage
- **AnalyzeCrashPatterns**: Check for common crash patterns and provide automated analysis

### CDB Session Management
- **CdbSession**: Core class managing CDB process lifecycle, command execution, and output parsing
- **Automatic CDB Detection**: Automatically finds CDB.exe in common Windows Debugging Tools locations
- **Thread-Safe Operations**: All operations are thread-safe with proper locking
- **Resource Management**: Proper cleanup and disposal of debugger processes

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

The MCP server exposes various tools that can be used by MCP clients. The WinDBG tool provides debugging capabilities identical to the Python `mcp-windbg` implementation, allowing you to:

1. **Analyze crash dumps**: Open and analyze Windows crash dump files
2. **Remote debugging**: Connect to live debugging sessions
3. **Execute commands**: Run arbitrary WinDBG commands programmatically
4. **Automated analysis**: Use built-in analysis tools for common debugging scenarios
5. **Pattern detection**: Automatically detect common crash patterns and issues

## Tools Available

### Crash Dump Analysis
- `OpenWindbgDump` - Analyze a Windows crash dump file
- `CloseWindbgDump` - Unload a crash dump and release resources

### Remote Debugging
- `OpenWindbgRemote` - Connect to a remote debugging session
- `CloseWindbgRemote` - Disconnect from a remote debugging session

### General Commands
- `RunWindbgCmd` - Execute a specific WinDBG command
- `ListWindbgDumps` - List Windows crash dump files in a directory

### Analysis Tools
- `GetSessionInfo` - Get basic debugging session information
- `AnalyzeCallStack` - Analyze call stack with detailed information
- `AnalyzeMemory` - Analyze memory usage and information
- `AnalyzeCrashPatterns` - Check for common crash patterns

### Time Tools
- `GetCurrentTime` - Get current time for a city (legacy method)

## Architecture

The implementation consists of two main classes:

1. **CdbSession**: Manages the CDB process lifecycle, command execution, and output parsing
2. **WindbgTool**: Provides MCP tool methods that use CdbSession for debugging operations

This architecture mirrors the Python implementation's structure while providing the benefits of C#'s type safety and performance.

## Example Usage

```csharp
// The tools are automatically registered and available through MCP clients
// Example: Open a crash dump for analysis
var result = await windbgTool.OpenWindbgDump("C:\\dumps\\crash.dmp");

// Execute a WinDBG command
var callStack = await windbgTool.RunWindbgCmd("k");

// Analyze crash patterns
var analysis = await windbgTool.AnalyzeCrashPatterns();
```