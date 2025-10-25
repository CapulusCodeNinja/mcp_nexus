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

Import-Module "$PSScriptRoot\..\modules\McpNexusExtensions.psm1" -Force

Write-NexusProgress "Starting basic crash analysis workflow"
Write-NexusLog "Starting basic_crash_analysis extension" -Level Information

try {
    $results = @{}

    # Step 1: Automatic analysis (must run first as it's excluded from batching)
    Write-NexusProgress "Running automatic crash analysis (!analyze -v)..."
    Write-NexusLog "Executing automatic crash analysis (!analyze -v)" -Level Information
    $results["analyze"] = Invoke-NexusCommand "!analyze -v"

    # Step 2-5: Queue multiple commands for batching
    Write-NexusProgress "Queueing multiple commands for batch execution..."
    $batchCommands = @("!threads", "~*k", "!locks", "!runaway")
    $commandIds = Start-NexusCommand -Command $batchCommands
    
    Write-NexusProgress "Waiting for $($commandIds.Count) commands to complete..."
    Write-NexusLog "Executing $($commandIds.Count) commands using async batching" -Level Information

    # Wait for all results efficiently using bulk polling
    $outputs = Wait-NexusCommand -CommandId $commandIds -ReturnResults $false
    $results["threads"] = $outputs[0]
    $results["allStacks"] = $outputs[1]
    $results["locks"] = $outputs[2]
    $results["runaway"] = $outputs[3]

    Write-NexusProgress "Basic crash analysis complete"
    Write-NexusLog "Basic crash analysis completed successfully (5 commands executed, 4 using async batching)" -Level Information

    # Return structured result
    $result = @{
        success = $true
        workflow = "basic_crash_analysis"
        steps = @{
            automaticAnalysis = @{
                command = "!analyze -v"
                output = $results["analyze"]
            }
            threadInfo = @{
                command = "!threads"
                output = $results["threads"]
            }
            allThreadStacks = @{
                command = "~*k"
                output = $results["allStacks"]
            }
            locks = @{
                command = "!locks"
                output = $results["locks"]
            }
            cpuUsage = @{
                command = "!runaway"
                output = $results["runaway"]
            }
        }
        message = "Basic crash analysis completed successfully using async batching. Review the outputs to identify faulting thread, exception type, and crash context."
    } | ConvertTo-Json -Depth 10

    Write-Output $result
    exit 0
}
catch {
    Write-NexusLog "Extension failed with exception: $($_.Exception.Message)" -Level Error
    Write-Error "Extension failed: $_"
    $errorResult = @{
        success = $false
        workflow = "basic_crash_analysis"
        error = $_.Exception.Message
    } | ConvertTo-Json
    Write-Output $errorResult
    exit 1
}

