### Session Management (Feature)

WinAiDbg maintains analysis sessions so clients can open a dump once, enqueue many commands, and retrieve results reliably while the engine handles cleanup and lifecycle management.

#### What this covers

- Session creation and teardown
- Concurrency limits
- Idle/stale session cleanup

#### Configuration

Session management behavior is controlled via `WinAiDbg.SessionManagement`. See:

- `documentation/configuration/SessionManagement.md`
