# Available Tools

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## Debugging Tools (8 tools)

Windows debugging capabilities through WinDBG/CDB integration:

### Core Debugging Commands
- **Crash Dump Analysis**: `nexus_open_dump`, `nexus_close_dump`
- **Remote Debugging**: `nexus_start_remote_debug`, `nexus_stop_remote_debug`  
- **Command Execution**: `nexus_exec_debugger_command_async` (🔄 ASYNC QUEUE: Always returns commandId, use `nexus_debugger_command_status` for results)
- **Queue Management**: `nexus_debugger_command_status`, `nexus_debugger_command_cancel`, `nexus_list_debugger_commands`

### 🔄 Async Command Execution Workflow

**IMPORTANT**: All WinDBG commands use an async queue system with real-time notifications:

```bash
1. Call nexus_exec_debugger_command_async {"command": "!analyze -v"}
   → Returns: {"commandId": "abc-123", "status": "queued", ...}

2. Listen for real-time notifications:
   → notifications/commandStatus: {"status": "executing", "progress": 25, ...}
   → notifications/commandHeartbeat: {"elapsed": "30s", ...} (for long commands)
   → notifications/commandStatus: {"status": "completed", "result": "ACTUAL_OUTPUT"}

3. OR poll nexus_debugger_command_status {"commandId": "abc-123"}  
   → Returns: {"status": "executing", ...} (keep polling)
   → Returns: {"status": "completed", "result": "ACTUAL_OUTPUT"}

4. Extract the "result" field for your WinDBG command output
```

**⚠️ CRITICAL**: `nexus_exec_debugger_command_async` NEVER returns command results directly. You MUST use `nexus_debugger_command_status` to get results or listen for notifications!

## 📡 Real-Time Notifications

The server sends live notifications about command progress:

### Standard MCP Notifications
- **`notifications/tools/list_changed`**: When available tools change

### MCP Nexus Custom Notifications  
- **`notifications/commandStatus`**: Command execution progress (queued → executing → completed)
- **`notifications/commandHeartbeat`**: Long-running command updates with elapsed time
- **`notifications/sessionRecovery`**: Debugging session recovery events
- **`notifications/serverHealth`**: Server status updates

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
