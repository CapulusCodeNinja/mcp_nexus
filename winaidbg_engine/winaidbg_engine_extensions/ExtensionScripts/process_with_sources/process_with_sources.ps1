<#
.SYNOPSIS
Downloads sources for stacks across all threads in the current process.

.DESCRIPTION
This extension enumerates all threads (via `~`), retrieves each thread's stack trace (`~<tid>kL`),
collects return addresses, then downloads source files for frames that have symbols using the
`lsa` command.

Optimization: Uses `ln` to filter addresses before downloading sources, reducing command count.
Additional optimization: Deduplicates addresses across all threads so each address is processed once.

.PARAMETER ProcessId
Optional process selector (e.g., '.' for current process). For typical user-mode dump debugging
this can be left as '.'.

.EXAMPLE
.\process_with_sources.ps1

.EXAMPLE
.\process_with_sources.ps1 -ProcessId '.'

.NOTES
Commands: `~` -> `~<tid>kL` -> `ln` (unique) -> `lsa` (unique+filtered).
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$ProcessId = ".",  # Default to current process
    
    [Parameter(Mandatory=$true)]
    [string]$SessionId,
    
    [Parameter(Mandatory=$true)]
    [string]$Token,
    
    [Parameter(Mandatory=$true)]
    [string]$CallbackUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$Parameters
)

# Import WinAiDbg helper module
Import-Module "$PSScriptRoot\..\modules\WinAiDbgExtensions.psm1" -Force

# Initialize the extension with parameters
Initialize-WinAiDbgExtension -SessionId $SessionId -Token $Token -CallbackUrl $CallbackUrl -ScriptName "process_with_sources"

Write-WinAiDbgLog "Starting process_with_sources extension with ProcessId: $ProcessId" -Level Information

function Test-WinDbgOutputIndicatesThreadError {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Output
    )

    return ($Output -match "Invalid thread" -or
            $Output -match "Illegal thread" -or
            $Output -match "Thread not found" -or
            $Output -match "\^ .*error" -or
            $Output -match "Couldn't resolve error")
}

function Get-ThreadIdsFromTildeList {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ThreadsOutput
    )

    $threadIds = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)
    $lines = $ThreadsOutput -split "`n"

    foreach ($line in $lines) {
        # Typical WinDbg output:
        #  .  0  Id: 1234.5678 Suspend: 1 Teb: ...
        #     1  Id: 1234.9abc Suspend: 1 Teb: ...
        if ($line -match '^\s*[\.\*\+ ]*\s*([0-9]+)\s+Id:\s+') {
            [void]$threadIds.Add($matches[1])
        }
    }

    return $threadIds
}

function Get-ReturnAddressesFromStack {
    param(
        [Parameter(Mandatory=$true)]
        [string]$StackOutput
    )

    $addresses = @()
    $stackLines = $StackOutput -split "`n"

    foreach ($line in $stackLines) {
        # Match stack line format: "Child-SP         RetAddr           Call Site"
        # We want the middle column (RetAddr)
        if ($line -match '^\s*[0-9a-f`]+\s+([0-9a-f`]+)\s+') {
            $retAddr = $matches[1]

            # Skip inline functions, invalid addresses, and null addresses
            # Filter out null addresses like 00000000`00000000
            $isNullAddress = $retAddr -match '^[0`]+$'
            if ($retAddr -notmatch 'Inline' -and $retAddr -match '[0-9a-f`]{8,}' -and -not $isNullAddress) {
                $addresses += $retAddr
            }
        }
    }

    return $addresses
}

try {
    Write-WinAiDbgLog "Enable source verbosity..." -Level Information
    $null = Invoke-WinAiDbgCommand ".srcnoisy 3"
    
    Write-WinAiDbgLog "Enable the source server..." -Level Information
    $null = Invoke-WinAiDbgCommand ".srcfix+"

    if ($ProcessId -ne ".") {
        Write-WinAiDbgLog "Attempting to select process '$ProcessId'..." -Level Information
        # In kernel debugging, `|<index>s` selects a process. In user-mode dumps this is typically unnecessary.
        $processSelectOutput = Invoke-WinAiDbgCommand "|${ProcessId}s"
        if ($processSelectOutput -match "\^ .*error" -or $processSelectOutput -match "syntax") {
            Write-WinAiDbgLog "Process selection may have failed for ProcessId '$ProcessId'. Continuing with current process." -Level Warning
        }
    }

    # Step 1: Enumerate threads in the current process
    Write-WinAiDbgLog "Enumerating threads using '~'..." -Level Information
    $threadsOutput = Invoke-WinAiDbgCommand "~"
    if ([string]::IsNullOrWhiteSpace($threadsOutput)) {
        Write-WinAiDbgLog "Thread enumeration returned empty output; falling back to current thread only." -Level Warning
        $threadsOutput = ""
    }

    $threadIdsSet = Get-ThreadIdsFromTildeList -ThreadsOutput $threadsOutput
    if ($threadIdsSet.Count -eq 0) {
        [void]$threadIdsSet.Add(".")
        Write-WinAiDbgLog "No thread IDs parsed from '~' output; processing current thread only." -Level Warning
    }

    $threadIds = @($threadIdsSet)
    Write-WinAiDbgLog "Found $($threadIds.Count) thread(s) to process" -Level Information

    # Step 2: For each thread, get stack and collect addresses
    $threadStacks = @{}     # threadId -> stack text
    $threadAddresses = @{}  # threadId -> string[] addresses (not deduped)
    $totalFrames = 0

    foreach ($tid in $threadIds) {
        $threadDisplay = if ($tid -eq ".") { "current thread" } else { "thread $tid" }
        Write-WinAiDbgLog "Retrieving stack trace for $threadDisplay..." -Level Information

        $stackCommand = if ($tid -eq ".") { "kL" } else { "~${tid}kL" }
        $stackOutput = Invoke-WinAiDbgCommand $stackCommand

        if ([string]::IsNullOrWhiteSpace($stackOutput)) {
            Write-WinAiDbgLog "[$threadDisplay] Failed to retrieve stack trace - output was empty" -Level Warning
            $threadStacks[$tid] = ""
            $threadAddresses[$tid] = @()
            continue
        }

        if (Test-WinDbgOutputIndicatesThreadError -Output $stackOutput) {
            Write-WinAiDbgLog "[$threadDisplay] Thread not found or invalid" -Level Warning
            $threadStacks[$tid] = $stackOutput
            $threadAddresses[$tid] = @()
            continue
        }

        $threadStacks[$tid] = $stackOutput

        $addresses = Get-ReturnAddressesFromStack -StackOutput $stackOutput
        $threadAddresses[$tid] = $addresses
        $totalFrames += $addresses.Count

        Write-WinAiDbgLog "[$threadDisplay] Parsed $($addresses.Count) return address(es)" -Level Information
    }

    # Step 3: Deduplicate addresses across all threads
    $uniqueAddressSet = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($tid in $threadAddresses.Keys) {
        foreach ($addr in $threadAddresses[$tid]) {
            [void]$uniqueAddressSet.Add($addr)
        }
    }

    $uniqueAddresses = @($uniqueAddressSet)
    if ($uniqueAddresses.Count -eq 0) {
        Write-WinAiDbgLog "No return addresses found across all threads" -Level Warning
        $now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $md = @"
## Process With Sources

**Success:** False
**Executed:** $now
**Threads Processed:** $($threadIds.Count)
**Total Frames:** 0
**Unique Addresses:** 0
**Message:** No valid return addresses found in any thread stack trace.

### Hint

This extension prefetched sources for the **entire current process** by walking **all threads** and attempting to load source files for each stack frame address (via `lsa`). Prefetch summary: **0 attempted**, **0 verified**.

### Thread List (`~`)

``````
$threadsOutput
``````
"@
        Write-Output $md
        exit 0
    }

    Write-WinAiDbgLog "Collected $totalFrames total frame address(es) across threads; $($uniqueAddresses.Count) unique address(es)" -Level Information

    # Step 4: Filter unique addresses to those with symbols using ln (fast lookup)
    Write-WinAiDbgLog "Filtering unique addresses with symbols using ln..." -Level Information
    $lnCommands = @()
    foreach ($addr in $uniqueAddresses) {
        $lnCommands += "ln $addr"
    }

    $lnCommandIds = Start-WinAiDbgCommand -Command $lnCommands
    $addressesWithSymbols = @()

    for ($i = 0; $i -lt $uniqueAddresses.Count; $i++) {
        $addr = $uniqueAddresses[$i]
        try {
            $lnOutput = Wait-WinAiDbgCommand -CommandId $lnCommandIds[$i]

            if ($lnOutput[0] -match '\!' -or $lnOutput[0] -match '\.(cpp|c|h|hpp)\([0-9]+\)' -or $lnOutput[0] -match '\+\s*0x[0-9a-fA-F]+') {
                $addressesWithSymbols += $addr
            }
        }
        catch {
            Write-WinAiDbgLog "Failed to run ln for address $addr`: $_" -Level Warning
        }
    }

    Write-WinAiDbgLog "Found $($addressesWithSymbols.Count) unique address(es) with symbols (filtered from $($uniqueAddresses.Count))" -Level Information

    if ($addressesWithSymbols.Count -eq 0) {
        $now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $addrAll = ($uniqueAddresses -join "\n")
        $md = @"
## Process With Sources

**Success:** True
**Executed:** $now
**Threads Processed:** $($threadIds.Count)
**Total Frames:** $totalFrames
**Unique Addresses:** $($uniqueAddresses.Count)
**Unique Addresses With Symbols:** 0
**Sources Downloaded:** 0
**Message:** No addresses with symbols found, nothing to download.

### Hint

This extension prefetched sources for the **entire current process** by walking **all threads** and attempting to load source files for each stack frame address (via `lsa`). Prefetch summary: **0 attempted** (no symbolized stack addresses found), **0 verified**.

### Thread List (`~`)

``````
$threadsOutput
``````

### Unique Addresses (All)

``````
$addrAll
``````
"@
        Write-Output $md
        exit 0
    }

    # Step 5: Download sources for filtered unique addresses - using async batching
    Write-WinAiDbgLog "Queueing source downloads for $($addressesWithSymbols.Count) unique address(es) with symbols..." -Level Information
    $downloadCommands = @()
    foreach ($addr in $addressesWithSymbols) {
        $downloadCommands += "lsa $addr"
    }
    
    Write-WinAiDbgLog "Executing source downloads using async batching..." -Level Information
    $verifyCommandIds = Start-WinAiDbgCommand -Command $downloadCommands
    
    Write-WinAiDbgLog "Waiting for verification to complete and processing results..." -Level Information
    $downloadedCount = 0
    $failedAddresses = @()
    $sourceOutputs = @{}  # address -> output (array containing the string output)
    
    for ($i = 0; $i -lt $addressesWithSymbols.Count; $i++) {
        $addr = $addressesWithSymbols[$i]
        $cmdId = $verifyCommandIds[$i]
        $percent = [int]((($i + 1) / $addressesWithSymbols.Count) * 100)
        Write-WinAiDbgLog "Processing verification result for address $addr ($($i + 1) of $($addressesWithSymbols.Count), $percent%)" -Level Information
        
        try {
            $verifyOutput = Wait-WinAiDbgCommand -CommandId $cmdId
            $sourceOutputs[$addr] = $verifyOutput
            
            # Check if source was found in cache (with .srcnoisy 3, should show "already loaded")
            # Also accept if output shows line numbers (source is displayed)
            if ($verifyOutput[0] -match 'Found already loaded file:|already loaded' -or
                $verifyOutput[0] -match '^\s*\d+:') {
                $downloadedCount++
            }
            else {
                $failedAddresses += $addr
            }
        }
        catch {
            $failedAddresses += $addr
            $sourceOutputs[$addr] = "ERROR: $_"
            Write-WinAiDbgLog "Failed to verify source for address $addr`: $_" -Level Warning
        }
    }

    $successRate = [math]::Round(($downloadedCount / $addressesWithSymbols.Count) * 100, 2)
    Write-WinAiDbgLog "Source download completed: $downloadedCount/$($addressesWithSymbols.Count) sources verified ($successRate% success rate)" -Level Information

    # Compose Markdown report with summary and per-thread stacks
    $now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $addrWithSymbols = ($addressesWithSymbols -join "\n")
    $failedList = ($failedAddresses -join "\n")
    $addrAll = ($uniqueAddresses -join "\n")

    $threadSummaryText = New-Object System.Text.StringBuilder
    foreach ($tid in ($threadStacks.Keys | Sort-Object)) {
        $threadDisplay = if ($tid -eq ".") { "current thread" } else { "thread $tid" }
        $addrCount = if ($threadAddresses.ContainsKey($tid)) { $threadAddresses[$tid].Count } else { 0 }
        [void]$threadSummaryText.AppendLine("- $threadDisplay: $addrCount address(es)")
    }

    $threadStacksText = New-Object System.Text.StringBuilder
    foreach ($tid in ($threadStacks.Keys | Sort-Object)) {
        $threadDisplay = if ($tid -eq ".") { "current thread" } else { "thread $tid" }
        [void]$threadStacksText.AppendLine("### $threadDisplay")
        [void]$threadStacksText.AppendLine()
        [void]$threadStacksText.AppendLine("``````")
        [void]$threadStacksText.AppendLine($threadStacks[$tid])
        [void]$threadStacksText.AppendLine("``````")
        [void]$threadStacksText.AppendLine()
    }

    # Flatten sourceOutputs into a readable section
    $sourceOutputsText = New-Object System.Text.StringBuilder
    foreach ($kv in $sourceOutputs.GetEnumerator()) {
        [void]$sourceOutputsText.AppendLine("[$($kv.Key)]")
        [void]$sourceOutputsText.AppendLine($kv.Value)
        [void]$sourceOutputsText.AppendLine()
    }

    $md = @"
## Process With Sources

**Success:** True
**Executed:** $now
**Threads Processed:** $($threadIds.Count)
**Total Frames (sum across threads):** $totalFrames
**Unique Addresses:** $($uniqueAddresses.Count)
**Unique Addresses With Symbols:** $($addressesWithSymbols.Count)
**Sources Downloaded:** $downloadedCount
**Sources Missing:** $($failedAddresses.Count)
**Success Rate:** $successRate%

### Hint

This extension prefetched sources for the **entire current process** by walking **all threads** and attempting to load source files for each stack frame address (via `lsa`). Prefetch summary: **$($addressesWithSymbols.Count) attempted**, **$downloadedCount verified**.

### Thread List (`~`)

``````
$threadsOutput
``````

### Thread Summary

$($threadSummaryText.ToString())

### Stack Traces

$($threadStacksText.ToString())

### Unique Addresses With Symbols

``````
$addrWithSymbols
``````

### Failed Addresses

``````
$failedList
``````

### Unique Addresses (All)

``````
$addrAll
``````

### Source Verification Outputs

``````
$($sourceOutputsText.ToString())
``````
"@

    Write-Output $md
    exit 0
}
catch {
    Write-WinAiDbgLog "Extension failed with exception:`r`n$_" -Level Error
    exit 1
}

