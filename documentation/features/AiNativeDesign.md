### 🤖 AI-native Design

WinAiDbg is designed for AI clients first: operations are exposed as MCP tools with predictable inputs and outputs, making it easier to automate analysis workflows reliably.

#### 🧭 What this means in practice

- Tools have stable names and clear responsibilities (open session, enqueue command, poll status, read result, close session)
- Output is structured to be machine-consumable, not just human-oriented console text
- Long-running operations can be monitored via status tools and notifications
