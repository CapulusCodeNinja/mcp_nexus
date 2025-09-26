# Available Tools

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## Debugging Tools (10 tools)

Windows debugging capabilities through WinDBG/CDB integration:

### Core Debugging Commands
- **Crash Dump Analysis**: `nexus_open_dump_analyze_session`, `nexus_close_dump_analyze_session`
- **Session Management**: `nexus_list_dump_analyze_sessions`, `nexus_list_dump_analyze_session_async_commands`
- **Remote Debugging**: `nexus_start_remote_debug`, `nexus_stop_remote_debug`  
- **Command Execution**: `nexus_dump_analyze_session_async_command` (🔄 ASYNC QUEUE: Always returns commandId, use `nexus_dump_analyze_session_async_command_status` for results)
- **Queue Management**: `nexus_dump_analyze_session_async_command_status`, `nexus_debugger_command_cancel`, `nexus_list_debugger_commands`

### 🔄 Complete Debugging Workflow

**IMPORTANT**: All WinDBG commands use an async queue system with real-time notifications:

```bash
1. nexus_open_dump_analyze_session {"dumpPath": "C:\\crash.dmp"}
   → Returns: {"sessionId": "sess-000001-abc12345-12345678-0001", ...}

2. nexus_dump_analyze_session_async_command {"sessionId": "sess-000001-abc12345-12345678-0001", "command": "!analyze -v"}
   → Returns: {"commandId": "cmd-000001-abc12345-12345678-0001", "status": "queued", ...}

3. Listen for real-time notifications:
   → notifications/commandStatus: {"status": "executing", "progress": 25, ...}
   → notifications/commandHeartbeat: {"elapsed": "30s", ...} (for long commands)
   → notifications/commandStatus: {"status": "completed", "result": "ACTUAL_OUTPUT"}

4. OR poll nexus_dump_analyze_session_async_command_status {"commandId": "cmd-000001-abc12345-12345678-0001"}  
   → Returns: {"status": "executing", ...} (keep polling)
   → Returns: {"status": "completed", "result": "ACTUAL_OUTPUT"}

5. nexus_list_dump_analyze_sessions {} (optional)
   → Returns: {"sessions": [...]} - List all active sessions

6. nexus_list_dump_analyze_session_async_commands {"sessionId": "sess-000001-abc12345-12345678-0001"} (optional)
   → Returns: {"commands": [...]} - List all commands for this session

7. nexus_close_dump_analyze_session {"sessionId": "sess-000001-abc12345-12345678-0001"}
   → Returns: {"success": true, ...} - Clean up resources
```

**⚠️ CRITICAL**: `nexus_dump_analyze_session_async_command` NEVER returns command results directly. You MUST use `nexus_dump_analyze_session_async_command_status` to get results or listen for notifications!

### 📋 Session Management Commands

#### `nexus_list_dump_analyze_sessions`
Lists all active debugging sessions with detailed information.

**Parameters:** None

**Returns:**
```json
{
  "success": true,
  "operation": "nexus_list_dump_analyze_sessions",
  "message": "Found 2 active sessions",
  "sessions": [
    {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "dumpFile": "crash.dmp",
      "status": "Active",
      "createdAt": "2024-09-26 23:15:30 UTC",
      "lastActivity": "2024-09-26 23:20:15 UTC",
      "commandsProcessed": 5,
      "activeCommands": 2
    }
  ]
}
```

#### `nexus_list_dump_analyze_session_async_commands`
Lists all async commands for a specific session.

**Parameters:**
- `sessionId` (required): The exact sessionId from `nexus_open_dump_analyze_session`

**Returns:**
```json
{
  "success": true,
  "operation": "nexus_list_dump_analyze_session_async_commands",
  "message": "Command listing for session: sess-000001-abc12345-12345678-0001",
  "commands": [
    {
      "commandId": "cmd-000001-abc12345-12345678-0001",
      "command": "!analyze -v",
      "status": "completed",
      "queuedAt": "2024-09-26 23:15:30 UTC",
      "completedAt": "2024-09-26 23:15:45 UTC"
    }
  ]
}
```

## 📡 Real-Time Notifications

The server sends live notifications about command progress:

### Standard MCP Notifications
- **`notifications/tools/list_changed`**: When available tools change

### MCP Nexus Custom Notifications  
- **`notifications/commandStatus`**: Command execution progress (queued → executing → completed)
- **`notifications/commandHeartbeat`**: Long-running command updates with elapsed time
- **`notifications/sessionRecovery`**: Debugging session recovery events
- **`notifications/serverHealth`**: Server status updates
  
Notes:
- Notifications are broadcast automatically; clients do not need to register.
- In HTTP mode, connect to SSE at `GET /mcp/notifications` and read `data:` lines.
- In stdio mode, parse JSON-RPC `method` notifications from stdout.

### Notification Benefits
- **Real-time updates**: No need for constant polling
- **Progress tracking**: See command execution progress (0-100%)
- **Heartbeat monitoring**: Know long commands are still running
- **Error notifications**: Immediate failure alerts

### Notification Example
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/commandStatus",
  "params": {
    "commandId": "cmd-123",
    "command": "!analyze -v",
    "status": "executing",
    "progress": 75,
    "message": "Analyzing crash dump...",
    "timestamp": "2024-09-25T10:30:00Z"
  }
}
```

## Tool Categories

### Time Tools
- Basic time utilities (implementation details in source)

### Future Tools
The platform is designed to accommodate additional tool categories through its modular architecture.

---

## Next Steps

- **🔧 Setup:** [CONFIGURATION.md](CONFIGURATION.md) - Configure transport modes and Windows service
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Connect with Cursor IDE and other AI tools
- **👨‍💻 Development:** [DEVELOPMENT.md](DEVELOPMENT.md) - Add your own tools with notification support
