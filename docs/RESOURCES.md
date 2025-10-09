# MCP Resources Reference

**Windows Crash Dump Analysis Resources**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📋 Tools](TOOLS.md) | [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## 📊 Overview

MCP Nexus provides resources for session management, command tracking, and documentation access. These resources enable AI systems to discover, monitor, and access analysis data in a structured format.

## 🔍 Quick Reference

| Resource | Purpose |
|----------|---------|
| `sessions` | List all active analysis sessions |
| `commands` | List commands with status and timing |
| `extensions` | List available extension scripts |
| `usage` | Get complete usage guide |
| `metrics` | Get performance metrics |
| `circuits` | Get circuit breaker status |
| `health` | Get system health status |
| `cache` | Get cache statistics |

## 📋 Session Management Resources

### `sessions`

**Purpose**: List all active crash dump analysis sessions

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/read",
  "params": {
    "uri": "sessions"
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
        "text": "{\n  \"sessions\": [\n    {\n      \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n      \"dumpPath\": \"C:\\\\crashes\\\\MyApp_2024-01-15.dmp\",\n      \"dumpFile\": \"MyApp_2024-01-15.dmp\",\n      \"isActive\": true,\n      \"status\": \"Active\",\n      \"createdAt\": \"2024-01-15T10:00:00Z\",\n      \"lastActivity\": \"2024-01-15T10:30:00Z\",\n      \"symbolsLoaded\": true,\n      \"dumpSize\": \"256MB\",\n      \"dumpType\": \"Full\",\n      \"processName\": \"MyApp.exe\",\n      \"processId\": 1234,\n      \"crashTime\": \"2024-01-15T09:45:00Z\",\n      \"exceptionCode\": \"0xC0000005\",\n      \"exceptionDescription\": \"Access Violation\"\n    }\n  ],\n  \"count\": 1,\n  \"timestamp\": \"2024-01-15T11:30:00Z\"\n}"
      }
    ]
  }
}
```

## 🔧 Command Management Resources

### `commands`

**Purpose**: List commands from all sessions with status and timing information

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "resources/read",
  "params": {
    "uri": "commands"
  }
}
```

## 📚 Documentation Resources

### `extensions`

**Purpose**: List all available extension scripts with metadata, parameters, and descriptions

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "resources/read",
  "params": {
    "uri": "extensions"
  }
}
```

**Example Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"extensions\": [\n    {\n      \"name\": \"basic_crash_analysis\",\n      \"description\": \"Essential commands for initial crash investigation\",\n      \"version\": \"1.0.0\",\n      \"author\": \"MCP Nexus Team\",\n      \"scriptType\": \"powershell\",\n      \"timeout\": 1800000,\n      \"parameters\": []\n    },\n    {\n      \"name\": \"stack_with_sources\",\n      \"description\": \"Downloads stack trace with source code for all frames\",\n      \"version\": \"1.0.0\",\n      \"author\": \"MCP Nexus Team\",\n      \"scriptType\": \"powershell\",\n      \"timeout\": 1800000,\n      \"parameters\": [\n        {\n          \"name\": \"threadId\",\n          \"type\": \"string\",\n          \"description\": \"Thread ID to analyze\",\n          \"required\": false,\n          \"defaultValue\": \".\"\n        }\n      ]\n    }\n  ],\n  \"count\": 2,\n  \"enabled\": true,\n  \"timestamp\": \"2024-01-15T11:30:00Z\"\n}"
      }
    ]
  }
}
```

### `usage`

**Purpose**: Get complete usage guide for tools and resources

**Example Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "resources/read",
  "params": {
    "uri": "usage"
  }
}
```

## 📊 System Resources

### `metrics`

**Purpose**: Get comprehensive performance metrics and statistics

### `circuits`

**Purpose**: Get circuit breaker status and health information

### `health`

**Purpose**: Get comprehensive system health status

### `cache`

**Purpose**: Get cache statistics and memory usage information

## 🚨 Error Handling

### Common Error Scenarios

**Resource Not Found**:
```json
{
  "error": {
    "code": -32602,
    "message": "Resource not found: invalid-resource. Use resources/list to see available resources."
  }
}
```

**Invalid Parameters**:
```json
{
  "error": {
    "code": -32602,
    "message": "Invalid parameters for resource. Check parameter format and requirements."
  }
}
```

## 🔄 Usage Patterns

### Complete Analysis Workflow

1. **Check existing sessions**: `sessions`
2. **Create session if needed**: `nexus_open_dump_analyze_session`
3. **Queue command**: `nexus_enqueue_async_dump_analyze_command`
4. **Monitor progress**: `nexus_read_dump_analyze_command_result` (poll until completed)
5. **List all commands**: `commands?sessionId=<sessionId>`
6. **Clean up**: `nexus_close_dump_analyze_session`

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

- Use `sessions` to verify session exists
- Use `commands` to see available commands
- Check `nexus_read_dump_analyze_command_result` for detailed error information
- Reference `extensions` to discover available automated analysis scripts

## 🎯 Integration Tips

1. **Always validate sessions** before executing commands
2. **Poll `nexus_read_dump_analyze_command_result`** for command completion (every 1-2 seconds)
3. **Use `commands`** to track command history
4. **Reference `extensions`** to discover available automated analysis scripts
5. **Use `usage`** for API reference and examples
6. **Monitor `health`** for system status

## 📊 Resource Lifecycle

- **Sessions**: Created via `nexus_open_dump_analyze_session`, listed via `sessions`
- **Commands**: Queued via `nexus_enqueue_async_dump_analyze_command` or `nexus_enqueue_async_extension_command`, tracked via `commands`
- **Extensions**: Dynamically discovered scripts listed via `extensions`, executed via `nexus_enqueue_async_extension_command`
- **Documentation**: Static resources available anytime via `usage`
- **System**: Dynamic resources updated as system runs via `metrics`, `circuits`, `health`, `cache`

All resources return JSON data wrapped in MCP's standard `contents` array format for consistent integration with MCP clients.

---

## Next Steps

- **🔍 Overview:** [OVERVIEW.md](OVERVIEW.md) - Understand the analysis capabilities
- **📋 Tools:** [TOOLS.md](TOOLS.md) - Learn about available analysis tools
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Configure your environment
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Set up AI integration
- **📊 Examples:** [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md) - See analysis workflows in action