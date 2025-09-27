# Available Tools

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## Debugging Tools (10 tools)

Windows debugging capabilities through WinDBG/CDB integration:

### Tools vs Resources

**TOOLS** (use `tools/call` method):
- Execute actions: open sessions, run commands, close sessions
- Examples: `nexus_open_dump_analyze_session`, `nexus_enqueue_async_dump_analyze_command`

**RESOURCES** (use `resources/read` method):
- Access data: command results, session lists, documentation
- Examples: `mcp://nexus/commands/result`, `mcp://nexus/sessions/list`, `mcp://nexus/docs/workflows`

**Common Mistake**: Don't use resource URIs as tool names!
```json
// WRONG - This will fail
{"method":"tools/call","params":{"name":"mcp://nexus/commands/result?sessionId=abc&commandId=cmd123"}}

// CORRECT - Use resources/read for resources
{"method":"resources/read","params":{"uri":"mcp://nexus/commands/result?sessionId=abc&commandId=cmd123"}}
```

### Core Debugging Commands
- **Crash Dump Analysis**: `nexus_open_dump_analyze_session`, `nexus_close_dump_analyze_session`
- **Session Management**: Available via MCP Resources (`mcp://nexus/sessions/list`, `mcp://nexus/commands/list`)
- **Remote Debugging**: `nexus_start_remote_debug`, `nexus_stop_remote_debug`  
- **Command Execution**: `nexus_enqueue_async_dump_analyze_command` (🔄 ASYNC QUEUE: Always returns commandId, use MCP Resources for results)
- **Queue Management**: `nexus_debugger_command_cancel`, `nexus_list_debugger_commands`

### 🔄 Complete Debugging Workflow

**IMPORTANT**: All WinDBG commands use an async queue system with real-time notifications:

```bash
1. nexus_open_dump_analyze_session {"dumpPath": "C:\\crash.dmp"}
   → Returns: {"sessionId": "sess-000001-abc12345-12345678-0001", ...}

2. nexus_enqueue_async_dump_analyze_command {"sessionId": "sess-000001-abc12345-12345678-0001", "command": "!analyze -v"}
   → Returns: {"commandId": "cmd-000001-abc12345-12345678-0001", "status": "queued", ...}

3. Listen for real-time notifications:
   → notifications/commandStatus: {"status": "executing", "progress": 25, ...}
   → notifications/commandHeartbeat: {"elapsed": "30s", ...} (for long commands)
   → notifications/commandStatus: {"status": "completed", "result": "ACTUAL_OUTPUT"}

4. OR use MCP Resource: 
   ```json
   {"method":"resources/read","params":{"uri":"mcp://nexus/commands/result?sessionId=sess-000001-abc12345&commandId=cmd-000001-abc12345-12345678-0001"}}
   ```
   → Returns: {"status": "executing", ...} (keep polling)
   → Returns: {"status": "completed", "result": "ACTUAL_OUTPUT"}

5. Use MCP Resources for session management:
   ```json
   // List all sessions
   {"method":"resources/read","params":{"uri":"mcp://nexus/sessions/list"}}
   
   // List commands for a specific session
   {"method":"resources/read","params":{"uri":"mcp://nexus/commands/list?sessionId=sess-000001-abc12345"}}
   
   // Get command result
   {"method":"resources/read","params":{"uri":"mcp://nexus/commands/result?sessionId=sess-000001-abc12345&commandId=cmd-000001-abc12345-0001"}}
   ```

7. nexus_close_dump_analyze_session {"sessionId": "sess-000001-abc12345-12345678-0001"}
   → Returns: {"success": true, ...} - Clean up resources
```

**⚠️ CRITICAL**: `nexus_enqueue_async_dump_analyze_command` NEVER returns command results directly. You MUST use MCP Resources (`mcp://nexus/commands/result`) to get results or listen for notifications!

### 📋 Session Management (via MCP Resources)

Session management is now available through MCP Resources for better integration and discoverability:

#### Available Resources

- **`mcp://nexus/sessions/list`** - List all active debugging sessions
- **`mcp://nexus/commands/list`** - List commands from all sessions with advanced filtering (sessionId, command text, time range, pagination, sorting)  
- **`mcp://nexus/commands/result`** - Get status and results of specific commands
- **`mcp://nexus/docs/workflows`** - Comprehensive crash analysis workflows and examples
- **`mcp://nexus/docs/usage`** - Complete usage guide for tools and resources

#### How to Use Resources

**1. List Active Sessions:**
```json
// MCP Request
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/sessions/list"
  }
}

// Response
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "contents": [{
      "type": "text",
      "text": "{\"sessions\": [...], \"count\": 2, \"timestamp\": \"2024-01-15T10:30:00Z\"}"
    }]
  }
}
```

**2. List Commands (All Sessions):**
```json
// MCP Request
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "resources/read", 
  "params": {
    "uri": "mcp://nexus/commands/list"
  }
}
```

**3. List Commands (Specific Session):**
```json
// MCP Request
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/list?sessionId=sess-000001-abc12345"
  }
}
```

**4. Get Command Result:**
```json
// MCP Request
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/result?sessionId=sess-000001-abc12345&commandId=cmd-000001-abc12345-12345678-0001"
  }
}

// Response (Command Completed)
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "contents": [{
      "type": "text", 
      "text": "{\"sessionId\": \"sess-000001-abc12345\", \"commandId\": \"cmd-000001-abc12345-12345678-0001\", \"status\": \"Completed\", \"result\": \"ACTUAL_WINDBG_OUTPUT\", \"timestamp\": \"2024-01-15T10:30:00Z\"}"
    }]
  }
}

// Response (Command In Progress)
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "contents": [{
      "type": "text",
      "text": "{\"sessionId\": \"sess-000001-abc12345\", \"commandId\": \"cmd-000001-abc12345-12345678-0001\", \"status\": \"In Progress\", \"result\": null, \"message\": \"Command is still executing - check again in a few seconds.\", \"timestamp\": \"2024-01-15T10:30:00Z\"}"
    }]
  }
}
```

**5. Get Crash Analysis Workflows:**
```json
// MCP Request
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/docs/workflows"
  }
}
```

**6. Get Usage Guide:**
```json
// MCP Request
{
  "jsonrpc": "2.0", 
  "id": 6,
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/docs/usage"
  }
}
```

#### Resource Benefits

- **Better Integration**: Resources work seamlessly with MCP clients
- **Structured Data**: JSON responses with consistent formatting
- **Error Handling**: Clear error messages with helpful hints
- **Real-time Updates**: Resources reflect current server state
- **Documentation**: Built-in usage guides and workflows

> 📚 **For detailed resource specifications and usage patterns:** **[📚 RESOURCES.md](RESOURCES.md)**

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
