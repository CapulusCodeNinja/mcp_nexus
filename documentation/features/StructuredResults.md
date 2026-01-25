### Structured Results

WinAiDbg returns tool results as **Markdown-formatted strings** with a consistent structure (see `winaidbg_protocol/Utilities/MarkdownFormatter.cs`).
This keeps debugger output readable while still being predictable for automation.

#### Practical impact

- Consistent Markdown sections for status, output, and errors
- Reliable “chain” workflows (enqueue → poll → read → reason → decide next command)
- Output can preserve code blocks and formatting from debugger output
