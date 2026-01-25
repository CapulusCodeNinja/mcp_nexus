### 🛠️ Automated Recovery Configuration

WinAiDbg includes automated recovery behavior to handle slow/complex commands and to perform periodic health checks.

#### ⚙️ Settings

```json
{
  "WinAiDbg": {
    "AutomatedRecovery": {
      "DefaultCommandTimeoutMinutes": 10,
      "ComplexCommandTimeoutMinutes": 30,
      "MaxCommandTimeoutMinutes": 60,
      "HealthCheckIntervalSeconds": 30,
      "MaxRecoveryAttempts": 3,
      "RecoveryDelaySeconds": 5
    }
  }
}
```

- **`DefaultCommandTimeoutMinutes`**: Baseline timeout for typical commands.
- **`ComplexCommandTimeoutMinutes`**: Timeout used for known “complex” commands.
- **`MaxCommandTimeoutMinutes`**: Upper bound for timeouts after adjustment.
- **`HealthCheckIntervalSeconds`**: How frequently the engine performs health checks.
- **`MaxRecoveryAttempts`**: Maximum retries/recovery attempts before giving up.
- **`RecoveryDelaySeconds`**: Delay between recovery attempts.

#### 📝 Notes

- In the current codebase, only `DefaultCommandTimeoutMinutes` is referenced (via `GetDefaultCommandTimeout()`).
- The other fields are configuration-only today (no code references found).
- If you frequently analyze very large dumps, you may want to increase `ComplexCommandTimeoutMinutes` and `MaxCommandTimeoutMinutes`.
