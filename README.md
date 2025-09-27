# MCP Nexus

**A comprehensive Model Context Protocol (MCP) server platform with real-time notifications.**

MCP Nexus serves as a unified platform for exposing various tools and capabilities through the Model Context Protocol, enabling AI systems to interact with specialized tools and services seamlessly with live progress updates.

## 📑 Table of Contents

- [🌟 Key Features](#-key-features)
- [📡 Real-Time Notifications](#-whats-new-real-time-notifications)
- [🚀 Quick Start](#-quick-start)
- [📚 Documentation](#-documentation)
- [🔄 Transport Modes](#-transport-modes)
- [🛠 Available Tools](#-available-tools-8-tools)
- [🏃‍♂️ Windows Service](#️-windows-service)
- [🎯 AI Tool Integration](#-ai-tool-integration)
- [🧪 Testing](#-testing)
- [🛠 Development](#-development)
- [🆘 Troubleshooting](#-troubleshooting)

## 🌟 Key Features

- **📡 Real-Time Notifications**: Live command execution progress via server-initiated notifications
- **🔄 Dual Transport Support**: Both stdio and HTTP transport modes with notification support
- **🛠 Modular Architecture**: Easy to extend with new tool categories  
- **🎯 Standards Compliant**: Full JSON-RPC 2.0 and MCP specification compliance
- **🔧 Production Ready**: Robust logging, error handling, and resource management
- **🚀 AI Integration**: Seamless integration with AI tools like Cursor IDE
- **⚡ Async Queue System**: Non-blocking command execution with progress tracking

## 📡 What's New: Real-Time Notifications

MCP Nexus now provides live updates about command execution:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/commandStatus", 
  "params": {
    "commandId": "cmd-123",
    "status": "executing",
    "progress": 75,
    "message": "Analyzing crash dump..."
  }
}
```

**Benefits:**
- **No polling needed** - Get instant updates
- **Progress tracking** - See 0-100% completion
- **Error notifications** - Immediate failure alerts
- **Heartbeat monitoring** - Know long commands are running

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 or later
- Windows (for debugging tools)

### Installation

```bash
# Clone the repository
git clone https://github.com/your-username/mcp_nexus.git
cd mcp_nexus

# Build the project
dotnet build

# Run in stdio mode (default) - with notifications
dotnet run --project mcp_nexus/mcp_nexus.csproj

# Run in HTTP mode - with SSE notifications
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --http
```

### Basic Usage

The server automatically exposes all available tools through the MCP protocol with real-time notification support. Connect using any MCP-compatible client or integrate directly with AI tools like Cursor.

## 📚 Documentation

### 🛠 **[📋 Available Tools](docs/TOOLS.md)**
Complete tool reference, async workflows, notification examples

### ⚙️ **[🔧 Configuration](docs/CONFIGURATION.md)**  
Transport modes, Windows service, environment setup, appsettings.json keys

### 🔌 **[🤖 AI Integration](docs/INTEGRATION.md)**
Cursor IDE setup, MCP clients, notification handling

### 💻 **[👨‍💻 Development](docs/DEVELOPMENT.md)**
Architecture, testing, contribution guide

> 💡 **New to MCP Nexus?** Start with [🔧 CONFIGURATION.md](docs/CONFIGURATION.md) for setup, then [🤖 INTEGRATION.md](docs/INTEGRATION.md) for AI tool integration.

## 🔄 Transport Modes

### Stdio Transport (Recommended)
- **Protocol**: JSON-RPC over stdin/stdout
- **Notifications**: Real-time via stdout
- **Performance**: High performance, low latency
- **Use Case**: Direct integration with AI tools

### HTTP Transport  
- **Protocol**: JSON-RPC over HTTP
- **Notifications**: Server-Sent Events (SSE) at `/mcp/notifications`
- **Endpoint**: `http://localhost:5000/mcp`
- **Use Case**: Development, debugging, web integration

> 📖 **Detailed setup instructions:** [🔧 CONFIGURATION.md](docs/CONFIGURATION.md)

## 🛠 Available Tools (10 tools)

### Windows Debugging Tools
- **Crash Dump Analysis**: `nexus_open_dump_analyze_session`, `nexus_close_dump_analyze_session`
- **Session Management**: Available via MCP Resources (`sessions://list`, `commands://list`)
- **Remote Debugging**: `nexus_start_remote_debug`, `nexus_stop_remote_debug`  
- **Async Command Queue**: `nexus_dump_analyze_session_async_command`, `nexus_debugger_command_cancel`, `nexus_list_debugger_commands`

**🔄 Complete Debugging Workflow:**
```bash
1. nexus_open_dump_analyze_session → Create session, returns sessionId
2. nexus_dump_analyze_session_async_command → Queue command, returns commandId
3. Listen for notifications/commandStatus → Real-time progress updates
4. commands://result → Get final results via MCP Resource
5. Use MCP Resources for session management:
   - `sessions://list` → List all active sessions
   - `commands://list` → List commands for all sessions or filter by sessionId
7. nexus_close_dump_analyze_session → Clean up resources
```

## 📚 MCP Resources

The server provides rich resources for session management and documentation:

### Available Resources
- **`sessions://list`** - List all active debugging sessions
- **`commands://list`** - List commands from all sessions or filter by sessionId
- **`commands://result`** - Get status and results of specific commands
- **`docs://workflows`** - Comprehensive crash analysis workflows and examples
- **`docs://usage`** - Complete usage guide for tools and resources

### Using Resources
```json
// List all sessions
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/read",
  "params": { "uri": "sessions://list" }
}

// Get command result
{
  "jsonrpc": "2.0", 
  "id": 2,
  "method": "resources/read",
  "params": { "uri": "commands://result?sessionId=abc123&commandId=cmd456" }
}

// Get crash analysis workflows
{
  "jsonrpc": "2.0",
  "id": 3, 
  "method": "resources/read",
  "params": { "uri": "docs://workflows" }
}
```

> 📖 **Complete tool reference with examples:** **[📋 TOOLS.md](docs/TOOLS.md)**  
> 📚 **MCP Resources reference:** **[📚 RESOURCES.md](docs/RESOURCES.md)**

## 🏃‍♂️ Windows Service

Install MCP Nexus as a Windows service for persistent operation:

```bash
# Install as Windows service (administrator required)
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --install

# Update service files
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --update

# Access HTTP endpoint + notifications
# http://localhost:5000/mcp
# http://localhost:5000/mcp/notifications
```

> 📖 **Service installation guide:** [🔧 CONFIGURATION.md](docs/CONFIGURATION.md#windows-service-configuration)

## 🎯 AI Tool Integration

### Cursor IDE (Recommended)

**Stdio Mode with Notifications** (`.cursor/mcp.json`):
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

**HTTP Mode with SSE** (`.cursor/mcp.json`):
```json
{
  "servers": {
    "mcp-nexus-http": {
      "url": "http://localhost:5000/mcp",
      "type": "http"
    }
  }
}
```

> 📖 **Complete integration guide:** **[🤖 INTEGRATION.md](docs/INTEGRATION.md)**

## 🧪 Testing

```bash
# Run all tests (381 tests, ~4-5 seconds)
dotnet test

# Run notification-specific tests
dotnet test --filter "Notification"

# Test coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Test Coverage:**
- ✅ **527 tests passing** (100% success rate)
- ✅ **Zero warnings** in build
- ✅ **46%+ line coverage** with comprehensive notification testing
- ✅ **7 dedicated notification test classes**

## 🛠 Development

The platform uses a modular architecture with integrated notification support:

```
MCP Nexus
├── Core Services (MCP protocol, tools, notifications)
├── Transport Layer (stdio + HTTP with notifications)  
├── Notification System (real-time command updates)
└── Tool Modules (debugging, time, extensible)
```

> 📖 **Architecture and contribution guide:** **[👨‍💻 DEVELOPMENT.md](docs/DEVELOPMENT.md)**

## 🆘 Troubleshooting

### Common Issues
- **Connection**: Check file paths (stdio) or server status (HTTP)
- **Notifications**: Verify client supports MCP notification capabilities
- **Build**: Run `dotnet build` before starting
- **Permissions**: Administrator required for system-level debugging

### Getting Help
1. **Check Logs**: Review application logs for detailed error information
2. **Test Manually**: Use curl to test HTTP endpoints and SSE notifications
3. **Read Docs**: Check [🔧 CONFIGURATION.md](docs/CONFIGURATION.md) and [🤖 INTEGRATION.md](docs/INTEGRATION.md)
4. **Community**: Report issues on GitHub

> 📖 **Troubleshooting guides in:** [🔧 CONFIGURATION.md](docs/CONFIGURATION.md) and [🤖 INTEGRATION.md](docs/INTEGRATION.md)

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- [Model Context Protocol](https://modelcontextprotocol.io/) specification
- Windows Debugging Tools community  
- .NET and ASP.NET Core teams

---

**MCP Nexus** - Bridging AI and specialized tools through real-time Model Context Protocol communication.