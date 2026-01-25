### Advanced Crash Analysis

WinAiDbg is built on Microsoft’s debugger stack (WinDBG/CDB) and exposes it through MCP tools so AI clients can perform deep crash dump investigation in a repeatable way.

#### What this enables

- Run standard crash triage (`!analyze -v`, stack traces, thread inspection)
- Iterate quickly by enqueueing multiple commands and reading structured results
- Apply consistent workflows across dumps and environments

#### Where to start

- Start with the “Example Workflow” in the root `README.md`
- Make sure the “Debugging” configuration is correct (especially `CdbPath` and `SymbolSearchPath`): `documentation/configuration/Debugging.md`
