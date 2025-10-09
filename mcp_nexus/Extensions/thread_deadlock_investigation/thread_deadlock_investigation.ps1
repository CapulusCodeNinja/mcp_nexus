<#
.SYNOPSIS
Performs thread deadlock investigation workflow.

.DESCRIPTION
Analyzes thread synchronization issues and deadlocks:
- !threads to see all threads
- !locks for lock information
- !cs -l for critical sections
- ~*k for all thread stacks
- !runaway for CPU usage
#>

Import-Module "$PSScriptRoot\..\modules\McpNexusExtensions.psm1" -Force

Write-NexusProgress "Starting thread deadlock investigation workflow"

try {
    $results = @{}

    # Step 1: Thread information
    Write-NexusProgress "Getting thread information (!threads)..."
    $results["threads"] = Invoke-NexusCommand "!threads"

    # Step 2: Lock information
    Write-NexusProgress "Getting lock information (!locks)..."
    $results["locks"] = Invoke-NexusCommand "!locks"

    # Step 3: Critical sections
    Write-NexusProgress "Listing critical sections (!cs -l)..."
    $results["criticalSections"] = Invoke-NexusCommand "!cs -l"

    # Step 4: All thread stacks
    Write-NexusProgress "Getting all thread call stacks (~*k)..."
    $results["allStacks"] = Invoke-NexusCommand "~*k"

    # Step 5: CPU usage
    Write-NexusProgress "Checking thread CPU usage (!runaway)..."
    $results["runaway"] = Invoke-NexusCommand "!runaway"

    Write-NexusProgress "Thread deadlock investigation complete"

    # Return structured result
    $result = @{
        success = $true
        workflow = "thread_deadlock_investigation"
        steps = @{
            threadInfo = @{
                command = "!threads"
                output = $results["threads"]
            }
            locks = @{
                command = "!locks"
                output = $results["locks"]
            }
            criticalSections = @{
                command = "!cs -l"
                output = $results["criticalSections"]
            }
            allThreadStacks = @{
                command = "~*k"
                output = $results["allStacks"]
            }
            cpuUsage = @{
                command = "!runaway"
                output = $results["runaway"]
            }
        }
        message = "Thread deadlock investigation completed. Review thread stacks and lock information for synchronization issues."
    } | ConvertTo-Json -Depth 10

    Write-Output $result
    exit 0
}
catch {
    Write-Error "Extension failed: $_"
    $errorResult = @{
        success = $false
        workflow = "thread_deadlock_investigation"
        error = $_.Exception.Message
    } | ConvertTo-Json
    Write-Output $errorResult
    exit 1
}

