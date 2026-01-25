### 🛡️ Session Management Configuration

WinAiDbg manages debugger sessions (opening a dump, running commands, and cleaning up resources). These settings control concurrency, timeouts, and cleanup behavior.

#### ⚙️ Settings

These settings live under:

```json
{
  "WinAiDbg": {
    "SessionManagement": {
      "...": "..."
    }
  }
}
```

- **`MaxConcurrentSessions`**: Maximum number of sessions that may be active at the same time.
- **`SessionTimeoutMinutes`**: How long an idle session may remain open before being considered stale and eligible for cleanup.
- **`CleanupIntervalSeconds`**: How often background cleanup runs to reclaim stale sessions/resources.
- **`DisposalTimeoutSeconds`**: Present in configuration defaults, but not referenced in session disposal logic (no code references found).
- **`DefaultCommandTimeoutMinutes`**: Present in configuration defaults, but not referenced for command execution timeouts (no code references found).
- **`MemoryCleanupThresholdMB`**: Present in configuration defaults, but not referenced by the current session cleanup logic (no code references found).
- **`DeleteDumpFileOnSessionClose`**: If `true`, WinAiDbg will delete the dump file when the session closes. Keep this `false` if you want to preserve dumps for later re-analysis.

#### 🧩 Example

```json
{
  "WinAiDbg": {
    "SessionManagement": {
      "MaxConcurrentSessions": 1000,
      "SessionTimeoutMinutes": 10,
      "CleanupIntervalSeconds": 30,
      "DisposalTimeoutSeconds": 30,
      "DefaultCommandTimeoutMinutes": 10,
      "MemoryCleanupThresholdMB": 1024,
      "DeleteDumpFileOnSessionClose": false
    }
  }
}
```

#### 📝 Notes

- If you expect heavy parallel usage, increase `MaxConcurrentSessions` based on available CPU/RAM and debugger tool limits.
- If you run WinAiDbg as a long-lived service, keep `CleanupIntervalSeconds` reasonably small so abandoned sessions don’t accumulate.
