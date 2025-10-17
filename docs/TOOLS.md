# Available Tools

**Windows Crash Dump Analysis Tools**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📚 Resources](RESOURCES.md) | [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## 🛠️ Tool Categories

### Core Analysis Tools
- **Session Management**: Open, close, and manage crash dump analysis sessions
- **Command Execution**: Execute debugging commands with real-time progress tracking
- **Result Retrieval**: Get analysis results and command outputs
- **Session Monitoring**: Track active sessions and command status

### Advanced Analysis Tools
- **Remote Debugging**: Start and stop remote debugging sessions
- **Command Management**: Cancel running commands and list available commands
- **Resource Management**: Monitor system resources and cleanup

### MCP Resources
- **Session Data**: List and query active analysis sessions
- **Command Data**: Access command results and status information
- **Documentation**: Access analysis workflows and usage guides

## 🔍 Core Analysis Tools

### Session Management

#### `nexus_open_dump_analyze_session`
**Purpose**: Open a crash dump file for analysis
**Category**: Session Management
**Real-time**: ✅ Provides live progress updates

**Parameters**:
- `dumpPath` (string, required): Path to the crash dump file (.dmp, .mdmp, etc.)
- `symbolsPath` (string, optional): Custom symbol search path
- `timeoutMinutes` (number, optional): Session timeout in minutes (default: 30)

**Example**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crashes\\application.dmp",
      "symbolsPath": "C:\\Symbols",
      "timeoutMinutes": 60
    }
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n  \"dumpFile\": \"application.dmp\",\n  \"success\": true,\n  \"message\": \"Session created successfully\",\n  \"symbolsLoaded\": true,\n  \"dumpSize\": \"256MB\",\n  \"createdAt\": \"2024-01-15T10:00:00Z\"\n}"
      }
    ]
  }
}
```

#### `nexus_close_dump_analyze_session`
**Purpose**: Close an analysis session and cleanup resources
**Category**: Session Management
**Real-time**: ✅ Provides cleanup progress updates

**Parameters**:
- `sessionId` (string, required): Session ID to close

**Example**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_close_dump_analyze_session",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001"
    }
  }
}
```

### Command Execution

#### `nexus_enqueue_async_dump_analyze_command`
**Purpose**: Execute debugging commands asynchronously with progress tracking
**Category**: Command Execution
**Real-time**: ✅ Provides live progress updates and notifications

**Parameters**:
- `sessionId` (string, required): Active session ID
- `command` (string, required): WinDBG/CDB command to execute
- `timeoutMinutes` (number, optional): Command timeout in minutes (default: 10)

**Example**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!analyze -v",
      "timeoutMinutes": 15
    }
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n  \"commandId\": \"cmd-000001-abc12345-12345678-0001\",\n  \"success\": true,\n  \"message\": \"Command queued successfully\",\n  \"status\": \"queued\",\n  \"createdAt\": \"2024-01-15T10:00:00Z\"\n}"
      }
    ]
  }
}
```

#### `nexus_read_dump_analyze_command_result`
**Purpose**: Get the result of a completed command or extension
**Category**: Command Execution
**Real-time**: ❌ Synchronous operation

**Parameters**:
- `sessionId` (string, required): Active session ID
- `commandId` (string, required): Command ID or extension command ID (prefixed with "ext-")

**Example**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_read_dump_analyze_command_result",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "commandId": "cmd-000001-abc12345-12345678-0001"
    }
  }
}
```

**Note**: This tool also retrieves results from extension commands (commandId starting with "ext-").

#### `nexus_get_dump_analyze_commands_status`
**Purpose**: Get status of ALL commands for a specific session
**Category**: Command Monitoring
**Real-time**: Yes - Provides current status of all commands

**Description**: Returns status of ALL commands in a session, not just one command. This is the recommended tool for polling command progress. You can queue multiple commands and then call this tool once to check status of all queued commands at once, then use nexus_read_dump_analyze_command_result to get specific command results when completed.

**Parameters**:
- `sessionId` (string, required): Active session ID

**Example**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_get_dump_analyze_commands_status",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001"
    }
  }
}
```

**Response**:
```json
{
  "sessionId": "sess-000001-abc12345-12345678-0001",
  "commands": {
    "cmd-000001": {
      "commandId": "cmd-000001",
      "command": "!analyze -v",
      "status": "Completed",
      "isFinished": true,
      "eta": null,
      "elapsed": "2min 15s"
    },
    "cmd-000002": {
      "commandId": "cmd-000002",
      "command": "lm",
      "status": "Executing",
      "isFinished": false,
      "eta": "30s",
      "elapsed": "5s"
    }
  },
  "commandCount": 2,
  "timestamp": "2024-01-15T10:00:00Z",
  "note": "All commands for this session. Poll this tool to monitor command progress."
}
```

**Key Points**:
- Returns ALL commands for the session (not just one)
- Use this for polling command status
- AI clients should call this once per session to get all command statuses
- Contains timing information (eta, elapsed) to help AI decide when to poll again
- When isFinished is true, use nexus_read_dump_analyze_command_result to get the actual output

### Extension Execution

#### `nexus_enqueue_async_extension_command`
**Purpose**: Queue an extension script for complex multi-command workflows
**Category**: Extension Execution
**Real-time**: ✅ Provides live progress updates

**Description**: Extensions are PowerShell scripts that execute multiple debugging commands and implement sophisticated analysis patterns. They are ideal for workflows that require parsing output and making decisions based on intermediate results.

**Parameters**:
- `sessionId` (string, required): Active session ID
- `extensionName` (string, required): Name of the extension to execute
- `parameters` (object, optional): JSON parameters to pass to the extension

**Discovering Available Extensions**:
If you attempt to execute a non-existent extension, the error response will include a list of all available extensions. Extensions are dynamically discovered from the server's extensions directory.

**Example**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_extension_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "extensionName": "example_extension_name",
      "parameters": {
        "param1": "value1"
      }
    }
  }
}
```

**Response**:
```json
{
  "sessionId": "sess-000001-abc12345-12345678-0001",
  "commandId": "ext-a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "extensionName": "example_extension_name",
  "status": "Queued",
  "message": "Extension 'example_extension_name' queued successfully. Use the 'commands' resource to monitor all commands or the 'nexus_read_dump_analyze_command_result' tool to get results.",
  "note": "Extensions may take several minutes to complete as they execute multiple debugging commands",
  "timeoutMinutes": 30
}
```

**Error Response (Extension Not Found)**:
```json
{
  "sessionId": "sess-000001-abc12345-12345678-0001",
  "commandId": null,
  "extensionName": "invalid_extension",
  "status": "Failed",
  "message": "Extension 'invalid_extension' not found. Available extensions: basic_crash_analysis, stack_with_sources, memory_corruption_analysis, thread_deadlock_investigation",
  "availableExtensions": ["basic_crash_analysis", "stack_with_sources", "memory_corruption_analysis", "thread_deadlock_investigation"]
}
```

**Getting Results**:
Use `nexus_read_dump_analyze_command_result` with the returned `commandId` (prefixed with "ext-"):
```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_read_dump_analyze_command_result",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "commandId": "ext-a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    }
  }
}
```


## 📊 MCP Resources

### Session Management Resources

#### `sessions`
**Purpose**: List all active analysis sessions
**Category**: Session Data
**Parameters**: None

**Example**:
```json
{
  "method": "resources/read",
  "params": {
    "uri": "sessions"
  }
}
```

### Command Management Resources

#### `commands`
**Purpose**: List commands from all sessions with status and timing information
**Category**: Command Data
**Parameters**: None

**Example**:
```json
{
  "method": "resources/read",
  "params": {
    "uri": "commands"
  }
}
```

### Documentation Resources


#### `usage`
**Purpose**: Access complete usage guide and examples
**Category**: Documentation
**Parameters**: None

**Example**:
```json
{
  "method": "resources/read",
  "params": {
    "uri": "usage"
  }
}
```


## 🔄 Complete Analysis Workflow

### Step-by-Step Process

1. **Open Analysis Session**:
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "nexus_open_dump_analyze_session",
       "arguments": {
         "dumpPath": "C:\\crashes\\application.dmp"
       }
     }
   }
   ```

2. **Execute Analysis Commands**:
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "nexus_enqueue_async_dump_analyze_command",
       "arguments": {
         "sessionId": "sess-000001-abc12345-12345678-0001",
         "command": "!analyze -v"
       }
     }
   }
   ```

3. **Poll Command Status**:
   Use `nexus_get_dump_analyze_commands_status` to check status of all commands
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "nexus_read_dump_analyze_command_result",
       "arguments": {
         "sessionId": "sess-000001-abc12345-12345678-0001",
         "commandId": "cmd-000001-abc12345-12345678-0001"
       }
     }
   }
   ```

4. **Get Analysis Results**:
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "nexus_read_dump_analyze_command_result",
       "arguments": {
         "sessionId": "sess-000001-abc12345-12345678-0001",
         "commandId": "cmd-000001-abc12345-12345678-0001"
       }
     }
   }
   ```

5. **Close Session**:
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "nexus_close_dump_analyze_session",
       "arguments": {
         "sessionId": "sess-000001-abc12345-12345678-0001"
       }
     }
   }
   ```

## 📡 Real-Time Notifications

### Notification Types

#### `notifications/commandStatus`
**Purpose**: Command execution progress updates
**Frequency**: Real-time during command execution

**Example**:
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/commandStatus",
  "params": {
    "commandId": "cmd-000001-abc12345-12345678-0001",
    "command": "!analyze -v",
    "status": "executing",
    "progress": 75,
    "message": "Analyzing crash dump...",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

#### `notifications/commandHeartbeat`
**Purpose**: Long-running command updates
**Frequency**: Every 30 seconds for long commands

**Example**:
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/commandHeartbeat",
  "params": {
    "commandId": "cmd-000001-abc12345-12345678-0001",
    "command": "!analyze -v",
    "elapsed": "00:05:30",
    "message": "Command still running...",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

#### `notifications/sessionRecovery`
**Purpose**: Session recovery and error events
**Frequency**: As needed

**Example**:
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/sessionRecovery",
  "params": {
    "sessionId": "sess-000001-abc12345-12345678-0001",
    "event": "recovered",
    "message": "Session recovered from timeout",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

## 🎯 Common Analysis Commands

### Basic Analysis Commands
- `!analyze -v`: Comprehensive crash analysis
- `!analyze -f`: Force analysis without user input
- `!analyze -hang`: Analyze hang dumps
- `!analyze -show`: Show analysis results

### Memory Analysis Commands
- `!heap -p -a`: Analyze heap corruption
- `!address`: Show memory usage
- `!pool`: Show pool usage
- `!memusage`: Show memory usage statistics

### Thread Analysis Commands
- `!locks`: Show lock information
- `!runaway`: Show thread CPU usage
- `~*k`: Show all thread stacks
- `!threads`: Show thread information

### Stack Analysis Commands
- `kb`: Show stack trace with parameters
- `k`: Show stack trace
- `kv`: Show stack trace with frame data
- `kP`: Show stack trace with parameters

## 🚨 Error Handling

### Common Error Scenarios

**Session Not Found**:
```json
{
  "error": {
    "code": -32602,
    "message": "Session not found: sess-invalid. Use mcp://nexus/sessions/list to see available sessions."
  }
}
```

**Command Not Found**:
```json
{
  "error": {
    "code": -32602,
    "message": "Command not found. Use mcp://nexus/commands/list to see available commands."
  }
}
```

**Dump File Not Accessible**:
```json
{
  "error": {
    "code": -32602,
    "message": "Dump file not accessible: C:\\crashes\\application.dmp. Check file path and permissions."
  }
}
```

**Symbol Loading Failed**:
```json
{
  "error": {
    "code": -32602,
    "message": "Symbol loading failed. Check symbol path configuration and network connectivity."
  }
}
```

## 🔧 Best Practices

### Tool Usage
1. **Always validate sessions** before executing commands
2. **Use async commands** for long-running operations
3. **Monitor progress** via notifications or polling
4. **Clean up resources** by closing sessions when done
5. **Handle errors gracefully** with proper error handling

### Performance Optimization
1. **Use appropriate timeouts** for different command types
2. **Limit concurrent sessions** based on system resources
3. **Configure symbol caching** for better performance
4. **Monitor memory usage** during analysis
5. **Use resource cleanup** to prevent memory leaks

### Security Considerations
1. **Run with appropriate permissions** for system dumps
2. **Secure dump file access** with proper file permissions
3. **Use secure symbol servers** for symbol loading
4. **Monitor resource usage** to prevent abuse
5. **Implement proper logging** for audit trails

---

## Next Steps

- **🔍 Overview:** [OVERVIEW.md](OVERVIEW.md) - Understand the analysis capabilities
- **📚 Resources:** [RESOURCES.md](RESOURCES.md) - MCP Resources reference
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Configure your environment
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Set up AI integration
- **📊 Examples:** [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md) - See analysis workflows in action