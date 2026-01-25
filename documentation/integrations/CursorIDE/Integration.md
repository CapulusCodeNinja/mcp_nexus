### Cursor IDE Integration

This page shows how to connect **WinAiDbg** to **Cursor** via MCP.
The GIFs are quick visual walkthroughs; the JSON blocks below are the copy/paste configuration you’ll actually use.

**Before you start:**
- Install **.NET 8**.
- Make sure the **Windows Debugging Tools** (WinDBG/CDB) are installed and available on the machine that will run WinAiDbg.
- Update any file paths in the snippets to match where you cloned this repo.

#### STDIO Integration

Use **STDIO** when you want Cursor to start WinAiDbg for you (typical for local development).

![Cursor Stdio](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/integrations/Cursor_stdio.gif?raw=true)

```json
{
  "mcpServers": {
    "winaidbg": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Sources\\Github\\CapulusCodeNinja\\mcp-win-ai-dbg\\winaidbg\\winaidbg.csproj",
        "--",
        "--stdio"
      ]
    }
  }
}
```

#### HTTP Integration

Use **HTTP** when WinAiDbg is already running as a separate process/service and Cursor should connect to it over the network.
If you change the host/port in your WinAiDbg configuration, update the `url` here to match.
Note: `0.0.0.0` is a server bind address (listen on all interfaces). For a local client connection, use `localhost` (or `127.0.0.1`).

![Cursor Http](https://github.com/CapulusCodeNinja/mcp-win-ai-dbg/blob/main/images/integrations/Cursor_http.gif?raw=true)

```json
{
  "mcpServers": {
    "winaidbg": {
      "url": "http://localhost:5511/",
      "headers": {
        "Content-Type": "application/json"
      }
    }
  }
}
```

After updating configuration, restart Cursor and verify the WinAiDbg tools appear and can be invoked. For the available tool list and example workflow, see [`README.md`](../../../README.md).