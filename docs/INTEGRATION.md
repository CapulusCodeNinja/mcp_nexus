# AI Tool Integration

> 🏠 **[← Back to Main README](../README.md)** | 📚 **Other Docs:** [📋 Tools](TOOLS.md) | [🔧 Configuration](CONFIGURATION.md) | [👨‍💻 Development](DEVELOPMENT.md)

## Cursor IDE Integration

### Configuration for Stdio Mode (Recommended)

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

### Configuration for HTTP Mode

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

Start the server first: `dotnet run --project mcp_nexus/mcp_nexus.csproj -- --http`

### Notification Support in Cursor

- **Stdio Mode**: Cursor receives real-time notifications via stdout
- **HTTP Mode**: Cursor can connect to SSE endpoint for notifications
- **Auto-discovery**: Cursor automatically discovers notification capabilities during initialization

## Other MCP Clients

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

# Listen to notifications via SSE
curl -N -H "Accept: text/event-stream" http://localhost:5000/mcp/notifications
```

## Notification Integration

### For AI Clients
1. **Discovery**: Check server capabilities during `initialize`
2. **Automatic**: Listen for notifications automatically
3. **No Registration**: No explicit registration required
4. **Real-time**: Receive live updates about command execution

### For Custom Clients
```javascript
// HTTP + SSE mode
const eventSource = new EventSource('http://localhost:5000/mcp/notifications');
eventSource.onmessage = function(event) {
  const notification = JSON.parse(event.data);
  console.log('Notification:', notification);
};

// Stdio mode - listen to stdout for JSON-RPC notifications
process.stdout.on('data', (data) => {
  try {
    const message = JSON.parse(data.toString());
    if (message.method && message.method.startsWith('notifications/')) {
      console.log('Notification:', message);
    }
  } catch (e) {
    // Not a JSON message
  }
});
```

---

## Next Steps

- **📋 Tools:** [TOOLS.md](TOOLS.md) - Explore available tools and notification examples
- **🔧 Configuration:** [CONFIGURATION.md](CONFIGURATION.md) - Advanced configuration options
- **👨‍💻 Development:** [DEVELOPMENT.md](DEVELOPMENT.md) - Build custom integrations
