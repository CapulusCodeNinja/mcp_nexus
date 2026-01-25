### 🧩 Google Antigravity Integration

This page shows how to connect **WinAiDbg** to **Google Antigravity** via MCP.
The GIFs are quick visual walkthroughs; the JSON blocks below are the copy/paste configuration you’ll actually use.

**Before you start:**
- Install **.NET 8**.
- Make sure the **Windows Debugging Tools** (WinDBG/CDB) are installed and available on the machine that will run WinAiDbg.
- Update any file paths in the snippets to match where you cloned this repo.

#### 🔌 STDIO Integration

Use **STDIO** when you want Antigravity to start WinAiDbg for you (typical for local development).

![Antigravity Stdio](Antigravity_stdio.gif)

```json
{
    "mcpServers": {
        "winaidbg": {
            "command": "C:\\Program Files\\dotnet\\dotnet.exe",
            "args": [
                "run",
                "--project",
                "C:\\Sources\\Github\\CapulusCodeNinja\\mcp-win-ai-dbg\\winaidbg\\winaidbg.csproj",
                "--",
                "--stdio"
            ]
        }
    },
    "inputs": []
}
```

#### 🌐 HTTP Integration

Use **HTTP** when WinAiDbg is already running as a separate process/service and Antigravity should connect to it over the network.
If you change the host/port in your WinAiDbg configuration, update the `serverUrl` here to match.
Note: `0.0.0.0` is a server bind address (listen on all interfaces). For a local client connection, use `localhost` (or `127.0.0.1`).

![Antigravity Http](Antigravity_http.gif)

```json
{
    "mcpServers": {
        "winaidbg": {
            "serverUrl": "http://localhost:5511/",
            "headers": {
                "Content-Type": "application/json"
            }
        }
    }
}
```

After updating configuration, restart Antigravity and verify the WinAiDbg tools appear and can be invoked. For the available tool list and example workflow, see [`README.md`](../../../README.md).