### Server Configuration

Controls the host and port WinAiDbg binds to when running in HTTP mode.

#### Settings

```json
{
  "WinAiDbg": {
    "Server": {
      "Host": "0.0.0.0",
      "Port": 5511
    }
  }
}
```

- **`Host`**: The bind address/interface.
  - Use `0.0.0.0` to listen on all IPv4 interfaces.
  - Use `127.0.0.1` to listen only locally.
- **`Port`**: The TCP port for the HTTP server.

#### Notes

- If you change `Port`, update any client integrations that hard-code the URL (see the integration pages under `documentation/integrations/`).
