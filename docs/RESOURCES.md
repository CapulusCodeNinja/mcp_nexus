# MCP Resources Reference

**Windows Crash Dump Analysis Resources**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📋 Tools](TOOLS.md) | [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## 📊 Overview

MCP Nexus provides rich resources for crash dump analysis, session management, and documentation access. These resources enable AI systems to discover, monitor, and access analysis data in a structured, consistent format.

## 🔍 Quick Reference

| Resource | Purpose | Parameters | Real-time |
|----------|---------|------------|-----------|
| `mcp://nexus/sessions/list` | List all active analysis sessions | None | ✅ |
| `mcp://nexus/commands/list` | List commands with filtering | `sessionId`, `command`, `from`, `to`, `limit`, `offset`, `sortBy`, `order` | ✅ |
| `mcp://nexus/commands/result` | Get command status/result | `sessionId`, `commandId` (required) | ✅ |
| `mcp://nexus/docs/workflows` | Get crash analysis workflows | None | ❌ |
| `mcp://nexus/docs/usage` | Get complete usage guide | None | ❌ |
| `mcp://nexus/analysis/patterns` | Get common crash patterns | None | ❌ |
| `mcp://nexus/analysis/reports` | Get analysis reports | `sessionId`, `reportType` | ✅ |

## 📋 Session Management Resources

### `mcp://nexus/sessions/list`

**Purpose**: List all active crash dump analysis sessions
**Parameters**: None
**Real-time**: ✅ Updates as sessions are created/closed

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/sessions/list"
  }
}
```

**Example Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"sessions\": [\n    {\n      \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n      \"dumpPath\": \"C:\\\\crashes\\\\MyApp_2024-01-15.dmp\",\n      \"dumpFile\": \"MyApp_2024-01-15.dmp\",\n      \"isActive\": true,\n      \"status\": \"Active\",\n      \"createdAt\": \"2024-01-15T10:00:00Z\",\n      \"lastActivity\": \"2024-01-15T10:30:00Z\",\n      \"symbolsLoaded\": true,\n      \"dumpSize\": \"256MB\",\n      \"dumpType\": \"Full\",\n      \"processName\": \"MyApp.exe\",\n      \"processId\": 1234,\n      \"crashTime\": \"2024-01-15T09:45:00Z\",\n      \"exceptionCode\": \"0xC0000005\",\n      \"exceptionDescription\": \"Access Violation\"\n    },\n    {\n      \"sessionId\": \"sess-000002-def67890-12345678-0002\",\n      \"dumpPath\": \"C:\\\\crashes\\\\SystemCrash.dmp\",\n      \"dumpFile\": \"SystemCrash.dmp\",\n      \"isActive\": true,\n      \"status\": \"Active\",\n      \"createdAt\": \"2024-01-15T11:00:00Z\",\n      \"lastActivity\": \"2024-01-15T11:15:00Z\",\n      \"symbolsLoaded\": true,\n      \"dumpSize\": \"2GB\",\n      \"dumpType\": \"Kernel\",\n      \"processName\": \"System\",\n      \"processId\": 4,\n      \"crashTime\": \"2024-01-15T10:30:00Z\",\n      \"exceptionCode\": \"0x0000001E\",\n      \"exceptionDescription\": \"KMODE_EXCEPTION_NOT_HANDLED\"\n    }\n  ],\n  \"count\": 2,\n  \"timestamp\": \"2024-01-15T11:30:00Z\"\n}"
      }
    ]
  }
}
```

**Session Object Properties**:
- `sessionId`: Unique session identifier
- `dumpPath`: Full path to the dump file
- `dumpFile`: Dump file name
- `isActive`: Whether the session is currently active
- `status`: Session status (Active, Inactive, Error)
- `createdAt`: Session creation timestamp
- `lastActivity`: Last activity timestamp
- `symbolsLoaded`: Whether debugging symbols are loaded
- `dumpSize`: Size of the dump file
- `dumpType`: Type of dump (Full, Kernel, Mini, etc.)
- `processName`: Name of the crashed process
- `processId`: Process ID of the crashed process
- `crashTime`: When the crash occurred
- `exceptionCode`: Exception code that caused the crash
- `exceptionDescription`: Human-readable exception description

## 🔧 Command Management Resources

### `mcp://nexus/commands/list`

**Purpose**: List commands from all sessions with advanced filtering options
**Parameters**: All optional
**Real-time**: ✅ Updates as commands are queued/completed

**Basic Usage**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/list"
  }
}
```

**Advanced Filtering**:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/list?sessionId=sess-000001-abc12345&command=!analyze&limit=10&sortBy=createdAt&order=desc"
  }
}
```

**Filter Parameters**:
- `sessionId`: Filter by specific session ID
- `command`: Filter by command text (case-insensitive substring match)
- `from`: Filter commands created from this DateTime (ISO 8601 format)
- `to`: Filter commands created until this DateTime (ISO 8601 format)
- `limit`: Maximum number of results to return
- `offset`: Number of results to skip (for pagination)
- `sortBy`: Sort field: `command`, `status`, `createdAt` (default: `createdAt`)
- `order`: Sort order: `asc`, `desc` (default: `desc`)

**Example Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"commands\": {\n    \"sess-000001-abc12345-12345678-0001\": {\n      \"cmd-000001-abc12345-12345678-0001\": {\n        \"command\": \"!analyze -v\",\n        \"status\": \"Completed\",\n        \"isFinished\": true,\n        \"createdAt\": \"2024-01-15T10:00:00Z\",\n        \"completedAt\": \"2024-01-15T10:01:00Z\",\n        \"duration\": \"00:01:00\",\n        \"error\": null,\n        \"resultSize\": \"15KB\",\n        \"commandType\": \"Analysis\"\n      },\n      \"cmd-000001-abc12345-12345678-0002\": {\n        \"command\": \"kb\",\n        \"status\": \"In Progress\",\n        \"isFinished\": false,\n        \"createdAt\": \"2024-01-15T10:01:00Z\",\n        \"completedAt\": null,\n        \"duration\": null,\n        \"error\": null,\n        \"resultSize\": null,\n        \"commandType\": \"Stack Trace\"\n      }\n    }\n  },\n  \"totalSessions\": 1,\n  \"totalCommands\": 2,\n  \"timestamp\": \"2024-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

### `mcp://nexus/commands/result`

**Purpose**: Get status and results of a specific command
**Parameters**: `sessionId`, `commandId` (both required)
**Real-time**: ✅ Updates as command progresses

**URI Format**:
```
mcp://nexus/commands/result?sessionId=sess-000001-abc12345&commandId=cmd-000001-abc12345-12345678-0001
```

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/result?sessionId=sess-000001-abc12345&commandId=cmd-000001-abc12345-12345678-0001"
  }
}
```

**Completed Command Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n  \"commandId\": \"cmd-000001-abc12345-12345678-0001\",\n  \"command\": \"!analyze -v\",\n  \"status\": \"Completed\",\n  \"result\": \"*** ERROR ANALYSIS ***\\n\\nFAULTING_IP: \\nMyApp!ProcessUserInput+0x45\\n00007ff8`12345678 488b01          mov     rax,qword ptr [rcx]\\n\\nEXCEPTION_RECORD:  ffffffff`ffffffff -- (.exr 0xffffffff`ffffffff)\\nExceptionAddress: 00007ff8`12345678 (MyApp!ProcessUserInput+0x0000000000000045)\\n   ExceptionCode: c0000005 (Access violation)\\n  ExceptionFlags: 00000000\\nNumberParameters: 2\\n   Parameter[0]: 0000000000000000\\n   Parameter[1]: 0000000000000000\\nAttempted to read from address 0000000000000000\\n\\nSTACK_TEXT:  \\n00000000`0012f8c0 00007ff8`12345678 : 00000000`00000000 00000000`00000000 00000000`00000000 00000000`00000000 : MyApp!ProcessUserInput+0x45\\n00000000`0012f8c8 00007ff8`12345680 : 00000000`00000000 00000000`00000000 00000000`00000000 00000000`00000000 : MyApp!Main+0x20\\n\\nSYMBOL_STACK_INDEX:  0\\n\\nSYMBOL_NAME:  MyApp!ProcessUserInput+0x45\\n\\nFOLLOWUP_NAME:  MachineOwner\\n\\nMODULE_NAME: MyApp\\n\\nIMAGE_NAME:  MyApp.exe\\n\\nBUCKET_ID:  NULL_POINTER_READ\\n\\nPRIMARY_PROBLEM_CLASS:  NULL_POINTER_READ\\n\\nANALYSIS_VERSION: 6.3.9600.16384 amd64fre\\n\\nLAST_CONTROL_TRANSFER:  from 00000000`0012f8c0 to 00007ff8`12345678\\n\\nSTACK_COMMAND:  .cxr ; kb\\n\\nFAILURE_BUCKET_ID:  NULL_POINTER_READ_c0000005_MyApp.exe!ProcessUserInput\\n\\nANALYSIS_SOURCE:  FRE\\n\\nFAILURE_ID_HASH_STRING:  km:NULL_POINTER_READ_c0000005_MyApp.exe!ProcessUserInput\\n\\nFollowup: MachineOwner\\n\",\n  \"error\": null,\n  \"createdAt\": \"2024-01-15T10:00:00Z\",\n  \"completedAt\": \"2024-01-15T10:01:00Z\",\n  \"duration\": \"00:01:00\",\n  \"timestamp\": \"2024-01-15T10:30:00Z\",\n  \"resultSize\": \"15KB\",\n  \"commandType\": \"Analysis\",\n  \"analysisSummary\": {\n    \"crashType\": \"Access Violation\",\n    \"exceptionCode\": \"0xC0000005\",\n    \"faultingModule\": \"MyApp.exe\",\n    \"faultingFunction\": \"ProcessUserInput\",\n    \"rootCause\": \"NULL_POINTER_READ\",\n    \"confidence\": \"High\"\n  }\n}"
      }
    ]
  }
}
```

**In Progress Command Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n  \"commandId\": \"cmd-000001-abc12345-12345678-0001\",\n  \"command\": \"!analyze -v\",\n  \"status\": \"In Progress\",\n  \"result\": null,\n  \"error\": null,\n  \"createdAt\": \"2024-01-15T10:00:00Z\",\n  \"completedAt\": null,\n  \"duration\": null,\n  \"timestamp\": \"2024-01-15T10:30:00Z\",\n  \"message\": \"Command is still executing - check again in a few seconds.\",\n  \"progress\": 75,\n  \"estimatedCompletion\": \"2024-01-15T10:31:00Z\"\n}"
      }
    ]
  }
}
```

## 📚 Documentation Resources

### `mcp://nexus/docs/workflows`

**Purpose**: Get comprehensive crash analysis workflows and examples
**Parameters**: None
**Real-time**: ❌ Static content

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/docs/workflows"
  }
}
```

**Example Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"workflows\": [\n    {\n      \"id\": \"basic-crash-analysis\",\n      \"name\": \"Basic Crash Analysis\",\n      \"description\": \"Standard workflow for analyzing application crashes\",\n      \"complexity\": \"Beginner\",\n      \"estimatedTime\": \"5-10 minutes\",\n      \"steps\": [\n        {\n          \"step\": 1,\n          \"command\": \"!analyze -v\",\n          \"description\": \"Run comprehensive crash analysis\",\n          \"expectedOutcome\": \"Identify crash type and root cause\"\n        },\n        {\n          \"step\": 2,\n          \"command\": \"kb\",\n          \"description\": \"Examine stack trace\",\n          \"expectedOutcome\": \"Understand execution path\"\n        },\n        {\n          \"step\": 3,\n          \"command\": \"!locks\",\n          \"description\": \"Check for deadlocks\",\n          \"expectedOutcome\": \"Identify synchronization issues\"\n        }\n      ],\n      \"commonIssues\": [\n        \"Access violations\",\n        \"Null pointer dereferences\",\n        \"Buffer overflows\"\n      ]\n    },\n    {\n      \"id\": \"memory-corruption-analysis\",\n      \"name\": \"Memory Corruption Analysis\",\n      \"description\": \"Workflow for analyzing heap corruption and memory leaks\",\n      \"complexity\": \"Intermediate\",\n      \"estimatedTime\": \"15-30 minutes\",\n      \"steps\": [\n        {\n          \"step\": 1,\n          \"command\": \"!heap -p -a\",\n          \"description\": \"Analyze heap corruption\",\n          \"expectedOutcome\": \"Identify corrupted memory blocks\"\n        },\n        {\n          \"step\": 2,\n          \"command\": \"!memusage\",\n          \"description\": \"Check memory usage\",\n          \"expectedOutcome\": \"Identify memory leaks\"\n        },\n        {\n          \"step\": 3,\n          \"command\": \"~*k\",\n          \"description\": \"Analyze all thread stacks\",\n          \"expectedOutcome\": \"Find source of corruption\"\n        }\n      ],\n      \"commonIssues\": [\n        \"Heap corruption\",\n        \"Buffer overflows\",\n        \"Use-after-free\"\n      ]\n    }\n  ],\n  \"timestamp\": \"2024-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

### `mcp://nexus/docs/usage`

**Purpose**: Get complete usage guide for tools and resources
**Parameters**: None
**Real-time**: ❌ Static content

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/docs/usage"
  }
}
```

## 🔍 Analysis Resources

### `mcp://nexus/analysis/patterns`

**Purpose**: Get common crash patterns and their characteristics
**Parameters**: None
**Real-time**: ❌ Static content

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/analysis/patterns"
  }
}
```

**Example Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"patterns\": [\n    {\n      \"id\": \"access-violation\",\n      \"name\": \"Access Violation\",\n      \"exceptionCode\": \"0xC0000005\",\n      \"description\": \"Attempt to access memory that is not accessible\",\n      \"commonCauses\": [\n        \"Null pointer dereference\",\n        \"Buffer overflow\",\n        \"Use-after-free\",\n        \"Stack corruption\"\n      ],\n      \"analysisCommands\": [\n        \"!analyze -v\",\n        \"kb\",\n        \"!address\",\n        \"!heap -p -a\"\n      ],\n      \"severity\": \"High\",\n      \"frequency\": \"Very Common\"\n    },\n    {\n      \"id\": \"heap-corruption\",\n      \"name\": \"Heap Corruption\",\n      \"exceptionCode\": \"0xC0000374\",\n      \"description\": \"Heap structure has been corrupted\",\n      \"commonCauses\": [\n        \"Buffer overflow\",\n        \"Double free\",\n        \"Use-after-free\",\n        \"Stack overflow\"\n      ],\n      \"analysisCommands\": [\n        \"!heap -p -a\",\n        \"!memusage\",\n        \"~*k\",\n        \"!pool\"\n      ],\n      \"severity\": \"Critical\",\n      \"frequency\": \"Common\"\n    }\n  ],\n  \"timestamp\": \"2024-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

### `mcp://nexus/analysis/reports`

**Purpose**: Get analysis reports for specific sessions
**Parameters**: `sessionId` (required), `reportType` (optional)
**Real-time**: ✅ Updates as analysis progresses

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/analysis/reports?sessionId=sess-000001-abc12345&reportType=summary"
  }
}
```

**Example Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n  \"reportType\": \"summary\",\n  \"report\": {\n    \"crashSummary\": {\n      \"crashType\": \"Access Violation\",\n      \"exceptionCode\": \"0xC0000005\",\n      \"faultingModule\": \"MyApp.exe\",\n      \"faultingFunction\": \"ProcessUserInput\",\n      \"rootCause\": \"NULL_POINTER_READ\",\n      \"confidence\": \"High\",\n      \"severity\": \"High\"\n    },\n    \"analysisResults\": {\n      \"commandsExecuted\": 3,\n      \"totalAnalysisTime\": \"00:02:30\",\n      \"symbolsLoaded\": true,\n      \"analysisQuality\": \"Complete\"\n    },\n    \"recommendations\": [\n      \"Add null pointer checks before dereferencing pointers\",\n      \"Implement proper input validation\",\n      \"Use static analysis tools to detect similar issues\",\n      \"Add unit tests for edge cases\"\n    ],\n    \"nextSteps\": [\n      \"Review the ProcessUserInput function\",\n      \"Check input validation logic\",\n      \"Verify pointer initialization\"\n    ]\n  },\n  \"timestamp\": \"2024-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

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

**Missing Parameters**:
```json
{
  "error": {
    "code": -32602,
    "message": "Session ID and Command ID required. Use: mcp://nexus/commands/result?sessionId=<sessionId>&commandId=<commandId>"
  }
}
```

**Resource Not Available**:
```json
{
  "error": {
    "code": -32602,
    "message": "Resource not available: mcp://nexus/analysis/reports. Check resource URI and parameters."
  }
}
```

## 🔄 Usage Patterns

### Complete Analysis Workflow

1. **Check existing sessions**: `mcp://nexus/sessions/list`
2. **Create session if needed**: `nexus_open_dump_analyze_session`
3. **Queue command**: `nexus_enqueue_async_dump_analyze_command`
4. **Monitor progress**: `mcp://nexus/commands/result` (poll until completed)
5. **List all commands**: `mcp://nexus/commands/list?sessionId=<sessionId>`
6. **Get analysis report**: `mcp://nexus/analysis/reports?sessionId=<sessionId>`
7. **Clean up**: `nexus_close_dump_analyze_session`

### Resource Discovery

```json
// List all available resources
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/list",
  "params": {}
}
```

### Error Recovery

- Use `mcp://nexus/sessions/list` to verify session exists
- Use `mcp://nexus/commands/list` to see available commands
- Check `mcp://nexus/commands/result` for detailed error information
- Reference `mcp://nexus/docs/workflows` for troubleshooting guidance

## 🎯 Integration Tips

1. **Always validate sessions** before executing commands
2. **Poll `mcp://nexus/commands/result`** for command completion (every 1-2 seconds)
3. **Use `mcp://nexus/commands/list`** to track command history
4. **Reference `mcp://nexus/docs/workflows`** for analysis guidance
5. **Check `mcp://nexus/analysis/patterns`** for common crash patterns
6. **Use `mcp://nexus/analysis/reports`** for structured analysis results

## 📊 Resource Lifecycle

- **Sessions**: Created via `nexus_open_dump_analyze_session`, listed via `mcp://nexus/sessions/list`
- **Commands**: Queued via `nexus_enqueue_async_dump_analyze_command`, tracked via `mcp://nexus/commands/list` and `mcp://nexus/commands/result`
- **Documentation**: Static resources available anytime via `mcp://nexus/docs/workflows` and `mcp://nexus/docs/usage`
- **Analysis**: Dynamic resources updated as analysis progresses via `mcp://nexus/analysis/patterns` and `mcp://nexus/analysis/reports`

All resources return JSON data wrapped in MCP's standard `contents` array format for consistent integration with MCP clients.

---

## Next Steps

- **🔍 Overview:** [OVERVIEW.md](OVERVIEW.md) - Understand the analysis capabilities
- **📋 Tools:** [TOOLS.md](TOOLS.md) - Learn about available analysis tools
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Configure your environment
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Set up AI integration
- **📊 Examples:** [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md) - See analysis workflows in action