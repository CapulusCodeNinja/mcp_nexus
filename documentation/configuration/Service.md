### Service Configuration

Controls paths and naming used when WinAiDbg is installed and managed as a Windows Service.

#### Settings

```json
{
  "WinAiDbg": {
    "Service": {
      "InstallPath": "C:\\Program Files\\WinAiDbg",
      "BackupPath": "C:\\Program Files\\WinAiDbg\\backups",
      "ServiceName": "WinAiDbg",
      "DisplayName": "WinAiDbg Debugging Server"
    }
  }
}
```

- **`InstallPath`**: Default installation directory for service deployments.
- **`BackupPath`**: Directory used for backups during updates/service operations.
- **`ServiceName`**: The Windows Service name (internal identifier).
- **`DisplayName`**: The friendly name shown in the Services UI.
