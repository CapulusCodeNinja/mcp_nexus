# Usage Examples

**Comprehensive Windows Crash Dump Analysis Workflows**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📋 Tools](TOOLS.md) | [📚 Resources](RESOURCES.md) | [🔧 Configuration](CONFIGURATION.md) | [🤖 Integration](INTEGRATION.md)

## 🎯 Overview

This document provides comprehensive, real-world examples of using MCP Nexus for Windows crash dump analysis. Each example includes complete workflows, common pitfalls, and best practices for AI-powered crash analysis.

## 📋 Quick Reference

| Workflow Type | Description | Complexity | Time Estimate |
|---------------|-------------|------------|---------------|
| **Basic Crash Analysis** | Standard application crash investigation | Beginner | 5-10 minutes |
| **Memory Corruption** | Heap corruption and memory leak analysis | Intermediate | 15-30 minutes |
| **Thread Deadlock** | Thread synchronization issue investigation | Intermediate | 10-20 minutes |
| **System Crash (BSOD)** | Blue Screen of Death analysis | Advanced | 20-45 minutes |
| **Performance Issues** | CPU spikes and resource exhaustion | Intermediate | 15-25 minutes |
| **Driver Problems** | Kernel driver crash investigation | Advanced | 30-60 minutes |

## 🔍 Basic Crash Analysis Workflow

### Scenario: Application Crash with Access Violation

**Problem**: A .NET application crashes with an access violation (0xC0000005) when processing user input.

### Step 1: Open the Crash Dump

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crashes\\MyApp_2024-01-15_10-30-45.dmp",
      "symbolsPath": "C:\\Symbols",
      "timeoutMinutes": 60
    }
  },
  "id": 1
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
        "text": "{\n  \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n  \"dumpFile\": \"MyApp_2024-01-15_10-30-45.dmp\",\n  \"success\": true,\n  \"message\": \"Session created successfully\",\n  \"symbolsLoaded\": true,\n  \"dumpSize\": \"128MB\",\n  \"createdAt\": \"2024-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

### Step 2: Run Initial Analysis

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
  },
  "id": 2
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
        "text": "{\n  \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n  \"commandId\": \"cmd-000001-abc12345-12345678-0001\",\n  \"success\": true,\n  \"message\": \"Command queued successfully\",\n  \"status\": \"queued\",\n  \"createdAt\": \"2024-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

### Step 3: Monitor Progress and Get Results

```json
{
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/result?sessionId=sess-000001-abc12345-12345678-0001&commandId=cmd-000001-abc12345-12345678-0001"
  },
  "id": 3
}
```

**Response** (Analysis Complete):
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "contents": [
      {
        "type": "text",
        "text": "{\n  \"sessionId\": \"sess-000001-abc12345-12345678-0001\",\n  \"commandId\": \"cmd-000001-abc12345-12345678-0001\",\n  \"status\": \"completed\",\n  \"result\": \"*** ERROR ANALYSIS ***\\n\\nFAULTING_IP: \\nMyApp!ProcessUserInput+0x45\\n00007ff8`12345678 488b01          mov     rax,qword ptr [rcx]\\n\\nEXCEPTION_RECORD:  ffffffff`ffffffff -- (.exr 0xffffffff`ffffffff)\\nExceptionAddress: 00007ff8`12345678 (MyApp!ProcessUserInput+0x0000000000000045)\\n   ExceptionCode: c0000005 (Access violation)\\n  ExceptionFlags: 00000000\\nNumberParameters: 2\\n   Parameter[0]: 0000000000000000\\n   Parameter[1]: 0000000000000000\\nAttempted to read from address 0000000000000000\\n\\nSTACK_TEXT:  \\n00000000`0012f8c0 00007ff8`12345678 : 00000000`00000000 00000000`00000000 00000000`00000000 00000000`00000000 : MyApp!ProcessUserInput+0x45\\n00000000`0012f8c8 00007ff8`12345680 : 00000000`00000000 00000000`00000000 00000000`00000000 00000000`00000000 : MyApp!Main+0x20\\n\\nSYMBOL_STACK_INDEX:  0\\n\\nSYMBOL_NAME:  MyApp!ProcessUserInput+0x45\\n\\nFOLLOWUP_NAME:  MachineOwner\\n\\nMODULE_NAME: MyApp\\n\\nIMAGE_NAME:  MyApp.exe\\n\\nBUCKET_ID:  NULL_POINTER_READ\\n\\nPRIMARY_PROBLEM_CLASS:  NULL_POINTER_READ\\n\\nANALYSIS_VERSION: 6.3.9600.16384 amd64fre\\n\\nLAST_CONTROL_TRANSFER:  from 00000000`0012f8c0 to 00007ff8`12345678\\n\\nSTACK_COMMAND:  .cxr ; kb\\n\\nFAILURE_BUCKET_ID:  NULL_POINTER_READ_c0000005_MyApp.exe!ProcessUserInput\\n\\nANALYSIS_SOURCE:  FRE\\n\\nFAILURE_ID_HASH_STRING:  km:NULL_POINTER_READ_c0000005_MyApp.exe!ProcessUserInput\\n\\nFollowup: MachineOwner\\n\",\n  \"error\": null,\n  \"createdAt\": \"2024-01-15T10:30:00Z\",\n  \"completedAt\": \"2024-01-15T10:30:15Z\",\n  \"duration\": \"00:00:15\",\n  \"timestamp\": \"2024-01-15T10:30:15Z\"\n}"
      }
    ]
  }
}
```

### Step 4: Additional Analysis Commands

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "kb",
      "timeoutMinutes": 5
    }
  },
  "id": 4
}
```

### Step 5: Close Session

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_close_dump_analyze_session",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001"
    }
  },
  "id": 5
}
```

## 💾 Memory Corruption Analysis Workflow

### Scenario: Heap Corruption in C++ Application

**Problem**: A C++ application crashes with heap corruption when freeing memory.

### Step 1: Open Dump and Run Heap Analysis

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crashes\\CppApp_HeapCorruption.dmp"
    }
  },
  "id": 1
}
```

### Step 2: Analyze Heap Corruption

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!heap -p -a",
      "timeoutMinutes": 20
    }
  },
  "id": 2
}
```

### Step 3: Check Memory Usage

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!memusage",
      "timeoutMinutes": 10
    }
  },
  "id": 3
}
```

### Step 4: Analyze Stack Traces

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "~*k",
      "timeoutMinutes": 10
    }
  },
  "id": 4
}
```

## 🧵 Thread Deadlock Analysis Workflow

### Scenario: Multi-threaded Application Deadlock

**Problem**: A multi-threaded application hangs due to a deadlock between two threads.

### Step 1: Open Dump and Check for Deadlocks

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crashes\\MultiThreadApp_Deadlock.dmp"
    }
  },
  "id": 1
}
```

### Step 2: Analyze Lock Information

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!locks",
      "timeoutMinutes": 15
    }
  },
  "id": 2
}
```

### Step 3: Check Thread States

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!runaway",
      "timeoutMinutes": 10
    }
  },
  "id": 3
}
```

### Step 4: Analyze Thread Stacks

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "~*k",
      "timeoutMinutes": 15
    }
  },
  "id": 4
}
```

## 🔒 System Crash (BSOD) Analysis Workflow

### Scenario: Blue Screen of Death Analysis

**Problem**: Windows system crashes with a Blue Screen of Death (BSOD) due to a driver issue.

### Step 1: Open System Dump

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\Windows\\MEMORY.DMP",
      "symbolsPath": "C:\\Symbols"
    }
  },
  "id": 1
}
```

### Step 2: Run Comprehensive Analysis

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!analyze -v",
      "timeoutMinutes": 30
    }
  },
  "id": 2
}
```

### Step 3: Analyze Driver Stack

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!irql",
      "timeoutMinutes": 10
    }
  },
  "id": 3
}
```

### Step 4: Check Driver Information

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "lm",
      "timeoutMinutes": 10
    }
  },
  "id": 4
}
```

## ⚡ Performance Issues Analysis Workflow

### Scenario: High CPU Usage and Memory Leaks

**Problem**: Application experiences high CPU usage and memory leaks during peak load.

### Step 1: Open Performance Dump

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crashes\\PerformanceIssue.dmp"
    }
  },
  "id": 1
}
```

### Step 2: Check Memory Usage

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!memusage",
      "timeoutMinutes": 15
    }
  },
  "id": 2
}
```

### Step 3: Analyze Thread CPU Usage

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!runaway",
      "timeoutMinutes": 10
    }
  },
  "id": 3
}
```

### Step 4: Check Process Information

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!process",
      "timeoutMinutes": 10
    }
  },
  "id": 4
}
```

## 🔧 Driver Problems Analysis Workflow

### Scenario: Kernel Driver Crash

**Problem**: A kernel driver crashes causing system instability.

### Step 1: Open Kernel Dump

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\Windows\\MEMORY.DMP",
      "symbolsPath": "C:\\Symbols"
    }
  },
  "id": 1
}
```

### Step 2: Analyze Kernel Stack

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!analyze -v",
      "timeoutMinutes": 45
    }
  },
  "id": 2
}
```

### Step 3: Check Driver Stack

```json
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!irql",
      "timeoutMinutes": 15
    }
  },
  "id": 3
}
```

### Step 4: Analyze Driver Information

```json
{
  "method": "tools/call",
    "params": {
      "name": "nexus_enqueue_async_dump_analyze_command",
      "arguments": {
        "sessionId": "sess-000001-abc12345-12345678-0001",
        "command": "lm",
        "timeoutMinutes": 15
      }
    },
    "id": 4
  }
```

## 📊 Advanced Analysis Patterns

### Pattern 1: Multi-Session Analysis

**Use Case**: Analyzing multiple related crash dumps from the same application.

```json
// Open multiple sessions
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crashes\\App_Crash1.dmp"
    }
  },
  "id": 1
}

{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crashes\\App_Crash2.dmp"
    }
  },
  "id": 2
}

// Compare results
{
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/list?command=!analyze"
  },
  "id": 3
}
```

### Pattern 2: Automated Analysis Pipeline

**Use Case**: Running a series of analysis commands automatically.

```json
// Run multiple commands in sequence
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!analyze -v"
    }
  },
  "id": 1
}

{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!locks"
    }
  },
  "id": 2
}

{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!runaway"
    }
  },
  "id": 3
}
```

### Pattern 3: Real-time Monitoring

**Use Case**: Monitoring long-running analysis commands with progress updates.

```json
// Start long-running command
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "command": "!heap -p -a",
      "timeoutMinutes": 30
    }
  },
  "id": 1
}

// Monitor progress (poll every 5 seconds)
{
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/result?sessionId=sess-000001-abc12345-12345678-0001&commandId=cmd-000001-abc12345-12345678-0001"
  },
  "id": 2
}
```

## 🚨 Common Mistakes and Solutions

### Mistake 1: Using Resource URI as Tool Name

**❌ Wrong**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "mcp://nexus/commands/result?sessionId=abc&commandId=cmd123"
  }
}
```

**✅ Correct**:
```json
{
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/result?sessionId=abc&commandId=cmd123"
  }
}
```

### Mistake 2: Not Handling Async Commands

**❌ Wrong**:
```json
// Trying to get results immediately after queuing
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-123",
      "command": "!analyze -v"
    }
  },
  "id": 1
}

// This will fail - command is still queued
{
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/result?sessionId=sess-123&commandId=cmd-456"
  },
  "id": 2
}
```

**✅ Correct**:
```json
// Queue command
{
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_dump_analyze_command",
    "arguments": {
      "sessionId": "sess-123",
      "command": "!analyze -v"
    }
  },
  "id": 1
}

// Wait and poll for completion
{
  "method": "resources/read",
  "params": {
    "uri": "mcp://nexus/commands/result?sessionId=sess-123&commandId=cmd-456"
  },
  "id": 2
}
```

### Mistake 3: Not Closing Sessions

**❌ Wrong**:
```json
// Opening sessions but never closing them
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crash.dmp"
    }
  },
  "id": 1
}
// ... analysis work ...
// Session remains open, consuming resources
```

**✅ Correct**:
```json
// Always close sessions when done
{
  "method": "tools/call",
  "params": {
    "name": "nexus_open_dump_analyze_session",
    "arguments": {
      "dumpPath": "C:\\crash.dmp"
    }
  },
  "id": 1
}
// ... analysis work ...
{
  "method": "tools/call",
  "params": {
    "name": "nexus_close_dump_analyze_session",
    "arguments": {
      "sessionId": "sess-123"
    }
  },
  "id": 2
}
```

## 🎯 Best Practices

### 1. Session Management
- **Always close sessions** when analysis is complete
- **Use appropriate timeouts** for different analysis types
- **Monitor session count** to prevent resource exhaustion
- **Handle session errors** gracefully with proper error handling

### 2. Command Execution
- **Use async commands** for long-running operations
- **Monitor progress** via notifications or polling
- **Handle command failures** with appropriate error handling
- **Use appropriate timeouts** based on command complexity

### 3. Error Handling
- **Check command status** before accessing results
- **Handle timeout errors** gracefully
- **Validate session IDs** before executing commands
- **Provide meaningful error messages** to users

### 4. Performance Optimization
- **Limit concurrent sessions** based on system resources
- **Use appropriate command timeouts** for different analysis types
- **Monitor memory usage** during analysis
- **Clean up resources** promptly when done

### 5. Security Features
- **Command validation** - Dangerous commands are blocked (format, del, shutdown, etc.)
- **Path traversal protection** - Prevents `../` directory traversal attacks
- **SQL injection protection** - Basic SQL injection pattern detection
- **Input validation** - Empty and whitespace-only commands are rejected
- **Command length limits** - Commands limited to 1000 characters maximum

---

## Next Steps

- **🔍 Overview:** [OVERVIEW.md](OVERVIEW.md) - Understand the analysis capabilities
- **📋 Tools:** [TOOLS.md](TOOLS.md) - Learn about available analysis tools
- **📚 Resources:** [RESOURCES.md](RESOURCES.md) - MCP Resources reference
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Configure your environment
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Set up AI integration