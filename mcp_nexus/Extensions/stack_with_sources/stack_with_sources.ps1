<#
.SYNOPSIS
Downloads stack trace with source code for all frames.

.DESCRIPTION
This extension gets the stack trace for a specified thread and downloads
source files for all frames using the lsa command.

.NOTES
This is the workflow we discussed - gets stack with kL and runs lsa on each return address.
#>

param(
    [string]$ThreadId = "."  # Default to current thread
)

# Import MCP Nexus helper module
Import-Module "$PSScriptRoot\..\modules\McpNexusExtensions.psm1" -Force

# Display user-friendly thread description
$threadDisplay = if ($ThreadId -eq ".") { "current thread" } else { "thread $ThreadId" }

Write-NexusProgress "Starting stack analysis with source download for $threadDisplay"
Write-NexusLog "Starting stack_with_sources extension for thread: $ThreadId" -Level Information

try {
    # Step 1: Get stack with line numbers
    Write-NexusProgress "Retrieving stack trace for $threadDisplay..."
    $stackCommand = if ($ThreadId -eq ".") { "kL" } else { "~${ThreadId}kL" }
    $stackOutput = Invoke-NexusCommand $stackCommand

    if ([string]::IsNullOrWhiteSpace($stackOutput)) {
        Write-NexusLog "Failed to retrieve stack trace - output was empty" -Level Error
        Write-Error "Failed to get stack trace - output was empty"
        exit 1
    }

    # Step 1.5: Validate that the thread exists (check for common error patterns)
    if ($stackOutput -match "Invalid thread" -or 
        $stackOutput -match "Illegal thread" -or 
        $stackOutput -match "Thread not found" -or
        $stackOutput -match "\^ .*error" -or
        $stackOutput -match "Couldn't resolve error") {
        
        Write-NexusLog "[$threadDisplay] Thread not found or invalid (ThreadId: $ThreadId)" -Level Error
        Write-Error "[$threadDisplay] not found or invalid"
        
        $result = @{
            success = $false
            threadId = $ThreadId
            totalFrames = 0
            sourcesDownloaded = 0
            addresses = @()
            error = "Thread '$ThreadId' not found. The thread ID may be invalid or the thread may not exist in this dump file."
            suggestion = "Use the '!threads' or '~' command to list available threads, then try again with a valid thread ID."
        } | ConvertTo-Json
        Write-Output $result
        exit 0
    }

    Write-NexusProgress "Stack trace retrieved successfully"

    # Step 2: Parse stack to extract return addresses (middle column)
    Write-NexusProgress "Parsing stack to extract return addresses..."
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
        Write-Warning "No valid return addresses found in stack trace"
        $result = @{
            success = $false
            threadId = $ThreadId
            totalFrames = 0
            sourcesDownloaded = 0
            addresses = @()
            error = "No valid return addresses found in stack trace"
        } | ConvertTo-Json
        Write-Output $result
        exit 0
    }

    Write-NexusLog "Found $($addresses.Count) return addresses to process" -Level Information
    Write-NexusProgress "Found $($addresses.Count) return addresses to process"

    # Step 3: Download sources for each address
    $downloadedCount = 0
    $failedAddresses = @()
    $processedCount = 0

    foreach ($addr in $addresses) {
        $processedCount++
        $percent = [int](($processedCount / $addresses.Count) * 100)
        Write-NexusProgress "[$threadDisplay] Downloading source for address $addr ($processedCount of $($addresses.Count), $percent%)"

        try {
            $lsaOutput = Invoke-NexusCommand "lsa $addr"
            
            # Check if source was actually downloaded
            if ($lsaOutput -match 'source' -or $lsaOutput -match '\.c' -or $lsaOutput -match '\.cpp' -or $lsaOutput -match '\.h') {
                $downloadedCount++
            }
            else {
                $failedAddresses += $addr
            }
        }
        catch {
            Write-Warning "Failed to download source for address $addr`: $_"
            $failedAddresses += $addr
        }
    }

    Write-NexusProgress "[$threadDisplay] Source download complete: $downloadedCount of $($addresses.Count) sources downloaded"
    
    $successRate = [math]::Round(($downloadedCount / $addresses.Count) * 100, 2)
    Write-NexusLog "[$threadDisplay] Source download completed: $downloadedCount/$($addresses.Count) sources ($successRate% success rate)" -Level Information
    
    if ($failedAddresses.Count -gt 0) {
        Write-NexusLog "Failed to download sources for $($failedAddresses.Count) addresses" -Level Warning
    }

    # Return structured result
    $result = @{
        success = $true
        threadId = $ThreadId
        totalFrames = $addresses.Count
        sourcesDownloaded = $downloadedCount
        sourcesMissing = $failedAddresses.Count
        successRate = $successRate
        addresses = $addresses
        failedAddresses = $failedAddresses
        message = "Successfully downloaded $downloadedCount of $($addresses.Count) source files"
    } | ConvertTo-Json

    Write-Output $result
    exit 0
}
catch {
    Write-NexusLog "Extension failed with exception: $($_.Exception.Message)" -Level Error
    Write-Error "Extension failed: $_"
    $errorResult = @{
        success = $false
        threadId = $ThreadId
        error = $_.Exception.Message
        stackTrace = $_.ScriptStackTrace
    } | ConvertTo-Json
    Write-Output $errorResult
    exit 1
}

