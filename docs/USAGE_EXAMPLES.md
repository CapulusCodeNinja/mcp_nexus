# MCP Nexus Usage Examples

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## Complete Crash Dump Analysis Workflow

This document provides step-by-step examples of how to properly use the MCP Nexus server for crash dump analysis.

### Tools vs Resources - Quick Reference

| Type | Method | Purpose | Examples |
|------|--------|---------|----------|
| **Tools** | `tools/call` | Execute actions | `nexus_open_dump_analyze_session`, `nexus_enqueue_async_dump_analyze_command` |
| **Resources** | `resources/read` | Access data | `commands://result`, `sessions://list`, `docs://workflows` |

### Step 1: Open a Debugging Session

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\Users\\david\\Downloads\\test\\code.dmp",
      "symbolsPath": "C:\\Symbols"
    }
  },
  "id": 1
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"sessionId\": \"sess-000001-8eead28d-1998ADFF5AE-1088\",\n  \"dumpFile\": \"code.dmp\",\n  \"success\": true,\n  \"message\": \"Session created successfully\"\n}"
      }
    ]
  }
}
```

### Step 2: Execute Debugging Commands

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-8eead28d-1998ADFF5AE-1088",
      "command": "!analyze -v"
    }
  },
  "id": 2
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"sessionId\": \"sess-000001-8eead28d-1998ADFF5AE-1088\",\n  \"commandId\": \"cmd-sess-000001-8eead28d-1998ADFF5AE-1088-0001\",\n  \"success\": true,\n  \"message\": \"Command queued successfully\"\n}"
      }
    ]
  }
}
```

### Step 3: Get Command Results (Using Resources)

```json
{
  "method": "resources/read",
  "params": {
    "uri": "commands://result?sessionId=sess-000001-8eead28d-1998ADFF5AE-1088&commandId=cmd-sess-000001-8eead28d-1998ADFF5AE-1088-0001"
  },
  "id": 3
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "contents": [
      {
        "uri": "commands://result?sessionId=sess-000001-8eead28d-1998ADFF5AE-1088&commandId=cmd-sess-000001-8eead28d-1998ADFF5AE-1088-0001",
        "mimeType": "application/json",
        "text": "{\n  \"status\": \"completed\",\n  \"result\": \"*** ERROR ANALYSIS ***\\n\\n*** WARNING: Unable to verify timestamp for ntoskrnl.exe\\n\\n*** Either you are not connected to the internet or your computer doesn't have the correct time set.\\n\\n*** For analysis of this file, run !analyze -v; .ecxr ; kb\\n\\nFAULTING_IP: \\nnt!KeBugCheckEx+0x1e\\nfffff800`01234567 48894c2408      mov     qword ptr [rsp+8],rcx\\n\\nBUGCHECK_STR:  0x1E\\n\\nDEFAULT_BUCKET_ID:  INVALID_PROCESS_ATTACH_ATTEMPT\\n\\nPROCESS_NAME:  System\\n\\nCURRENT_IRQL:  2\\n\\nLAST_CONTROL_TRANSFER:  from fffff800`01234567 to fffff800`01234567\\n\\nSTACK_TEXT:  \\nfffff800`01234567 fffff800`01234567 : 00000000`0000001e 00000000`00000000 00000000`00000000 00000000`00000000 : nt!KeBugCheckEx+0x1e\\n\\nSYMBOL_STACK_INDEX:  0\\n\\nSYMBOL_NAME:  nt!KeBugCheckEx+0x1e\\n\\nFOLLOWUP_NAME:  MachineOwner\\n\\nMODULE_NAME: nt\\n\\nIMAGE_NAME:  ntoskrnl.exe\\n\\nDEBUG_FLR_IMAGE_TIMESTAMP:  0\\n\\nBUCKET_ID:  INVALID_PROCESS_ATTACH_ATTEMPT\\n\\nANALYSIS_VERSION: 6.3.9600.16384 (debuggers(dbg).140716-0322) amd64fre\\n\\nPRIMARY_PROBLEM_CLASS:  INVALID_PROCESS_ATTACH_ATTEMPT\\n\\nBUGCHECK_STR:  0x1E\\n\\nLAST_CONTROL_TRANSFER:  from fffff800`01234567 to fffff800`01234567\\n\\nSTACK_TEXT:  \\nfffff800`01234567 fffff800`01234567 : 00000000`0000001e 00000000`00000000 00000000`00000000 00000000`00000000 : nt!KeBugCheckEx+0x1e\\n\\nSTACK_COMMAND:  .cxr ; kb\\n\\nFAILURE_BUCKET_ID:  INVALID_PROCESS_ATTACH_ATTEMPT\\n\\nBUCKET_ID:  INVALID_PROCESS_ATTACH_ATTEMPT\\n\\nANALYSIS_SOURCE:  FRE\\n\\nFAILURE_ID_HASH_STRING:  km:INVALID_PROCESS_ATTACH_ATTEMPT\\n\\nFAILURE_ID_HASH:  {00000000-0000-0000-0000-000000000000}\\n\\nFollowup: MachineOwner\\n\"\n}"
      }
    ]
  }
}
```

### Step 4: List All Sessions

```json
{
  "method": "resources/read",
  "params": {
    "uri": "sessions://list"
  },
  "id": 4
}
```

### Step 5: List Commands for a Session

```json
{
  "method": "resources/read",
  "params": {
    "uri": "commands://list?sessionId=sess-000001-8eead28d-1998ADFF5AE-1088"
  },
  "id": 5
}
```

### Step 6: Close the Session

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_close_dump_analyze_session",
    "arguments": {
      "sessionId": "sess-000001-8eead28d-1998ADFF5AE-1088"
    }
  },
  "id": 6
}
```

## Common Mistakes and Solutions

### Wrong: Using Resource URI as Tool Name

```json
{
  "method": "tools/call",
  "params": {
    "name": "commands://result?sessionId=abc&commandId=cmd123"
  }
}
```

**Error Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32602,
    "message": "TOOL CALL ERROR: 'commands://result?sessionId=abc&commandId=cmd123' is a RESOURCE URI, not a tool name. Use resources/read method to access resources. Use tools/list to see available tools."
  }
}
```

### Correct: Using Resources/Read for Resource Access

```json
{
  "method": "resources/read",
  "params": {
    "uri": "commands://result?sessionId=abc&commandId=cmd123"
  }
}
```

## Available Resources

### Documentation Resources
- `docs://workflows` - Crash analysis workflows
- `docs://usage` - Usage information

### Session Resources
- `sessions://list` - List all sessions
- `sessions://list?status=Active` - Filter by status
- `sessions://{sessionId}` - Specific session info

### Command Resources
- `commands://list` - List all commands
- `commands://list?sessionId={sessionId}` - Commands for specific session
- `commands://result?sessionId={sessionId}&commandId={commandId}` - Get command result

## Error Handling

The server provides helpful error messages when you make common mistakes:

1. **Using resource URI as tool name**: Clear message explaining the difference
2. **Missing parameters**: Specific guidance on what's required
3. **Unknown resources**: Suggestion to use `resources/list` to see available resources
4. **Unknown tools**: Suggestion to use `tools/list` to see available tools

## Best Practices

1. **Always use the correct method**:
   - `tools/call` for actions (open, close, execute commands)
   - `resources/read` for data access (results, lists, documentation)

2. **Store session and command IDs** from responses for later use

3. **Use resources/list** to discover available resources

4. **Use tools/list** to discover available tools

5. **Handle async commands properly** by polling the result resource or listening for notifications
