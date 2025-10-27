<#
.SYNOPSIS
Downloads stack trace with source code for all frames.

.DESCRIPTION
This extension gets the stack trace for a specified thread and downloads
source files for all frames using the lsa command.

.PARAMETER ThreadId
Thread ID to analyze (e.g., '8' or '.' for current thread). Defaults to current thread ('.').

.EXAMPLE
.\stack_with_sources.ps1 -ThreadId '5'

.EXAMPLE
.\stack_with_sources.ps1

.NOTES
This is the workflow we discussed - gets stack with kL and runs lsa on each return address.
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
        
        $result = @{
            success = $false
            threadId = $ThreadId
            totalFrames = 0
            sourcesDownloaded = 0
            addresses = @()
            error = "Thread '$ThreadId' not found. The thread ID may be invalid or the thread may not exist in this dump file."
            suggestion = "Use the '!threads' or '~' command to list available threads, then try again with a valid thread ID."
            stackTrace = $stackOutput
        } | ConvertTo-Json -Depth 10
        Write-Output $result
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
            
            # Skip inline functions and invalid addresses
            if ($retAddr -notmatch 'Inline' -and $retAddr -match '[0-9a-f`]{8,}') {
                $addresses += $retAddr
            }
        }
    }

    if ($addresses.Count -eq 0) {
        Write-NexusLog "No valid return addresses found in stack trace" -Level Warning
        $result = @{
            success = $false
            threadId = $ThreadId
            totalFrames = 0
            sourcesDownloaded = 0
            addresses = @()
            error = "No valid return addresses found in stack trace"
            stackTrace = $stackOutput
        } | ConvertTo-Json -Depth 10
        Write-Output $result
        exit 0
    }

    Write-NexusLog "Found $($addresses.Count) return addresses to process" -Level Information

    # Step 3: Download sources for each address (first pass) - using async batching
    Write-NexusLog "[$threadDisplay] Queueing source downloads for $($addresses.Count) addresses..." -Level Information
    $downloadCommands = @()
    foreach ($addr in $addresses) {
        $downloadCommands += "lsa $addr"
    }
    
    Write-NexusLog "[$threadDisplay] Executing first pass source downloads using async batching..." -Level Information
    $downloadCommandIds = Start-NexusCommand -Command $downloadCommands
    
    Write-NexusLog "[$threadDisplay] Waiting for first pass downloads to complete..." -Level Information
    try {
        $null = Wait-NexusCommand -CommandId $downloadCommandIds -ReturnResults $false
        Write-NexusLog "First pass source downloads completed successfully" -Level Information
    }
    catch {
        Write-NexusLog "Failed to execute lsa commands in first pass: $_" -Level Warning
    }
    
    # Step 4: Verify downloaded sources (second pass) - using async batching
    Write-NexusLog "[$threadDisplay] Queueing source verification for $($addresses.Count) addresses..." -Level Information
    $verifyCommands = @()
    foreach ($addr in $addresses) {
        $verifyCommands += "lsa $addr"
    }
    
    Write-NexusLog "[$threadDisplay] Executing second pass source verification using async batching..." -Level Information
    $verifyCommandIds = Start-NexusCommand -Command $verifyCommands
    
    Write-NexusLog "[$threadDisplay] Waiting for verification to complete and processing results..." -Level Information
    $downloadedCount = 0
    $failedAddresses = @()
    $sourceOutputs = @{}  # Collect all lsa outputs
    
    for ($i = 0; $i -lt $addresses.Count; $i++) {
        $addr = $addresses[$i]
        $cmdId = $verifyCommandIds[$i]
        $percent = [int]((($i + 1) / $addresses.Count) * 100)
        Write-NexusLog "[$threadDisplay] Processing verification result for address $addr ($($i + 1) of $($addresses.Count), $percent%)" -Level Information
        
        try {
            $verifyOutput = Wait-NexusCommand -CommandId $cmdId
            $sourceOutputs[$addr] = $verifyOutput
            
            # Check if source was found in cache (with .srcnoisy 3, should show "already loaded")
            # Also accept if output shows line numbers (source is displayed)
            if ($verifyOutput -match 'Found already loaded file:|already loaded' -or
                $verifyOutput -match '^\s*\d+:') {
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

    Write-NexusLog "[$threadDisplay] Source download complete: $downloadedCount of $($addresses.Count) sources verified" -Level Information
    
    $successRate = [math]::Round(($downloadedCount / $addresses.Count) * 100, 2)
    Write-NexusLog "[$threadDisplay] Source download completed: $downloadedCount/$($addresses.Count) sources verified ($successRate% success rate)" -Level Information
    
    if ($failedAddresses.Count -gt 0) {
        Write-NexusLog "Failed to download sources for $($failedAddresses.Count) addresses" -Level Warning
    }

    # Return structured result with all command outputs
    $result = @{
        success = $true
        threadId = $ThreadId
        totalFrames = $addresses.Count
        sourcesDownloaded = $downloadedCount
        sourcesMissing = $failedAddresses.Count
        successRate = $successRate
        addresses = $addresses
        failedAddresses = $failedAddresses
        message = "Successfully downloaded and verified $downloadedCount of $($addresses.Count) source files using async batching"
        stackTrace = $stackOutput
        sourceOutputs = $sourceOutputs
    } | ConvertTo-Json -Depth 10

    Write-Output $result
    exit 0
}
catch {
    $errorResult = @{
        success = $false
        threadId = $ThreadId
        error = $_.Exception.Message
        stackTrace = $_.ScriptStackTrace
    } | ConvertTo-Json
    Write-NexusLog "Extension failed with exception:`r`n$errorResult" -Level Error
    exit 1
}

