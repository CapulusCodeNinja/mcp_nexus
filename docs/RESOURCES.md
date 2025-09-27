# MCP Resources Reference

This document provides a comprehensive reference for all MCP Resources available in the Nexus server.

> 📋 **For complete tool documentation and integration examples:** **[📋 TOOLS.md](TOOLS.md)**

## Quick Reference

| Resource | Purpose | Parameters |
|----------|---------|------------|
| `sessions://list` | List all active sessions | None |
| `commands://list` | List commands from sessions | `sessionId`, `command`, `from`, `to`, `limit`, `offset`, `sortBy`, `order` (all optional) |
| `commands://result` | Get command status/result | `sessionId`, `commandId` (required) |
| `docs://workflows` | Get crash analysis workflows | None |
| `docs://usage` | Get complete usage guide | None |

## Resource Details

### Session Management

#### `sessions://list`
**Purpose:** List all active debugging sessions  
**Parameters:** None  
**Returns:** Array of session objects with metadata

**Example Response:**
```json
{
  "sessions": [
    {
      "sessionId": "sess-000001-abc12345",
      "dumpPath": "C:\\dumps\\crash.dmp",
      "isActive": true,
      "status": "Active",
      "createdAt": "2024-01-15T10:00:00Z",
      "lastActivity": "2024-01-15T10:30:00Z"
    }
  ],
  "count": 1,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Command Management

#### `commands://list`
**Purpose:** List commands from all sessions with advanced filtering options  
**Parameters:** All optional - `sessionId`, `command`, `from`, `to`, `limit`, `offset`, `sortBy`, `order`  
**Returns:** Commands organized by session with applied filters

**Basic Usage:**
```
commands://list                                    # All commands
commands://list?sessionId=abc123                  # Specific session
```

**Advanced Filtering:**
```
commands://list?command=!analyze                  # Filter by command text
commands://list?from=2024-01-15T10:00:00Z         # Time range filtering
commands://list?limit=10&offset=20                # Pagination
commands://list?sortBy=createdAt&order=desc       # Sorting
commands://list?sessionId=abc123&command=!analyze&limit=5&sortBy=command&order=asc  # Combined
```

**Filter Parameters:**
- `sessionId` - Filter by specific session ID
- `command` - Filter by command text (case-insensitive substring match)
- `from` - Filter commands created from this DateTime (ISO 8601 format)
- `to` - Filter commands created until this DateTime (ISO 8601 format)
- `limit` - Maximum number of results to return
- `offset` - Number of results to skip (for pagination)
- `sortBy` - Sort field: `command`, `status`, `createdAt` (default: `createdAt`)
- `order` - Sort order: `asc`, `desc` (default: `desc`)

**Example Response:**
```json
{
  "commands": {
    "sess-000001-abc12345": {
      "cmd-000001-abc12345-12345678-0001": {
        "command": "!analyze -v",
        "status": "Completed",
        "isFinished": true,
        "createdAt": "2024-01-15T10:00:00Z",
        "completedAt": "2024-01-15T10:01:00Z",
        "duration": "00:01:00",
        "error": null
      }
    }
  },
  "totalSessions": 1,
  "totalCommands": 1,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### `commands://result`
**Purpose:** Get status and results of a specific command  
**Parameters:** `sessionId`, `commandId` (both required)  
**Returns:** Command details with status and results

**URI Format:**
```
commands://result?sessionId=sess-000001-abc12345&commandId=cmd-000001-abc12345-12345678-0001
```

**Completed Command Response:**
```json
{
  "sessionId": "sess-000001-abc12345",
  "commandId": "cmd-000001-abc12345-12345678-0001",
  "command": "!analyze -v",
  "status": "Completed",
  "result": "ACTUAL_WINDBG_OUTPUT_HERE",
  "error": null,
  "createdAt": "2024-01-15T10:00:00Z",
  "completedAt": "2024-01-15T10:01:00Z",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**In Progress Command Response:**
```json
{
  "sessionId": "sess-000001-abc12345",
  "commandId": "cmd-000001-abc12345-12345678-0001",
  "command": "!analyze -v",
  "status": "In Progress",
  "result": null,
  "error": null,
  "createdAt": "2024-01-15T10:00:00Z",
  "completedAt": null,
  "timestamp": "2024-01-15T10:30:00Z",
  "message": "Command is still executing - check again in a few seconds."
}
```

### Documentation

#### `docs://workflows`
**Purpose:** Get comprehensive crash analysis workflows and examples  
**Parameters:** None  
**Returns:** Structured workflows with step-by-step guidance

**Key Features:**
- 7 different analysis workflows (Basic Crash, Memory Corruption, Thread Deadlock, etc.)
- Step-by-step command sequences
- Expected outcomes for each workflow
- General tips and best practices
- Exploration guidance beyond basic patterns

#### `docs://usage`
**Purpose:** Get complete usage guide for tools and resources  
**Parameters:** None  
**Returns:** Comprehensive usage documentation

**Includes:**
- Complete tool reference
- Resource usage patterns
- Input/output specifications
- Error handling guidance
- Integration examples

## Error Handling

### Common Error Scenarios

**Session Not Found:**
```json
{
  "error": {
    "code": -32602,
    "message": "Session not found: sess-invalid. Use sessions://list to see available sessions."
  }
}
```

**Command Not Found:**
```json
{
  "error": {
    "code": -32602,
    "message": "Command not found. Use commands://list to see available commands."
  }
}
```

**Missing Parameters:**
```json
{
  "error": {
    "code": -32602,
    "message": "Session ID and Command ID required. Use: commands://result?sessionId=<sessionId>&commandId=<commandId>"
  }
}
```

## Usage Patterns

### Complete Debugging Workflow
1. **Check existing sessions:** `sessions://list`
2. **Create session if needed:** `nexus_open_dump_analyze_session`
3. **Queue command:** `nexus_dump_analyze_session_async_command`
4. **Monitor progress:** `commands://result` (poll until completed)
5. **List all commands:** `commands://list?sessionId=<sessionId>`
6. **Clean up:** `nexus_close_dump_analyze_session`

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
- Use `sessions://list` to verify session exists
- Use `commands://list` to see available commands
- Check `commands://result` for detailed error information
- Reference `docs://usage` for troubleshooting guidance

## Integration Tips

1. **Always validate sessions** before executing commands
2. **Poll `commands://result`** for command completion (every 1-2 seconds)
3. **Use `commands://list`** to track command history
4. **Reference `docs://workflows`** for analysis guidance
5. **Check `docs://usage`** for complete API reference

## Resource Lifecycle

- **Sessions:** Created via `nexus_open_dump_analyze_session`, listed via `sessions://list`
- **Commands:** Queued via `nexus_dump_analyze_session_async_command`, tracked via `commands://list` and `commands://result`
- **Documentation:** Static resources available anytime via `docs://workflows` and `docs://usage`

All resources return JSON data wrapped in MCP's standard `contents` array format for consistent integration with MCP clients.
