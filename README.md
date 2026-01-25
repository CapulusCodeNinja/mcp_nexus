# WinAiDbg

**AI-Powered Windows Crash Dump Analysis Platform**

![WinAiDbg Icon](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/winaidbg_small.png?raw=true)

![Tests](https://img.shields.io/badge/tests-1265%20passing-brightgreen)
![Coverage](https://img.shields.io/badge/coverage-85.7%25%20lines-green)
![Build](https://img.shields.io/badge/build-passing-brightgreen)
![License](https://img.shields.io/badge/license-Apache%202.0-blue)

WinAiDbg is a comprehensive Model Context Protocol (MCP) server that provides AI systems with advanced Windows crash dump analysis capabilities. It combines the power of Microsoft's debugging tools (WinDBG/CDB) with intelligent analysis workflows, making professional-grade crash investigation accessible to AI assistants.

## What is WinAiDbg?

WinAiDbg is a platform that provides structured access to Windows debugging tools through the Model Context Protocol. It makes crash dump analysis more accessible by providing standardized tools and real-time progress tracking.

### The Problem We Solve

**Traditional Crash Analysis Challenges:**
- **Complexity**: Requires deep knowledge of Windows internals and debugging tools
- **Time-consuming**: Manual analysis can take hours or days
- **Expertise Required**: Need specialized debugging skills and experience
- **Inconsistent Results**: Different analysts may reach different conclusions
- **Tool Complexity**: WinDBG/CDB have steep learning curves

**WinAiDbg Solution:**
- **Structured Access**: Provides standardized tools for debugging operations
- **Real-time Updates**: Live progress tracking and notifications
- **Consistent Results**: Provides structured output formats
- **MCP Integration**: Works with AI clients through Model Context Protocol
- **Professional Tools**: Built on Microsoft's industry-standard debugging infrastructure

## Key Features

- **Advanced Crash Analysis**: Leverage WinDBG/CDB for comprehensive dump analysis
- **AI-Native Design**: Built specifically for AI agent integration via MCP
- **Command Batching**: Intelligent command grouping for improved throughput
- **Real-time Notifications**: Live updates during analysis operations
- **Session Management**: Robust session lifecycle with automatic cleanup
- **Extensible Architecture**: PowerShell-based extension system for custom workflows
- **Structured Results**: Parse debugging output into AI-friendly formats

## Quick Start

### Prerequisites

- **.NET 8.0 Runtime** or SDK
- **Windows Debugging Tools** (WinDBG/CDB) - [Download from Microsoft](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools)

### Installation

```bash
# Clone the repository
git clone https://github.com/CapulusCodeNinja/mcp-win-ai-dbg.git
cd mcp-win-ai-dbg

# Build the project
dotnet build

# Run the server
dotnet run --project winaidbg/winaidbg.csproj
```

## AI Integration

In this chapter, we provide example configurations for integrating WinAiDbg with popular AI development environments using both STDIO and HTTP protocols.
In similar fashion, you can adapt these configurations for other MCP-compatible clients.

### Cursor IDE Integration

#### STDIO Integration

![Cursor Stdio](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/integrations/Cursor_stdio.gif?raw=true)

```json
{
  "mcpServers": {
    "winaidbg": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Sources\\Github\\CapulusCodeNinja\\mcp-win-ai-dbg\\winaidbg\\winaidbg.csproj",
        "--",
        "--stdio"
      ]
    }
  }
}
```

#### HTTP Integration

![Cursor Http](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/integrations/Cursor_http.gif?raw=true)

```json
{
  "mcpServers": {
    "winaidbg": {
      "url": "http://0.0.0.0:5511/",
      "headers": {
        "Content-Type": "application/json"
      }
    }
  }
}
```

### Google Antigravity Integration

#### STDIO Integration

![Antigravity Stdio](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/integrations/Antigravity_stdio.gif?raw=true)

```json
{
    "mcpServers": {
        "winaidbg": {
            "command": "C:\\Program Files\\dotnet\\dotnet.exe",
            "args": [
                "run",
                "--project",
                "C:\\Sources\\Github\\CapulusCodeNinja\\mcp-win-ai-dbg\\winaidbg\\winaidbg.csproj",
                "--",
                "--stdio"
            ]
        }
    },
    "inputs": []
}
```

#### HTTP Integration

![Antigravity Http](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/integrations/Antigravity_http.gif?raw=true)

```json
{
    "mcpServers": {
        "winaidbg": {
            "serverUrl": "http://0.0.0.0:5511/",
            "headers": {
                "Content-Type": "application/json"
            }
        }
    }
}
```

### Visual Studio Code Integration

#### STDIO Integration

![VSCode Stdio](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/integrations/VSCode_stdio.gif?raw=true)

```json
{
	"servers": {
		"winaidbg": {
			"type": "stdio",
			"command": "dotnet",
			"args": [
				"run",
				"--project",
				"C:\\Sources\\Github\\CapulusCodeNinja\\mcp-win-ai-dbg\\winaidbg\\winaidbg.csproj",
				"--",
				"--stdio"
			]
		}
	}
}
```

#### HTTP Integration

![VSCode Http](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/integrations/VSCode_http.gif?raw=true)

```json
{
	"servers": {
		"winaidbg": {
			"url": "http://0.0.0.0:5511/",
			"headers": {
				"Content-Type": "application/json"
			}
		}
	}
}
```

## Available MCP Tools

### Core Analysis Tools

- **`winaidbg_open_dump_analyze_session`** - Open a crash dump for analysis
- **`winaidbg_enqueue_async_dump_analyze_command`** - Execute WinDBG commands asynchronously
- **`winaidbg_get_dump_analyze_commands_status`** - Get status of all commands (bulk polling)
- **`winaidbg_read_dump_analyze_command_result`** - Read individual command results
- **`winaidbg_cancel_command`** - Cancel a running command
- **`winaidbg_close_dump_analyze_session`** - Close session and cleanup resources

### MCP Resources

- **`sessions`** - List all active debugging sessions
- **`commands`** - List commands with filtering by session/state
- **`extensions`** - List available PowerShell extension scripts
- **`usage`** - Complete usage guide with examples
- **`health`** - System health and metrics

## Example Workflow

**Typical AI-driven crash analysis workflow:**

1. **Open Session**: `winaidbg_open_dump_analyze_session` with dump file path
2. **Queue Commands**: Use `winaidbg_enqueue_async_dump_analyze_command` for:
   - `!analyze -v` (automatic crash analysis)
   - `kL` (stack trace with source lines)
   - `!threads` (thread information)
3. **Monitor Progress**: `winaidbg_get_dump_analyze_commands_status` (bulk polling)
4. **Retrieve Results**: `winaidbg_read_dump_analyze_command_result` for each command
5. **Close Session**: `winaidbg_close_dump_analyze_session` for cleanup

**Example conversation with AI assistant:**

```
User: "Analyze the crash dump at C:\dumps\myapp.dmp"

AI: I'll analyze that crash dump for you.
    [Opens session, queues !analyze -v, kL, !threads commands]
    
    Analysis Results:
    - Exception: Access Violation (0xC0000005)
    - Faulting Module: myapp.exe
    - Root Cause: Null pointer dereference in ProcessData()
    - Stack shows: ProcessData+0x42 called with null buffer
    
    Recommendation: Add null check before buffer access.
```

## Architecture

### Modular Structure

```
winaidbg/                      - Main application (entry point, hosting)
winaidbg_config/               - Configuration and logging
winaidbg_engine/               - Debug engine (CDB sessions, command queue)
winaidbg_engine_batch/         - Command batching system
winaidbg_protocol/             - MCP protocol layer (tools, resources)
winaidbg_setup/                - Service installation and management
winaidbg_external_apis/        - Shared utilities (file system, process, etc.)
winaidbg_extensions/           - PowerShell extension system
```

### Key Design Patterns

- **Modular Architecture**: Clear separation of concerns across libraries
- **Singleton Pattern**: Core engine accessible without DI overhead
- **Command Queue**: Asynchronous command processing with state management
- **Batching**: Transparent command grouping for improved performance
- **Event-Driven**: Real-time notifications for command state changes

## Testing & Quality

### Test Statistics

- **1265 total tests** (all passing)
- **85.7% line coverage** (target: 75%)
- **75.6% branch coverage** (target: 75%)
- **Fast execution** (~16 seconds for full suite)
- **Zero build warnings**

## Configuration

### Command Batching

WinAiDbg intelligently batches commands for improved throughput:

```json
{
  "WinAiDbg": {
    "DebugEngine": {
      "Batching": {
        "Enabled": true,
        "MinBatchSize": 2,
        "MaxBatchSize": 5,
        "ExcludedCommands": [
          "!analyze", "!dump", "!heap", "!memusage"
        ]
      }
    }
  }
}
```

### Session Management

```json
{
  "WinAiDbg": {
    "SessionManagement": {
      "MaxConcurrentSessions": 10,
      "SessionTimeoutMinutes": 30,
      "CleanupIntervalSeconds": 300,
      "DefaultCommandTimeoutMinutes": 10,
      "DeleteDumpFileOnSessionClose": false
    }
  }
}
```

### Logging

```json
{
  "Logging": {
    "LogLevel": "Information"
  }
}
```

**Supported levels**: Trace, Debug, Information, Warning, Error, Critical

## Advanced Features

### Extension System

Create custom analysis workflows with PowerShell:

```powershell
# extensions/my-analysis/my-analysis.ps1
Import-Module WinAiDbgExtensions

$result1 = Invoke-WinAiDbgCommand -Command "!analyze -v"
$result2 = Invoke-WinAiDbgCommand -Command "kL"

# Process results and return structured data
return @{
    CrashType = "Access Violation"
    RootCause = "Null pointer dereference"
    Recommendations = @("Add null checks", "Review error handling")
}
```

### Real-time Notifications

Receive live updates during analysis:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/commandStatus",
  "params": {
    "commandId": "cmd-abc123",
    "sessionId": "session-xyz789",
    "state": "Executing",
    "timestamp": "2025-01-15T10:30:00Z"
  }
}
```

## Service Mode

Run as a Windows Service for production environments:

```bash
# Install as service
dotnet run --project winaidbg/winaidbg.csproj -- service install

# Start service
net start WinAiDbg

# Stop service
net stop WinAiDbg

# Uninstall service
dotnet run --project winaidbg/winaidbg.csproj -- service uninstall
```

## License

This project is licensed under the **Apache License 2.0** - see the [LICENSE](LICENSE) file for details.

The Apache License 2.0 allows you to:
- Use the software commercially
- Modify and distribute
- Sublicense
- Use patent claims
- Include copyright notice

## Acknowledgments

- [Model Context Protocol](https://modelcontextprotocol.io/) - MCP specification
- [C# SDK for MCP](https://github.com/modelcontextprotocol/csharp-sdk) - MCP implementation
- [Microsoft Debugging Tools](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/) - WinDBG/CDB
- [NLog](https://nlog-project.org/) - Logging framework
