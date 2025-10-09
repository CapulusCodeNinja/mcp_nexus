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

try {
    $results = @{}

    # Step 1: Automatic analysis
    Write-NexusProgress "Running automatic crash analysis (!analyze -v)..."
    $results["analyze"] = Invoke-NexusCommand "!analyze -v"

    # Step 2: Thread information
    Write-NexusProgress "Getting thread information (!threads)..."
    $results["threads"] = Invoke-NexusCommand "!threads"

    # Step 3: All thread stacks
    Write-NexusProgress "Getting all thread call stacks (~*k)..."
    $results["allStacks"] = Invoke-NexusCommand "~*k"

    # Step 4: Lock information
    Write-NexusProgress "Checking synchronization objects (!locks)..."
    $results["locks"] = Invoke-NexusCommand "!locks"

    # Step 5: CPU usage
    Write-NexusProgress "Checking thread CPU usage (!runaway)..."
    $results["runaway"] = Invoke-NexusCommand "!runaway"

    Write-NexusProgress "Basic crash analysis complete"

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
        message = "Basic crash analysis completed successfully. Review the outputs to identify faulting thread, exception type, and crash context."
    } | ConvertTo-Json -Depth 10

    Write-Output $result
    exit 0
}
catch {
    Write-Error "Extension failed: $_"
    $errorResult = @{
        success = $false
        workflow = "basic_crash_analysis"
        error = $_.Exception.Message
    } | ConvertTo-Json
    Write-Output $errorResult
    exit 1
}

