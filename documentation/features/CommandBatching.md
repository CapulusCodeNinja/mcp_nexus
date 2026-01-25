### Command Batching (Feature)

Command batching improves throughput by grouping compatible debugger commands together when it is safe to do so.

#### Why it matters

- Reduces per-command overhead for workloads that enqueue many short commands
- Can improve overall latency when multiple compatible commands are queued in quick succession

#### Configuration

Batching behavior is controlled via `WinAiDbg.Batching`. See:

- `documentation/configuration/CommandBatching.md`
