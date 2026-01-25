<#
.SYNOPSIS
Performs basic crash analysis workflow.

.DESCRIPTION
Essential commands for initial crash investigation:
- !analyze -v for automatic analysis
- !threads to see all threads
- ~*k for all thread stacks
- !locks for synchronization objects
- !runaway for CPU usage
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$SessionId,
    
    [Parameter(Mandatory=$true)]
    [string]$Token,
    
    [Parameter(Mandatory=$true)]
    [string]$CallbackUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$Parameters
)

Import-Module "$PSScriptRoot\..\modules\WinAiDbgExtensions.psm1" -Force

# Initialize the extension with parameters
Initialize-WinAiDbgExtension -SessionId $SessionId -Token $Token -CallbackUrl $CallbackUrl -ScriptName "basic_crash_analysis"

Write-WinAiDbgLog "Starting basic_crash_analysis extension" -Level Information

try {
    $results = @{}

    # Step 1: Automatic analysis (must run first as it's excluded from batching)
    Write-WinAiDbgLog "Executing automatic crash analysis (!analyze -v)" -Level Information
    $results["analyze"] = Invoke-WinAiDbgCommand "!analyze -v"

    # Step 2-5: Queue multiple commands for batching
    Write-WinAiDbgLog "Queueing multiple commands for batch execution..." -Level Information
    $batchCommands = @("!threads", "~*k", "!locks", "!runaway")
    $commandIds = Start-WinAiDbgCommand -Command $batchCommands
    
    Write-WinAiDbgLog "Waiting for $($commandIds.Count) commands to complete..." -Level Information

    # Wait for all results efficiently using bulk polling
    $outputs = Wait-WinAiDbgCommand -CommandId $commandIds
    $results["threads"] = $outputs[0]
    $results["allStacks"] = $outputs[1]
    $results["locks"] = $outputs[2]
    $results["runaway"] = $outputs[3]

    Write-WinAiDbgLog "Basic crash analysis completed successfully (5 commands executed, 4 using async batching)" -Level Information

    # Compose Markdown report
    $now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $md = @"
## Basic Crash Analysis

**Workflow:** ``basic_crash_analysis``
**Executed:** $now

### !analyze -v

``````
$($results["analyze"])
``````

### !threads

``````
$($results["threads"])
``````

### ~*k

``````
$($results["allStacks"])
``````

### !locks

``````
$($results["locks"])
``````

### !runaway

``````
$($results["runaway"])
``````

"@

    Write-Output $md
    exit 0
}
catch {
    Write-WinAiDbgLog "Extension failed with exception:`r`n$_" -Level Error
    exit 1
}

