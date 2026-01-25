### Transport Configuration

Controls how WinAiDbg exposes MCP transport and whether it should run as a Windows Service.

#### Settings

```json
{
  "WinAiDbg": {
    "Transport": {
      "Mode": "http",
      "ServiceMode": true
    }
  }
}
```

- **`Mode`**: Which transport mode to run.
  - Common values: `http` (HTTP server) and `stdio` (STDIO transport).
- **`ServiceMode`**: When enabled, WinAiDbg is intended to be hosted/managed as a Windows Service.

#### Notes

- Client integrations differ depending on `Mode` (see `documentation/integrations/*/Integration.md`).
