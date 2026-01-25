### Extensions Configuration

Controls the PowerShell-based extension system.

#### Settings

```json
{
  "WinAiDbg": {
    "Extensions": {
      "Enabled": true,
      "ExtensionsPath": "extensions",
      "CallbackPort": 0,
      "GracefulTerminationTimeoutMs": 2000
    }
  }
}
```

- **`Enabled`**: Present in configuration defaults, but not referenced by the current extension loader (no code references found).
- **`ExtensionsPath`**: Directory that WinAiDbg scans for extension metadata JSON files.
- **`CallbackPort`**: Port used by the extension callback server. `0` means “auto-select an available port”.
- **`GracefulTerminationTimeoutMs`**: Present in configuration defaults, but not referenced by the current extension executor (no code references found).
