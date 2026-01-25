### Debugging Configuration

Controls how WinAiDbg launches and communicates with the Windows debugger (CDB/WinDBG engine), along with timeouts and symbol settings.

#### Settings

```json
{
  "WinAiDbg": {
    "Debugging": {
      "CdbPath": null,
      "CommandTimeoutMs": 600000,
      "IdleTimeoutMs": 300000,
      "SymbolServerMaxRetries": 1,
      "SymbolSearchPath": "",
      "StartupDelayMs": 500,
      "OutputReadingTimeoutMs": 300000,
      "EnableCommandPreprocessing": true
    }
  }
}
```

- **`CdbPath`**: Optional explicit path to `cdb.exe`. If `null`, WinAiDbg will attempt to locate it via standard installation locations/PATH.
- **`CommandTimeoutMs`**: Present in configuration defaults and shown in the startup banner, but not referenced for command execution timeouts (no code references found).
- **`IdleTimeoutMs`**: Present in configuration defaults, but not referenced by the current debugger session implementation (no code references found).
- **`SymbolServerMaxRetries`**: Present in configuration defaults and shown in the startup banner, but not referenced elsewhere (no code references found).
- **`SymbolSearchPath`**: Symbol path/search configuration (used by dump validation and shown in the startup banner).
- **`StartupDelayMs`**: Short startup delay to allow the debugger process to initialize.
- **`OutputReadingTimeoutMs`**: Present in configuration defaults, but not referenced by the current debugger session implementation (no code references found).
- **`EnableCommandPreprocessing`**: Enables internal preprocessing/normalization of commands before execution.

#### Notes

- `CdbPath` resolution uses the Windows Kits tool locator logic (probing known SDK install locations and configured paths) when not explicitly set.
