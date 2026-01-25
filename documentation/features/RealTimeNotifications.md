### 🔄 Real-time Notifications

WinAiDbg can emit real-time notifications while long-running operations are executing (for example, command state changes).
This lets clients show progress updates without polling every single command individually.

#### 🧩 Example notification payload

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/commandStatus",
  "params": {
    "commandId": "cmd-abc123",
    "sessionId": "session-xyz789",
    "command": "!analyze -v",
    "status": "Executing",
    "progress": 25,
    "message": "Running command",
    "timestamp": "2025-01-15T10:30:00Z"
  }
}
```

#### 📝 Notes

- The exact notification types and payload shapes are client-facing contract; keep clients tolerant to new fields.
- For bulk status checks (polling), prefer the dedicated “get status” tool described in `README.md`.
