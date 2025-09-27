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

- **📡 Real-Time Notifications**: Live command execution progress via server-initiated notifications (STDIO only)
- **🔄 Dual Transport Support**: Both stdio and HTTP transport modes (notifications in STDIO only)
- **🛠 Official SDK Integration**: Built on official MCP C# SDK for future-proofing
- **🎯 Standards Compliant**: Full JSON-RPC 2.0 and MCP specification compliance
- **🔧 Production Ready**: Robust logging, error handling, and resource management
- **🚀 AI Integration**: Seamless integration with AI tools like Cursor IDE
- **⚡ Async Queue System**: Non-blocking command execution with progress tracking

## 📡 Real-Time Notifications

MCP Nexus provides live updates about command execution (STDIO mode only):

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

> ⚠️ **Note**: Notifications are only available in STDIO mode. HTTP mode provides basic tool/resource functionality without notifications.

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

### 📖 **[💡 Usage Examples](docs/USAGE_EXAMPLES.md)**
Step-by-step crash dump analysis workflow with real examples

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
- **Notifications**: ✅ Real-time via stdout
- **Performance**: High performance, low latency
- **Use Case**: Direct integration with AI tools like Cursor

### HTTP Transport  
- **Protocol**: JSON-RPC over HTTP
- **Notifications**: ❌ Not available (SDK limitation)
- **Endpoint**: `http://localhost:5511/mcp`
- **Use Case**: Development, debugging, basic tool/resource access

> 📖 **Detailed setup instructions:** [🔧 CONFIGURATION.md](docs/CONFIGURATION.md)

## 🛠 Available Tools (4 tools)

### Windows Debugging Tools
- **🚀 Session Management**: `nexus_open_dump_analyze_session`, `nexus_close_dump_analyze_session`
- **⚡ Command Execution**: `nexus_enqueue_async_dump_analyze_command`, `nexus_read_dump_analyze_command_result`

**🔄 Complete Debugging Workflow:**
```bash
1. nexus_open_dump_analyze_session → Create session, returns sessionId
2. nexus_enqueue_async_dump_analyze_command → Queue command, returns commandId
3. nexus_read_dump_analyze_command_result → Get command results (replaces resource)
4. Use MCP Resources for monitoring:
   - sessions → List all active sessions
   - commands → List commands from all sessions
   - workflows → Get analysis patterns
   - usage → Get this usage guide
5. nexus_close_dump_analyze_session → Clean up resources
```

## 📚 MCP Resources

The server provides rich resources for session management and documentation:

### Available Resources
- **`sessions`** - List all active debugging sessions
- **`commands`** - List commands from all sessions
- **`workflows`** - Comprehensive crash analysis workflows and examples
- **`usage`** - Complete usage guide for tools and resources

### Using Resources
```json
// List all sessions
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/read",
  "params": { "uri": "sessions" }
}

// List all commands
{
  "jsonrpc": "2.0", 
  "id": 2,
  "method": "resources/read",
  "params": { "uri": "commands" }
}

// Get crash analysis workflows
{
  "jsonrpc": "2.0",
  "id": 3, 
  "method": "resources/read",
  "params": { "uri": "workflows" }
}

// Get usage guide
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "resources/read", 
  "params": { "uri": "usage" }
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