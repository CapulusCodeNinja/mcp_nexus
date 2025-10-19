# MCP Nexus

**AI-Powered Windows Crash Dump Analysis Platform**

[![Tests](https://img.shields.io/badge/tests-2,574%20total-brightgreen?style=flat-square)](https://github.com/CapulusCodeNinja/mcp_nexus)
[![Coverage](https://img.shields.io/badge/coverage-78.7%25-green?style=flat-square)](https://github.com/CapulusCodeNinja/mcp_nexus)
[![Build](https://img.shields.io/badge/build-0%20warnings-brightgreen?style=flat-square)](https://github.com/CapulusCodeNinja/mcp_nexus)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue?style=flat-square)](LICENSE)

MCP Nexus is a comprehensive Model Context Protocol (MCP) server that provides AI systems with advanced Windows crash dump analysis capabilities. It combines the power of Microsoft's debugging tools (WinDBG/CDB) with intelligent analysis workflows to help identify root causes of system crashes, memory corruption, and application failures.

## 🎯 What is MCP Nexus?

MCP Nexus is a platform that provides structured access to Windows debugging tools through the Model Context Protocol. It makes crash dump analysis more accessible by providing standardized tools and real-time progress tracking.

### The Problem We Solve

**Traditional Crash Analysis Challenges:**
- 🔍 **Complexity**: Requires deep knowledge of Windows internals and debugging tools
- ⏱️ **Time-consuming**: Manual analysis can take hours or days
- 🧠 **Expertise Required**: Need specialized debugging skills and experience
- 📊 **Inconsistent Results**: Different analysts may reach different conclusions
- 🔧 **Tool Complexity**: WinDBG/CDB have steep learning curves

**MCP Nexus Solution:**
- 🔧 **Structured Access**: Provides standardized tools for debugging operations
- ⚡ **Real-time Updates**: Live progress tracking and notifications
- 📚 **Consistent Results**: Provides structured output formats
- 🔄 **MCP Integration**: Works with AI clients through Model Context Protocol
- 🛠️ **Professional Tools**: Built on Microsoft's industry-standard debugging infrastructure

## 📑 Table of Contents

- [🌟 Key Features](#-key-features)
- [🚀 Quick Start](#-quick-start)
- [🔍 Analysis Capabilities](#-analysis-capabilities)
- [📚 Documentation](#-documentation)
- [🤖 AI Integration](#-ai-integration)
- [🛠️ Available Tools](#️-available-tools)
- [📊 Analysis Workflows](#-analysis-workflows)
- [⚙️ Configuration](#️-configuration)
- [🧪 Testing](#-testing)
- [🛠️ Development](#-development)

## 🌟 Key Features

### Core Analysis Capabilities
- **📋 Crash Dump Analysis**: Comprehensive analysis of Windows crash dumps (.dmp files)
- **🔍 Root Cause Detection**: Automated identification of crash causes and patterns
- **📊 Memory Analysis**: Deep inspection of memory corruption and leaks
- **🧵 Thread Analysis**: Thread deadlock and synchronization issue detection
- **⚡ Performance Analysis**: System performance bottleneck identification
- **🔧 Symbol Resolution**: Automatic symbol loading and debugging information

### AI-Powered Features
- **🤖 MCP Integration**: Native Model Context Protocol support for AI systems
- **📡 Real-time Notifications**: Live progress updates during analysis
- **📚 Structured Results**: AI-friendly analysis output formats
- **🔄 Async Processing**: Non-blocking analysis with progress tracking
- **📖 Intelligent Workflows**: Pre-built analysis patterns for common crash types

### Professional Tools
- **🛠️ WinDBG/CDB Integration**: Built on Microsoft's debugging tools
- **📁 Multiple Dump Formats**: Support for various Windows dump types
- **🔗 Symbol Server Support**: Automatic symbol downloading and caching
- **⚙️ Configurable Analysis**: Customizable analysis parameters and timeouts
- **📊 Rich Reporting**: Detailed analysis reports with actionable insights
- **⚡ Command Batching**: Intelligent batching of multiple commands for improved throughput

## 🚀 Quick Start

### Prerequisites

- **Windows 10/11** or **Windows Server 2016+**
- **.NET 8.0** or later
- **Windows Debugging Tools** (WinDBG/CDB)
- **AI Client** (Cursor IDE, Claude Desktop, or compatible MCP client)

### Installation

```bash
# Clone the repository
git clone https://github.com/CapulusCodeNinja/mcp_nexus.git
cd mcp_nexus

# Build the project
dotnet build

# Run in stdio mode (recommended for AI integration)
dotnet run --project mcp_nexus/mcp_nexus.csproj

# Run in HTTP mode (for web integration)
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --http
```

### Basic Usage

1. **Open a crash dump**:
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "nexus_open_dump_analyze_session",
       "arguments": {
         "dumpPath": "C:\\crashes\\application.dmp"
       }
     }
   }
   ```

2. **Analyze the crash**:
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "nexus_enqueue_async_dump_analyze_command",
       "arguments": {
         "sessionId": "sess-123",
         "command": "!analyze -v"
       }
     }
   }
   ```

3. **Get results**:
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "nexus_read_dump_analyze_command_result",
       "arguments": {
         "sessionId": "sess-123",
         "commandId": "cmd-456"
       }
     }
   }
   ```

## 🔍 Analysis Capabilities

### Crash Types Supported
- **💥 Application Crashes**: Unhandled exceptions and access violations
- **🔒 System Crashes**: Blue Screen of Death (BSOD) analysis
- **💾 Memory Corruption**: Heap corruption, buffer overflows, use-after-free
- **🧵 Thread Issues**: Deadlocks, race conditions, thread synchronization
- **⚡ Performance Issues**: CPU spikes, memory leaks, resource exhaustion
- **🔧 Driver Problems**: Kernel driver crashes and system instability

### Analysis Techniques
- **📊 Stack Trace Analysis**: Detailed call stack examination
- **🔍 Memory Dump Inspection**: Heap and stack memory analysis
- **📈 Performance Profiling**: System resource usage analysis
- **🔗 Symbol Resolution**: Automatic debugging symbol loading
- **📋 Pattern Recognition**: Common crash pattern identification
- **🎯 Root Cause Analysis**: Systematic cause identification

## 📚 Documentation

### 📖 **[🔍 Analysis Overview](docs/OVERVIEW.md)**
Comprehensive guide to AI-powered crash analysis workflows and capabilities

### 🛠️ **[📋 Available Tools](docs/TOOLS.md)**
Complete reference for all debugging tools and analysis commands

### 📊 **[📚 Analysis Workflows](docs/USAGE_EXAMPLES.md)**
Step-by-step crash analysis workflows with real-world examples

### ⚙️ **[🔧 Configuration](docs/CONFIGURATION.md)**
Setup guide for debugging tools, symbol servers, and analysis parameters

### 🤖 **[🤖 AI Integration](docs/INTEGRATION.md)**
Integration guide for Cursor IDE, Claude Desktop, and other AI clients

### 💻 **[👨‍💻 Development](docs/DEVELOPMENT.md)**
Architecture overview and contribution guide for developers

### 📊 **[📚 Resources](docs/RESOURCES.md)**
MCP Resources reference for session management and analysis data

## 🤖 AI Integration

### Cursor IDE Integration

**Configuration** (`.cursor/mcp.json`):
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

### Claude Desktop Integration

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

### Real-time Notifications

MCP Nexus provides live updates during analysis:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/commandStatus",
  "params": {
    "commandId": "cmd-123",
    "status": "executing",
    "progress": 75,
    "message": "Analyzing crash dump...",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

## 🛠️ Available Tools

### Core Analysis Tools
- **`nexus_open_dump_analyze_session`**: Open a crash dump for analysis
- **`nexus_close_dump_analyze_session`**: Close analysis session and cleanup
- **`nexus_enqueue_async_dump_analyze_command`**: Execute debugging commands
- **`nexus_read_dump_analyze_command_result`**: Get command results


### MCP Resources
- **`sessions`**: List all active analysis sessions
- **`commands`**: List commands with filtering options
- **`extensions`**: List available extension scripts with metadata
- **`usage`**: Complete usage guide and examples
- **`metrics`**: Performance metrics and statistics
- **`health`**: System health status

## 📊 Analysis Workflows

### Basic Crash Analysis
1. **Open dump** → **Run !analyze -v** → **Examine results** → **Identify root cause**
2. **Check stack trace** → **Analyze memory state** → **Review error codes**
3. **Generate report** → **Document findings** → **Recommend fixes**

### Memory Corruption Analysis
1. **Open dump** → **Run !heap -p -a** → **Check for corruption**
2. **Analyze stack traces** → **Identify corrupted memory** → **Find source**
3. **Review allocation patterns** → **Check for leaks** → **Document findings**

### Thread Deadlock Analysis
1. **Open dump** → **Run !locks** → **Check for deadlocks**
2. **Analyze thread states** → **Identify waiting threads** → **Find blocking resources**
3. **Review synchronization** → **Document deadlock chain** → **Recommend fixes**

## ⚙️ Configuration

### Quick Setup
```bash
# Install Windows Debugging Tools
# Download from Microsoft or install via Windows SDK

# Configure symbol path
dotnet run --project mcp_nexus/mcp_nexus.csproj -- --cdb-path "C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe"
```

### Configuration Files
- **`appsettings.json`** - Main configuration (Production defaults)
- **`appsettings.Development.json`** - Development overrides
- **`appsettings.Production.json`** - Production overrides  
- **`appsettings.Service.json`** - Windows service overrides

### Simplified Logging
The logging system now uses a single `LogLevel` setting:

```json
{
  "Logging": {
    "LogLevel": "Information"  // Trace, Debug, Information, Warning, Error, Critical
  }
}
```

**Environment Defaults:**
- **Development**: `"Debug"`
- **Service/Production**: `"Information"`

### Command Batching
MCP Nexus intelligently batches multiple commands for improved throughput:

```json
{
  "McpNexus": {
    "Batching": {
      "Enabled": true,
      "MaxBatchSize": 5,
      "BatchWaitTimeoutMs": 2000,
      "BatchTimeoutMultiplier": 1.0,
      "MaxBatchTimeoutMinutes": 30,
      "ExcludedCommands": [
        "!analyze", "!dump", "!heap", "!memusage", "!runaway",
        "~*k", "!locks", "!cs", "!gchandles"
      ]
    }
  }
}
```

**Features:**
- **Automatic Batching**: Commands are automatically grouped for execution
- **Configurable Limits**: Set maximum batch size and timeout multipliers
- **Smart Exclusions**: Long-running or resource-intensive commands execute individually
- **Transparent to AI**: Batching happens internally, no changes needed in AI clients

### 📖 **[Complete Configuration Guide](docs/CONFIGURATION.md)**
For detailed configuration options, environment settings, and best practices, see the comprehensive configuration documentation.

## 🧪 Testing

### Test Suite Overview

MCP Nexus maintains a comprehensive test suite ensuring reliability and quality:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test --filter "FullyQualifiedName~CrashAnalysis"
dotnet test --filter "FullyQualifiedName~MemoryAnalysis"
dotnet test --filter "FullyQualifiedName~ThreadAnalysis"
dotnet test --filter "Notification"
```

### Test Statistics

- ✅ **2,574 total tests** (all passing)
- ✅ **78.7% line coverage** (exceeds ≥75% threshold)
- ✅ **74.5% branch coverage** (exceeds ≥75% threshold)
- ✅ **0 warnings** in build (clean codebase)
- ✅ **Fast execution** (~90 seconds for full suite)
- ✅ **Comprehensive mocking** for reliable testing

## 🛠️ Development

### Architecture
```
MCP Nexus
├── Core Analysis Engine
│   ├── Crash Dump Processing
│   ├── Memory Analysis
│   ├── Thread Analysis
│   └── Performance Analysis
├── AI Integration Layer
│   ├── MCP Protocol Handler
│   ├── Real-time Notifications
│   └── Structured Results
├── Debugging Tools Integration
│   ├── WinDBG/CDB Wrapper
│   ├── Symbol Resolution
│   └── Command Execution
└── Analysis Workflows
    ├── Pre-built Patterns
    ├── Custom Analysis
    └── Result Processing
```

### Adding New Analysis Tools
1. **Define tool schema** in `McpToolDefinitionService`
2. **Implement analysis logic** in `McpToolExecutionService`
3. **Add notifications** for real-time updates
4. **Write comprehensive tests**
5. **Update documentation**

## 🆘 Troubleshooting

### Common Issues
- **Symbol Loading**: Ensure symbol path is configured correctly
- **Permission Errors**: Check file access permissions
- **Timeout Issues**: Increase command timeout for large dumps
- **Memory Issues**: Ensure sufficient RAM for large dump analysis

### Getting Help
1. **Check Logs**: Review application logs for detailed error information
2. **Test Manually**: Use WinDBG directly to verify dump accessibility
3. **Read Documentation**: Check [🔧 CONFIGURATION.md](docs/CONFIGURATION.md) for setup issues
4. **Community Support**: Report issues on GitHub

## 📄 License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

The Apache License 2.0 is a permissive open-source license that allows you to:
- ✅ Use the software for any purpose
- ✅ Distribute the software
- ✅ Modify the software
- ✅ Distribute modified versions
- ✅ Use the software in commercial applications

For more information, see the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) documentation.

## 🙏 Acknowledgments

- [Model Context Protocol](https://modelcontextprotocol.io/) specification
- [C# SDK for Model Context Protocol](https://github.com/modelcontextprotocol/csharp-sdk)
- [ModelContextProtocol.AspNetCore NuGet package](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/absoluteLatest)
- [Microsoft debugging tools](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/)
- [NLog](https://nlog-project.org/)

---

**MCP Nexus** - Professional Windows crash dump analysis using Microsoft debugging tools.
