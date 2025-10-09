<#
.SYNOPSIS
Performs memory corruption analysis workflow.

.DESCRIPTION
Investigates memory-related crashes and corruption:
- !analyze -v for automatic analysis
- !heap -stat for heap statistics
- Memory address analysis
- Heap corruption detection
#>

Import-Module "$PSScriptRoot\..\modules\McpNexusExtensions.psm1" -Force

Write-NexusProgress "Starting memory corruption analysis workflow"

try {
    $results = @{}

    # Step 1: Automatic analysis
    Write-NexusProgress "Running automatic crash analysis (!analyze -v)..."
    $results["analyze"] = Invoke-NexusCommand "!analyze -v"

    # Step 2: Heap statistics
    Write-NexusProgress "Getting heap statistics (!heap -stat)..."
    $results["heapStat"] = Invoke-NexusCommand "!heap -stat"

    # Step 3: Try to identify faulting address from !analyze output
    Write-NexusProgress "Analyzing crash context for memory addresses..."
    $faultingAddress = $null
    
    if ($results["analyze"] -match 'Faulting address:\s*([0-9a-f`]+)') {
        $faultingAddress = $matches[1]
        Write-NexusProgress "Found faulting address: $faultingAddress"
        
        # Analyze the faulting address
        Write-NexusProgress "Analyzing faulting address (!address $faultingAddress)..."
        $results["addressInfo"] = Invoke-NexusCommand "!address $faultingAddress"
        
        Write-NexusProgress "Getting virtual memory protection (!vprot $faultingAddress)..."
        $results["vprot"] = Invoke-NexusCommand "!vprot $faultingAddress"
    }
    else {
        Write-NexusProgress "No specific faulting address identified in analysis"
        $results["addressInfo"] = "No faulting address found in crash analysis"
        $results["vprot"] = "Skipped - no faulting address"
    }

    Write-NexusProgress "Memory corruption analysis complete"

    # Return structured result
    $result = @{
        success = $true
        workflow = "memory_corruption_analysis"
        faultingAddress = $faultingAddress
        steps = @{
            automaticAnalysis = @{
                command = "!analyze -v"
                output = $results["analyze"]
            }
            heapStatistics = @{
                command = "!heap -stat"
                output = $results["heapStat"]
            }
            addressInformation = @{
                command = if ($faultingAddress) { "!address $faultingAddress" } else { "N/A" }
                output = $results["addressInfo"]
            }
            virtualProtection = @{
                command = if ($faultingAddress) { "!vprot $faultingAddress" } else { "N/A" }
                output = $results["vprot"]
            }
        }
        message = "Memory corruption analysis completed. Review heap statistics and address information for signs of corruption."
    } | ConvertTo-Json -Depth 10

    Write-Output $result
    exit 0
}
catch {
    Write-Error "Extension failed: $_"
    $errorResult = @{
        success = $false
        workflow = "memory_corruption_analysis"
        error = $_.Exception.Message
    } | ConvertTo-Json
    Write-Output $errorResult
    exit 1
}

