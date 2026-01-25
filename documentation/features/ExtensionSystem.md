### 🧩 Extension System

WinAiDbg supports a PowerShell-based extension system so you can bundle multi-step debugging workflows into reusable scripts.
This is useful when you want to:

- Run a standard set of commands every time (triage)
- Post-process results into a structured summary
- Share repeatable analysis recipes across a team

#### 🧩 Example

Extensions are discovered by scanning the configured extensions directory for JSON metadata files (for example `metadata.json`). Metadata must include at least `name` and `scriptFile`, and the referenced script must exist.

Example structure:

```
extensions/
  basic_crash_analysis/
    metadata.json
    basic_crash_analysis.ps1
  modules/
    WinAiDbgExtensions.psm1
```

```powershell
# extensions/basic_crash_analysis/basic_crash_analysis.ps1
Import-Module "$PSScriptRoot\..\modules\WinAiDbgExtensions.psm1" -Force

# Initialize is performed by the extension script (example)
Initialize-WinAiDbgExtension -SessionId $SessionId -Token $Token -CallbackUrl $CallbackUrl -ScriptName "basic_crash_analysis"

$result1 = Invoke-WinAiDbgCommand -Command "!analyze -v"
$result2 = Invoke-WinAiDbgCommand -Command "kL"

# Extensions typically write a report to stdout (often Markdown) for the client to display/consume
Write-Output $result1
```

#### 📝 Notes

- Extensions are intended to be deterministic workflows: keep them focused and avoid interactive prompts.
- Extension scripts included in this repo live under `winaidbg_engine/winaidbg_engine_extensions/ExtensionScripts/` and provide working examples.
