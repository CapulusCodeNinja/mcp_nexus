# MCP Resources Reference

This document provides a comprehensive reference for all MCP Resources available in the Nexus server.

> 📋 **For complete tool documentation and integration examples:** **[📋 TOOLS.md](TOOLS.md)**

## Quick Reference

| Resource | Purpose | Parameters |
|----------|---------|------------|
| `mcp://nexus/sessions/list` | List all active sessions | None |
| `mcp://nexus/commands/list` | List commands from sessions | `sessionId`, `command`, `from`, `to`, `limit`, `offset`, `sortBy`, `order` (all optional) |
| `mcp://nexus/commands/result` | Get command status/result | `sessionId`, `commandId` (required) |
| `mcp://nexus/docs/workflows` | Get crash analysis workflows | None |
| `mcp://nexus/docs/usage` | Get complete usage guide | None |

## Resource Details

### Session Management

#### `mcp://nexus/sessions/list`
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

#### `mcp://nexus/commands/list`
**Purpose:** List commands from all sessions with advanced filtering options  
**Parameters:** All optional - `sessionId`, `command`, `from`, `to`, `limit`, `offset`, `sortBy`, `order`  
**Returns:** Commands organized by session with applied filters

**Basic Usage:**
```
mcp://nexus/commands/list                                    # All commands
mcp://nexus/commands/list?sessionId=abc123                  # Specific session
```

**Advanced Filtering:**
```
mcp://nexus/commands/list?command=!analyze                  # Filter by command text
mcp://nexus/commands/list?from=2024-01-15T10:00:00Z         # Time range filtering
mcp://nexus/commands/list?limit=10&offset=20                # Pagination
mcp://nexus/commands/list?sortBy=createdAt&order=desc       # Sorting
mcp://nexus/commands/list?sessionId=abc123&command=!analyze&limit=5&sortBy=command&order=asc  # Combined
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

#### `mcp://nexus/commands/result`
**Purpose:** Get status and results of a specific command  
**Parameters:** `sessionId`, `commandId` (both required)  
**Returns:** Command details with status and results

**URI Format:**
```
mcp://nexus/commands/result?sessionId=sess-000001-abc12345&commandId=cmd-000001-abc12345-12345678-0001
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

#### `mcp://nexus/docs/workflows`
**Purpose:** Get comprehensive crash analysis workflows and examples  
**Parameters:** None  
**Returns:** Structured workflows with step-by-step guidance

**Key Features:**
- 7 different analysis workflows (Basic Crash, Memory Corruption, Thread Deadlock, etc.)
- Step-by-step command sequences
- Expected outcomes for each workflow
- General tips and best practices
- Exploration guidance beyond basic patterns

#### `mcp://nexus/docs/usage`
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
    "message": "Session not found: sess-invalid. Use mcp://nexus/sessions/list to see available sessions."
  }
}
```

**Command Not Found:**
```json
{
  "error": {
    "code": -32602,
    "message": "Command not found. Use mcp://nexus/commands/list to see available commands."
  }
}
```

**Missing Parameters:**
```json
{
  "error": {
    "code": -32602,
    "message": "Session ID and Command ID required. Use: mcp://nexus/commands/result?sessionId=<sessionId>&commandId=<commandId>"
  }
}
```

## Usage Patterns

### Complete Debugging Workflow
1. **Check existing sessions:** `mcp://nexus/sessions/list`
2. **Create session if needed:** `nexus_open_dump_analyze_session`
3. **Queue command:** `nexus_enqueue_async_dump_analyze_command`
4. **Monitor progress:** `mcp://nexus/commands/result` (poll until completed)
5. **List all commands:** `mcp://nexus/commands/list?sessionId=<sessionId>`
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
- Use `mcp://nexus/sessions/list` to verify session exists
- Use `mcp://nexus/commands/list` to see available commands
- Check `mcp://nexus/commands/result` for detailed error information
- Reference `mcp://nexus/docs/usage` for troubleshooting guidance

## Integration Tips

1. **Always validate sessions** before executing commands
2. **Poll `mcp://nexus/commands/result`** for command completion (every 1-2 seconds)
3. **Use `mcp://nexus/commands/list`** to track command history
4. **Reference `mcp://nexus/docs/workflows`** for analysis guidance
5. **Check `mcp://nexus/docs/usage`** for complete API reference

## Resource Lifecycle

- **Sessions:** Created via `nexus_open_dump_analyze_session`, listed via `mcp://nexus/sessions/list`
- **Commands:** Queued via `nexus_enqueue_async_dump_analyze_command`, tracked via `mcp://nexus/commands/list` and `mcp://nexus/commands/result`
- **Documentation:** Static resources available anytime via `mcp://nexus/docs/workflows` and `mcp://nexus/docs/usage`

All resources return JSON data wrapped in MCP's standard `contents` array format for consistent integration with MCP clients.
