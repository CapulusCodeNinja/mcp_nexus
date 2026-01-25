### Command Batching Configuration

WinAiDbg can batch multiple debugger commands together to improve throughput and reduce per-command overhead.
This is most useful when you queue many short WinDBG/CDB commands in a row.

#### Settings

These settings live under:

```json
{
  "WinAiDbg": {
    "Batching": {
      "...": "..."
    }
  }
}
```

- **`Enabled`**: Enables/disables batching.
- **`MaxBatchSize`**: Maximum number of commands that will be grouped into a single batch.
- **`MinBatchSize`**: Minimum number of queued commands required before WinAiDbg will attempt to create a batch.
- **`CommandCollectionWaitMs`**: Optional short collection window to allow more commands to accumulate before forming a batch.
- **`BatchWaitTimeoutMs`**: Present in configuration defaults, but not referenced by the current batching implementation (no code references found).
- **`BatchTimeoutMultiplier`**: Present in configuration defaults, but not referenced by the current batching implementation (no code references found).
- **`MaxBatchTimeoutMinutes`**: Present in configuration defaults, but not referenced by the current batching implementation (no code references found).
- **`ExcludedCommands`**: A list of command prefixes that should never be batched (for example, long-running or highly stateful commands).

#### Example

```json
{
  "WinAiDbg": {
    "Batching": {
      "Enabled": true,
      "MaxBatchSize": 8,
      "MinBatchSize": 2,
      "CommandCollectionWaitMs": 0,
      "BatchWaitTimeoutMs": 2000,
      "BatchTimeoutMultiplier": 1.0,
      "MaxBatchTimeoutMinutes": 30,
      "ExcludedCommands": [
        "!analyze",
        "!dump",
        "!heap",
        "!memusage",
        "!runaway",
        "~*k",
        "!locks",
        "!cs",
        "!gchandles"
      ]
    }
  }
}
```

#### Notes

- Batching is designed to be transparent: you still enqueue commands individually, and WinAiDbg decides when it is safe/beneficial to batch.
- If you see unexpected behavior with a specific command, add its prefix to `ExcludedCommands` to force it to run standalone.
