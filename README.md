# Nexus Model Context Protocol (MCP) server

A Model Context Protocol (MCP) server that provides comprehensive Windows debugging capabilities.

## Features

### WinDBG Tool
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
- **Configurable CDB Path**: Multiple methods to specify custom CDB.exe locations (constructor, config, environment)
- **Automatic CDB Detection**: Automatically finds CDB.exe in common Windows Debugging Tools locations as fallback
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

## Integration with Cursor

### Prerequisites
- Cursor IDE installed
- Windows Debugging Tools installed
- .NET 8.0 or later

### Step 1: Configure MCP in Cursor

1. **Open Cursor Settings**:
   - Press `Ctrl+,` (or `Cmd+,` on Mac) to open settings
   - Search for "MCP" in the settings search bar

2. **Enable MCP**:
   - Find "Model Context Protocol" settings
   - Enable MCP support if not already enabled

### Step 2: Add the MCP Server

1. **Open MCP Configuration**:
   - Go to Cursor Settings → Extensions → Model Context Protocol
   - Click "Add MCP Server" or "Configure MCP Servers"

2. **Add Server Configuration**:
   ```json
   {
     "mcpServers": {
       "mcp-nexus": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "C:\\Sources\\Github\\CapulusCodeNinja\\mcp_nexus\\mcp_nexus\\mcp_nexus.csproj"
         ],
         "cwd": "C:\\Sources\\Github\\CapulusCodeNinja\\mcp_nexus\\mcp_nexus"
       }
     }
   }
   ```

3. **Update Paths**:
   - Replace the paths with your actual project location
   - Ensure the path points to your `mcp_nexus.csproj` file

### Step 3: Alternative Configuration Methods

#### Method 1: Global Configuration
Add to your global Cursor settings:

```json
{
  "mcp.servers": {
    "mcp-nexus": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\your\\mcp_nexus\\mcp_nexus\\mcp_nexus.csproj"],
      "type": "stdio"
    }
  }
}
```

#### Method 2: Workspace Configuration
Create a `.cursor/mcp.json` file in your workspace root:

```json
{
  "servers": {
    "mcp-nexus": {
      "command": "dotnet",
      "args": ["run", "--project", "./mcp_nexus/mcp_nexus.csproj"],
      "type": "stdio"
    }
  }
}
```

### Step 4: Verify Integration

1. **Restart Cursor**: Close and reopen Cursor to load the new MCP configuration

2. **Check MCP Status**:
   - Look for MCP indicators in the Cursor interface
   - Check the MCP panel or settings to see if the server is connected

3. **Test Tools**:
   - Open a chat or AI conversation in Cursor
   - Try asking: "List the available MCP tools" or "Show me WinDBG tools"
   - The AI should be able to access and use the WinDBG debugging tools

### Step 5: Using the WinDBG Tools

Once integrated, you can use the tools through natural language in Cursor:

```
# Example queries:
"Open a crash dump file at C:\dumps\crash.dmp"
"List all dump files in C:\dumps directory"
"Connect to a remote debugging session at tcp:Port=5005,Server=192.168.0.100"
"Analyze the call stack of the current debugging session"
"Check for common crash patterns in the loaded dump"
```

### Troubleshooting

#### Server Not Connecting
- **Check Paths**: Ensure all file paths in the configuration are correct
- **Build First**: Run `dotnet build` in the project directory before starting Cursor
- **Check Logs**: Look at Cursor's developer console for MCP connection errors

#### Tools Not Available
- **Restart Cursor**: Sometimes a restart is needed after configuration changes
- **Check MCP Status**: Verify the server shows as connected in MCP settings
- **Test Manually**: Try running `dotnet run` in the project directory to ensure it starts correctly

#### Permission Issues
- **Run as Administrator**: If debugging system processes, Cursor may need elevated privileges
- **Check CDB Path**: Ensure Windows Debugging Tools are properly installed and CDB.exe is accessible

#### CDB Path Configuration Issues
- **Custom Path Not Found**: Verify the custom CDB path passed via `--cdb-path` exists and points to a valid executable

### Advanced Configuration

#### CDB Path Configuration

The MCP server supports two methods to configure the CDB.exe path:

**1. Command Line Parameter (Recommended)**
Use the `--cdb-path` parameter when starting the server:
```json
{
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "C:\\path\\to\\mcp_nexus\\mcp_nexus.csproj",
    "--",
    "--cdb-path",
    "C:\\Program Files\\Windows Kits\\10\\Debuggers\\x64\\cdb.exe"
  ]
}
```

**2. Automatic Detection (Default)**
If no `--cdb-path` is specified, the server automatically searches standard Windows Debugging Tools installation paths and system PATH.

#### Custom Symbol Path
You can configure symbol paths by modifying the server arguments:

```json
{
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "C:\\path\\to\\mcp_nexus\\mcp_nexus.csproj",
    "--",
    "--cdb-path",
    "C:\\MyTools\\cdb.exe",
    "--symbols-path",
    "SRV*C:\\Symbols*https://msdl.microsoft.com/download/symbols"
  ]
}
```

#### Debug Mode
Enable verbose logging for troubleshooting:

```json
{
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "C:\\path\\to\\mcp_nexus\\mcp_nexus.csproj",
    "--",
    "--cdb-path",
    "C:\\MyTools\\cdb.exe",
    "--verbose"
  ]
}
```

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
