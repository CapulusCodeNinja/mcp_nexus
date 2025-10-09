# AI Integration Guide

**AI-Powered Windows Crash Dump Analysis Integration**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📋 Tools](TOOLS.md) | [📚 Resources](RESOURCES.md) | [🔧 Configuration](CONFIGURATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## 🎯 Overview

MCP Nexus is designed to seamlessly integrate with AI systems for intelligent crash dump analysis. This guide covers integration with popular AI clients, configuration options, and best practices for AI-powered analysis workflows.

## 🤖 Supported AI Clients

### Cursor IDE (Recommended)

**Why Cursor IDE?**
- Native MCP support with real-time notifications
- Excellent debugging and analysis workflow integration
- Built-in code analysis and pattern recognition
- Seamless integration with development workflows

#### Configuration

**Global Configuration** (`~/.cursor/mcp.json`):
```json
{
  "mcpServers": {
    "mcp-nexus": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/mcp_nexus/mcp_nexus.csproj"
      ],
      "cwd": "/path/to/mcp_nexus",
      "type": "stdio"
    }
  }
}
```

**Workspace Configuration** (`.cursor/mcp.json`):
```json
{
  "servers": {
    "mcp-nexus": {
      "command": "dotnet",
      "args": ["run", "--project", "./mcp_nexus/mcp_nexus.csproj"],
      "type": "stdio"
    }
  }
}
```

#### Usage in Cursor IDE

**Basic Analysis Request**:
```
Analyze the crash dump at C:\crashes\MyApp.dmp and identify the root cause of the crash.
```

**Advanced Analysis Request**:
```
Perform a comprehensive analysis of the crash dump at C:\crashes\SystemCrash.dmp. 
Include memory analysis, thread analysis, and provide recommendations for fixing the issue.
```

**Pattern Recognition Request**:
```
Analyze the crash dump and identify if this follows any known crash patterns. 
Compare with similar crashes and suggest preventive measures.
```

### Claude Desktop

**Configuration** (`claude_desktop_config.json`):
```json
{
  "mcpServers": {
    "mcp-nexus": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\mcp_nexus\\mcp_nexus.csproj"],
      "cwd": "C:\\path\\to\\mcp_nexus"
    }
  }
}
```

#### Usage in Claude Desktop

**Crash Analysis Conversation**:
```
User: I have a crash dump from my Windows application. Can you help me analyze it?

Claude: I'd be happy to help you analyze the crash dump! I can use MCP Nexus to perform a comprehensive analysis. Let me start by opening the crash dump and running some initial analysis commands.

First, I'll open the crash dump session and then run the standard analysis commands to identify the root cause of the crash.
```

### Other MCP Clients

**Generic MCP Configuration**:
```json
{
  "mcpServers": {
    "mcp-nexus": {
      "command": "dotnet",
      "args": ["run", "--project", "./mcp_nexus/mcp_nexus.csproj"],
      "cwd": "./mcp_nexus",
      "type": "stdio"
    }
  }
}
```

## 🔄 Transport Modes

### Stdio Transport (Recommended for AI)

**Protocol**: JSON-RPC over stdin/stdout
**Performance**: High performance, low latency
**Notifications**: Real-time via stdout
**Use Case**: Direct integration with AI tools

**Advantages**:
- ✅ Real-time notifications
- ✅ High performance
- ✅ Low latency
- ✅ Native AI integration
- ✅ Progress tracking

**Configuration**:
```json
{
  "servers": {
    "mcp-nexus": {
      "command": "dotnet",
      "args": ["run", "--project", "./mcp_nexus/mcp_nexus.csproj"],
      "type": "stdio"
    }
  }
}
```

### HTTP Transport

**Protocol**: JSON-RPC over HTTP
**Endpoint**: `http://localhost:5000/` (root path)`
**Use Case**: Web integration, development, debugging

**Advantages**:
- ✅ Web integration
- ✅ Development debugging
- ✅ Multiple client support
- ✅ REST-like interface

**Disadvantages**:
- ❌ No real-time notifications (SDK limitation)
- ❌ Higher latency
- ❌ More complex setup

**Configuration**:
```json
{
  "servers": {
    "mcp-nexus-http": {
      "url": "http://localhost:5000/",
      "type": "http"
    }
  }
}
```

**Port Configuration**:
- **Development**: `http://localhost:5117/`
- **Production**: `http://localhost:5000/`
- **Service Mode**: `http://localhost:5511/`
- **Custom Port**: `http://localhost:<PORT>/`

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

### Notification Benefits for AI

**Real-time Updates**:
- No need for constant polling
- Immediate feedback on command progress
- Better user experience during long analysis

**Progress Tracking**:
- See 0-100% completion progress
- Understand what's happening during analysis
- Provide meaningful status updates to users

**Error Notifications**:
- Immediate failure alerts
- Better error handling and recovery
- Improved debugging experience

## 🔍 AI Analysis Workflows

### Basic Crash Analysis Workflow

**AI Prompt**:
```
Analyze the crash dump at C:\crashes\MyApp.dmp and provide a comprehensive analysis including:
1. Root cause identification
2. Stack trace analysis
3. Memory state examination
4. Recommendations for fixing the issue
```

**AI Response Pattern**:
```
I'll help you analyze this crash dump. Let me start by opening the dump file and running the standard analysis commands.

1. First, I'll open the crash dump session
2. Then I'll run !analyze -v to get the initial analysis
3. I'll examine the stack trace with kb
4. I'll check for memory issues with !heap -p -a
5. Finally, I'll provide recommendations based on the findings

Let me start the analysis now...
```

### Memory Corruption Analysis Workflow

**AI Prompt**:
```
The application is experiencing memory corruption issues. Analyze the crash dump and identify:
1. Type of memory corruption
2. Source of the corruption
3. Memory leak patterns
4. Thread synchronization issues
```

**AI Response Pattern**:
```
I'll perform a comprehensive memory analysis to identify the corruption source. This will involve:

1. Opening the crash dump
2. Running heap analysis (!heap -p -a)
3. Checking memory usage (!memusage)
4. Analyzing thread stacks (~*k)
5. Examining lock information (!locks)

Let me start the memory analysis...
```

### System Crash (BSOD) Analysis Workflow

**AI Prompt**:
```
The system crashed with a Blue Screen of Death. Analyze the kernel dump and identify:
1. Driver or component causing the crash
2. System state at time of crash
3. IRQL level and context
4. Recommendations for system stability
```

**AI Response Pattern**:
```
I'll analyze this system crash dump to identify the root cause. This requires:

1. Opening the kernel dump (MEMORY.DMP)
2. Running comprehensive analysis (!analyze -v)
3. Checking IRQL levels (!irql)
4. Examining driver information (lm)
5. Analyzing the kernel stack

Let me start the system crash analysis...
```

## 🎯 AI Integration Best Practices

### 1. Prompt Engineering

**Effective Prompts**:
- Be specific about the analysis type needed
- Include relevant context about the application
- Specify the level of detail required
- Ask for actionable recommendations

**Example Prompts**:
```
Good: "Analyze this crash dump and identify the root cause of the access violation in the ProcessUserInput function."

Bad: "Look at this crash dump."
```

### 2. Error Handling

**Handle Common Scenarios**:
- Dump file not accessible
- Symbol loading failures
- Command timeouts
- Session errors

**AI Response Pattern**:
```
I encountered an issue accessing the dump file. Let me check a few things:

1. Verify the file path is correct
2. Check if the file exists and is readable
3. Ensure we have the necessary permissions
4. Try alternative analysis approaches if needed

Let me investigate this issue...
```

### 3. Progress Communication

**Keep Users Informed**:
- Explain what's happening during analysis
- Provide progress updates
- Explain any delays or issues
- Set realistic expectations

**AI Response Pattern**:
```
I'm currently running the analysis commands. This may take a few minutes depending on the dump size and complexity.

Current progress:
- ✅ Dump file opened successfully
- ✅ Symbols loaded
- 🔄 Running !analyze -v (75% complete)
- ⏳ Next: Stack trace analysis

I'll update you as the analysis progresses...
```

### 4. Result Interpretation

**Provide Context**:
- Explain what the results mean
- Identify key findings
- Provide actionable recommendations
- Suggest next steps

**AI Response Pattern**:
```
Based on the analysis, I've identified the root cause:

**Root Cause**: Null pointer dereference in ProcessUserInput function
**Exception**: 0xC0000005 (Access Violation)
**Location**: MyApp!ProcessUserInput+0x45

**Analysis**:
The crash occurred when the code attempted to read from address 0x0000000000000000, which indicates a null pointer was dereferenced.

**Recommendations**:
1. Add null pointer checks before dereferencing pointers
2. Implement proper input validation
3. Use static analysis tools to detect similar issues
4. Add unit tests for edge cases

Would you like me to examine any specific aspects of this analysis in more detail?
```

## 🔧 Advanced Integration Features

### Custom Analysis Extensions

**Discover Available Extensions**:
Use the `extensions` resource to list all available extension scripts:
```json
{
  "jsonrpc": "2.0",
  "method": "resources/read",
  "params": {
    "uri": "extensions"
  }
}
```

**Execute Extensions**:
Use the `nexus_enqueue_async_extension_command` tool to execute extensions:
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "nexus_enqueue_async_extension_command",
    "arguments": {
      "sessionId": "sess-000001-abc12345-12345678-0001",
      "extensionName": "memory_corruption_analysis",
      "parameters": {}
    }
  }
}
```

### Pattern Recognition Integration

**AI Pattern Recognition**:
```
Based on the crash analysis, I can see this follows the "Buffer Overflow" pattern:

**Pattern Characteristics**:
- Access violation at specific memory address
- Stack corruption indicators
- Heap corruption signs
- Common in C/C++ applications

**Prevention Strategies**:
1. Use bounds checking
2. Implement stack canaries
3. Use safe string functions
4. Enable compiler security features

This pattern is commonly seen in applications that don't properly validate input lengths.
```

### Automated Reporting

**Generate Analysis Reports**:
```
I've completed the analysis and generated a comprehensive report:

**Crash Analysis Report**
- **Date**: 2024-01-15
- **Dump File**: MyApp_2024-01-15.dmp
- **Analysis Time**: 2 minutes 30 seconds
- **Root Cause**: Null pointer dereference
- **Severity**: High
- **Confidence**: 95%

**Key Findings**:
1. Access violation in ProcessUserInput function
2. Null pointer dereference at address 0x0000000000000000
3. Stack trace shows clear execution path
4. No memory corruption detected

**Recommendations**:
1. Add null pointer validation
2. Implement input sanitization
3. Add defensive programming practices
4. Consider using static analysis tools

**Next Steps**:
1. Review ProcessUserInput function
2. Add unit tests for edge cases
3. Implement code review process
4. Monitor for similar issues

Would you like me to save this report or analyze any specific aspects further?
```

## 🚨 Troubleshooting Integration Issues

### Log Locations

**Service Mode (Windows Service):**
- **Main Logs**: `C:\ProgramData\MCP-Nexus\Logs\`
- **Internal Logs**: `C:\ProgramData\MCP-Nexus\Logs\mcp-nexus-internal.log`
- **Archive**: `C:\ProgramData\MCP-Nexus\Logs\archive\`

**Interactive Mode:**
- **Main Logs**: `.\logs\` (relative to application directory)
- **Internal Logs**: `.\mcp-nexus-internal.log`

### Common Issues

**Connection Problems**:
- Check MCP client configuration
- Verify MCP Nexus is running
- Ensure correct file paths
- Check permissions

**Symbol Loading Issues**:
- Verify symbol path configuration
- Check internet connectivity
- Ensure symbol server access
- Verify dump file accessibility

**Command Timeout Issues**:
- Increase timeout values
- Check system resources
- Verify dump file size
- Monitor command progress

### Debugging Steps

1. **Check MCP Client Logs**:
   - Review client-side error messages
   - Check connection status
   - Verify configuration

2. **Check MCP Nexus Logs**:
   - Review server-side logs
   - Check command execution
   - Verify resource usage

3. **Test Manually**:
   - Use WinDBG directly
   - Test MCP commands manually
   - Verify dump file accessibility

4. **Check System Resources**:
   - Monitor memory usage
   - Check CPU utilization
   - Verify disk space

## 📊 Performance Optimization

### For AI Integration

**Optimize Response Times**:
- Use appropriate command timeouts
- Implement command queuing
- Cache analysis results
- Use parallel processing when possible

**Memory Management**:
- Limit concurrent sessions
- Clean up resources promptly
- Monitor memory usage
- Implement resource limits

**Network Optimization**:
- Use local symbol servers
- Cache symbols locally
- Optimize symbol loading
- Minimize network calls


---

## Next Steps

- **🔍 Overview:** [OVERVIEW.md](OVERVIEW.md) - Understand the analysis capabilities
- **📋 Tools:** [TOOLS.md](TOOLS.md) - Learn about available analysis tools
- **📚 Resources:** [RESOURCES.md](RESOURCES.md) - MCP Resources reference
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Configure your environment
- **👨‍💻 Development:** [DEVELOPMENT.md](DEVELOPMENT.md) - Understand the architecture