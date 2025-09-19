# MCP Nexus

**A comprehensive Model Context Protocol (MCP) server platform providing diverse tools for AI integration.**

MCP Nexus serves as a unified platform for exposing various tools and capabilities through the Model Context Protocol, enabling AI systems to interact with specialized tools and services seamlessly.

## Overview

MCP Nexus implements the [Model Context Protocol](https://modelcontextprotocol.io/) specification, providing a standardized way for AI systems to access external tools and resources. The platform supports multiple transport modes and is designed to accommodate various tool categories beyond its initial debugging focus.

### Key Features

- **🔄 Dual Transport Support**: Both stdio and HTTP transport modes
- **🛠 Modular Architecture**: Easy to extend with new tool categories  
- **🎯 Standards Compliant**: Full JSON-RPC 2.0 and MCP specification compliance
- **🔧 Production Ready**: Robust logging, error handling, and resource management
- **🚀 AI Integration**: Seamless integration with AI tools like Cursor IDE

## Quick Start

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

# Run in stdio mode (default)
dotnet run

# Run in HTTP mode
dotnet run -- --http
```

### Basic Usage

The server automatically exposes all available tools through the MCP protocol. Connect using any MCP-compatible client or integrate directly with AI tools like Cursor.

## Transport Modes

### Stdio Transport (Recommended)
- **Protocol**: JSON-RPC over stdin/stdout
- **Performance**: High performance, low latency
- **Use Case**: Direct integration with AI tools
- **Command**: `dotnet run`

### HTTP Transport
- **Protocol**: JSON-RPC over HTTP
- **Endpoint**: `http://localhost:5000/mcp`
- **Use Case**: Development, debugging, web integration
- **Command**: `dotnet run -- --http`

## Available Tools

### Debugging Tools (10 tools)
Windows debugging capabilities through WinDBG/CDB integration:

- **Crash Dump Analysis**: `open_windbg_dump`, `close_windbg_dump`
- **Remote Debugging**: `open_windbg_remote`, `close_windbg_remote`  
- **Command Execution**: `run_windbg_cmd`
- **File Management**: `list_windbg_dumps`
- **Advanced Analysis**: `get_session_info`, `analyze_call_stack`, `analyze_memory`, `analyze_crash_patterns`

### Utility Tools (1 tool)
- **Time Services**: `get_current_time` - Get current time for any city

### Future Tool Categories
The platform is designed to support additional tool categories:
- **System Administration Tools** (planned)
- **Development Tools** (planned)
- **Data Analysis Tools** (planned)
- **Network Tools** (planned)

## Integration with AI Tools

### Cursor IDE Integration

#### Configuration for Stdio Mode (Recommended)

Create or edit your MCP configuration:

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

#### Configuration for HTTP Mode

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

Start the server first: `dotnet run -- --http`

### Other MCP Clients

Any MCP-compatible client can connect to MCP Nexus:

```bash
# Test with curl (HTTP mode)
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list",
    "params": {}
  }'
```

## Configuration

### Command Line Options

- `--http`: Run in HTTP transport mode
- `--cdb-path <path>`: Custom path to CDB.exe for debugging tools
- `--verbose`: Enable verbose logging

### Environment Variables

- `MCP_NEXUS_CDB_PATH`: Default CDB.exe path
- `MCP_NEXUS_LOG_LEVEL`: Logging level (Debug, Info, Warn, Error)

### Advanced Configuration

#### Debugging Tools Setup

For Windows debugging capabilities:

1. **Install Windows Debugging Tools**:
   - Download from Microsoft
   - Or install via Windows SDK

2. **Configure CDB Path** (optional):
   ```bash
   dotnet run -- --cdb-path "C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe"
   ```

3. **Automatic Detection**: If no path specified, the system searches:
   - Windows Kits installation paths
   - System PATH environment
   - Common installation directories

## Development

### Architecture

The platform follows a modular architecture:

```
MCP Nexus
├── Core Services
│   ├── McpProtocolService    # MCP protocol handling
│   ├── McpToolDefinitionService  # Tool definitions
│   └── McpToolExecutionService   # Tool execution
├── Transport Layer
│   ├── Stdio Transport       # stdin/stdout communication
│   └── HTTP Transport        # HTTP API endpoints
└── Tool Modules
    ├── Debugging Tools       # WinDBG/CDB integration
    ├── Time Tools           # Time utilities
    └── [Future Tools]       # Extensible tool system
```

### Adding New Tools

1. **Define Tool Schema**: Add tool definition to `McpToolDefinitionService`
2. **Implement Logic**: Add execution logic to `McpToolExecutionService`  
3. **Register Services**: Update dependency injection in `Program.cs`
4. **Update Documentation**: Add tool description to README

### Contributing

1. Fork the repository
2. Create a feature branch
3. Add your tool implementation
4. Update documentation
5. Submit a pull request

## Troubleshooting

### Common Issues

#### Connection Problems
- **Stdio Mode**: Check file paths in MCP configuration
- **HTTP Mode**: Ensure server is running before client connects
- **Build Issues**: Run `dotnet build` before starting

#### Tool-Specific Issues
- **Debugging Tools**: Verify Windows Debugging Tools installation
- **Permissions**: Run as administrator for system-level debugging
- **Port Conflicts**: Use different port for HTTP mode if 5000 is occupied

#### Performance Issues
- **Stdio Mode**: Preferred for performance-critical applications
- **HTTP Mode**: Better for development and debugging
- **Logging**: Adjust log level in production environments

### Getting Help

1. **Check Logs**: Review application logs for detailed error information
2. **Test Manually**: Use curl to test HTTP mode endpoints
3. **Verify Tools**: Ensure all prerequisite tools are installed
4. **Community**: Report issues on GitHub

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Model Context Protocol](https://modelcontextprotocol.io/) specification
- Windows Debugging Tools community
- .NET and ASP.NET Core teams

---

**MCP Nexus** - Bridging AI and specialized tools through the Model Context Protocol.