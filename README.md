# WinAiDbg

**AI-Powered Windows Crash Dump Analysis Platform**

![WinAiDbg Icon](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/winaidbg_small.png?raw=true)

![Tests](https://img.shields.io/badge/tests-1272%20passing-brightgreen)
![Coverage](https://img.shields.io/badge/coverage-86.9%25%20lines-green)
![Build](https://img.shields.io/badge/build-passing-brightgreen)
![License](https://img.shields.io/badge/license-Apache%202.0-blue)

WinAiDbg is a comprehensive Model Context Protocol (MCP) server that provides AI systems with advanced Windows crash dump analysis capabilities. It combines the power of Microsoft's debugging tools (WinDBG/CDB) with intelligent analysis workflows, making professional-grade crash investigation accessible to AI assistants.

## 🎯 What is WinAiDbg?

WinAiDbg is a platform that provides structured access to Windows debugging tools through the Model Context Protocol. It makes crash dump analysis more accessible by providing standardized tools and real-time progress tracking.

### The Problem We Solve

**Traditional Crash Analysis Challenges:**
- 🔍 **Complexity**: Requires deep knowledge of Windows internals and debugging tools
- ⏱️ **Time-consuming**: Manual analysis can take hours or days
- 🧠 **Expertise Required**: Need specialized debugging skills and experience
- 📊 **Inconsistent Results**: Different analysts may reach different conclusions
- 🔧 **Tool Complexity**: WinDBG/CDB have steep learning curves

**WinAiDbg Solution:**
- 🔧 **Structured Access**: Provides standardized tools for debugging operations
- ⚡ **Real-time Updates**: Live progress tracking and notifications
- 📚 **Consistent Results**: Provides structured output formats
- 🔄 **MCP Integration**: Works with AI clients through Model Context Protocol
- 🛠️ **Professional Tools**: Built on Microsoft's industry-standard debugging infrastructure

## ✨ Key Features

- 🔍 **Advanced Crash Analysis**: Leverage WinDBG/CDB for comprehensive dump analysis
- 🤖 **AI-Native Design**: Built specifically for AI agent integration via MCP
- ⚡ **Command Batching**: Intelligent command grouping for improved throughput
- 🔄 **Real-time Notifications**: Live updates during analysis operations
- 🛡️ **Session Management**: Robust session lifecycle with automatic cleanup
- 🎯 **Extensible Architecture**: PowerShell-based extension system for custom workflows
- 📊 **Structured Results**: Parse debugging output into AI-friendly formats

## 🎯 Quick Start

### Prerequisites

- **.NET 8.0 Runtime** or SDK
- **Windows Debugging Tools** (WinDBG/CDB) - [Download from Microsoft](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools)

### Installation

For local development (or a one-off manual run), clone the repo, build it, and run WinAiDbg as a normal console process.

This approach is typically the best fit for **STDIO-based integrations**, where the client starts WinAiDbg and communicates over stdin/stdout.

```bash
# Clone the repository
git clone https://github.com/CapulusCodeNinja/mcp-win-ai-dbg.git
cd mcp-win-ai-dbg

# Build the project
dotnet build

# Run the server (interactive / foreground)
dotnet run --project winaidbg/winaidbg.csproj
```

### 🛡️ Service Mode

Use Service Mode when you want WinAiDbg to run **in the background** (always-on) and be managed by the Windows Service Control Manager.

This is the recommended setup for production/long-running environments.

This approach is typically the best fit for **HTTP-based integrations**, where clients connect to a long-running WinAiDbg instance over the network.

```bash
# Install WinAiDbg as a Windows Service
dotnet run --project winaidbg/winaidbg.csproj -- service install

# Start the service
net start WinAiDbg

# Stop the service
net stop WinAiDbg

# Uninstall the Windows Service
dotnet run --project winaidbg/winaidbg.csproj -- service uninstall
```

## 🤖 AI Integration

This section is an index of supported AI development environments.
Open the relevant integration page below for the **environment-specific setup and usage instructions** (configuration files, transport selection for STDIO vs HTTP, and run/debug tips).

- [Cursor IDE](documentation/integrations/CursorIDE/Integration.md)
- [Google Antigravity](documentation/integrations/GoogleAntigravity/Integration.md)
- [Visual Studio Code](documentation/integrations/VisualStudioCode/Integration.md)

In similar fashion, you can adapt these configurations for other MCP-compatible clients.

## 🛠️ Available MCP Tools

### Core Analysis Tools

- **`winaidbg_open_dump_analyze_session`** - Open a crash dump for analysis
- **`winaidbg_enqueue_async_dump_analyze_command`** - Execute WinDBG commands asynchronously
- **`winaidbg_get_dump_analyze_commands_status`** - Get status of all commands (bulk polling)
- **`winaidbg_read_dump_analyze_command_result`** - Read individual command results
- **`winaidbg_cancel_dump_analyze_command`** - Cancel a running command
- **`winaidbg_close_dump_analyze_session`** - Close session and cleanup resources

### MCP Resources

- **`sessions`** - List all active debugging sessions
- **`commands`** - List commands across all active sessions

## 📊 Example Workflow

**Typical AI-driven crash analysis workflow:**

1. **Open Session**: `winaidbg_open_dump_analyze_session` with dump file path
2. **Queue Commands**: Use `winaidbg_enqueue_async_dump_analyze_command` for:
   - `!analyze -v` (automatic crash analysis)
   - `kL` (stack trace with source lines)
   - `!threads` (thread information)
3. **Monitor Progress**: `winaidbg_get_dump_analyze_commands_status` (bulk polling)
4. **Retrieve Results**: `winaidbg_read_dump_analyze_command_result` for each command
5. **Close Session**: `winaidbg_close_dump_analyze_session` for cleanup

**Example conversation with AI assistant:**

```
User: "Analyze the crash dump at C:\dumps\myapp.dmp"

AI: I'll analyze that crash dump for you.
    [Opens session, queues !analyze -v, kL, !threads commands]
    
    Analysis Results:
    - Exception: Access Violation (0xC0000005)
    - Faulting Module: myapp.exe
    - Root Cause: Null pointer dereference in ProcessData()
    - Stack shows: ProcessData+0x42 called with null buffer
    
    Recommendation: Add null check before buffer access.
```

## 🏗️ Architecture

### Modular Structure

```
winaidbg/                      - Main application (entry point, hosting)
winaidbg_config/               - Configuration and logging
winaidbg_engine/               - Debug engine + subcomponents (CDB sessions, queue, batching, extensions)
winaidbg_protocol/             - MCP protocol layer (tools, resources)
winaidbg_setup/                - Service installation and management
winaidbg_external_apis/        - Shared utilities (file system, process, etc.)
winaidbg_web/                  - Static admin UI and docs
```

### Key Design Patterns

- **Modular Architecture**: Clear separation of concerns across libraries
- **Singleton Pattern**: Core engine accessible without DI overhead
- **Command Queue**: Asynchronous command processing with state management
- **Batching**: Transparent command grouping for improved performance
- **Event-Driven**: Real-time notifications for command state changes

## 🧪 Testing & Quality

### Test Statistics

- **1272 total tests** (all passing)
- **86.9% line coverage** (target: 75%)
- **76.9% branch coverage** (target: 75%)
- **Fast execution** (~25 seconds for full suite)
- **Zero build warnings**

## ⚙️ Configuration

Configuration is documented in these section pages:

- **Logging**: [Logging.md](documentation/configuration/Logging.md)
- **WinAiDbg.Server**: [Server.md](documentation/configuration/Server.md)
- **WinAiDbg.Transport**: [Transport.md](documentation/configuration/Transport.md)
- **WinAiDbg.Debugging**: [Debugging.md](documentation/configuration/Debugging.md)
- **WinAiDbg.Validation**: [Validation.md](documentation/configuration/Validation.md)
- **WinAiDbg.AutomatedRecovery**: [AutomatedRecovery.md](documentation/configuration/AutomatedRecovery.md)
- **WinAiDbg.Service**: [Service.md](documentation/configuration/Service.md)
- **WinAiDbg.SessionManagement**: [SessionManagement.md](documentation/configuration/SessionManagement.md)
- **WinAiDbg.Extensions**: [Extensions.md](documentation/configuration/Extensions.md)
- **WinAiDbg.Batching**: [CommandBatching.md](documentation/configuration/CommandBatching.md)
- **WinAiDbg.ProcessStatistics**: [ProcessStatistics.md](documentation/configuration/ProcessStatistics.md)
- **IpRateLimiting**: [IpRateLimiting.md](documentation/configuration/IpRateLimiting.md)

## 🚀 Features

Features are documented in the pages below:

- **Extension system**: [ExtensionSystem.md](documentation/features/ExtensionSystem.md)
- **Real-time notifications**: [RealTimeNotifications.md](documentation/features/RealTimeNotifications.md)
- **Advanced crash analysis**: [AdvancedCrashAnalysis.md](documentation/features/AdvancedCrashAnalysis.md)
- **AI-native design**: [AiNativeDesign.md](documentation/features/AiNativeDesign.md)
- **Command batching**: [CommandBatching.md](documentation/features/CommandBatching.md)
- **Session management**: [SessionManagement.md](documentation/features/SessionManagement.md)
- **Structured results**: [StructuredResults.md](documentation/features/StructuredResults.md)

## 📄 License

This project is licensed under the **Apache License 2.0** - see the [LICENSE](LICENSE) file for details.

The Apache License 2.0 allows you to:
- ✅ Use the software commercially
- ✅ Modify and distribute
- ✅ Sublicense
- ✅ Use patent claims
- ⚠️ Include copyright notice

## 🙏 Acknowledgments

- [Model Context Protocol](https://modelcontextprotocol.io/) - MCP specification
- [C# SDK for MCP](https://github.com/modelcontextprotocol/csharp-sdk) - MCP implementation
- [Microsoft Debugging Tools](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/) - WinDBG/CDB
- [NLog](https://nlog-project.org/) - Logging framework
