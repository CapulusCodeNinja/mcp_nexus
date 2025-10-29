<#
.SYNOPSIS
Downloads stack trace with source code for all frames.

.DESCRIPTION
This extension gets the stack trace for a specified thread and downloads
source files for frames that have symbols using the lsa command. Optimization:
uses 'ln' to filter addresses before downloading sources, reducing command count.

.PARAMETER ThreadId
Thread ID to analyze (e.g., '8' or '.' for current thread). Defaults to current thread ('.').

.EXAMPLE
.\stack_with_sources.ps1 -ThreadId '5'

.EXAMPLE
.\stack_with_sources.ps1

.NOTES
Optimization: Uses 'ln' to quickly filter addresses with symbols before downloading sources.
This reduces the number of 'lsa' commands from 2*N to N+filtered_count, significantly
improving performance. Commands: kL -> ln -> lsa (filtered).
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$ThreadId = ".",  # Default to current thread
    
    [Parameter(Mandatory=$true)]
    [string]$SessionId,
    
    [Parameter(Mandatory=$true)]
    [string]$Token,
    
    [Parameter(Mandatory=$true)]
    [string]$CallbackUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$Parameters
)

# Import MCP Nexus helper module
Import-Module "$PSScriptRoot\..\modules\McpNexusExtensions.psm1" -Force

# Initialize the extension with parameters
Initialize-McpNexusExtension -SessionId $SessionId -Token $Token -CallbackUrl $CallbackUrl -ScriptName "stack_with_sources"

Write-NexusLog "Starting stack_with_sources extension with ThreadId: $ThreadId" -Level Information

# Display user-friendly thread description
$threadDisplay = if ($ThreadId -eq ".") { "current thread" } else { "thread $ThreadId" }

Write-NexusLog "Starting stack analysis with source download for $threadDisplay" -Level Information

try {
    Write-NexusLog "Enable source verbosity for $threadDisplay..." -Level Information
    $stackOutput = Invoke-NexusCommand ".srcnoisy 3"
    
    Write-NexusLog "Enable the source server for $threadDisplay..." -Level Information
    $stackOutput = Invoke-NexusCommand ".srcfix+"

    # Step 1: Get stack with line numbers
    Write-NexusLog "Retrieving stack trace for $threadDisplay..." -Level Information
    $stackCommand = if ($ThreadId -eq ".") { "kL" } else { "~${ThreadId}kL" }
    $stackOutput = Invoke-NexusCommand $stackCommand

    if ([string]::IsNullOrWhiteSpace($stackOutput)) {
        Write-NexusLog "Failed to retrieve stack trace - output was empty" -Level Error
        exit 1
    }

    # Step 1.5: Validate that the thread exists (check for common error patterns)
    if ($stackOutput -match "Invalid thread" -or 
        $stackOutput -match "Illegal thread" -or 
        $stackOutput -match "Thread not found" -or
        $stackOutput -match "\^ .*error" -or
        $stackOutput -match "Couldn't resolve error") {
        
        Write-NexusLog "[$threadDisplay] Thread not found or invalid (ThreadId: $ThreadId)" -Level Error
        
        $md = @"
## Stack With Sources

**Success:** False
**Thread ID:** `$ThreadId`
**Message:** Thread '$ThreadId' not found. The thread ID may be invalid or may not exist in this dump.
**Suggestion:** Use `!threads` or `~` to list available threads, then retry with a valid thread ID.

### Stack Trace

```
$stackOutput
```
"@
        Write-Output $md
        exit 0
    }

    Write-NexusLog "Stack trace retrieved successfully" -Level Information

    # Step 2: Parse stack to extract return addresses (middle column)
    Write-NexusLog "Parsing stack to extract return addresses..." -Level Information
    $addresses = @()
    $stackLines = $stackOutput -split "`n"

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

    if ($addresses.Count -eq 0) {
        Write-NexusLog "No valid return addresses found in stack trace" -Level Warning
        $md = @"
## Stack With Sources

**Success:** False
**Thread ID:** `$ThreadId`
**Message:** No valid return addresses found in stack trace

### Stack Trace

```
$stackOutput
```
"@
        Write-Output $md
        exit 0
    }

    Write-NexusLog "Found $($addresses.Count) return addresses to process" -Level Information

    # Step 3: Filter addresses to those with symbols using ln (fast lookup)
    Write-NexusLog "[$threadDisplay] Filtering addresses with symbols using ln command..." -Level Information
    $lnCommands = @()
    for ($i = 0; $i -lt $addresses.Count; $i++) {
        $lnCommands += "ln $($addresses[$i])"
    }
    
    $lnCommandIds = Start-NexusCommand -Command $lnCommands
    $addressesWithSymbols = @()
    
    for ($i = 0; $i -lt $addresses.Count; $i++) {
        try {
            $lnOutput = Wait-NexusCommand -CommandId $lnCommandIds[$i]
                  
            if ($lnOutput[0] -match '\!' -or $lnOutput[0] -match '\.(cpp|c|h|hpp)\([0-9]+\)' -or $lnOutput[0] -match '\+\s*0x[0-9a-fA-F]+') {
                $addressesWithSymbols += $addresses[$i]
            }
            else {
                Write-NexusLog "Address $($addresses[$i]) has no symbols" -Level Debug
            }
        }
        catch {
            Write-NexusLog "Extension failed with exception:`r`n$_" -Level Warning
        }
    }
    
    Write-NexusLog "[$threadDisplay] Found $($addressesWithSymbols.Count) addresses with symbols (filtered from $($addresses.Count) total)" -Level Information
    
    if ($addressesWithSymbols.Count -eq 0) {
        Write-NexusLog "No addresses with symbols found to download sources for" -Level Warning
        $addrList = ($addresses -join "\n")
        $md = @"
## Stack With Sources

**Success:** True
**Thread ID:** `$ThreadId`
**Total Frames:** $($addresses.Count)
**Frames With Symbols:** 0
**Sources Downloaded:** 0
**Success Rate:** 0%
**Message:** No addresses with symbols found, nothing to download

### Stack Trace

```
$stackOutput
```

### Addresses (All)

```
$addrList
```
"@
        Write-Output $md
        exit 0
    }

    # Step 4: Download sources for filtered addresses - using async batching
    Write-NexusLog "[$threadDisplay] Queueing source downloads for $($addressesWithSymbols.Count) addresses with symbols..." -Level Information
    $downloadCommands = @()
    foreach ($addr in $addressesWithSymbols) {
        $downloadCommands += "lsa $addr"
    }
    
    Write-NexusLog "[$threadDisplay] Executing source downloads using async batching..." -Level Information
    $verifyCommandIds = Start-NexusCommand -Command $downloadCommands
    
    Write-NexusLog "[$threadDisplay] Waiting for verification to complete and processing results..." -Level Information
    $downloadedCount = 0
    $failedAddresses = @()
    $sourceOutputs = @{}  # Collect all lsa outputs
    
    for ($i = 0; $i -lt $addressesWithSymbols.Count; $i++) {
        $addr = $addressesWithSymbols[$i]
        $cmdId = $verifyCommandIds[$i]
        $percent = [int]((($i + 1) / $addressesWithSymbols.Count) * 100)
        Write-NexusLog "[$threadDisplay] Processing verification result for address $addr ($($i + 1) of $($addressesWithSymbols.Count), $percent%)" -Level Information
        
        try {
            $verifyOutput = Wait-NexusCommand -CommandId $cmdId
            $sourceOutputs[$addr] = $verifyOutput
            
            # Check if source was found in cache (with .srcnoisy 3, should show "already loaded")
            # Also accept if output shows line numbers (source is displayed)
            if ($verifyOutput[0] -match 'Found already loaded file:|already loaded' -or
                $verifyOutput[0] -match '^\s*\d+:') {
                $downloadedCount++
                Write-NexusLog "[$threadDisplay] Verified source for address $addr (cached)" -Level Debug
            }
            else {
                $failedAddresses += $addr
                Write-NexusLog "[$threadDisplay] No source found for address $addr" -Level Debug
            }
        }
        catch {
            $failedAddresses += $addr
            $sourceOutputs[$addr] = "ERROR: $_"
            Write-NexusLog "Failed to verify source for address $addr`: $_" -Level Warning
        }
    }

    Write-NexusLog "[$threadDisplay] Source download complete: $downloadedCount of $($addressesWithSymbols.Count) sources verified" -Level Information
    
    $successRate = [math]::Round(($downloadedCount / $addressesWithSymbols.Count) * 100, 2)
    Write-NexusLog "[$threadDisplay] Source download completed: $downloadedCount/$($addressesWithSymbols.Count) sources verified ($successRate% success rate)" -Level Information
    
    if ($failedAddresses.Count -gt 0) {
        Write-NexusLog "Failed to download sources for $($failedAddresses.Count) addresses" -Level Warning
    }

    # Compose Markdown report with summary and outputs
    $now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $addrAll = ($addresses -join "\n")
    $addrWithSymbols = ($addressesWithSymbols -join "\n")
    $failedList = ($failedAddresses -join "\n")

    # Flatten sourceOutputs into a readable section
    $sourceOutputsText = New-Object System.Text.StringBuilder
    foreach ($kv in $sourceOutputs.GetEnumerator()) {
        [void]$sourceOutputsText.AppendLine("[$($kv.Key)]")
        [void]$sourceOutputsText.AppendLine($kv.Value)
        [void]$sourceOutputsText.AppendLine()
    }

    $md = @"
## Stack With Sources

**Success:** True
**Thread ID:** `$ThreadId`
**Executed:** $now
**Total Frames:** $($addresses.Count)
**Frames With Symbols:** $($addressesWithSymbols.Count)
**Sources Downloaded:** $downloadedCount
**Sources Missing:** $($failedAddresses.Count)
**Success Rate:** $successRate%

### Stack Trace

```
$stackOutput
```

### Addresses With Symbols

```
$addrWithSymbols
```

### Failed Addresses

```
$failedList
```

### All Addresses

```
$addrAll
```

### Source Verification Outputs

```
$($sourceOutputsText.ToString())
```
"@

    Write-Output $md
    exit 0
}
catch {
    Write-NexusLog "Extension failed with exception:`r`n$_" -Level Error
    exit 1
}

