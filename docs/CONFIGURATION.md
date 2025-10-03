# Configuration Guide

**MCP Nexus Server Configuration Options**

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [🔍 Overview](OVERVIEW.md) | [📋 Tools](TOOLS.md) | [📚 Resources](RESOURCES.md) | [🤖 Integration](INTEGRATION.md) | [👨‍💻 Development](DEVELOPMENT.md) | [📖 Usage Examples](USAGE_EXAMPLES.md)

## Configuration Files

- `appsettings.json` - Main configuration (Production defaults)
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides  
- `appsettings.Service.json` - Windows service overrides
- `appsettings.example.json` - Example configuration with all options

## Logging Configuration

### Log Directory Locations

**Service Mode (Windows Service):**
- **Main Logs**: `C:\ProgramData\MCP-Nexus\Logs\`
- **Internal Logs**: `C:\ProgramData\MCP-Nexus\Logs\mcp-nexus-internal.log`
- **Archive**: `C:\ProgramData\MCP-Nexus\Logs\archive\`
- **Auto-Creation**: Directories are created automatically during service startup

**Interactive Mode (Development/Production):**
- **Main Logs**: `.\logs\` (relative to application directory)
- **Internal Logs**: `.\mcp-nexus-internal.log`
- **Archive**: `.\logs\archive\`

### Single LogLevel Setting

The logging system uses a simplified single `LogLevel` setting:

```json
{
  "Logging": {
    "LogLevel": "Information"
  }
}
```

**Valid Values:**
- `"Trace"` - Most verbose logging
- `"Debug"` - Detailed debugging information
- `"Information"` - General information (default for Service/Production)
- `"Warning"` - Warning messages only
- `"Error"` - Error messages only
- `"Critical"` - Critical errors only

**Environment Defaults:**
- **Development**: `"Debug"`
- **Service/Production**: `"Information"`

## MCP Nexus Server Configuration

### Server Settings

```json
{
  "McpNexus": {
    "Server": {
      "Host": "0.0.0.0",    // Host address (0.0.0.0 = all interfaces, 127.0.0.1 = localhost only)
      "Port": 5511          // Port number for HTTP server
    }
  }
}
```

**Default Ports by Mode:**
- **Development Mode**: `5117` (when `ASPNETCORE_ENVIRONMENT=Development`)
- **Production Mode**: `5000` (default HTTP port)
- **Service Mode**: `5511` (Windows service default)
- **Configuration Override**: Any port specified in `McpNexus:Server:Port`
- **Command Line Override**: `--port <PORT>` option

**HTTP Endpoint:**
- **Base URL**: `http://localhost:<PORT>/` (root path, not `/mcp`)
- **MCP Protocol**: JSON-RPC over HTTP at the root endpoint

### Transport Configuration

```json
{
  "McpNexus": {
    "Transport": {
      "Mode": "http",       // Transport mode: "http" or "stdio"
      "ServiceMode": true   // Whether to run in Windows service mode
    }
  }
}
```

**Mode Options:**
- `"http"` - HTTP server mode (default)
- `"stdio"` - Standard I/O mode for MCP clients

**ServiceMode:**
- `true` - Windows service mode (affects logging and startup behavior)
- `false` - Regular application mode

### Debugging Configuration

```json
{
  "McpNexus": {
    "Debugging": {
      "CdbPath": null,                    // Path to CDB.exe (null = auto-detect from PATH)
      "CommandTimeoutMs": 600000,         // Command execution timeout (10 minutes)
      "SymbolServerTimeoutMs": 300000,    // Symbol server timeout (5 minutes)
      "SymbolServerMaxRetries": 1,        // Maximum retries for symbol server requests
      "SymbolSearchPath": "...",          // Windows symbol path format
      "StartupDelayMs": 2000             // Initial delay for CDB process startup
    }
  }
}
```

**CdbPath Options:**
- `null` - Auto-detect CDB.exe from PATH environment variable
- `"C:\\Path\\To\\cdb.exe"` - Explicit path to CDB executable

**SymbolSearchPath Format:**
Windows symbol path format with multiple sources:
- `cache*` - Local cache directory
- `srv*` - Symbol server with local cache and remote URL

### Automated Recovery Settings

```json
{
  "McpNexus": {
    "AutomatedRecovery": {
      "DefaultCommandTimeoutMinutes": 10,    // Default timeout for simple commands
      "ComplexCommandTimeoutMinutes": 30,    // Timeout for complex commands
      "MaxCommandTimeoutMinutes": 60,        // Maximum allowed timeout
      "HealthCheckIntervalSeconds": 30,      // Health check interval
      "MaxRecoveryAttempts": 3,              // Maximum recovery attempts
      "RecoveryDelaySeconds": 5              // Delay between recovery attempts
    }
  }
}
```

### Service Installation Paths

```json
{
  "McpNexus": {
    "Service": {
      "InstallPath": "C:\\Program Files\\MCP-Nexus",           // Installation directory
      "BackupPath": "C:\\Program Files\\MCP-Nexus\\backups"    // Backup directory for updates
    }
  }
}
```

### Session Management

```json
{
  "McpNexus": {
    "SessionManagement": {
      "MaxConcurrentSessions": 1000,        // Maximum concurrent debugging sessions
      "SessionTimeoutMinutes": 30,          // Session timeout (cleanup after this time)
      "CleanupIntervalMinutes": 5,          // Cleanup interval for expired sessions
      "DisposalTimeoutSeconds": 30,         // Timeout for session disposal
      "DefaultCommandTimeoutMinutes": 10,   // Default command timeout for new sessions
      "MemoryCleanupThresholdMB": 1024      // Memory cleanup threshold (MB)
    }
  }
}
```

## Rate Limiting Configuration

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,     // Enable rate limiting for HTTP endpoints
    "StackBlockedRequests": false,          // Don't stack blocked requests
    "RealIpHeader": "X-Real-IP",            // Header for real IP detection (behind proxy)
    "ClientIdHeader": "X-ClientId",         // Header for client identification
    "GeneralRules": [
      {
        "Endpoint": "*:/",                  // Endpoint pattern (all MCP endpoints)
        "Period": "1m",                     // Time period for rate limiting
        "Limit": 100                        // Maximum requests per period
      }
    ]
  }
}
```

## Environment-Specific Overrides

### Development Environment
- **File**: `appsettings.Development.json`
- **LogLevel**: `"Debug"` (more verbose logging)
- **ServiceMode**: `false` (regular application mode)

### Production Environment
- **File**: `appsettings.Production.json`
- **LogLevel**: `"Information"` (standard logging)
- **ServiceMode**: `false` (regular application mode)

### Windows Service Environment
- **File**: `appsettings.Service.json`
- **LogLevel**: `"Information"` (optimized for service logging)
- **ServiceMode**: `true` (Windows service mode)
- **StartupDelayMs**: `1000` (reduced startup delay)
- **Log Directory**: `C:\ProgramData\MCP-Nexus\Logs\` (automatic directory creation)

## Configuration Validation

The application validates configuration settings and will throw clear error messages for invalid values:

**Invalid LogLevel Example:**
```
ArgumentException: Invalid log level 'InvalidLevel'. Valid values are: Trace, Debug, Information, Warning, Error, Critical
```

## Best Practices

1. **Logging**: Use `"Debug"` for development, `"Information"` for production
2. **Host Binding**: Use `"127.0.0.1"` for localhost-only access, `"0.0.0.0"` for all interfaces
3. **Timeouts**: Adjust timeouts based on your network and system performance
4. **Memory Limits**: Set appropriate memory cleanup thresholds for your system
5. **Rate Limiting**: Configure rate limits based on expected usage patterns

## Example Configurations

### Minimal Configuration
```json
{
  "Logging": {
    "LogLevel": "Information"
  },
  "McpNexus": {
    "Server": {
      "Host": "127.0.0.1",
      "Port": 5000
    }
  }
}
```

**Usage:**
- **Development**: `dotnet run -- --http` (uses port 5117)
- **Production**: `dotnet run -- --http` (uses port 5000)
- **Custom Port**: `dotnet run -- --http --port 8080`

### High-Performance Configuration
```json
{
  "Logging": {
    "LogLevel": "Warning"
  },
  "McpNexus": {
    "Server": {
      "Host": "0.0.0.0",
      "Port": 5000
    },
    "SessionManagement": {
      "MaxConcurrentSessions": 5000,
      "SessionTimeoutMinutes": 60,
      "MemoryCleanupThresholdMB": 2048
    }
  }
}
```

### Development Configuration
```json
{
  "Logging": {
    "LogLevel": "Debug"
  },
  "McpNexus": {
    "Server": {
      "Host": "127.0.0.1",
      "Port": 5117
    },
    "Transport": {
      "Mode": "http",
      "ServiceMode": false
    }
  }
}
```

## Related Documentation

- **[Tools Guide](TOOLS.md)** - Available MCP tools and their usage
- **[Resources Guide](RESOURCES.md)** - Available MCP resources
- **[Integration Guide](INTEGRATION.md)** - How to integrate with MCP clients
- **[Development Guide](DEVELOPMENT.md)** - Development setup and guidelines
- **[Usage Examples](USAGE_EXAMPLES.md)** - Practical usage examples