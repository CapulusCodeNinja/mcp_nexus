# Configuration Guide

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [📋 Tools](TOOLS.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## Command Line Options

- `--http`: Run in HTTP transport mode
- `--service`: Run in Windows service mode (implies --http)
- `--cdb-path <path>`: Custom path to CDB.exe for debugging tools
- `--install`: Install MCP Nexus as Windows service (Windows only)
- `--update`: Update existing Windows service files and restart (Windows only)
- `--uninstall`: Uninstall MCP Nexus Windows service (Windows only)
- `--force-uninstall`: Force uninstall service with registry cleanup (Windows only)
- `--help`: Show command line help

## Environment Variables

- `MCP_NEXUS_CDB_PATH`: Default CDB.exe path
- `MCP_NEXUS_LOG_LEVEL`: Logging level (Debug, Info, Warn, Error)

## Transport Modes

### Stdio Transport (Recommended)
- **Protocol**: JSON-RPC over stdin/stdout
- **Performance**: High performance, low latency
- **Notifications**: Real-time via stdout
- **Use Case**: Direct integration with AI tools
- **Command**: `dotnet run --project mcp_nexus/mcp_nexus.csproj`

### HTTP Transport
- **Protocol**: JSON-RPC over HTTP
- **Endpoint**: `http://localhost:5000/mcp`
- **Notifications**: Server-Sent Events (SSE) at `/mcp/notifications`
- **Use Case**: Development, debugging, web integration
- **Command**: `dotnet run --project mcp_nexus/mcp_nexus.csproj -- --http`

## Debugging Tools Setup

For Windows debugging capabilities:

1. **Install Windows Debugging Tools**:
   - Download from Microsoft
   - Or install via Windows SDK

2. **Configure CDB Path** (optional):
   ```bash
   dotnet run --project mcp_nexus/mcp_nexus.csproj -- --cdb-path "C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe"
   ```

3. **Automatic Detection**: If no path specified, the system searches:
   - Windows Kits installation paths
   - System PATH environment
   - Common installation directories

## Windows Service Configuration

### Installation
```bash
# Install as Windows service (requires administrator privileges)
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --install

# Update existing Windows service (stop, update files, restart)
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --update

# Uninstall the Windows service
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --uninstall
```

### Service Features
- **Auto-start**: Service starts automatically on system boot
- **HTTP Mode**: Service runs in HTTP transport mode with notifications
- **Program Files**: Installed to `C:\Program Files\MCP-Nexus`
- **Event Logging**: Logs to Windows Event Log and files
- **Management**: Use Windows Services console or command line
- **Safe Updates**: Automatic backups to `C:\Program Files\MCP-Nexus\backups\[timestamp]`

### Service Management
```bash
# Check service status
sc query "MCP-Nexus"

# Start/stop service manually
sc start "MCP-Nexus"
sc stop "MCP-Nexus"

# Access HTTP endpoint when service is running
# http://localhost:5000/mcp
# Notifications: http://localhost:5000/mcp/notifications
```

---

## Next Steps

- **📋 Tools:** [TOOLS.md](TOOLS.md) - Learn about available tools and notifications
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Connect with AI tools like Cursor IDE
- **👨‍💻 Development:** [DEVELOPMENT.md](DEVELOPMENT.md) - Understand the architecture
