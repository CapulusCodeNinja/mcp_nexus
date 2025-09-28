# Configuration Guide

**Windows Crash Dump Analysis Configuration**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📋 Tools](TOOLS.md) | [📚 Resources](RESOURCES.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## 🚀 Quick Setup

### Prerequisites
- **Windows 10/11** or **Windows Server 2016+**
- **.NET 8.0** or later
- **Windows Debugging Tools** (WinDBG/CDB)

### Installation Steps

1. **Install Windows Debugging Tools**:
   ```bash
   # Download from Microsoft
   # Or install via Windows SDK
   # Default location: C:\Program Files\Windows Kits\10\Debuggers\x64\
   ```

2. **Clone and Build MCP Nexus**:
   ```bash
   git clone https://github.com/your-username/mcp_nexus.git
   cd mcp_nexus
   dotnet build
   ```

3. **Configure AI Client** (see [Integration Guide](INTEGRATION.md))

## ⚙️ Command Line Options

### Basic Options
- `--http`: Run in HTTP transport mode (for web integration)
- `--service`: Run as Windows service (implies --http)
- `--cdb-path <path>`: Custom path to CDB.exe for debugging tools
- `--help`: Show command line help

### Service Management (Windows)
- `--install`: Install MCP Nexus as Windows service
- `--update`: Update existing Windows service files and restart
- `--uninstall`: Uninstall MCP Nexus Windows service
- `--force-uninstall`: Force uninstall with registry cleanup

### Environment Variables
- `MCP_NEXUS_CDB_PATH`: Default CDB.exe path
- `MCP_NEXUS_LOG_LEVEL`: Logging level (Debug, Info, Warn, Error)
- `MCP_NEXUS_SYMBOL_PATH`: Default symbol search path

## 🔧 Analysis Configuration

### Application Settings (appsettings.json)

MCP Nexus reads configuration from `appsettings.json` under the `McpNexus` root key.

#### Session Management

**Section**: `McpNexus:SessionManagement`

```json
{
  "McpNexus": {
    "SessionManagement": {
      "MaxConcurrentSessions": 1000,
      "SessionTimeoutMinutes": 30,
      "CleanupIntervalMinutes": 5,
      "DisposalTimeoutSeconds": 30,
      "DefaultCommandTimeoutMinutes": 10,
      "MemoryCleanupThresholdMB": 1024
    }
  }
}
```

**Configuration Details:**
- `MaxConcurrentSessions` (>0): Maximum active analysis sessions
- `SessionTimeoutMinutes` (>0): Idle timeout before auto-closing sessions
- `CleanupIntervalMinutes` (>0): How often expired sessions are cleaned
- `DisposalTimeoutSeconds` (>0): Max time to dispose a session during cleanup
- `DefaultCommandTimeoutMinutes` (>0): Default per-session command timeout
- `MemoryCleanupThresholdMB` (>0): Memory threshold to trigger extra cleanup

#### Debugging Tools Configuration

**Section**: `McpNexus:Debugging`

```json
{
  "McpNexus": {
    "Debugging": {
      "CdbPath": null,
      "CommandTimeoutMs": 600000,
      "SymbolServerTimeoutMs": 300000,
      "SymbolServerMaxRetries": 1,
      "SymbolSearchPath": "srv*C:\\Symbols*https://msdl.microsoft.com/download/symbols",
      "StartupDelayMs": 2000,
      "EnableVerboseLogging": false,
      "MaxDumpSizeMB": 2048,
      "AutoSymbolLoading": true
    }
  }
}
```

**Configuration Details:**
- `CdbPath`: Optional absolute path to `cdb.exe` (CLI `--cdb-path` overrides)
- `CommandTimeoutMs` (>0): Timeout for CDB operations (10 minutes default)
- `SymbolServerTimeoutMs` (>=0): Symbol fetch timeout (5 minutes default)
- `SymbolServerMaxRetries` (>=0): Retries for symbol server calls
- `SymbolSearchPath`: Windows symbol path (use doubled backslashes in JSON)
- `StartupDelayMs`: Initial delay for CDB process startup
- `EnableVerboseLogging`: Enable detailed debugging output
- `MaxDumpSizeMB`: Maximum dump file size to process (2GB default)
- `AutoSymbolLoading`: Automatically load symbols during analysis

#### Analysis Configuration

**Section**: `McpNexus:Analysis`

```json
{
  "McpNexus": {
    "Analysis": {
      "DefaultAnalysisCommands": [
        "!analyze -v",
        "!locks",
        "!runaway",
        "!memusage"
      ],
      "EnablePatternRecognition": true,
      "AutoGenerateReports": true,
      "ReportFormat": "json",
      "IncludeStackTraces": true,
      "IncludeMemoryAnalysis": true,
      "IncludeThreadAnalysis": true
    }
  }
}
```

**Configuration Details:**
- `DefaultAnalysisCommands`: Commands to run automatically on dump load
- `EnablePatternRecognition`: Enable AI-powered pattern recognition
- `AutoGenerateReports`: Automatically generate analysis reports
- `ReportFormat`: Report format (json, xml, text)
- `IncludeStackTraces`: Include stack trace analysis in reports
- `IncludeMemoryAnalysis`: Include memory analysis in reports
- `IncludeThreadAnalysis`: Include thread analysis in reports

## 🔍 Symbol Server Configuration

### Microsoft Symbol Server

**Default Configuration:**
```json
{
  "SymbolSearchPath": "srv*C:\\Symbols*https://msdl.microsoft.com/download/symbols"
}
```

**Custom Symbol Servers:**
```json
{
  "SymbolSearchPath": "srv*C:\\Symbols*https://msdl.microsoft.com/download/symbols;srv*C:\\LocalSymbols*\\\\server\\symbols"
}
```

### Symbol Server Setup

1. **Create Symbol Directory**:
   ```bash
   mkdir C:\Symbols
   ```

2. **Configure Symbol Path**:
   ```json
   {
     "McpNexus": {
       "Debugging": {
         "SymbolSearchPath": "srv*C:\\Symbols*https://msdl.microsoft.com/download/symbols"
       }
     }
   }
   ```

3. **Test Symbol Loading**:
   ```bash
   # Run a test analysis to verify symbol loading
   dotnet run --project mcp_nexus/mcp_nexus.csproj
   ```

## 🌐 Transport Modes

### Stdio Transport (Recommended for AI)

**Protocol**: JSON-RPC over stdin/stdout
**Performance**: High performance, low latency
**Notifications**: Real-time via stdout
**Use Case**: Direct integration with AI tools like Cursor IDE

**Command**:
```bash
dotnet run --project mcp_nexus/mcp_nexus.csproj
```

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
**Endpoint**: `http://localhost:5000/mcp`
**Notifications**: Server-Sent Events (SSE) at `/mcp/notifications`
**Use Case**: Development, debugging, web integration

**Command**:
```bash
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --http
```

**Configuration**:
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

## 🏃‍♂️ Windows Service Configuration

### Installation

```bash
# Install as Windows service (requires administrator privileges)
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --install

# Update existing Windows service
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

## 🔧 Debugging Tools Setup

### Windows Debugging Tools Installation

1. **Download from Microsoft**:
   - Visit [Windows SDK Downloads](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/)
   - Download Windows 10/11 SDK
   - Install with "Debugging Tools for Windows" option

2. **Install via Windows SDK**:
   ```bash
   # Download Windows SDK installer
   # Run installer and select "Debugging Tools for Windows"
   ```

3. **Verify Installation**:
   ```bash
   # Check if CDB is available
   "C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe" -version
   ```

### CDB Path Configuration

**Automatic Detection**:
MCP Nexus automatically searches for CDB in:
- Windows Kits installation paths
- System PATH environment
- Common installation directories

**Manual Configuration**:
```bash
# Command line
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --cdb-path "C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe"

# Environment variable
set MCP_NEXUS_CDB_PATH=C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe

# Configuration file
{
  "McpNexus": {
    "Debugging": {
      "CdbPath": "C:\\Program Files\\Windows Kits\\10\\Debuggers\\x64\\cdb.exe"
    }
  }
}
```

## 📊 Performance Tuning

### Memory Configuration

**For Large Dump Files**:
```json
{
  "McpNexus": {
    "SessionManagement": {
      "MemoryCleanupThresholdMB": 2048,
      "MaxConcurrentSessions": 10
    },
    "Debugging": {
      "MaxDumpSizeMB": 4096,
      "CommandTimeoutMs": 1200000
    }
  }
}
```

**For High-Volume Analysis**:
```json
{
  "McpNexus": {
    "SessionManagement": {
      "MaxConcurrentSessions": 100,
      "SessionTimeoutMinutes": 60,
      "CleanupIntervalMinutes": 2
    }
  }
}
```

### Symbol Server Optimization

**Local Symbol Cache**:
```json
{
  "McpNexus": {
    "Debugging": {
      "SymbolSearchPath": "srv*C:\\Symbols*https://msdl.microsoft.com/download/symbols",
      "SymbolServerTimeoutMs": 600000,
      "SymbolServerMaxRetries": 3
    }
  }
}
```

## 🔒 Security Configuration

### File Access Permissions

**Dump File Access**:
- Ensure MCP Nexus has read access to dump files
- Consider using dedicated service account for production

**Symbol Server Access**:
- Configure firewall rules for symbol server access
- Use HTTPS for symbol server connections
- Consider proxy configuration for corporate environments

### Service Account Configuration

**Recommended Service Account**:
- Create dedicated service account for MCP Nexus
- Grant minimal required permissions
- Configure for automatic logon if needed

## 🆘 Troubleshooting

### Common Issues

**Symbol Loading Failures**:
- Check symbol path configuration
- Verify internet connectivity
- Check firewall settings
- Ensure sufficient disk space for symbol cache

**Permission Errors**:
- Check file access permissions
- Verify service account permissions

**Timeout Issues**:
- Increase command timeout values
- Check system resources
- Verify dump file accessibility

**Memory Issues**:
- Increase memory limits
- Reduce concurrent sessions
- Check system available memory

### Debugging Configuration

**Enable Verbose Logging**:
```json
{
  "McpNexus": {
    "Debugging": {
      "EnableVerboseLogging": true
    }
  }
}
```

**Log Level Configuration**:
```bash
# Environment variable
set MCP_NEXUS_LOG_LEVEL=Debug

# Command line
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --log-level Debug
```

### Getting Help

1. **Check Logs**: Review application logs for detailed error information
2. **Test Manually**: Use WinDBG directly to verify dump accessibility
3. **Read Documentation**: Check [🔍 Overview](OVERVIEW.md) for analysis guidance
4. **Community Support**: Report issues on GitHub

---

## Next Steps

- **🔍 Overview:** [OVERVIEW.md](OVERVIEW.md) - Understand the analysis capabilities
- **📋 Tools:** [TOOLS.md](TOOLS.md) - Learn about available analysis tools
- **📚 Resources:** [RESOURCES.md](RESOURCES.md) - MCP Resources reference
- **🤖 Integration:** [INTEGRATION.md](INTEGRATION.md) - Set up AI integration
- **👨‍💻 Development:** [DEVELOPMENT.md](DEVELOPMENT.md) - Understand the architecture