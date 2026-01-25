### Logging Configuration

This section controls WinAiDbg logging behavior.

#### Settings

```json
{
  "Logging": {
    "LogLevel": "Debug",
    "RetentionDays": 7
  }
}
```

- **`LogLevel`**: The minimum severity level that will be written.
- **`RetentionDays`**: How long log files are retained before cleanup.

**Supported levels**: Trace, Debug, Information, Warning, Error, Critical

#### Notes

- Use `Debug` or `Trace` when troubleshooting (these levels can be noisy).
- Use `Information` for normal day-to-day operation.
